using System.Text.Json.Serialization;

namespace Valora.Infrastructure.Scraping.Models;

/// <summary>
/// Root object for the interesting parts of the Nuxt state we extract.
/// We usually map this from the specific "cachedListingData" part of the state.
/// </summary>
public record FundaNuxtListingData
{
    [JsonPropertyName("features")]
    public FundaNuxtFeatures? Features { get; init; }

    [JsonPropertyName("media")]
    public FundaNuxtMedia? Media { get; init; }

    [JsonPropertyName("description")]
    public FundaNuxtDescription? Description { get; init; }
    
    [JsonPropertyName("objectType")]
    public FundaNuxtObjectType? ObjectType { get; init; }
    
    // Phase 4: Additional Data
    [JsonPropertyName("coordinates")]
    public FundaNuxtCoordinates? Coordinates { get; init; }
    
    [JsonPropertyName("localInsights")]
    public FundaNuxtLocalInsights? LocalInsights { get; init; }
    
    [JsonPropertyName("objectInsights")]
    public FundaNuxtObjectInsights? ObjectInsights { get; init; }
    
    [JsonPropertyName("videos")]
    public List<FundaNuxtVideo>? Videos { get; init; }
    
    [JsonPropertyName("photos360")]
    public List<FundaNuxtPhoto360>? Photos360 { get; init; }
    
    [JsonPropertyName("floorPlan")]
    public List<FundaNuxtFloorPlan>? FloorPlans { get; init; }
    
    [JsonPropertyName("brochure")]
    public string? BrochureUrl { get; init; }
    
    [JsonPropertyName("openHouseDates")]
    public List<FundaNuxtOpenHouse>? OpenHouseDates { get; init; }
}

public record FundaNuxtFeatures
{
    [JsonPropertyName("indeling")]
    public FundaNuxtFeatureGroup? Indeling { get; init; }

    [JsonPropertyName("afmetingen")]
    public FundaNuxtFeatureGroup? Afmetingen { get; init; }

    [JsonPropertyName("energie")]
    public FundaNuxtFeatureGroup? Energie { get; init; }
    
    [JsonPropertyName("bouw")]
    public FundaNuxtFeatureGroup? Bouw { get; init; }
}

public record FundaNuxtFeatureGroup
{
    [JsonPropertyName("Title")]
    public string? Title { get; init; }

    [JsonPropertyName("KenmerkenList")]
    public List<FundaNuxtFeatureItem> KenmerkenList { get; init; } = [];
}

public record FundaNuxtFeatureItem
{
    [JsonPropertyName("Label")]
    public string? Label { get; init; }
    
    [JsonPropertyName("Value")]
    public string? Value { get; init; }
    
    [JsonPropertyName("KenmerkenList")]
    public List<FundaNuxtFeatureItem> KenmerkenList { get; init; } = [];
}

public record FundaNuxtMedia
{
    [JsonPropertyName("items")]
    public List<FundaNuxtMediaItem> Items { get; init; } = [];
}

public record FundaNuxtMediaItem
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    
    [JsonPropertyName("type")]
    public int Type { get; init; } // 1 is usually photo
}

public record FundaNuxtDescription
{
    [JsonPropertyName("content")]
    public string? Content { get; init; }
}

public record FundaNuxtObjectType
{
    [JsonPropertyName("propertyspecification")]
    public FundaNuxtPropertySpecification? PropertySpecification { get; init; }
}

public record FundaNuxtPropertySpecification
{
    [JsonPropertyName("selectedArea")]
    public int SelectedArea { get; init; } // usage area in m2
    
    [JsonPropertyName("selectedPlotArea")]
    public int SelectedPlotArea { get; init; } // plot area in m2
}

// Phase 4: Extended Models

public record FundaNuxtCoordinates
{
    [JsonPropertyName("lat")]
    public double? Lat { get; init; }
    
    [JsonPropertyName("lng")]
    public double? Lng { get; init; }
}

public record FundaNuxtLocalInsights
{
    [JsonPropertyName("inhabitants")]
    public int? Inhabitants { get; init; }
    
    [JsonPropertyName("avgPricePerM2")]
    public decimal? AvgPricePerM2 { get; init; }
}

public record FundaNuxtObjectInsights
{
    [JsonPropertyName("views")]
    public int? Views { get; init; }
    
    [JsonPropertyName("saves")]
    public int? Saves { get; init; }
}

public record FundaNuxtVideo
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }
    
    [JsonPropertyName("thumbnailUrl")]
    public string? ThumbnailUrl { get; init; }
}

public record FundaNuxtFloorPlan
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    
    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

public record FundaNuxtPhoto360
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }
    
    [JsonPropertyName("scene")]
    public string? Scene { get; init; }
}

public record FundaNuxtOpenHouse
{
    [JsonPropertyName("date")]
    public DateTime? Date { get; init; }
}

