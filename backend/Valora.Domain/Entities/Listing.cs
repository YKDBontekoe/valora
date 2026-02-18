using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class Listing : BaseEntity
{
    public required string FundaId { get; set; }
    public required string Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Price { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? LivingAreaM2 { get; set; }
    public int? PlotAreaM2 { get; set; }
    public string? PropertyType { get; set; }
    public string? Status { get; set; }
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? ListedDate { get; set; }
    
    public ICollection<PriceHistory> PriceHistory { get; set; } = [];
    
    // Rich Data Fields
    public string? Description { get; set; }
    public string? EnergyLabel { get; set; }
    public int? YearBuilt { get; set; }
    public List<string> ImageUrls { get; set; } = [];
    
    // Phase 2: Expanded Data Points
    public string? OwnershipType { get; set; } // e.g., "Volle eigendom", "Erfpacht"
    public string? CadastralDesignation { get; set; } // e.g., "AMSTERDAM A 1234"
    public decimal? VVEContribution { get; set; } // Monthly contribution
    public string? HeatingType { get; set; } // e.g., "CV-ketel"
    public string? InsulationType { get; set; } // e.g., "Dubbel glas"
    public string? GardenOrientation { get; set; } // e.g., "Zuid-west"
    public bool HasGarage { get; set; }
    public string? ParkingType { get; set; } // e.g., "Betaald parkeren"

    // Phase 3: Total Data Capture (Catch-All)
    public string? AgentName { get; set; } // Often useful to know who is selling
    public int? VolumeM3 { get; set; } // Inhoud
    public int? BalconyM2 { get; set; } // Gebouwgebonden buitenruimte
    public int? GardenM2 { get; set; } // Tuin area (often implied)
    public int? ExternalStorageM2 { get; set; } // Externe bergruimte
    
    // JSONB column to store ALL raw features found in the scrape
    public Dictionary<String, String> Features { get; set; } = [];

    // Phase 4: Complete Data Capture
    // Geo
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // Media
    public string? VideoUrl { get; set; } // Cloudflare Stream or similar
    public string? VirtualTourUrl { get; set; } // 360° photo tour
    public List<string> FloorPlanUrls { get; set; } = [];
    public string? BrochureUrl { get; set; } // PDF download
    
    // Insights & Engagement
    public int? ViewCount { get; set; }
    public int? SaveCount { get; set; }
    public int? NeighborhoodPopulation { get; set; }
    public decimal? NeighborhoodAvgPriceM2 { get; set; }
    
    // Events
    public List<DateTime> OpenHouseDates { get; set; } = [];
    
    // Construction Details
    public string? RoofType { get; set; } // e.g., "Zadeldak"
    public int? NumberOfFloors { get; set; }
    public string? ConstructionPeriod { get; set; } // e.g., "1981-1990"
    public string? CVBoilerBrand { get; set; }
    public int? CVBoilerYear { get; set; }
    
    // Broker/Agent Details (from Contact Details API)
    public int? BrokerOfficeId { get; set; }
    public string? BrokerPhone { get; set; }
    public string? BrokerLogoUrl { get; set; }
    public string? BrokerAssociationCode { get; set; } // e.g., "nvm", "vbo"
    
    // Infrastructure (from Fiber Availability API)
    public bool? FiberAvailable { get; set; }
    
    // Status Tracking (from Summary API)
    public DateTime? PublicationDate { get; set; }
    public bool IsSoldOrRented { get; set; }
    public List<string> Labels { get; set; } = []; // e.g., "Nieuw", "Open huis"
    
    // Cache Tracking (for dynamic search)
    /// <summary>
    /// Last time this listing was fetched/refreshed from Funda.
    /// Used for cache invalidation in dynamic search.
    /// </summary>
    public DateTime? LastFundaFetchUtc { get; set; }

    // Phase 5: Context Scores & Report
    // Scores are stored as separate columns to enable efficient filtering and sorting
    public double? ContextCompositeScore { get; set; }
    public double? ContextSafetyScore { get; set; }
    public double? ContextSocialScore { get; set; }
    public double? ContextAmenitiesScore { get; set; }
    public double? ContextEnvironmentScore { get; set; }

    // Phase 6: Market Data
    public int? WozValue { get; set; }
    public DateTime? WozReferenceDate { get; set; }
    public string? WozValueSource { get; set; }

    // Full report stored as JSONB for detailed display without joins
    public Valora.Domain.Models.ContextReportModel? ContextReport { get; set; }
}
