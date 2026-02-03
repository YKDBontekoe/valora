using System.Text.Json.Serialization;

namespace Valora.Infrastructure.Scraping.Models;

/// <summary>
/// Response from Funda's Topposition API.
/// This API returns sponsored/featured listings but works reliably without anti-bot measures.
/// </summary>
public record FundaApiResponse
{
    [JsonPropertyName("friendlyUrl")]
    public string? FriendlyUrl { get; init; }
    
    [JsonPropertyName("listings")]
    public List<FundaApiListing> Listings { get; init; } = [];
}

/// <summary>
/// Individual listing from the Funda API.
/// </summary>
public record FundaApiListing
{
    [JsonPropertyName("globalId")]
    public int GlobalId { get; init; }
    
    [JsonPropertyName("price")]
    public string? Price { get; init; }
    
    [JsonPropertyName("isSinglePrice")]
    public bool IsSinglePrice { get; init; }
    
    [JsonPropertyName("agentName")]
    public string? AgentName { get; init; }
    
    [JsonPropertyName("agentUrl")]
    public string? AgentUrl { get; init; }
    
    [JsonPropertyName("listingUrl")]
    public string? ListingUrl { get; init; }
    
    [JsonPropertyName("image")]
    public FundaApiImage? Image { get; init; }
    
    [JsonPropertyName("address")]
    public FundaApiAddress? Address { get; init; }
    
    [JsonPropertyName("labels")]
    public List<string> Labels { get; init; } = [];
    
    [JsonPropertyName("isProject")]
    public bool IsProject { get; init; }
}

/// <summary>
/// Image URLs in various sizes from the Funda API.
/// </summary>
public record FundaApiImage
{
    [JsonPropertyName("default")]
    public string? Default { get; init; }
    
    [JsonPropertyName("180")]
    public string? Size180 { get; init; }
    
    [JsonPropertyName("360")]
    public string? Size360 { get; init; }
    
    [JsonPropertyName("720")]
    public string? Size720 { get; init; }
}

/// <summary>
/// Address information from the Funda API.
/// </summary>
public record FundaApiAddress
{
    [JsonPropertyName("listingAddress")]
    public string? ListingAddress { get; init; }
    
    [JsonPropertyName("city")]
    public string? City { get; init; }
}

/// <summary>
/// Detailed summary from the detailed-summary API.
/// </summary>
public record FundaApiListingSummary
{
    [JsonPropertyName("identifiers")]
    public FundaApiIdentifiers? Identifiers { get; init; }
    
    [JsonPropertyName("price")]
    public FundaApiSummaryPrice? Price { get; init; }
    
    [JsonPropertyName("address")]
    public FundaApiSummaryAddress? Address { get; init; }
    
    [JsonPropertyName("fastView")]
    public FundaApiFastView? FastView { get; init; }
    
    [JsonPropertyName("brokers")]
    public List<FundaApiBroker> Brokers { get; init; } = [];
    
    // New fields from verified API response
    [JsonPropertyName("publicationDate")]
    public DateTime? PublicationDate { get; init; }
    
    [JsonPropertyName("isSoldOrRented")]
    public bool IsSoldOrRented { get; init; }
    
    [JsonPropertyName("labels")]
    public List<FundaApiLabel> Labels { get; init; } = [];
    
    [JsonPropertyName("tracking")]
    public FundaApiTracking? Tracking { get; init; }
}

public record FundaApiIdentifiers
{
    [JsonPropertyName("globalId")]
    public int GlobalId { get; init; }
    
    [JsonPropertyName("tinyId")]
    public string? TinyId { get; init; }
}

public record FundaApiSummaryPrice
{
    [JsonPropertyName("sellingPrice")]
    public string? SellingPrice { get; init; }
}

public record FundaApiSummaryAddress
{
    [JsonPropertyName("title")]
    public string? Street { get; init; }
    
    [JsonPropertyName("subTitle")]
    public string? PostalCodeCity { get; init; }
    
    [JsonPropertyName("city")]
    public string? City { get; init; }
    
    [JsonPropertyName("postCode")]
    public string? PostalCode { get; init; }
}

public record FundaApiFastView
{
    [JsonPropertyName("livingArea")]
    public string? LivingArea { get; init; }
    
    [JsonPropertyName("numberOfBedrooms")]
    public string? NumberOfBedrooms { get; init; }
    
    [JsonPropertyName("energyLabel")]
    public string? EnergyLabel { get; init; }
}

public record FundaApiBroker
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

public record FundaApiSimilarListingsResponse
{
    [JsonPropertyName("recentlyListed")]
    public List<FundaApiShortId> RecentlyListed { get; init; } = [];
    
    [JsonPropertyName("recentlySold")]
    public List<FundaApiShortId> RecentlySold { get; init; } = [];
}

public record FundaApiShortId
{
    [JsonPropertyName("globalId")]
    public int GlobalId { get; init; }
}

// ===== Extended Summary API Response Fields =====

public record FundaApiLabel
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }
    
    [JsonPropertyName("type")]
    public string? Type { get; init; }
}

public record FundaApiTracking
{
    [JsonPropertyName("values")]
    public FundaApiTrackingValues? Values { get; init; }
}

public record FundaApiTrackingValues
{
    [JsonPropertyName("listing_askingprice")]
    public int? AskingPrice { get; init; }
    
    [JsonPropertyName("listing_status")]
    public string? Status { get; init; }
    
    [JsonPropertyName("listing_type")]
    public string? Type { get; init; }
    
    [JsonPropertyName("listing_postal_code")]
    public string? PostalCode { get; init; }
}

// ===== Contact Details API Response =====, 
// Endpoint: https://contacts-bff.funda.io/api/v3/listings/{GlobalId}/contact-details?website=1
// Example: {"id":"7879910","listingId":7879910,"tinyId":"43224373",...
//          "contactBlockDetails":[{"id":12285,"logoUrl":"...","displayName":"Makelaarsland",
//          "phoneNumber":"088-2002000","isContactingEnabled":true,...}]}

public record FundaContactDetailsResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    
    [JsonPropertyName("listingId")]
    public int ListingId { get; init; }
    
    [JsonPropertyName("tinyId")]
    public string? TinyId { get; init; }
    
    [JsonPropertyName("listingStatus")]
    public string? ListingStatus { get; init; }
    
    [JsonPropertyName("contactBlockDetails")]
    public List<FundaContactBlockDetail> ContactDetails { get; init; } = [];
}

public record FundaContactBlockDetail
{
    [JsonPropertyName("id")]
    public int Id { get; init; }
    
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
    
    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; init; }
    
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; init; }
    
    [JsonPropertyName("associationCode")]
    public string? AssociationCode { get; init; }
    
    [JsonPropertyName("isContactingEnabled")]
    public bool IsContactingEnabled { get; init; }
}

// ===== Fiber Availability API Response =====
// Endpoint: https://kpnopticfiber.funda.io/api/v1/{FullPostalCode}
// Example: {"postalCode":"1096DE","message":"...has access to KPN optic fiber.","availability":true}

public record FundaFiberResponse
{
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; init; }
    
    [JsonPropertyName("availability")]
    public bool Availability { get; init; }
    
    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

