using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public record SeedDto(
    [Required] [MaxLength(100)] string Region
);
