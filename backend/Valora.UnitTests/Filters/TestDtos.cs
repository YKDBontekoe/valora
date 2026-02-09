using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public record TestValidationDto
{
    [Required]
    public string Name { get; init; } = string.Empty;
}

public record InvalidValidationDto
{
    [Required]
    public string? Name { get; init; }
}
