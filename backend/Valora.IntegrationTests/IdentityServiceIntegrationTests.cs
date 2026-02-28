using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

public class IdentityServiceIntegrationTests : BaseIntegrationTest
{
    public IdentityServiceIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task DeleteUser_ShouldCleanupAllData()
    {
        // Arrange
        var email = "delete-me@test.com";
        var userId = await AuthenticateAsync(email);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            
            var property = new Property { BagId = "D1", Address = "Del St 1" };
            db.Properties.Add(property);
            await db.SaveChangesAsync();

            var ws = new Workspace { Name = "Delete WS", OwnerId = userId };
            db.Workspaces.Add(ws);
            await db.SaveChangesAsync();

            db.SavedProperties.Add(new SavedProperty { WorkspaceId = ws.Id, PropertyId = property.Id, AddedByUserId = userId });
            await db.SaveChangesAsync();
        }

        // Act
        var identityService = Factory.Services.GetRequiredService<IIdentityService>();
        var result = await identityService.DeleteUserAsync(userId);

        // Assert
        Assert.True(result.Succeeded);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            Assert.False(await db.Workspaces.AnyAsync(w => w.OwnerId == userId));
            Assert.False(await db.SavedProperties.AnyAsync(s => s.AddedByUserId == userId));
        }
    }
}
