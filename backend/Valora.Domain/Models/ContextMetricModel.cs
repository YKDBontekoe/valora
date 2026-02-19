namespace Valora.Domain.Models;

public sealed record ContextMetricModel
{
    public string Key { get; init; } = null!;
    public string Label { get; init; } = null!;
    public double? Value { get; init; }
    public string? Unit { get; init; }
    public double? Score { get; init; }
    public string Source { get; init; } = null!;
    public string? Note { get; init; }

    public ContextMetricModel() { }

    public ContextMetricModel(
        string key,
        string label,
        double? value,
        string? unit,
        double? score,
        string source,
        string? note = null)
    {
        Key = key;
        Label = label;
        Value = value;
        Unit = unit;
        Score = score;
        Source = source;
        Note = note;
    }
}
