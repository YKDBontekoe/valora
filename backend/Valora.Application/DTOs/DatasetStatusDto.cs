namespace Valora.Application.DTOs;

public record DatasetStatusDto(
    string City,
    int NeighborhoodCount,
    DateTime? LastUpdated
);
