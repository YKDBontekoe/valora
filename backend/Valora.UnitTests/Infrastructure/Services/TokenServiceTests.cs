using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.Common.Models;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.UnitTests.Infrastructure.Services;

public class TokenServiceTests
{
    private readonly ValoraDbContext _dbContext;
    private readonly Mock<IOptions<JwtOptions>> _mockOptions;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ValoraDbContext(options);

        _mockOptions = new Mock<IOptions<JwtOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(new JwtOptions
        {
            Secret = "SuperSecretKeyForTestingPurposes123!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60
        });

        _tokenService = new TokenService(_mockOptions.Object, _dbContext);
    }

    [Fact]
    public void GenerateToken_WithValidUser_ReturnsToken()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "testuser", Email = "test@example.com" };

        // Act
        var token = _tokenService.GenerateToken(user);

        // Assert
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsValidToken()
    {
        // Act
        var refreshToken = _tokenService.GenerateRefreshToken("user1");

        // Assert
        Assert.NotNull(refreshToken);
        Assert.False(string.IsNullOrEmpty(refreshToken.Token));
        Assert.Equal("user1", refreshToken.UserId);
        Assert.True(refreshToken.IsActive);
        Assert.False(refreshToken.IsExpired);
    }

    [Fact]
    public async Task SaveRefreshTokenAsync_PersistsToken()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Token = "test_token",
            UserId = "user1",
            Expires = DateTime.UtcNow.AddDays(1)
        };

        // Act
        await _tokenService.SaveRefreshTokenAsync(refreshToken);

        // Assert
        var savedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == "test_token");
        Assert.NotNull(savedToken);
    }

    [Fact]
    public async Task GetRefreshTokenAsync_ReturnsTokenWithUser()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "test", Email = "test@test.com" };
        _dbContext.Users.Add(user);
        var refreshToken = new RefreshToken
        {
            Token = "test_token_get",
            UserId = "user1",
            Expires = DateTime.UtcNow.AddDays(1)
        };
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var retrievedToken = await _tokenService.GetRefreshTokenAsync("test_token_get");

        // Assert
        Assert.NotNull(retrievedToken);
        Assert.NotNull(retrievedToken!.User);
        Assert.Equal("user1", retrievedToken.User.Id);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_RevokesToken()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            Token = "test_token_revoke",
            UserId = "user1",
            Expires = DateTime.UtcNow.AddDays(1)
        };
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        // Act
        await _tokenService.RevokeRefreshTokenAsync("test_token_revoke");

        // Assert
        var revokedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == "test_token_revoke");
        Assert.NotNull(revokedToken);
        Assert.NotNull(revokedToken!.Revoked);
        Assert.False(revokedToken.IsActive);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithNonExistentToken_DoesNothing()
    {
        // Act & Assert
        await _tokenService.RevokeRefreshTokenAsync("non_existent_token");
        // Should not throw
    }
}
