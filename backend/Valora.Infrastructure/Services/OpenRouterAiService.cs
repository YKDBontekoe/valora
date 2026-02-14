using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;

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

    public async Task<string> ChatAsync(string prompt, string? model = null, AiExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var modelToUse = !string.IsNullOrEmpty(model) ? model : _defaultModel;

        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = _endpoint
        };

        // Add policies to handle OpenRouter specifics
        clientOptions.AddPolicy(new OpenRouterHeadersPolicy(_siteUrl, _siteName), PipelinePosition.PerCall);
        clientOptions.AddPolicy(new OpenRouterDefaultModelPolicy(), PipelinePosition.PerCall);

        var client = new ChatClient(
            model: modelToUse,
            credential: new ApiKeyCredential(_apiKey),
            options: clientOptions
        );

        var completionOptions = new ChatCompletionOptions();

        if (options?.MaxOutputTokens is > 0)
        {
            completionOptions.MaxOutputTokenCount = options.MaxOutputTokens;
        }

        if (options?.TelemetryTags is { Count: > 0 })
        {
            foreach (var tag in options.TelemetryTags)
            {
                completionOptions.Metadata[tag.Key] = tag.Value;
            }
        }

        ChatCompletion completion = await client.CompleteChatAsync(
            [new UserChatMessage(prompt)],
            options: completionOptions,
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
