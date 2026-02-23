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
        var existingToken = await _tokenService.GetRefreshTokenAsync(refreshToken);

        if (existingToken == null)
        {
            return null;
        }

        // Reuse Detection: If token is already revoked, it might be a theft attempt.
        if (existingToken.Revoked != null)
        {
            _logger.LogWarning("Security Alert: Reuse of revoked refresh token detected for user {UserId}. Revoking all sessions.", existingToken.UserId);
            await _tokenService.RevokeAllRefreshTokensAsync(existingToken.UserId);
            return null;
        }

        // Check if expired
        if (existingToken.Expires <= _timeProvider.GetUtcNow().UtcDateTime)
        {
            return null;
        }

        if (existingToken.User == null)
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
        // Generates a cryptographically strong password
        // Requirements: 12 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char
        const string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowers = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string specials = "!@#$%^&*()";
        const string allChars = uppers + lowers + digits + specials;

        var chars = new char[16];
        var bytes = new byte[chars.Length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);

        // Ensure at least one of each required type
        chars[0] = uppers[bytes[0] % uppers.Length];
        chars[1] = lowers[bytes[1] % lowers.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = specials[bytes[3] % specials.Length];

        // Fill the rest randomly
        for (int i = 4; i < chars.Length; i++)
        {
            chars[i] = allChars[bytes[i] % allChars.Length];
        }

        // Shuffle the result to avoid predictable pattern at start
        // Fisher-Yates shuffle
        for (int i = chars.Length - 1; i > 0; i--)
        {
            // We can reuse bytes array for shuffle indices if we regenerate or just pick from it
            // Let's just generate a new random index
            var randByte = new byte[1];
            System.Security.Cryptography.RandomNumberGenerator.Fill(randByte);
            int j = randByte[0] % (i + 1);

            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
