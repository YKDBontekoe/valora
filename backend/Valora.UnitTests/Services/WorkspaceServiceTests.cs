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
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IActivityLogService> _activityLogServiceMock;
    private readonly WorkspaceService _service;
    private readonly WorkspaceRepository _repository;

    public WorkspaceServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _identityServiceMock = new Mock<IIdentityService>();
        _activityLogServiceMock = new Mock<IActivityLogService>();
        _repository = new WorkspaceRepository(_context);
        _service = new WorkspaceService(_repository, _activityLogServiceMock.Object);
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

        _activityLogServiceMock.Verify(a => a.LogActivityAsync(It.IsAny<Workspace>(), userId, ActivityLogType.WorkspaceCreated, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
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

        _activityLogServiceMock.Verify(a => a.LogActivityAsync((Guid?)null, userId, ActivityLogType.WorkspaceCreated, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
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

        _activityLogServiceMock.Verify(a => a.LogActivityAsync(It.IsAny<Workspace>(), ownerId, ActivityLogType.WorkspaceDeleted, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
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
