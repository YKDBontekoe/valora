using Google.Apis.Auth;
using Moq;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces.External;
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.UnitTests.Services;

public class ExternalAuthServiceTests
{
    private readonly Mock<IGoogleTokenValidator> _mockGoogleValidator;
    private readonly ExternalAuthService _service;

    public ExternalAuthServiceTests()
    {
        _mockGoogleValidator = new Mock<IGoogleTokenValidator>();
        _service = new ExternalAuthService(_mockGoogleValidator.Object);
    }

    [Fact]
    public async Task VerifyTokenAsync_ValidGoogleToken_ReturnsUser()
    {
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "123",
            Email = "test@gmail.com",
            Name = "Test User"
        };

        _mockGoogleValidator.Setup(x => x.ValidateAsync("valid_token"))
            .ReturnsAsync(payload);

        var result = await _service.VerifyTokenAsync("google", "valid_token");

        Assert.Equal("google", result.Provider);
        Assert.Equal("123", result.ProviderUserId);
        Assert.Equal("test@gmail.com", result.Email);
        Assert.Equal("Test User", result.Name);
    }

    [Fact]
    public async Task VerifyTokenAsync_InvalidGoogleToken_ThrowsValidationException()
    {
        _mockGoogleValidator.Setup(x => x.ValidateAsync("invalid_token"))
            .ThrowsAsync(new InvalidJwtException("Invalid"));

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.VerifyTokenAsync("google", "invalid_token"));
    }

    [Fact]
    public async Task VerifyTokenAsync_GoogleValidationFails_ThrowsValidationException()
    {
        _mockGoogleValidator.Setup(x => x.ValidateAsync("error_token"))
            .ThrowsAsync(new Exception("Network error"));

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.VerifyTokenAsync("google", "error_token"));
    }

    [Fact]
    public async Task VerifyTokenAsync_UnsupportedProvider_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.VerifyTokenAsync("facebook", "token"));
    }
}
