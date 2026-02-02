namespace Valora.Application.DTOs;

public class ScrapedListingDto
{
    public required string FundaId { get; set; }
    public required string Url { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Price { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public int? Bedrooms { get; set; }
    public int? LivingAreaM2 { get; set; }
    public int? PlotAreaM2 { get; set; }
    public string? PropertyType { get; set; }
    public string? Status { get; set; }
}
