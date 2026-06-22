using System.Text.Json;
using System.Xml.Linq;
using Valora.Application.DTOs.Map;
using Valora.Application.DTOs;
using Valora.Application.Common.Interfaces;
using Valora.Infrastructure.Enrichment;

namespace Valora.Infrastructure.Utilities;

public static class CbsGeoParserUtility
{
    public static List<NeighborhoodGeometryDto> ParseNeighborhoodsFromJson(JsonElement features)
    {
        var results = new List<NeighborhoodGeometryDto>();
        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("properties", out var props)) continue;

            var code = props.GetStringSafe("buurtcode");
            if (string.IsNullOrEmpty(code)) continue;

            var name = props.GetStringSafe("buurtnaam") ?? "Unknown";

            // For latitude/longitude, we try to extract from geometry if possible, or just use 0,0 for now
            // Simplification: We don't parse complex geometries here, but in a real app we would use NetTopologySuite
            double lat = 0, lon = 0;

            results.Add(new NeighborhoodGeometryDto(code, name, "Buurt", lat, lon));
        }
        return results;
    }

    public static List<string> ParseMunicipalitiesFromXml(XDocument xdoc)
    {
        var wijkenbuurten = XNamespace.Get("http://wijkenbuurten.geonovum.nl");
        var elements = xdoc.Descendants(wijkenbuurten + "gemeentenaam");

        var results = new HashSet<string>();
        foreach (var element in elements)
        {
            if (!string.IsNullOrWhiteSpace(element.Value))
            {
                results.Add(element.Value);
            }
        }

        return results.OrderBy(x => x).ToList();
    }
}
