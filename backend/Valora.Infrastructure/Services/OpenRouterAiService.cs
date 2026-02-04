using System.ClientModel;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Services;

public class OpenRouterAiService : IAiService
{
    private readonly string _apiKey;
    private readonly string _defaultModel;
    private readonly Uri _endpoint = new("https://openrouter.ai/api/v1");

    public OpenRouterAiService(IConfiguration configuration)
    {
        _apiKey = configuration["OPENROUTER_API_KEY"] ?? throw new InvalidOperationException("OPENROUTER_API_KEY is not configured.");
        // Fallback to a widely supported model if not configured
        _defaultModel = configuration["OPENROUTER_MODEL"] ?? "openai/gpt-3.5-turbo";
    }

    public async Task<string> ChatAsync(string prompt, string? model = null, CancellationToken cancellationToken = default)
    {
        var modelToUse = !string.IsNullOrEmpty(model) ? model : _defaultModel;

        // ChatClient is lightweight and designed to be created for specific models.
        // The underlying pipeline handles connection pooling if default transport is used.
        var client = new ChatClient(
            model: modelToUse,
            credential: new ApiKeyCredential(_apiKey),
            options: new OpenAIClientOptions { Endpoint = _endpoint }
        );

        ChatCompletion completion = await client.CompleteChatAsync(
            [new UserChatMessage(prompt)],
            options: null,
            cancellationToken
        );

        // Return the first content part text
        if (completion.Content.Count > 0)
        {
            return completion.Content[0].Text;
        }

        return string.Empty;
    }
}
