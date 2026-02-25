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
    private readonly WorkspaceService _service;
    private readonly WorkspaceRepository _repository;

    public WorkspaceServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _identityServiceMock = new Mock<IIdentityService>();
        _repository = new WorkspaceRepository(_context);
        _service = new WorkspaceService(_repository, _identityServiceMock.Object);
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

    [Fact]
    public async Task SaveListingAsync_ShouldSaveListing_WhenNotAlreadySaved()
    {
        var userId = "user";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Editor });
        _context.Workspaces.Add(workspace);

        var listing = new Listing { FundaId = "1", Address = "A" };
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync();

        var result = await _service.SaveListingAsync(userId, workspace.Id, listing.Id, "notes");

        Assert.NotNull(result);
        Assert.Equal(listing.Id, result.ListingId);
    }

    [Fact]
    public async Task SaveListingAsync_ShouldReturnExisting_WhenAlreadySaved()
    {
        var userId = "user";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Editor });

        var listing = new Listing { FundaId = "1", Address = "A" };
        var existingSaved = new SavedListing { Workspace = workspace, Listing = listing, AddedByUserId = userId };
        _context.SavedListings.Add(existingSaved);
        await _context.SaveChangesAsync();

        var result = await _service.SaveListingAsync(userId, workspace.Id, listing.Id, "new notes");

        Assert.Equal(existingSaved.Id, result.Id);
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
    public async Task SaveListingAsync_ShouldThrowNotFound_WhenListingDoesNotExist()
    {
        var userId = "user";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Editor });
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.SaveListingAsync(userId, workspace.Id, Guid.NewGuid(), "notes"));
    }

    [Fact]
    public async Task GetSavedListingsAsync_ShouldReturnListings()
    {
        var userId = "user";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Viewer });

        var listing = new Listing { FundaId = "1", Address = "A" };
        var saved = new SavedListing { Workspace = workspace, Listing = listing, AddedByUserId = "owner" };
        _context.SavedListings.Add(saved);
        await _context.SaveChangesAsync();

        var result = await _service.GetSavedListingsAsync(userId, workspace.Id);

        Assert.Single(result);
        Assert.Equal(saved.Id, result.First().Id);
    }

    [Fact]
    public async Task RemoveSavedListingAsync_EditorCanRemove_ShouldSucceed()
    {
        var userId = "user";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Editor });

        var listing = new Listing { FundaId = "1", Address = "A" };
        var saved = new SavedListing { Workspace = workspace, Listing = listing, AddedByUserId = "owner" };
        _context.SavedListings.Add(saved);
        await _context.SaveChangesAsync();

        await _service.RemoveSavedListingAsync(userId, workspace.Id, saved.Id);

        var exists = await _context.SavedListings.AnyAsync(sl => sl.Id == saved.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task RemoveSavedListingAsync_ViewerCannotRemove_ShouldThrowForbidden()
    {
        var userId = "viewer";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Viewer });
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.RemoveSavedListingAsync(userId, workspace.Id, Guid.NewGuid()));
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

    [Fact]
    public async Task AddCommentAsync_ShouldThrowNotFound_WhenSavedListingDoesNotExist()
    {
        var userId = "user";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Editor });
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        var dto = new AddCommentDto("Hello", null);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.AddCommentAsync(userId, workspace.Id, Guid.NewGuid(), dto));
    }

    [Fact]
    public async Task GetCommentsAsync_ShouldReturnThreadedComments()
    {
        var userId = "user";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Viewer });

        var savedListing = new SavedListing { Workspace = workspace, ListingId = Guid.NewGuid(), AddedByUserId = "owner" };
        var parentComment = new ListingComment { SavedListing = savedListing, UserId = "owner", Content = "Parent" };
        var replyComment = new ListingComment { SavedListing = savedListing, UserId = userId, Content = "Reply", ParentComment = parentComment };

        _context.ListingComments.AddRange(parentComment, replyComment);
        await _context.SaveChangesAsync();

        var result = await _service.GetCommentsAsync(userId, workspace.Id, savedListing.Id);

        Assert.Single(result); // Only parent at root
        Assert.Single(result.First().Replies); // Reply nested
        Assert.Equal("Reply", result.First().Replies.First().Content);
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
}
