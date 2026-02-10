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

    public AuthService(IIdentityService identityService, ITokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<Result> RegisterAsync(RegisterDto request)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return Result.Failure(new[] { ErrorMessages.PasswordsDoNotMatch });
        }

        var (result, userId) = await _identityService.CreateUserAsync(request.Email, request.Password);

        return result;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto request)
    {
        var user = await ValidateUserAsync(request.Email, request.Password);
        if (user == null)
        {
            return null;
        }

        var token = await _tokenService.GenerateTokenAsync(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);
        await _tokenService.SaveRefreshTokenAsync(refreshToken);

        return new AuthResponseDto(
            token,
            refreshToken.RawToken,
            user.Email!,
            user.Id
        );
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
    {
        var existingToken = await _tokenService.GetActiveRefreshTokenAsync(refreshToken);

        if (existingToken == null || existingToken.User == null)
        {
            return null;
        }

        var newRefreshToken = await RotateRefreshTokenAsync(refreshToken, existingToken.UserId);
        var newAccessToken = await _tokenService.GenerateTokenAsync(existingToken.User);

        return new AuthResponseDto(
            newAccessToken,
            newRefreshToken.RawToken,
            existingToken.User.Email!,
            existingToken.User.Id
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
        if (!isValidPassword)
        {
            return null;
        }

        return user;
    }

    private async Task<RefreshToken> RotateRefreshTokenAsync(string oldToken, string userId)
    {
        await _tokenService.RevokeRefreshTokenAsync(oldToken);
        var newRefreshToken = _tokenService.GenerateRefreshToken(userId);
        await _tokenService.SaveRefreshTokenAsync(newRefreshToken);
        return newRefreshToken;
    }
}
