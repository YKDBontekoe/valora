namespace Valora.Domain.Models;

public sealed record ResolvedLocationModel
{
    public string Query { get; init; } = null!;
    public string DisplayAddress { get; init; } = null!;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double? RdX { get; init; }
    public double? RdY { get; init; }
    public string? MunicipalityCode { get; init; }
    public string? MunicipalityName { get; init; }
    public string? DistrictCode { get; init; }
    public string? DistrictName { get; init; }
    public string? NeighborhoodCode { get; init; }
    public string? NeighborhoodName { get; init; }
    public string? PostalCode { get; init; }

    public ResolvedLocationModel() { }

    public ResolvedLocationModel(
        string query,
        string displayAddress,
        double latitude,
        double longitude,
        double? rdX,
        double? rdY,
        string? municipalityCode,
        string? municipalityName,
        string? districtCode,
        string? districtName,
        string? neighborhoodCode,
        string? neighborhoodName,
        string? postalCode)
    {
        Query = query;
        DisplayAddress = displayAddress;
        Latitude = latitude;
        Longitude = longitude;
        RdX = rdX;
        RdY = rdY;
        MunicipalityCode = municipalityCode;
        MunicipalityName = municipalityName;
        DistrictCode = districtCode;
        DistrictName = districtName;
        NeighborhoodCode = neighborhoodCode;
        NeighborhoodName = neighborhoodName;
        PostalCode = postalCode;
    }
}
