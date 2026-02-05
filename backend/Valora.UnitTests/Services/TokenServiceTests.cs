using Microsoft.Extensions.Options;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Valora.Application.Common.Models;
using Valora.Domain.Entities;
using Valora.Infrastructure.Services;
using Valora.Infrastructure.Persistence;

namespace Valora.UnitTests.Services;

public class TokenServiceTests
{
    private readonly Mock<IOptions<JwtOptions>> _mockOptions;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;

    public TokenServiceTests()
    {
        _mockOptions = new Mock<IOptions<JwtOptions>>();

        var store = new Mock<IUserStore<ApplicationUser>>();
        var identityOptions = new Mock<IOptions<IdentityOptions>>();
        identityOptions.Setup(x => x.Value).Returns(new IdentityOptions());
        var passwordHasher = new Mock<IPasswordHasher<ApplicationUser>>();
        var userValidators = new List<IUserValidator<ApplicationUser>>();
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
        var lookupNormalizer = new Mock<ILookupNormalizer>();
        var errorDescriber = new IdentityErrorDescriber();
        var serviceProvider = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<UserManager<ApplicationUser>>>();

        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            identityOptions.Object,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            lookupNormalizer.Object,
            errorDescriber,
            serviceProvider.Object,
            logger.Object);
    }

    [Fact]
    public async Task GenerateToken_ReturnsToken_WhenOptionsAreValid()
    {
        // Arrange
        var jwtOptions = new JwtOptions
        {
            Secret = "SuperSecretKeyThatIsLongEnoughForHmacSha256",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60
        };

        _mockOptions.Setup(x => x.Value).Returns(jwtOptions);

        // Mock Roles logic
        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });

        var tokenService = new TokenService(
            _mockOptions.Object,
            new Mock<ValoraDbContext>(new DbContextOptions<ValoraDbContext>()).Object,
            TimeProvider.System,
            _mockUserManager.Object);

        var user = new ApplicationUser
        {
            Id = "user-id",
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        // Act
        var token = await tokenService.GenerateTokenAsync(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Contains("TestAudience", jwtToken.Audiences);

        // Check for NameIdentifier.
        // Note: JwtSecurityTokenHandler might map ClaimTypes.NameIdentifier to "nameid" or keep it as is.
        var nameIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")
                          ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        Assert.NotNull(nameIdClaim);
        Assert.Equal("user-id", nameIdClaim.Value);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("User", roleClaim.Value);
    }

    [Fact]
    public async Task GenerateToken_ThrowsException_WhenSecretIsMissing()
    {
        // Arrange
        var jwtOptions = new JwtOptions
        {
            Secret = "", // Missing secret
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60
        };

        _mockOptions.Setup(x => x.Value).Returns(jwtOptions);

        var tokenService = new TokenService(
            _mockOptions.Object,
            new Mock<ValoraDbContext>(new DbContextOptions<ValoraDbContext>()).Object,
            TimeProvider.System,
            _mockUserManager.Object);

        var user = new ApplicationUser { Id = "1", UserName = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => tokenService.GenerateTokenAsync(user));
    }
}
