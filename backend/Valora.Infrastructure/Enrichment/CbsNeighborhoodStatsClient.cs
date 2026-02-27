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

        var url = BuildCbsQueryUrl(regionCode);

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CBS lookup failed for region {RegionCode} with status {StatusCode}", regionCode.Trim(), response.StatusCode);
            response.EnsureSuccessStatusCode();
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

            var residents = row.GetIntSafe("AantalInwoners_5");
            var density = row.GetIntSafe("Bevolkingsdichtheid_34");
            var woz = row.GetDoubleSafe("GemiddeldeWOZWaardeVanWoningen_36");
            var lowIncome = row.GetDoubleSafe("HuishoudensMetEenLaagInkomen_87");

            if (residents == null || density == null || woz == null || lowIncome == null)
            {
                _logger.LogDebug("CBS 85618NED partial data for {RegionCode}: Residents={R}, Density={D}, WOZ={W}, LowIncome={L}", 
                    regionCode.Trim(), residents, density, woz, lowIncome);
            }

            var result = new NeighborhoodStatsDto(
                RegionCode: row.GetStringSafe("WijkenEnBuurten")?.Trim() ?? regionCode.Trim(),
                RegionType: row.GetStringSafe("SoortRegio_2")?.Trim() ?? "Onbekend",
                Residents: residents,
                PopulationDensity: density,
                AverageWozValueKeur: woz,
                LowIncomeHouseholdsPercent: lowIncome,
                // New Fields Sourcing
                Men: row.GetIntSafe("Mannen_6"),
                Women: row.GetIntSafe("Vrouwen_7"),
                Age0To15: row.GetIntSafe("k_0Tot15Jaar_8"),
                Age15To25: row.GetIntSafe("k_15Tot25Jaar_9"),
                Age25To45: row.GetIntSafe("k_25Tot45Jaar_10"),
                Age45To65: row.GetIntSafe("k_45Tot65Jaar_11"),
                Age65Plus: row.GetIntSafe("k_65JaarOfOuder_12"),
                SingleHouseholds: row.GetIntSafe("Eenpersoonshuishoudens_30"),
                HouseholdsWithoutChildren: row.GetIntSafe("HuishoudensZonderKinderen_31"),
                HouseholdsWithChildren: row.GetIntSafe("HuishoudensMetKinderen_32"),
                AverageHouseholdSize: row.GetDoubleSafe("GemiddeldeHuishoudensgrootte_33"),
                Urbanity: row.GetStringSafe("MateVanStedelijkheid_125"),
                AverageIncomePerRecipient: row.GetDoubleSafe("GemiddeldInkomenPerInkomensontvanger_80"),
                AverageIncomePerInhabitant: row.GetDoubleSafe("GemiddeldInkomenPerInwoner_81"),
                EducationLow: row.GetIntSafe("BasisonderwijsVmboMbo1_70"),
                EducationMedium: row.GetIntSafe("HavoVwoMbo24_71"),
                EducationHigh: row.GetIntSafe("HboWo_72"),
                // Phase 2: Housing
                PercentageOwnerOccupied: row.GetIntSafe("Koopwoningen_41"),
                PercentageRental: row.GetIntSafe("HuurwoningenTotaal_42"),
                PercentageSocialHousing: row.GetIntSafe("InBezitWoningcorporatie_43"),
                PercentagePrivateRental: row.GetIntSafe("InBezitOverigeVerhuurders_44"),
                PercentagePre2000: row.GetIntSafe("BouwjaarVoor2000_46"),
                PercentagePost2000: row.GetIntSafe("BouwjaarVanaf2000_47"),
                PercentageMultiFamily: row.GetIntSafe("PercentageMeergezinswoning_38"),
                // Phase 2: Mobility
                CarsPerHousehold: row.GetDoubleSafe("PersonenautoSPerHuishouden_112"),
                CarDensity: row.GetIntSafe("PersonenautoSNaarOppervlakte_113"),
                TotalCars: row.GetIntSafe("PersonenautoSTotaal_109"),
                // Phase 2: Proximity
                DistanceToGp: row.GetDoubleSafe("AfstandTotHuisartsenpraktijk_115"),
                DistanceToSupermarket: row.GetDoubleSafe("AfstandTotGroteSupermarkt_116"),
                DistanceToDaycare: row.GetDoubleSafe("AfstandTotKinderdagverblijf_117"),
                DistanceToSchool: row.GetDoubleSafe("AfstandTotSchool_118"),
                SchoolsWithin3km: row.GetDoubleSafe("ScholenBinnen3Km_119"),
                RetrievedAtUtc: DateTimeOffset.UtcNow);

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(_options.CbsCacheMinutes));
            return result;
        }
    }

    private string BuildCbsQueryUrl(string regionCode)
    {
        var escapedCode = Uri.EscapeDataString(regionCode);
        return $"{_options.CbsBaseUrl.TrimEnd('/')}/85618NED/TypedDataSet?$filter=WijkenEnBuurten%20eq%20'{escapedCode}'&$top=1&$select=" +
               "WijkenEnBuurten,SoortRegio_2,AantalInwoners_5,Bevolkingsdichtheid_34,GemiddeldeWOZWaardeVanWoningen_36,HuishoudensMetEenLaagInkomen_87," +
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
}
