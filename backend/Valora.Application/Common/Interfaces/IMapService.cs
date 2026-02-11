using Valora.Application.DTOs.Map;

namespace Valora.Application.Common.Interfaces;

public interface IMapService
{
    Task<List<MapCityInsightDto>> GetCityInsightsAsync(CancellationToken cancellationToken = default);
}
