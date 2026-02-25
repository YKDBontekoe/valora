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

public class WorkspaceMemberServiceTests
{
    private readonly ValoraDbContext _context;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IActivityLogService> _activityLogServiceMock;
    private readonly WorkspaceMemberService _service;
    private readonly WorkspaceRepository _repository;

    public WorkspaceMemberServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _identityServiceMock = new Mock<IIdentityService>();
        _activityLogServiceMock = new Mock<IActivityLogService>();
        _repository = new WorkspaceRepository(_context);
        _service = new WorkspaceMemberService(_repository, _identityServiceMock.Object, _activityLogServiceMock.Object);
    }

    [Fact]
    public async Task GetMembersAsync_ShouldReturnMembers_WhenUserIsMember()
    {
        var userId = "user1";
        var ws = new Workspace { Name = "WS", OwnerId = userId };
        ws.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Owner });
        _context.Workspaces.Add(ws);
        await _context.SaveChangesAsync();

        var result = await _service.GetMembersAsync(userId, ws.Id);

        Assert.Single(result);
        Assert.Equal(userId, result.First().UserId);
    }

    [Fact]
    public async Task GetMembersAsync_ShouldThrowForbidden_WhenUserIsNotMember()
    {
        var userId = "user1";
        var otherUser = "user2";
        var ws = new Workspace { Name = "WS", OwnerId = userId };
        ws.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Owner });
        _context.Workspaces.Add(ws);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.GetMembersAsync(otherUser, ws.Id));
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

        // Verify redacted log
        _activityLogServiceMock.Verify(a => a.LogActivityAsync(workspace.Id, ownerId, ActivityLogType.MemberInvited, "Invited member", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddMemberAsync_ShouldReturnEarly_WhenMemberAlreadyInvited()
    {
        var ownerId = "owner";
        var email = "existing@test.com";
        var workspace = new Workspace { Name = "WS", OwnerId = ownerId };
        workspace.Members.Add(new WorkspaceMember { UserId = ownerId, Role = WorkspaceRole.Owner });
        workspace.Members.Add(new WorkspaceMember { InvitedEmail = email, Role = WorkspaceRole.Viewer });
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        var dto = new InviteMemberDto(email, WorkspaceRole.Editor);

        await _service.AddMemberAsync(ownerId, workspace.Id, dto);

        var members = await _context.WorkspaceMembers.Where(m => m.InvitedEmail == email).ToListAsync();
        Assert.Single(members); // Should not add duplicate
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

        // Verify redacted log
        _activityLogServiceMock.Verify(a => a.LogActivityAsync(workspace.Id, ownerId, ActivityLogType.MemberRemoved, "Removed member", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldThrowInvalidOperation_WhenRemovingSelf()
    {
        var ownerId = "owner";
        var workspace = new Workspace { Name = "WS", OwnerId = ownerId };
        var ownerMember = new WorkspaceMember { UserId = ownerId, Role = WorkspaceRole.Owner };
        workspace.Members.Add(ownerMember);
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RemoveMemberAsync(ownerId, workspace.Id, ownerMember.Id));
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldThrowNotFound_WhenMemberDoesNotExist()
    {
        var ownerId = "owner";
        var workspace = new Workspace { Name = "WS", OwnerId = ownerId };
        workspace.Members.Add(new WorkspaceMember { UserId = ownerId, Role = WorkspaceRole.Owner });
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.RemoveMemberAsync(ownerId, workspace.Id, Guid.NewGuid()));
    }
}
