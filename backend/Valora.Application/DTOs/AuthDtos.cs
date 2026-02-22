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
    [MinLength(12)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{12,}$", ErrorMessage = "Password must be at least 12 characters long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
    public string Password { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = string.Empty;
}

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    string Email,
    string UserId,
    IEnumerable<string> Roles
);

public record RefreshTokenRequestDto(
    [property: Required] [property: StringLength(500)] string RefreshToken
);

public record ExternalLoginRequestDto(
    [property: Required] [property: StringLength(50)] string Provider,
    [property: Required] [property: StringLength(5000)] string IdToken
);

public record GoogleTokenPayloadDto
{
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Picture { get; init; }
    public string Subject { get; init; } = string.Empty;
}
