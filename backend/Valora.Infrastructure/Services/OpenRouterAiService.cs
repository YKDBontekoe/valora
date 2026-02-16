using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Services;

public class OpenRouterAiService : IAiService
{
    private readonly string _apiKey;
    private readonly string _defaultModel;
    private readonly Uri _endpoint;
    private readonly string _siteUrl;
    private readonly string _siteName;

    // This string must match the one in OpenRouterDefaultModelPolicy
    private const string PlaceholderModel = "openrouter/default";

    public OpenRouterAiService(IConfiguration configuration)
    {
        _apiKey = configuration["OPENROUTER_API_KEY"] ?? throw new InvalidOperationException("OPENROUTER_API_KEY is not configured.");

        // If configured, use it. Otherwise use the placeholder which triggers "default model" behavior via policy.
        _defaultModel = configuration["OPENROUTER_MODEL"] ?? PlaceholderModel;

        var baseUrl = configuration["OPENROUTER_BASE_URL"] ?? "https://openrouter.ai/api/v1";
        _endpoint = new Uri(baseUrl);

        _siteUrl = configuration["OPENROUTER_SITE_URL"] ?? "https://valora.app";
        _siteName = configuration["OPENROUTER_SITE_NAME"] ?? "Valora";
    }

    /// <summary>
    /// Sends a chat prompt to the AI service (via OpenRouter) and returns the response.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>OpenRouter Integration Strategy:</strong>
    /// We use the official <see cref="OpenAI"/> SDK but intercept the requests using
    /// <see cref="System.ClientModel.Primitives.PipelinePolicy"/>.
    /// </para>
    /// <para>
    /// This allows us to:
    /// 1. Inject required OpenRouter headers (`HTTP-Referer`, `X-Title`) via <see cref="OpenRouterHeadersPolicy"/>.
    /// 2. Handle the "default model" logic dynamically via <see cref="OpenRouterDefaultModelPolicy"/>.
    /// </para>
    /// </remarks>
    public async Task<string> ChatAsync(string prompt, string? systemPrompt = null, string? model = null, CancellationToken cancellationToken = default)
    {
        var modelToUse = !string.IsNullOrEmpty(model) ? model : _defaultModel;

        var options = new OpenAIClientOptions
        {
            Endpoint = _endpoint
        };

        // Add policies to handle OpenRouter specifics
        // Headers for attribution - configurable via settings
        options.AddPolicy(new OpenRouterHeadersPolicy(_siteUrl, _siteName), PipelinePosition.PerCall);

        // Policy to remove "model" field if it matches our placeholder (triggering user default at OpenRouter)
        options.AddPolicy(new OpenRouterDefaultModelPolicy(), PipelinePosition.PerCall);

        // ChatClient is lightweight and designed to be created for specific models.
        var client = new ChatClient(
            model: modelToUse,
            credential: new ApiKeyCredential(_apiKey),
            options: options
        );

        var messages = new List<ChatMessage>();

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new SystemChatMessage(systemPrompt));
        }

        messages.Add(new UserChatMessage(prompt));

        ChatCompletion completion = await client.CompleteChatAsync(
            messages,
            options: null,
            cancellationToken
        );

        try
        {
            if (completion.Content != null && completion.Content.Count > 0)
            {
                return completion.Content[0].Text;
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            // Accessing .Content property getter might throw if the collection is empty/malformed internally in the SDK.
            // This is defensive coding against SDK behavior when choices is empty.
        }

        return string.Empty;
    }
}
