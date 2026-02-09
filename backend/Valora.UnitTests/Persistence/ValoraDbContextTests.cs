using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.UnitTests.Persistence;

public class ValoraDbContextTests
{
    [Fact]
    public void Listing_Configuration_HasExpectedConstraints()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ValoraDbContext(options);
        var model = context.Model;
        var entity = model.FindEntityType(typeof(Listing));
        Assert.NotNull(entity);

        var descriptionProp = entity.FindProperty(nameof(Listing.Description));
        Assert.Equal(10000, descriptionProp?.GetMaxLength());

        var videoUrlProp = entity.FindProperty(nameof(Listing.VideoUrl));
        Assert.Equal(2048, videoUrlProp?.GetMaxLength());

        var virtualTourUrlProp = entity.FindProperty(nameof(Listing.VirtualTourUrl));
        Assert.Equal(2048, virtualTourUrlProp?.GetMaxLength());

        var brochureUrlProp = entity.FindProperty(nameof(Listing.BrochureUrl));
        Assert.Equal(2048, brochureUrlProp?.GetMaxLength());

        var brokerLogoUrlProp = entity.FindProperty(nameof(Listing.BrokerLogoUrl));
        Assert.Equal(2048, brokerLogoUrlProp?.GetMaxLength());
    }

    [Fact]
    public void RefreshToken_Configuration_HasExpectedConstraints()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ValoraDbContext(options);
        var model = context.Model;
        var entity = model.FindEntityType(typeof(RefreshToken));
        Assert.NotNull(entity);

        var tokenHashProp = entity.FindProperty(nameof(RefreshToken.TokenHash));
        Assert.Equal(256, tokenHashProp?.GetMaxLength());
        Assert.False(tokenHashProp?.IsNullable);
    }
}
