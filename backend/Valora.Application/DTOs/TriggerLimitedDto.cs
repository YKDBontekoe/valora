using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public record TriggerLimitedDto(
    [Required] [MaxLength(100)] string Region,
    [Range(1, 1000)] int Limit
);
