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
    private readonly TimeProvider _timeProvider;

    public AuthService(IIdentityService identityService, ITokenService tokenService, ILogger<AuthService> logger, IExternalAuthService externalAuthService, TimeProvider timeProvider)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _logger = logger;
        _externalAuthService = externalAuthService;
        _timeProvider = timeProvider;
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
        var user = await VerifyCredentialsAsync(loginDto.Email, loginDto.Password);
        if (user == null)
        {
            _logger.LogWarning("Login failed: Invalid credentials for email {EmailHash}", PrivacyUtils.HashEmail(loginDto.Email));
            return null;
        }

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        return await GenerateAuthResponseAsync(user);
    }

    private async Task<ApplicationUser?> VerifyCredentialsAsync(string email, string password)
    {
        // Check password first to prevent timing attacks.
        // IdentityService.CheckPasswordAsync should handle non-existent users securely (e.g. dummy hash).
        var isValidPassword = await _identityService.CheckPasswordAsync(email, password);
        if (!isValidPassword)
        {
            return null;
        }

        return await _identityService.GetUserByEmailAsync(email);
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var storedToken = await ValidateRefreshTokenAsync(refreshToken);
        if (storedToken?.User == null)
        {
            return null;
        }

        // Generate new access token and lookup roles first
        // If above operations succeed, then rotate refresh token
        var authResponse = await GenerateAuthResponseAsync(storedToken.User);

        // Only revoke old token AFTER the new one is successfully generated and persisted
        await _tokenService.RevokeRefreshTokenAsync(refreshToken);

        return authResponse;
    }

    private async Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _tokenService.GetRefreshTokenAsync(refreshToken);

        if (storedToken == null)
        {
            return null;
        }

        // Reuse Detection: If token is already revoked, it might be a theft attempt.
        if (storedToken.Revoked != null)
        {
            _logger.LogWarning("Security Alert: Reuse of revoked refresh token detected for user {UserId}. Revoking all sessions.", storedToken.UserId);
            await _tokenService.RevokeAllRefreshTokensAsync(storedToken.UserId);
            return null;
        }

        // Check if expired
        if (storedToken.Expires <= _timeProvider.GetUtcNow().UtcDateTime)
        {
            return null;
        }

        return storedToken;
    }

    public async Task<AuthResponseDto?> ExternalLoginAsync(ExternalLoginRequestDto request)
    {
        var externalUser = await _externalAuthService.VerifyTokenAsync(request.Provider, request.IdToken);

        var user = await _identityService.GetUserByEmailAsync(externalUser.Email);

        if (user == null)
        {
            var password = PasswordGenerator.Generate();
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

        var refreshToken = RefreshToken.Create(user.Id, _timeProvider);
        await _tokenService.SaveRefreshTokenAsync(refreshToken);

        return new AuthResponseDto(
            token,
            refreshToken.RawToken,
            user.Email!,
            user.Id,
            roles
        );
    }
}
