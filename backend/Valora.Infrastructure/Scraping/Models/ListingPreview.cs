namespace Valora.Infrastructure.Scraping.Models;

public record ListingPreview
{
    public required string FundaId { get; init; }
    public required string Url { get; init; }
    public decimal? Price { get; init; }
}
