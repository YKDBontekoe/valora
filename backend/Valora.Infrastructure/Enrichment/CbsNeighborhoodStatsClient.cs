using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;

namespace Valora.Infrastructure.Enrichment;

public sealed class CbsNeighborhoodStatsClient : ICbsNeighborhoodStatsClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<CbsNeighborhoodStatsClient> _logger;

    public CbsNeighborhoodStatsClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<CbsNeighborhoodStatsClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<NeighborhoodStatsDto?> GetStatsAsync(ResolvedLocationDto location, CancellationToken cancellationToken = default)
    {
        var candidates = BuildCandidates(location).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        foreach (var code in candidates)
        {
            var result = await GetForCodeAsync(code, cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private async Task<NeighborhoodStatsDto?> GetForCodeAsync(string regionCode, CancellationToken cancellationToken)
    {
        var cacheKey = $"cbs:{regionCode}";
        if (_cache.TryGetValue(cacheKey, out NeighborhoodStatsDto? cached))
        {
            return cached;
        }

        var escapedCode = Uri.EscapeDataString(regionCode);
        var url =
            $"{_options.CbsBaseUrl.TrimEnd('/')}/85618NED/TypedDataSet?$filter=WijkenEnBuurten%20eq%20'{escapedCode}'&$top=1&$select=" +
            "WijkenEnBuurten,SoortRegio_2,AantalInwoners_5,Bevolkingsdichtheid_34,GemiddeldeWOZWaardeVanWoningen_36,HuishoudensMetEenLaagInkomen_73," +
            "Mannen_6,Vrouwen_7,k_0Tot15Jaar_8,k_15Tot25Jaar_9,k_25Tot45Jaar_10,k_45Tot65Jaar_11,k_65JaarOfOuder_12," +
            "Eenpersoonshuishoudens_30,HuishoudensZonderKinderen_31,HuishoudensMetKinderen_32,GemiddeldeHuishoudensgrootte_33," +
            "MateVanStedelijkheid_125," +
            "GemiddeldInkomenPerInkomensontvanger_80,GemiddeldInkomenPerInwoner_81," +
            "BasisonderwijsVmboMbo1_70,HavoVwoMbo24_71,HboWo_72," +
            // Phase 2: Housing
            "Koopwoningen_41,HuurwoningenTotaal_42,InBezitWoningcorporatie_43,InBezitOverigeVerhuurders_44," +
            "BouwjaarVoor2000_46,BouwjaarVanaf2000_47,PercentageMeergezinswoning_38," +
            // Phase 2: Mobility
            "PersonenautoSPerHuishouden_112,PersonenautoSNaarOppervlakte_113,PersonenautoSTotaal_109," +
            // Phase 2: Proximity
            "AfstandTotHuisartsenpraktijk_115,AfstandTotGroteSupermarkt_116,AfstandTotKinderdagverblijf_117,AfstandTotSchool_118,ScholenBinnen3Km_119";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CBS lookup failed for region {RegionCode} with status {StatusCode}", regionCode.Trim(), response.StatusCode);
            return null;
        }

        JsonDocument document;
        try
        {
            using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "CBS lookup returned invalid JSON for region {RegionCode}", regionCode.Trim());
            return null;
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("value", out var values) ||
                values.ValueKind != JsonValueKind.Array ||
                values.GetArrayLength() == 0)
            {
                _cache.Set(cacheKey, null as NeighborhoodStatsDto, TimeSpan.FromMinutes(_options.CbsCacheMinutes));
                return null;
            }

            var row = values[0];

            var residents = GetInt(row, "AantalInwoners_5");
            var density = GetInt(row, "Bevolkingsdichtheid_34");
            var woz = GetDouble(row, "GemiddeldeWOZWaardeVanWoningen_36");
            var lowIncome = GetDouble(row, "HuishoudensMetEenLaagInkomen_73");

            if (residents == null || density == null || woz == null || lowIncome == null)
            {
                _logger.LogDebug("CBS 85618NED partial data for {RegionCode}: Residents={R}, Density={D}, WOZ={W}, LowIncome={L}", 
                    regionCode.Trim(), residents, density, woz, lowIncome);
            }

            var result = new NeighborhoodStatsDto(
                RegionCode: GetString(row, "WijkenEnBuurten")?.Trim() ?? regionCode.Trim(),
                RegionType: GetString(row, "SoortRegio_2")?.Trim() ?? "Onbekend",
                Residents: residents,
                PopulationDensity: density,
                AverageWozValueKeur: woz,
                LowIncomeHouseholdsPercent: lowIncome,
                // New Fields Sourcing
                Men: GetInt(row, "Mannen_6"),
                Women: GetInt(row, "Vrouwen_7"),
                Age0To15: GetInt(row, "k_0Tot15Jaar_8"),
                Age15To25: GetInt(row, "k_15Tot25Jaar_9"),
                Age25To45: GetInt(row, "k_25Tot45Jaar_10"),
                Age45To65: GetInt(row, "k_45Tot65Jaar_11"),
                Age65Plus: GetInt(row, "k_65JaarOfOuder_12"),
                SingleHouseholds: GetInt(row, "Eenpersoonshuishoudens_30"),
                HouseholdsWithoutChildren: GetInt(row, "HuishoudensZonderKinderen_31"),
                HouseholdsWithChildren: GetInt(row, "HuishoudensMetKinderen_32"),
                AverageHouseholdSize: GetDouble(row, "GemiddeldeHuishoudensgrootte_33"),
                Urbanity: GetString(row, "MateVanStedelijkheid_125"),
                AverageIncomePerRecipient: GetDouble(row, "GemiddeldInkomenPerInkomensontvanger_80"),
                AverageIncomePerInhabitant: GetDouble(row, "GemiddeldInkomenPerInwoner_81"),
                EducationLow: GetInt(row, "BasisonderwijsVmboMbo1_70"),
                EducationMedium: GetInt(row, "HavoVwoMbo24_71"),
                EducationHigh: GetInt(row, "HboWo_72"),
                // Phase 2: Housing
                PercentageOwnerOccupied: GetInt(row, "Koopwoningen_41"),
                PercentageRental: GetInt(row, "HuurwoningenTotaal_42"),
                PercentageSocialHousing: GetInt(row, "InBezitWoningcorporatie_43"),
                PercentagePrivateRental: GetInt(row, "InBezitOverigeVerhuurders_44"),
                PercentagePre2000: GetInt(row, "BouwjaarVoor2000_46"),
                PercentagePost2000: GetInt(row, "BouwjaarVanaf2000_47"),
                PercentageMultiFamily: GetInt(row, "PercentageMeergezinswoning_38"),
                // Phase 2: Mobility
                CarsPerHousehold: GetDouble(row, "PersonenautoSPerHuishouden_112"),
                CarDensity: GetInt(row, "PersonenautoSNaarOppervlakte_113"),
                TotalCars: GetInt(row, "PersonenautoSTotaal_109"),
                // Phase 2: Proximity
                DistanceToGp: GetDouble(row, "AfstandTotHuisartsenpraktijk_115"),
                DistanceToSupermarket: GetDouble(row, "AfstandTotGroteSupermarkt_116"),
                DistanceToDaycare: GetDouble(row, "AfstandTotKinderdagverblijf_117"),
                DistanceToSchool: GetDouble(row, "AfstandTotSchool_118"),
                SchoolsWithin3km: GetDouble(row, "ScholenBinnen3Km_119"),
                RetrievedAtUtc: DateTimeOffset.UtcNow);

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CbsCacheMinutes));
            return result;
        }
    }

    private static IEnumerable<string> BuildCandidates(ResolvedLocationDto location)
    {
        if (!string.IsNullOrWhiteSpace(location.NeighborhoodCode))
        {
            yield return location.NeighborhoodCode.Trim().PadRight(10);
        }

        if (!string.IsNullOrWhiteSpace(location.DistrictCode))
        {
            yield return location.DistrictCode.Trim().PadRight(10);
        }

        if (!string.IsNullOrWhiteSpace(location.MunicipalityCode))
        {
            yield return location.MunicipalityCode.Trim().PadRight(10);
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
        {
            return value;
        }

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value))
        {
            return value;
        }

        if (property.ValueKind == JsonValueKind.String &&
            double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
