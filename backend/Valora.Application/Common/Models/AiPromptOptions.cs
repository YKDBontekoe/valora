namespace Valora.Application.Common.Models;

public sealed class AiPromptOptions
{
    public bool StrictMode { get; set; } = true;
    public int DefaultMaxPromptChars { get; set; } = 6000;
    public int DefaultMaxPromptTokens { get; set; } = 1500;
    public int TopMetricCount { get; set; } = 3;
    public Dictionary<string, AiModelLimits> ModelLimits { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AiModelLimits
{
    public int? MaxPromptChars { get; set; }
    public int? MaxPromptTokens { get; set; }
    public int? MaxOutputTokens { get; set; }
    public string? TelemetryTag { get; set; }
}

public sealed record AiExecutionOptions(
    int? MaxOutputTokens = null,
    IReadOnlyDictionary<string, string>? TelemetryTags = null);
