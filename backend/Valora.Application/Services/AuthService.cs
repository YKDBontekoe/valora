using Microsoft.Extensions.Logging;
using Valora.Application.Common.Constants;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class AuthService : IAuthService
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IIdentityService identityService, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result> RegisterAsync(RegisterDto registerDto)
    {
        if (registerDto.Password != registerDto.ConfirmPassword)
        {
            _logger.LogWarning("Registration failed: Password confirmation mismatch for email {Email}", MaskEmail(registerDto.Email));
            return Result.Failure(new[] { ErrorMessages.PasswordsDoNotMatch });
        }

        var (result, userId) = await _identityService.CreateUserAsync(registerDto.Email, registerDto.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User registered successfully: {UserId}", userId);
        }
        else
        {
            _logger.LogWarning("Registration failed for email {Email}: {Errors}", MaskEmail(registerDto.Email), string.Join(", ", result.Errors));
        }

        return result;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await ValidateUserAsync(loginDto.Email, loginDto.Password);
        if (user == null)
        {
            _logger.LogWarning("Login failed: Invalid credentials for email {Email}", MaskEmail(loginDto.Email));
            return null;
        }

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        var token = await _tokenService.CreateJwtTokenAsync(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);
        await _tokenService.SaveRefreshTokenAsync(refreshToken);

        return new AuthResponseDto(
            token,
            refreshToken.RawToken,
            user.Email!,
            user.Id
        );
    }

    private async Task<ApplicationUser?> ValidateUserAsync(string email, string password)
    {
        var user = await _identityService.GetUserByEmailAsync(email);
        if (user == null)
        {
            return null;
        }

        var isValidPassword = await _identityService.CheckPasswordAsync(email, password);
        return isValidPassword ? user : null;
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "unknown";
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return "***" + (atIndex >= 0 ? email[atIndex..] : "");

        // Return first character + *** + domain
        return email[0] + "***" + email[atIndex..];
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
    {
        var existingToken = await _tokenService.GetActiveRefreshTokenAsync(refreshToken);

        if (existingToken == null || existingToken.User == null)
        {
            return null;
        }

        // Rotate Refresh Token
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);
        var newRefreshToken = _tokenService.GenerateRefreshToken(existingToken.UserId);
        await _tokenService.SaveRefreshTokenAsync(newRefreshToken);

        var newAccessToken = await _tokenService.CreateJwtTokenAsync(existingToken.User);

        return new AuthResponseDto(
            newAccessToken,
            newRefreshToken.RawToken,
            existingToken.User.Email!,
            existingToken.User.Id
        );
    }
}
