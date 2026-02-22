using System.Text.Json.Serialization;

namespace Valora.Application.DTOs.Ai;

public class MapQueryPlan
{
    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    [JsonPropertyName("actions")]
    public List<MapQueryAction> Actions { get; set; } = new();

    [JsonPropertyName("follow_up_questions")]
    public List<string> FollowUpQuestions { get; set; } = new();
}

public class MapQueryAction
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();
}
