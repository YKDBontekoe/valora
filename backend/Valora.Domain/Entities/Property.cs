using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class Property : BaseEntity
{
    public string? BagId { get; set; } // BAG ID from PDOK
    public required string Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public int? LivingAreaM2 { get; set; }
    public int? YearBuilt { get; set; }
    
    // Phase 4: Complete Data Capture
    // Geo
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // Insights & Engagement
    public int? SaveCount { get; set; }
    
    // Phase 5: Context Scores & Report
    // Scores are stored as separate columns to enable efficient filtering and sorting
    public double? ContextCompositeScore { get; set; }
    public double? ContextSafetyScore { get; set; }
    public double? ContextSocialScore { get; set; }
    public double? ContextAmenitiesScore { get; set; }
    public double? ContextEnvironmentScore { get; set; }

    // Full report stored as JSONB for detailed display without joins
    public Valora.Domain.Models.ContextReportModel? ContextReport { get; set; }
}
