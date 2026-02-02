using Microsoft.Extensions.Options;
using Moq;
using Microsoft.EntityFrameworkCore;
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

    public TokenServiceTests()
    {
        _mockOptions = new Mock<IOptions<JwtOptions>>();
    }

    [Fact]
    public void GenerateToken_ReturnsToken_WhenOptionsAreValid()
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

        var tokenService = new TokenService(_mockOptions.Object, new Mock<ValoraDbContext>(new DbContextOptions<ValoraDbContext>()).Object, TimeProvider.System);

        var user = new ApplicationUser
        {
            Id = "user-id",
            UserName = "test@example.com",
            Email = "test@example.com"
        };

        // Act
        var token = tokenService.GenerateToken(user);

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
    }

    [Fact]
    public void GenerateToken_ThrowsException_WhenSecretIsMissing()
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

        var tokenService = new TokenService(_mockOptions.Object, new Mock<ValoraDbContext>(new DbContextOptions<ValoraDbContext>()).Object, TimeProvider.System);

        var user = new ApplicationUser { Id = "1", UserName = "test" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => tokenService.GenerateToken(user));
    }
}
