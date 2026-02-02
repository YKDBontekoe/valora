using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public record TriggerLimitedDto(
    [Required] string Region,
    [Range(1, 1000)] int Limit
);

public record SeedDto(
    [Required] string Region
);
