using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Valora.Api.Extensions;
using Valora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using Valora.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Valora.UnitTests.Extensions;

public class ServiceCollectionAuthTests
{
    private readonly Mock<IHostEnvironment> _environment = new();
    private readonly IServiceCollection _services = new ServiceCollection();

    public ServiceCollectionAuthTests()
    {
        // Add minimal dependencies for Identity
        _services.AddLogging();
        _services.AddDbContext<ValoraDbContext>(options => options.UseInMemoryDatabase("AuthTestsDb"));
    }

    [Fact]
    public void AddIdentityAndAuth_WithValidConfig_Succeeds()
    {
        // Arrange
        var validSecret = new string('a', 32);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JWT_SECRET", validSecret },
                { "JWT_ISSUER", "valora" },
                { "JWT_AUDIENCE", "valora" }
            })
            .Build();

        _environment.Setup(e => e.EnvironmentName).Returns("Production");

        // Act
        _services.AddIdentityAndAuth(config, _environment.Object);

        // Assert
        // If no exception is thrown, it succeeded.
        // We can verify that services were registered.
        var provider = _services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<UserManager<ApplicationUser>>());
    }

    [Fact]
    public void AddIdentityAndAuth_WithMissingSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>()) // Empty config
            .Build();

        _environment.Setup(e => e.EnvironmentName).Returns("Production");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _services.AddIdentityAndAuth(config, _environment.Object));
        Assert.Contains("JWT_SECRET is not configured", ex.Message);
    }

    [Fact]
    public void AddIdentityAndAuth_WithShortSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var shortSecret = "too_short_secret";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JWT_SECRET", shortSecret }
            })
            .Build();

        _environment.Setup(e => e.EnvironmentName).Returns("Production");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _services.AddIdentityAndAuth(config, _environment.Object));
        Assert.Contains("JWT_SECRET must be at least 32 characters long", ex.Message);
    }

    [Fact]
    public void AddIdentityAndAuth_WithDefaultSecretInProduction_ThrowsInvalidOperationException()
    {
        // Arrange
        var defaultSecret = "DevelopmentOnlySecret_DoNotUseInProd_ChangeMe!"; // Length 46
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JWT_SECRET", defaultSecret }
            })
            .Build();

        _environment.Setup(e => e.EnvironmentName).Returns("Production");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _services.AddIdentityAndAuth(config, _environment.Object));
        Assert.Contains("Critical Security Risk", ex.Message);
    }

    [Fact]
    public void AddIdentityAndAuth_WithDefaultSecretInDevelopment_Succeeds()
    {
        // Arrange
        var defaultSecret = "DevelopmentOnlySecret_DoNotUseInProd_ChangeMe!";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JWT_SECRET", defaultSecret },
                { "JWT_ISSUER", "valora" },
                { "JWT_AUDIENCE", "valora" }
            })
            .Build();

        // Using IsDevelopment() extension method check logic: Checks if EnvironmentName is "Development"
        _environment.Setup(e => e.EnvironmentName).Returns("Development");

        // Act
        _services.AddIdentityAndAuth(config, _environment.Object);

        // Assert
        var provider = _services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<UserManager<ApplicationUser>>());
    }
}
