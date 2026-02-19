namespace Valora.Domain.Models;

public sealed record SourceAttributionModel
{
    public string Source { get; init; } = null!;
    public string Url { get; init; } = null!;
    public string License { get; init; } = null!;
    public DateTimeOffset RetrievedAtUtc { get; init; }

    public SourceAttributionModel() { }

    public SourceAttributionModel(
        string source,
        string url,
        string license,
        DateTimeOffset retrievedAtUtc)
    {
        Source = source;
        Url = url;
        License = license;
        RetrievedAtUtc = retrievedAtUtc;
    }
}
