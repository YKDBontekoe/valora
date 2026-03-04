using Valora.Application.DTOs.Map;

namespace Valora.Infrastructure.Services.AppServices.Utilities;

public static class AmenityClusterer
{
    public static List<MapAmenityClusterDto> ClusterAmenities(IEnumerable<MapAmenityDto> amenities, double cellSize)
    {
        if (amenities == null)
        {
            throw new ArgumentNullException(nameof(amenities));
        }

        if (cellSize <= 0 || double.IsNaN(cellSize) || double.IsInfinity(cellSize))
        {
            throw new ArgumentException("Cell size must be a positive, finite number.", nameof(cellSize));
        }

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
