using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Services;

public class OpenRouterAiService : IAiService
{
    private readonly string _apiKey;
    private readonly Uri _endpoint;
    private readonly string _siteUrl;
    private readonly string _siteName;
    private readonly ILogger<OpenRouterAiService> _logger;
    private readonly IAiModelService _aiModelService;
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenRouterAiService(
        IConfiguration configuration,
        ILogger<OpenRouterAiService> logger,
        IAiModelService aiModelService,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _aiModelService = aiModelService;
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["OPENROUTER_API_KEY"] ?? throw new InvalidOperationException("OPENROUTER_API_KEY is not configured.");

        var baseUrl = configuration["OPENROUTER_BASE_URL"] ?? "https://openrouter.ai/api/v1";
        _endpoint = new Uri(baseUrl);

        _siteUrl = configuration["OPENROUTER_SITE_URL"] ?? "https://valora.app";
        _siteName = configuration["OPENROUTER_SITE_NAME"] ?? "Valora";
    }

    public async Task<IEnumerable<AiModelDto>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("OpenRouter");

        var uriBuilder = new UriBuilder(_endpoint);
        if (!uriBuilder.Path.EndsWith("/")) uriBuilder.Path += "/";
        uriBuilder.Path += "models";

        var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        request.Headers.Add("HTTP-Referer", _siteUrl);
        request.Headers.Add("X-Title", _siteName);

        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        var models = new List<AiModelDto>();

        if (json.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString() ?? "";
                var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;
                var description = item.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";
                var contextLength = item.TryGetProperty("context_length", out var ctx) ? ctx.GetInt32() : 0;

                decimal promptPrice = 0;
                decimal completionPrice = 0;

                if (item.TryGetProperty("pricing", out var pricing))
                {
                    if (pricing.TryGetProperty("prompt", out var p))
                        decimal.TryParse(p.GetString(), out promptPrice);
                    if (pricing.TryGetProperty("completion", out var c))
                        decimal.TryParse(c.GetString(), out completionPrice);
                }

                models.Add(new AiModelDto(id, name, description, contextLength, promptPrice, completionPrice));
            }
        }

        return models;
    }

    public async Task<string> ChatAsync(string prompt, string? systemPrompt = null, string intent = "chat", CancellationToken cancellationToken = default)
    {
        var (primaryModel, fallbackModels) = await _aiModelService.GetModelsForIntentAsync(intent, cancellationToken);
        var modelsToTry = new List<string> { primaryModel };
        if (fallbackModels != null)
        {
            modelsToTry.AddRange(fallbackModels);
        }

        Exception? lastException = null;
        int attempt = 0;

        foreach (var model in modelsToTry)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;
            try
            {
                var response = await ExecuteChatWithModelAsync(prompt, systemPrompt, model, cancellationToken);

                if (string.IsNullOrWhiteSpace(response))
                {
                    throw new Exception($"Model {model} returned empty response.");
                }

                _logger.LogInformation("AI Chat Success. Intent: {Intent}, Model: {Model}, FallbackAttempt: {IsFallback}",
                    intent, model, attempt > 1);

                return response;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Failed to chat with model {Model} for intent {Intent}. Trying next fallback...", model, intent);
            }
        }

        _logger.LogError(lastException, "All models failed for intent {Intent}.", intent);

        if (lastException is ClientResultException clientEx)
        {
            if (clientEx.Status == 429 || clientEx.Status >= 500)
            {
                throw new HttpRequestException($"AI service temporarily unavailable: {clientEx.Message}", clientEx, System.Net.HttpStatusCode.ServiceUnavailable);
            }
            throw new HttpRequestException($"AI service client error: {clientEx.Message}", clientEx, (System.Net.HttpStatusCode)clientEx.Status);
        }

        throw lastException ?? new Exception("All models failed.");
    }

    private async Task<string> ExecuteChatWithModelAsync(string prompt, string? systemPrompt, string model, CancellationToken cancellationToken)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = _endpoint,
            RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(0)
        };

        options.AddPolicy(new OpenRouterHeadersPolicy(_siteUrl, _siteName), PipelinePosition.PerCall);
        options.AddPolicy(new OpenRouterDefaultModelPolicy(), PipelinePosition.PerCall);

        var client = new ChatClient(
            model: model,
            credential: new ApiKeyCredential(_apiKey),
            options: options
        );

        var messages = new List<ChatMessage>();

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new SystemChatMessage(systemPrompt));
        }

        messages.Add(new UserChatMessage(prompt));

        int maxRetries = 1;
        int delayMilliseconds = 1000;

        for (int i = 0; i <= maxRetries; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                ChatCompletion completion = await client.CompleteChatAsync(
                    messages,
                    options: null,
                    cancellationToken
                );

                if (completion.Content != null && completion.Content.Count > 0)
                {
                    return completion.Content[0].Text;
                }

                throw new Exception("Received empty content from AI provider.");
            }
            catch (ClientResultException ex) when (ex.Status == 429 || (ex.Status >= 500 && ex.Status < 600))
            {
                if (i == maxRetries)
                {
                    throw;
                }

                _logger.LogWarning(ex, "Model {Model} attempt {Attempt} failed with status {Status}. Retrying in {Delay}ms...", model, i + 1, ex.Status, delayMilliseconds);
                await Task.Delay(delayMilliseconds, cancellationToken);
                delayMilliseconds *= 2;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "ArgumentOutOfRangeException in AI SDK. Treating as failure.");
                throw new Exception("AI SDK error", ex);
            }
        }

        return string.Empty;
    }
}
