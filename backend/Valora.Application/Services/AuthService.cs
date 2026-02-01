using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;

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

    public async Task<Result> RegisterAsync(RegisterDto registerDto)
    {
        if (registerDto.Password != registerDto.ConfirmPassword)
        {
            return Result.Failure(new[] { "Passwords do not match" });
        }

        var (result, userId) = await _identityService.CreateUserAsync(registerDto.Email, registerDto.Password);

        return result;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await _identityService.GetUserByEmailAsync(loginDto.Email);
        if (user == null)
        {
            return null;
        }

        var isValidPassword = await _identityService.CheckPasswordAsync(loginDto.Email, loginDto.Password);
        if (!isValidPassword)
        {
            return null;
        }

        var token = _tokenService.GenerateToken(user);

        return new AuthResponseDto(
            token,
            user.Email!,
            user.Id
        );
    }
}
