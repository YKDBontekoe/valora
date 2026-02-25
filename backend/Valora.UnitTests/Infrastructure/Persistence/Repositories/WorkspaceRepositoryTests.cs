using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Valora.UnitTests.Infrastructure.Persistence.Repositories;

public class WorkspaceRepositoryTests
{
    private DbContextOptions<ValoraDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetUserWorkspaceDtosAsync_ShouldReturnWorkspacesForUser()
    {
        using var context = new ValoraDbContext(CreateOptions());
        var userId = "user1";
        var otherUser = "user2";

        var ws1 = new Workspace
        {
            Name = "WS1",
            Description = "Desc1",
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        ws1.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Owner });
        ws1.SavedListings.Add(new SavedListing { ListingId = Guid.NewGuid(), AddedByUserId = userId });

        var ws2 = new Workspace
        {
            Name = "WS2",
            OwnerId = otherUser,
            CreatedAt = DateTime.UtcNow
        };
        ws2.Members.Add(new WorkspaceMember { UserId = otherUser, Role = WorkspaceRole.Owner });

        context.Workspaces.AddRange(ws1, ws2);
        await context.SaveChangesAsync();

        var repository = new WorkspaceRepository(context);
        var result = await repository.GetUserWorkspaceDtosAsync(userId);

        Assert.Single(result);
        var dto = result.First();
        Assert.Equal(ws1.Id, dto.Id);
        Assert.Equal("WS1", dto.Name);
        Assert.Equal("Desc1", dto.Description);
        Assert.Equal(userId, dto.OwnerId);
        Assert.Equal(1, dto.MemberCount);
        Assert.Equal(1, dto.SavedListingCount);
    }

    [Fact]
    public async Task GetUserWorkspaceDtosAsync_ShouldSortByCreatedAtDescending()
    {
        using var context = new ValoraDbContext(CreateOptions());
        var userId = "user1";

        var ws1 = new Workspace
        {
            Name = "Old",
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        ws1.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Owner });

        var ws2 = new Workspace
        {
            Name = "New",
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };
        ws2.Members.Add(new WorkspaceMember { UserId = userId, Role = WorkspaceRole.Owner });

        context.Workspaces.AddRange(ws1, ws2);
        await context.SaveChangesAsync();

        var repository = new WorkspaceRepository(context);
        var result = await repository.GetUserWorkspaceDtosAsync(userId);

        Assert.Equal(2, result.Count);
        Assert.Equal(ws2.Id, result[0].Id); // Newest first
        Assert.Equal(ws1.Id, result[1].Id);
    }

    [Fact]
    public async Task GetSavedListingDtosAsync_ShouldReturnListingsForWorkspace()
    {
        using var context = new ValoraDbContext(CreateOptions());
        var userId = "user1";
        var workspaceId = Guid.NewGuid();
        var otherWorkspaceId = Guid.NewGuid();

        var listing = new Listing
        {
            FundaId = "1",
            Address = "Test St 1",
            City = "Amsterdam",
            Price = 500000,
            Bedrooms = 2,
            LivingAreaM2 = 80,
            ImageUrl = "http://img.com/1.jpg"
        };
        context.Listings.Add(listing);

        var saved1 = new SavedListing
        {
            WorkspaceId = workspaceId,
            Listing = listing,
            AddedByUserId = userId,
            Notes = "Note1",
            CreatedAt = DateTime.UtcNow
        };
        saved1.Comments.Add(new ListingComment { UserId = userId, Content = "Nice" });

        var saved2 = new SavedListing
        {
            WorkspaceId = otherWorkspaceId,
            Listing = listing,
            AddedByUserId = userId
        };

        context.SavedListings.AddRange(saved1, saved2);
        await context.SaveChangesAsync();

        var repository = new WorkspaceRepository(context);
        var result = await repository.GetSavedListingDtosAsync(workspaceId);

        Assert.Single(result);
        var dto = result.First();
        Assert.Equal(saved1.Id, dto.Id);
        Assert.Equal(listing.Id, dto.ListingId);
        Assert.Equal("Note1", dto.Notes);
        Assert.Equal(1, dto.CommentCount);

        Assert.NotNull(dto.Listing);
        Assert.Equal("Test St 1", dto.Listing.Address);
        Assert.Equal("Amsterdam", dto.Listing.City);
        Assert.Equal(500000, dto.Listing.Price);
        Assert.Equal(2, dto.Listing.Bedrooms);
        Assert.Equal(80, dto.Listing.LivingAreaM2);
        Assert.Equal("http://img.com/1.jpg", dto.Listing.ImageUrl);
    }

    [Fact]
    public async Task GetSavedListingDtosAsync_ShouldSortByCreatedAtDescending()
    {
        using var context = new ValoraDbContext(CreateOptions());
        var workspaceId = Guid.NewGuid();
        var userId = "user1";

        var listing = new Listing { FundaId = "1", Address = "A" };
        context.Listings.Add(listing);

        var saved1 = new SavedListing { WorkspaceId = workspaceId, Listing = listing, AddedByUserId = userId, CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var saved2 = new SavedListing { WorkspaceId = workspaceId, Listing = listing, AddedByUserId = userId, CreatedAt = DateTime.UtcNow.AddHours(-1) };

        context.SavedListings.AddRange(saved1, saved2);
        await context.SaveChangesAsync();

        var repository = new WorkspaceRepository(context);
        var result = await repository.GetSavedListingDtosAsync(workspaceId);

        Assert.Equal(2, result.Count);
        Assert.Equal(saved2.Id, result[0].Id); // Newest first
        Assert.Equal(saved1.Id, result[1].Id);
    }
}
