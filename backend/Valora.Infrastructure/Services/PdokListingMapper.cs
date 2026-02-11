using System.Globalization;
using System.Text.Json;
using Valora.Application.DTOs;
using Valora.Domain.Models;

namespace Valora.Infrastructure.Services;

public static class PdokListingMapper
{
    public static ListingDto MapFromPdok(
        JsonElement doc,
        string pdokId,
        double? compositeScore,
        double? safetyScore,
        ContextReportModel? contextReport,
        int? wozValue,
        DateTime? wozReferenceDate,
        string? wozValueSource)
    {
        // Extract Basic Info
        var address = GetString(doc, "weergavenaam");
        var city = GetString(doc, "woonplaatsnaam");
        var postcode = GetString(doc, "postcode");
        var lat = TryParseCoordinate(GetString(doc, "centroide_ll"), true);
        var lon = TryParseCoordinate(GetString(doc, "centroide_ll"), false);

        // Extract Building Info
        int? yearBuilt = TryParseInt(GetString(doc, "bouwjaar"));
        int? area = TryParseInt(GetString(doc, "oppervlakte"));
        var usage = GetString(doc, "gebruiksdoelverblijfsobject");

        return new ListingDto(
            Id: GenerateStableId(pdokId),
            FundaId: pdokId,
            Address: address ?? "Unknown Address",
            City: city,
            PostalCode: postcode,
            Price: null,
            Bedrooms: null,
            Bathrooms: null,
            LivingAreaM2: area,
            PlotAreaM2: null,
            PropertyType: usage,
            Status: "Unknown",
            Url: null,
            ImageUrl: null,
            ListedDate: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow,
            Description: BuildDescription(yearBuilt, usage),
            EnergyLabel: null,
            YearBuilt: yearBuilt,
            ImageUrls: new List<string>(),
            OwnershipType: null,
            CadastralDesignation: null,
            VVEContribution: null,
            HeatingType: null,
            InsulationType: null,
            GardenOrientation: null,
            HasGarage: false,
            ParkingType: null,
            AgentName: null,
            VolumeM3: null,
            BalconyM2: null,
            GardenM2: null,
            ExternalStorageM2: null,
            Features: new Dictionary<string, string>(),
            Latitude: lat,
            Longitude: lon,
            VideoUrl: null,
            VirtualTourUrl: null,
            FloorPlanUrls: new List<string>(),
            BrochureUrl: null,
            RoofType: null,
            NumberOfFloors: null,
            ConstructionPeriod: null,
            CVBoilerBrand: null,
            CVBoilerYear: null,
            BrokerPhone: null,
            BrokerLogoUrl: null,
            FiberAvailable: null,
            PublicationDate: null,
            IsSoldOrRented: false,
            Labels: new List<string>(),
            ContextCompositeScore: compositeScore,
            ContextSafetyScore: safetyScore,
            ContextReport: contextReport,
            WozValue: wozValue,
            WozReferenceDate: wozReferenceDate,
            WozValueSource: wozValueSource
        );
    }

    public static string? GetString(JsonElement doc, string key)
    {
        if (doc.TryGetProperty(key, out var prop))
        {
            return prop.ToString();
        }
        return null;
    }

    private static string? BuildDescription(int? yearBuilt, string? usage)
    {
        var parts = new List<string>();
        if (yearBuilt.HasValue)
        {
            parts.Add($"Built in {yearBuilt}");
        }
        if (!string.IsNullOrWhiteSpace(usage))
        {
            parts.Add($"Usage: {usage}");
        }

        if (parts.Count == 0) return null;
        return string.Join(". ", parts) + ".";
    }

    private static int? TryParseInt(string? value)
    {
        if (int.TryParse(value, out var result)) return result;
        return null;
    }

    private static double? TryParseCoordinate(string? wkt, bool isLat)
    {
        if (string.IsNullOrEmpty(wkt) || !wkt.StartsWith("POINT(") || !wkt.EndsWith(")")) return null;

        var content = wkt.Substring(6, wkt.Length - 7);
        var parts = content.Split(' ');
        if (parts.Length != 2) return null;

        if (double.TryParse(parts[isLat ? 1 : 0], NumberStyles.Any, CultureInfo.InvariantCulture, out var coord))
        {
            return coord;
        }
        return null;
    }

    private static Guid GenerateStableId(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
