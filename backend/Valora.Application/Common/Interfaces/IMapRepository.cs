using Valora.Application.DTOs.Map;

namespace Valora.Application.Common.Interfaces;

public interface IMapRepository
{
    Task<List<MapCityInsightDto>> GetCityInsightsAsync(CancellationToken cancellationToken = default);
    Task<List<ListingPriceData>> GetListingsPriceDataAsync(double minLat, double minLon, double maxLat, double maxLon, CancellationToken cancellationToken = default);
}
