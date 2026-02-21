using Valora.Application.DTOs.Map;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IMapRepository
{
    Task<List<MapCityInsightDto>> GetCityInsightsAsync(CancellationToken cancellationToken = default);
    Task<List<ListingPriceData>> GetListingsPriceDataAsync(double minLat, double minLon, double maxLat, double maxLon, CancellationToken cancellationToken = default);
    Task<Listing?> GetListingByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<MapPropertyDto>> GetMapPropertiesAsync(double minLat, double minLon, double maxLat, double maxLon, CancellationToken cancellationToken = default);
}
