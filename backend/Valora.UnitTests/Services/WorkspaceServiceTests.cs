using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Application.Services;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Services;

public class WorkspaceServiceTests
{
    private readonly ValoraDbContext _context;
    private readonly WorkspaceService _service;
    private readonly WorkspaceRepository _repository;

    public WorkspaceServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _repository = new WorkspaceRepository(_context);
        _service = new WorkspaceService(_repository);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_ShouldCreateWorkspaceAndLogActivity()
    {
        var userId = "user1";
        var dto = new CreateWorkspaceDto("Test Workspace", "Description");

        var result = await _service.CreateWorkspaceAsync(userId, dto);

        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(userId, result.OwnerId);

        var workspace = await _context.Workspaces.Include(w => w.Members).FirstAsync(w => w.Id == result.Id);
        Assert.Single(workspace.Members);
        Assert.Equal(WorkspaceRole.Owner, workspace.Members.First().Role);

        var log = await _context.ActivityLogs.FirstAsync(l => l.WorkspaceId == result.Id);
        Assert.Equal(ActivityLogType.WorkspaceCreated, log.Type);
    }

    [Fact]
    public async Task CreateWorkspaceAsync_ShouldThrow_WhenLimitReached()
    {
        var userId = "user1";
        for (int i = 0; i < 10; i++)
        {
            var ws = new Workspace
            {
                Name = $"WS{i}",
                OwnerId = userId,
                Members = new List<WorkspaceMember>
                {
                    new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Owner }
                }
            };
            _context.Workspaces.Add(ws);
        }
        await _context.SaveChangesAsync();

        var dto = new CreateWorkspaceDto("Test Workspace", "Description");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateWorkspaceAsync(userId, dto));

        var log = await _context.ActivityLogs.FirstOrDefaultAsync(l => l.ActorId == userId && l.Summary == "Workspace creation failed: limit reached");
        Assert.NotNull(log);
        Assert.Null(log.WorkspaceId); // Ensure log is not attached to a non-existent workspace
    }

    [Fact]
    public async Task GetUserWorkspacesAsync_ShouldReturnOnlyUserWorkspaces()
    {
        var userId = "user1";
        var otherUser = "user2";

        var ws1 = new Workspace { Name = "WS1", OwnerId = userId, Members = new List<WorkspaceMember> { new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Owner } } };
        var ws2 = new Workspace { Name = "WS2", OwnerId = otherUser, Members = new List<WorkspaceMember> { new WorkspaceMember { UserId = otherUser, Role = WorkspaceRole.Owner } } };

        _context.Workspaces.AddRange(ws1, ws2);
        await _context.SaveChangesAsync();

        var result = await _service.GetUserWorkspacesAsync(userId);

        Assert.Single(result);
        Assert.Equal(ws1.Id, result.First().Id);
    }

    [Fact]
    public async Task GetWorkspaceAsync_ShouldThrowForbidden_WhenUserNotMember()
    {
        var ws = new Workspace { Name = "WS", OwnerId = "owner" };
        _context.Workspaces.Add(ws);
        await _context.SaveChangesAsync();

        // Expect ForbiddenAccessException because the workspace exists but the user is not a member.
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.GetWorkspaceAsync("intruder", ws.Id));
    }

    [Fact]
    public async Task GetWorkspaceAsync_ShouldThrowNotFound_WhenWorkspaceDoesNotExist()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetWorkspaceAsync("user", Guid.NewGuid()));
    }

    [Fact]
    public async Task GetActivityLogsAsync_ShouldReturnLogs()
    {
        var userId = "user";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Viewer });

        var log = new ActivityLog { Workspace = workspace, ActorId = "owner", Type = ActivityLogType.WorkspaceCreated, Summary = "Created" };
        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();

        var result = await _service.GetActivityLogsAsync(userId, workspace.Id);

        Assert.Single(result);
        Assert.Equal("Created", result.First().Summary);
    }

    [Fact]
    public async Task DeleteWorkspaceAsync_OwnerCanDelete_ShouldSucceed()
    {
        var ownerId = "owner";
        var workspace = new Workspace { Name = "WS", OwnerId = ownerId };
        workspace.Members.Add(new WorkspaceMember { UserId = ownerId, Role = WorkspaceRole.Owner });
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        await _service.DeleteWorkspaceAsync(ownerId, workspace.Id);

        var exists = await _context.Workspaces.AnyAsync(w => w.Id == workspace.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteWorkspaceAsync_NonOwnerCannotDelete_ShouldThrowForbidden()
    {
        var ownerId = "owner";
        var otherId = "other";
        var workspace = new Workspace { Name = "WS", OwnerId = ownerId };
        workspace.Members.Add(new WorkspaceMember { UserId = ownerId, Role = WorkspaceRole.Owner });
        workspace.Members.Add(new WorkspaceMember { UserId = otherId, Role = WorkspaceRole.Editor });
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.DeleteWorkspaceAsync(otherId, workspace.Id));
    }
}
