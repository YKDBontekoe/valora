using Valora.Application.Common.Models;
using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
    Task<AuthResponseDto?> ExternalLoginAsync(ExternalLoginRequestDto request);
}
