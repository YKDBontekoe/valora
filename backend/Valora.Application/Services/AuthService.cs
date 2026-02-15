using Microsoft.Extensions.Logging;
using Valora.Application.Common.Constants;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.Common.Utilities;
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
            _logger.LogWarning("Registration failed: Password confirmation mismatch for email {EmailHash}", PrivacyUtils.HashEmail(registerDto.Email));
            return Result.Failure(new[] { ErrorMessages.PasswordsDoNotMatch });
        }

        var (result, userId) = await _identityService.CreateUserAsync(registerDto.Email, registerDto.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User registered successfully: {UserId}", userId);
        }
        else
        {
            _logger.LogWarning("Registration failed for email {EmailHash}: {Errors}", PrivacyUtils.HashEmail(registerDto.Email), string.Join(", ", result.Errors));
        }

        return result;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await ValidateUserAsync(loginDto.Email, loginDto.Password);
        if (user == null)
        {
            _logger.LogWarning("Login failed: Invalid credentials for email {EmailHash}", PrivacyUtils.HashEmail(loginDto.Email));
            return null;
        }

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        var token = await _tokenService.CreateJwtTokenAsync(user);
        var roles = await _identityService.GetUserRolesAsync(user);

        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);
        await _tokenService.SaveRefreshTokenAsync(refreshToken);

        return new AuthResponseDto(
            token,
            refreshToken.RawToken,
            user.Email!,
            user.Id,
            roles
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

    public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
    {
        var existingToken = await _tokenService.GetActiveRefreshTokenAsync(refreshToken);

        if (existingToken == null || existingToken.User == null)
        {
            return null;
        }

        // Generate new access token and lookup roles first
        var newAccessToken = await _tokenService.CreateJwtTokenAsync(existingToken.User);
        var roles = await _identityService.GetUserRolesAsync(existingToken.User);

        // If above operations succeed, then rotate refresh token
        await _tokenService.RevokeRefreshTokenAsync(refreshToken); // Revoke old token
        var newRefreshToken = _tokenService.GenerateRefreshToken(existingToken.UserId);
        await _tokenService.SaveRefreshTokenAsync(newRefreshToken); // Save new token

        return new AuthResponseDto(
            newAccessToken,
            newRefreshToken.RawToken,
            existingToken.User.Email!,
            existingToken.User.Id,
            roles
        );
    }
}
