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
}
