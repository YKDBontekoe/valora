using Moq;
using Valora.Application.Common.Constants;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Application.Services;

namespace Valora.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockTokenService = new Mock<ITokenService>();
        _authService = new AuthService(_mockIdentityService.Object, _mockTokenService.Object);
    }

    [Fact]
    public async Task RegisterAsync_PasswordsDoNotMatch_ReturnsFailure()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@test.com",
            Password = "password",
            ConfirmPassword = "mismatch"
        };

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(ErrorMessages.PasswordsDoNotMatch, result.Errors);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsSuccess()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test@test.com",
            Password = "password",
            ConfirmPassword = "password"
        };

        _mockIdentityService.Setup(x => x.CreateUserAsync(registerDto.Email, registerDto.Password))
            .ReturnsAsync((Result.Success(), "userId"));

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.True(result.Succeeded);
    }
}
