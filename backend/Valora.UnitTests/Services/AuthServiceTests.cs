using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly Mock<IExternalAuthService> _mockExternalAuthService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockTokenService = new Mock<ITokenService>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockExternalAuthService = new Mock<IExternalAuthService>();
        _authService = new AuthService(_mockIdentityService.Object, _mockTokenService.Object, _mockLogger.Object, _mockExternalAuthService.Object, TimeProvider.System);
    }

    [Fact]
    public async Task RegisterAsync_WithMismatchedPasswords_ReturnsFailure()
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
        _mockTokenService.Setup(x => x.CreateJwtTokenAsync(user)).ReturnsAsync("access_token");
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
    public async Task RefreshTokenAsync_RevokedToken_ReturnsNull()
    {
        // Reuse detection logic: returns null and revokes all tokens
        var revokedToken = new RefreshToken { UserId = "1", Revoked = DateTime.UtcNow.AddMinutes(-10) };
        _mockTokenService.Setup(x => x.GetRefreshTokenAsync("revoked"))
            .ReturnsAsync(revokedToken);

        var result = await _authService.RefreshTokenAsync("revoked");

        Assert.Null(result);
        _mockTokenService.Verify(x => x.RevokeAllRefreshTokensAsync("1"), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ReturnsNull()
    {
        var expiredToken = new RefreshToken { UserId = "1", Expires = DateTime.UtcNow.AddMinutes(-10) };
        _mockTokenService.Setup(x => x.GetRefreshTokenAsync("expired"))
            .ReturnsAsync(expiredToken);

        var result = await _authService.RefreshTokenAsync("expired");

        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_Success_RotatesTokens()
    {
        var user = new ApplicationUser { Id = "1", Email = "t@t.com" };
        var oldToken = new RefreshToken
        {
            RawToken = "old",
            UserId = "1",
            User = user,
            Expires = DateTime.UtcNow.AddMinutes(10)
        };
        var newToken = new RefreshToken { RawToken = "new", UserId = "1" };

        _mockTokenService.Setup(x => x.GetRefreshTokenAsync("old")).ReturnsAsync(oldToken);
        _mockTokenService.Setup(x => x.GenerateRefreshToken("1")).Returns(newToken);
        _mockTokenService.Setup(x => x.CreateJwtTokenAsync(user)).ReturnsAsync("new_access");

        var result = await _authService.RefreshTokenAsync("old");

        Assert.NotNull(result);
        Assert.Equal("new_access", result.Token);
        Assert.Equal("new", result.RefreshToken);

        _mockTokenService.Verify(x => x.RevokeRefreshTokenAsync("old"), Times.Once);
        _mockTokenService.Verify(x => x.SaveRefreshTokenAsync(newToken), Times.Once);
    }

    [Fact]
    public async Task ExternalLoginAsync_ExistingUser_ReturnsSuccess()
    {
        var request = new ExternalLoginRequestDto("google", "token");
        var externalUser = new ExternalUserDto("google", "123", "test@gmail.com", "User");
        var user = new ApplicationUser { Id = "1", Email = "test@gmail.com" };
        var refreshToken = new RefreshToken { RawToken = "refresh", UserId = "1" };

        _mockExternalAuthService.Setup(x => x.VerifyTokenAsync("google", "token"))
            .ReturnsAsync(externalUser);
        _mockIdentityService.Setup(x => x.GetUserByEmailAsync("test@gmail.com"))
            .ReturnsAsync(user);
        _mockTokenService.Setup(x => x.CreateJwtTokenAsync(user)).ReturnsAsync("access");
        _mockTokenService.Setup(x => x.GenerateRefreshToken("1")).Returns(refreshToken);

        var result = await _authService.ExternalLoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal("access", result.Token);
    }

    [Fact]
    public async Task ExternalLoginAsync_NewUser_RegistersAndReturnsSuccess()
    {
        var request = new ExternalLoginRequestDto("google", "token");
        var externalUser = new ExternalUserDto("google", "123", "new@gmail.com", "User");
        var user = new ApplicationUser { Id = "1", Email = "new@gmail.com" };
        var refreshToken = new RefreshToken { RawToken = "refresh", UserId = "1" };

        _mockExternalAuthService.Setup(x => x.VerifyTokenAsync("google", "token"))
            .ReturnsAsync(externalUser);
        _mockIdentityService.SetupSequence(x => x.GetUserByEmailAsync("new@gmail.com"))
            .ReturnsAsync((ApplicationUser?)null) // First call returns null (user doesn't exist)
            .ReturnsAsync(user); // Second call returns created user

        _mockIdentityService.Setup(x => x.CreateUserAsync("new@gmail.com", It.IsAny<string>()))
            .ReturnsAsync((Result.Success(), "1"));

        _mockTokenService.Setup(x => x.CreateJwtTokenAsync(user)).ReturnsAsync("access");
        _mockTokenService.Setup(x => x.GenerateRefreshToken("1")).Returns(refreshToken);

        var result = await _authService.ExternalLoginAsync(request);

        Assert.NotNull(result);
        Assert.Equal("access", result.Token);
        _mockIdentityService.Verify(x => x.CreateUserAsync("new@gmail.com", It.IsAny<string>()), Times.Once);
    }
}
