using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Domain.Enums;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class NotificationPipelineTests : BaseTestcontainersIntegrationTest
{
    public NotificationPipelineTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddComment_ShouldDispatchEventAndCreateNotificationForOtherMembers()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var ownerId = "owner-user-id";
        var memberId = "member-user-id";

        // Ensure clean state
        dbContext.Notifications.RemoveRange(dbContext.Notifications);
        await dbContext.SaveChangesAsync();

        var ownerUser = new ApplicationUser { Id = ownerId, UserName = "owner@test.com", Email = "owner@test.com" };
        var memberUser = new ApplicationUser { Id = memberId, UserName = "member@test.com", Email = "member@test.com" };

        dbContext.Users.Add(ownerUser);
        dbContext.Users.Add(memberUser);

        var workspace = new Workspace
        {
            Name = "Test Workspace",
            OwnerId = ownerId,
            Members = new List<WorkspaceMember>
            {
                new() { UserId = ownerId, Role = WorkspaceRole.Owner, JoinedAt = DateTime.UtcNow },
                new() { UserId = memberId, Role = WorkspaceRole.Viewer, JoinedAt = DateTime.UtcNow }
            }
        };

        dbContext.Workspaces.Add(workspace);

        var listing = new Listing
        {
            FundaId = "EXT123",
            Address = "Test Address",
            City = "Amsterdam",
            Price = 500000,
            Bedrooms = 2,
            LivingAreaM2 = 80,
            PropertyType = "House",
            YearBuilt = 2020,
            EnergyLabel = "A",
            Url = "http://test.com",
            Latitude = 52.3676,
            Longitude = 4.9041
        };
        dbContext.Listings.Add(listing);

        var savedListing = new SavedListing
        {
            WorkspaceId = workspace.Id,
            ListingId = listing.Id,
            AddedByUserId = ownerId
        };
        dbContext.SavedListings.Add(savedListing);

        await dbContext.SaveChangesAsync();

        var dto = new AddCommentDto("This is a test comment", null);

        // Act
        var workspaceListingService = scope.ServiceProvider.GetRequiredService<IWorkspaceListingService>();

        await workspaceListingService.AddCommentAsync(memberId, workspace.Id, savedListing.Id, dto);

        // Assert - Owner should get a notification
        var notifications = await dbContext.Notifications
            .Where(n => n.UserId == ownerId)
            .ToListAsync();

        notifications.Count.ShouldBeGreaterThan(0);
        var notification = notifications.First();
        notification.Type.ShouldBe(NotificationType.Info);
        notification.Title.ShouldBe("New Comment");
        notification.ActionUrl.ShouldBe($"/workspaces/{workspace.Id}/listings/{savedListing.Id}");

        // Verify member who commented does NOT get a notification
        var memberNotifications = await dbContext.Notifications
            .Where(n => n.UserId == memberId)
            .ToListAsync();

        memberNotifications.ShouldBeEmpty();
    }
}
