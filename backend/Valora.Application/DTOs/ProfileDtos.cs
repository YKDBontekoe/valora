using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public record UserProfileDto(
    string Email,
    string? FirstName,
    string? LastName,
    int DefaultRadiusMeters,
    bool BiometricsEnabled
);

public record UpdateProfileDto
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }

    [Range(100, 5000)]
    public int DefaultRadiusMeters { get; init; }

    public bool BiometricsEnabled { get; init; }
}

public record ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$", ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; init; } = string.Empty;
}
