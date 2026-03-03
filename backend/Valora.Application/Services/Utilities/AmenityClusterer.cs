using Valora.Application.DTOs.Map;

namespace Valora.Application.Services.Utilities;

public static class AmenityClusterer
{
    public static List<MapAmenityClusterDto> ClusterAmenities(IEnumerable<MapAmenityDto> amenities, double cellSize)
    {
        return amenities
            .GroupBy(amenity => (
                Lat: Math.Floor(amenity.Latitude / cellSize),
                Lon: Math.Floor(amenity.Longitude / cellSize)
            ))
            .Select(groupedAmenities =>
            {
                var count = groupedAmenities.Count();
                var lat = (groupedAmenities.Key.Lat * cellSize) + (cellSize / 2);
                var lon = (groupedAmenities.Key.Lon * cellSize) + (cellSize / 2);

                var typeCounts = groupedAmenities.GroupBy(amenity => amenity.Type)
                    .ToDictionary(typeGroup => typeGroup.Key, typeGroup => typeGroup.Count());

                return new MapAmenityClusterDto(lat, lon, count, typeCounts);
            })
            .ToList();
    }
}
