using Valora.Application.DTOs.Map;

namespace Valora.Application.DTOs.Ai;

public class MapQueryResponse
{
    public string Explanation { get; set; } = string.Empty;
    public List<string> FollowUpQuestions { get; set; } = new();

    // Map Data Payload
    public List<MapOverlayDto>? Overlays { get; set; }
    public List<MapAmenityDto>? Amenities { get; set; }
    public List<MapCityInsightDto>? CityInsights { get; set; }

    // Viewport hints
    public double? SuggestCenterLat { get; set; }
    public double? SuggestCenterLon { get; set; }
    public double? SuggestZoom { get; set; }
}
