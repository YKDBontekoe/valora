using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Exceptions;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Services;

public class WorkspaceListingServiceTests
{
    private readonly ValoraDbContext _context;
    private readonly WorkspaceListingService _service;
    private readonly WorkspaceRepository _repository;

    public WorkspaceListingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);
        _repository = new WorkspaceRepository(_context);
        _service = new WorkspaceListingService(_repository);
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
    public async Task AddCommentAsync_ShouldThrowInvalidOperation_WhenParentCommentBelongsToDifferentListing()
    {
        var userId = "user";
        var workspace = new Workspace { Name = "WS", OwnerId = "owner" };
        workspace.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Editor });

        var listing1 = new Listing { FundaId = "1", Address = "A" };
        var savedListing1 = new SavedListing { Workspace = workspace, Listing = listing1, AddedByUserId = userId };

        var listing2 = new Listing { FundaId = "2", Address = "B" };
        var savedListing2 = new SavedListing { Workspace = workspace, Listing = listing2, AddedByUserId = userId };

        _context.SavedListings.AddRange(savedListing1, savedListing2);

        var parentComment = new ListingComment { SavedListing = savedListing1, UserId = "owner", Content = "Parent" };
        _context.ListingComments.Add(parentComment);

        await _context.SaveChangesAsync();

        var dto = new AddCommentDto("Reply", parentComment.Id);

        // Try to add a reply to listing2 using a parent from listing1
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddCommentAsync(userId, workspace.Id, savedListing2.Id, dto));
    }
}
