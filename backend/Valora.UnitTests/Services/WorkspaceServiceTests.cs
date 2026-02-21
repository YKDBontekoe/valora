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
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.UnitTests.Services;

public class WorkspaceServiceTests
{
    private readonly ValoraDbContext _context;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly WorkspaceService _service;

    public WorkspaceServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _identityServiceMock = new Mock<IIdentityService>();
        _service = new WorkspaceService(_context, _identityServiceMock.Object);
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

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.GetWorkspaceAsync("intruder", ws.Id));
    }

    [Fact]
    public async Task AddMemberAsync_OwnerCanInvite_ShouldSucceed()
    {
        var ownerId = "owner";
        var workspace = new Workspace { Name = "WS", OwnerId = ownerId };
        workspace.Members.Add(new WorkspaceMember { UserId = ownerId, Role = WorkspaceRole.Owner });
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        var dto = new InviteMemberDto("invitee@test.com", WorkspaceRole.Editor);
        _identityServiceMock.Setup(s => s.GetUserByEmailAsync(dto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        await _service.AddMemberAsync(ownerId, workspace.Id, dto);

        var member = await _context.WorkspaceMembers.FirstOrDefaultAsync(m => m.InvitedEmail == dto.Email);
        Assert.NotNull(member);
        Assert.Equal(WorkspaceRole.Editor, member.Role);
        Assert.True(member.IsPending);
    }

    [Fact]
    public async Task AddMemberAsync_ViewerCannotInvite_ShouldThrowForbidden()
    {
        var viewerId = "viewer";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = viewerId, Role = WorkspaceRole.Viewer });
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        var dto = new InviteMemberDto("test@test.com", WorkspaceRole.Editor);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.AddMemberAsync(viewerId, workspace.Id, dto));
    }

    [Fact]
    public async Task RemoveMemberAsync_OwnerCanRemoveMember_ShouldSucceed()
    {
        var ownerId = "owner";
        var memberId = "member";
        var workspace = new Workspace { Name = "WS", OwnerId = ownerId };
        workspace.Members.Add(new WorkspaceMember { UserId = ownerId, Role = WorkspaceRole.Owner });
        var memberToRemove = new WorkspaceMember { UserId = memberId, Role = WorkspaceRole.Viewer };
        workspace.Members.Add(memberToRemove);
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        await _service.RemoveMemberAsync(ownerId, workspace.Id, memberToRemove.Id);

        var exists = await _context.WorkspaceMembers.AnyAsync(m => m.Id == memberToRemove.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task SaveListingAsync_ViewerCannotSave_ShouldThrowForbidden()
    {
        var viewerId = "viewer";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = viewerId, Role = WorkspaceRole.Viewer });
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.SaveListingAsync(viewerId, workspace.Id, Guid.NewGuid(), "notes"));
    }

    [Fact]
    public async Task AddCommentAsync_MemberCanComment_ShouldSucceed()
    {
        var userId = "user";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Editor });

        var listing = new Listing { FundaId = "1", Address = "A" };
        var savedListing = new SavedListing { Workspace = workspace, Listing = listing, AddedByUserId = userId };
        _context.SavedListings.Add(savedListing);
        await _context.SaveChangesAsync();

        var dto = new AddCommentDto("Hello", null);
        var result = await _service.AddCommentAsync(userId, workspace.Id, savedListing.Id, dto);

        Assert.NotNull(result);
        Assert.Equal("Hello", result.Content);

        var log = await _context.ActivityLogs.OrderByDescending(l => l.CreatedAt).FirstAsync();
        Assert.Equal(ActivityLogType.CommentAdded, log.Type);
    }
}
