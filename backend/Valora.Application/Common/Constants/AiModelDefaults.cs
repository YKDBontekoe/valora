namespace Valora.Application.Common.Constants;

public static class AiModelDefaults
{
    public static readonly Dictionary<string, string> DefaultModels = new()
    {
        { "quick_summary", "openai/gpt-4o-mini" },
        { "detailed_analysis", "openai/gpt-4o" },
        { "chat", "openai/gpt-4o-mini" }
    };
}
