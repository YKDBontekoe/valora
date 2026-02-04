using Microsoft.Extensions.Configuration;
using Moq;
using Valora.Application.Common.Constants;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockTokenService = new Mock<ITokenService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _authService = new AuthService(_mockIdentityService.Object, _mockTokenService.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task RegisterAsync_PasswordsDoNotMatch_ReturnsFailure()
    {
        var registerDto = new RegisterDto { Email = "t@t.com", Password = "p", ConfirmPassword = "x" };
        var result = await _authService.RegisterAsync(registerDto);
        Assert.False(result.Succeeded);
        Assert.Contains(ErrorMessages.PasswordsDoNotMatch, result.Errors);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsSuccess()
    {
        var registerDto = new RegisterDto { Email = "t@t.com", Password = "p", ConfirmPassword = "p" };
        
        _mockConfiguration.Setup(x => x["ADMIN_EMAIL"]).Returns((string?)null);
        _mockIdentityService.Setup(x => x.CreateUserAsync(registerDto.Email, registerDto.Password))
            .ReturnsAsync((Result.Success(), "userId"));

        var result = await _authService.RegisterAsync(registerDto);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsNull()
    {
        _mockIdentityService.Setup(x => x.GetUserByEmailAsync("t@t.com")).ReturnsAsync((ApplicationUser?)null);
        var result = await _authService.LoginAsync(new LoginDto("t@t.com", "p"));
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        var user = new ApplicationUser { Id = "1", Email = "t@t.com" };
        _mockIdentityService.Setup(x => x.GetUserByEmailAsync("t@t.com")).ReturnsAsync(user);
        _mockIdentityService.Setup(x => x.CheckPasswordAsync("t@t.com", "p")).ReturnsAsync(false);

        var result = await _authService.LoginAsync(new LoginDto("t@t.com", "p"));
        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_Success_ReturnsTokens()
    {
        var user = new ApplicationUser { Id = "1", Email = "t@t.com" };
        var refreshToken = new RefreshToken { RawToken = "refresh", UserId = "1" };

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync("t@t.com")).ReturnsAsync(user);
        _mockIdentityService.Setup(x => x.CheckPasswordAsync("t@t.com", "p")).ReturnsAsync(true);
        _mockTokenService.Setup(x => x.GenerateTokenAsync(user)).ReturnsAsync("access_token");
        _mockTokenService.Setup(x => x.GenerateRefreshToken(user.Id)).Returns(refreshToken);

        var result = await _authService.LoginAsync(new LoginDto("t@t.com", "p"));

        Assert.NotNull(result);
        Assert.Equal("access_token", result.Token);
        Assert.Equal("refresh", result.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ReturnsNull()
    {
        _mockTokenService.Setup(x => x.GetRefreshTokenAsync("bad")).ReturnsAsync((RefreshToken?)null);
        var result = await _authService.RefreshTokenAsync("bad");
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_Success_RotatesTokens()
    {
        var user = new ApplicationUser { Id = "1", Email = "t@t.com" };
        // IsActive check: Revoked == null && !IsExpired (UTC < Expires)
        var oldToken = new RefreshToken
        {
            RawToken = "old",
            UserId = "1",
            Revoked = null,
            Expires = DateTime.UtcNow.AddDays(1),
            User = user
        };
        var newToken = new RefreshToken { RawToken = "new", UserId = "1" };

        _mockTokenService.Setup(x => x.GetRefreshTokenAsync("old")).ReturnsAsync(oldToken);
        _mockTokenService.Setup(x => x.GenerateRefreshToken("1")).Returns(newToken);
        _mockTokenService.Setup(x => x.GenerateTokenAsync(user)).ReturnsAsync("new_access");

        var result = await _authService.RefreshTokenAsync("old");

        Assert.NotNull(result);
        Assert.Equal("new_access", result.Token);
        Assert.Equal("new", result.RefreshToken);

        _mockTokenService.Verify(x => x.RevokeRefreshTokenAsync("old"), Times.Once);
        _mockTokenService.Verify(x => x.SaveRefreshTokenAsync(newToken), Times.Once);
    }
}
