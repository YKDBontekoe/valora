using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IWozValuationService
{
    Task<WozValuationDto?> GetWozValuationAsync(string street, int number, string? suffix, string city, string? nummeraanduidingId = null, CancellationToken cancellationToken = default);
}
