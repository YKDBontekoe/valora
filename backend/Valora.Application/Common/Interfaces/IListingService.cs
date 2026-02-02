using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IListingService
{
    Task ProcessListingsAsync(List<ScrapedListingDto> listings, bool shouldNotify, CancellationToken cancellationToken = default);
}
