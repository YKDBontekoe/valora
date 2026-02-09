using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public record LoginDto(
    [property: Required] [property: EmailAddress] string Email,
    [property: Required] string Password
);

public record RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = string.Empty;
}

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    string Email,
    string UserId
);

public record RefreshTokenRequestDto(
    string RefreshToken
);
