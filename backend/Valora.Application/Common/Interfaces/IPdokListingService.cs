
using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IPdokListingService
{
    Task<ListingDto?> GetListingDetailsAsync(string pdokId, CancellationToken cancellationToken = default);
}
