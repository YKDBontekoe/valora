namespace Valora.Application.DTOs;

public record WozValuationDto(
    int Value,
    DateTime ReferenceDate,
    string Source = "WOZ-waardeloket"
);
