using Valora.Application.Common.Exceptions;
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
    private readonly IExternalAuthService _externalAuthService;

    public AuthService(IIdentityService identityService, ITokenService tokenService, ILogger<AuthService> logger, IExternalAuthService externalAuthService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _logger = logger;
        _externalAuthService = externalAuthService;
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

        return await GenerateAuthResponseAsync(user);
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
        // If above operations succeed, then rotate refresh token
        var authResponse = await GenerateAuthResponseAsync(existingToken.User);

        // Only revoke old token AFTER the new one is successfully generated and persisted
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);

        return authResponse;
    }

    public async Task<AuthResponseDto?> ExternalLoginAsync(ExternalLoginRequestDto request)
    {
        var externalUser = await _externalAuthService.VerifyTokenAsync(request.Provider, request.IdToken);

        var user = await _identityService.GetUserByEmailAsync(externalUser.Email);

        if (user == null)
        {
            var password = GenerateRandomPassword();
            var (result, userId) = await _identityService.CreateUserAsync(externalUser.Email, password);
            if (!result.Succeeded)
            {
                 _logger.LogWarning("Auto-registration failed for external user {EmailHash}: {Errors}", PrivacyUtils.HashEmail(externalUser.Email), string.Join(", ", result.Errors));
                 throw new ValidationException(result.Errors);
            }
            user = await _identityService.GetUserByEmailAsync(externalUser.Email);
        }

        _logger.LogInformation("User logged in via external provider {Provider}: {UserId}", request.Provider, user!.Id);

        return await GenerateAuthResponseAsync(user);
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user)
    {
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

    private static string GenerateRandomPassword()
    {
        // Generates a password satisfying the requirement:
        // 8+ chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char
        return Guid.NewGuid().ToString("N") + "A1!";
    }
}
