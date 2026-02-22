using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Services;

public class OpenRouterAiService : IAiService
{
    private readonly string _apiKey;
    private readonly Uri _endpoint;
    private readonly string _siteUrl;
    private readonly string _siteName;
    private readonly ILogger<OpenRouterAiService> _logger;
    private readonly IAiModelService _aiModelService;

    public OpenRouterAiService(
        IConfiguration configuration,
        ILogger<OpenRouterAiService> logger,
        IAiModelService aiModelService)
    {
        _logger = logger;
        _aiModelService = aiModelService;
        _apiKey = configuration["OPENROUTER_API_KEY"] ?? throw new InvalidOperationException("OPENROUTER_API_KEY is not configured.");

        var baseUrl = configuration["OPENROUTER_BASE_URL"] ?? "https://openrouter.ai/api/v1";
        _endpoint = new Uri(baseUrl);

        _siteUrl = configuration["OPENROUTER_SITE_URL"] ?? "https://valora.app";
        _siteName = configuration["OPENROUTER_SITE_NAME"] ?? "Valora";
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

                // Check for empty/invalid response which acts as a "soft failure" to trigger fallback
                if (string.IsNullOrWhiteSpace(response))
                {
                    throw new Exception($"Model {model} returned empty response.");
                }

                // Log success with specific model used for cost/reliability monitoring
                _logger.LogInformation("AI Chat Success. Intent: {Intent}, Model: {Model}, FallbackAttempt: {IsFallback}",
                    intent, model, attempt > 1);

                return response;
            }
            catch (OperationCanceledException)
            {
                throw; // Don't swallow cancellation
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Failed to chat with model {Model} for intent {Intent}. Trying next fallback...", model, intent);
                // Continue to next model
            }
        }

        _logger.LogError(lastException, "All models failed for intent {Intent}.", intent);

        // Wrap client exceptions in HttpRequestException for consistent handling upstream (e.g. middleware)
        if (lastException is ClientResultException clientEx)
        {
             // 429/5xx -> ServiceUnavailable (503) for Polly/Middleware to handle
             if (clientEx.Status == 429 || clientEx.Status >= 500)
             {
                 throw new HttpRequestException($"AI service temporarily unavailable: {clientEx.Message}", clientEx, System.Net.HttpStatusCode.ServiceUnavailable);
             }
             // Client errors (400, etc) -> BadRequest
             throw new HttpRequestException($"AI service client error: {clientEx.Message}", clientEx, (System.Net.HttpStatusCode)clientEx.Status);
        }

        throw lastException ?? new Exception("All models failed.");
    }

    private async Task<string> ExecuteChatWithModelAsync(string prompt, string? systemPrompt, string model, CancellationToken cancellationToken)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = _endpoint,
            RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(0) // Disable SDK internal retries
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

        // Reduced max retries per model to avoid long latency if falling back
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

                // If content is empty, throw to trigger fallback
                throw new Exception("Received empty content from AI provider.");
            }
            catch (ClientResultException ex) when (ex.Status == 429 || (ex.Status >= 500 && ex.Status < 600))
            {
                if (i == maxRetries)
                {
                    throw; // Rethrow to be caught by the fallback loop
                }

                _logger.LogWarning(ex, "Model {Model} attempt {Attempt} failed with status {Status}. Retrying in {Delay}ms...", model, i + 1, ex.Status, delayMilliseconds);
                await Task.Delay(delayMilliseconds, cancellationToken);
                delayMilliseconds *= 2;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                 // SDK internal error on empty choices? Treat as failure.
                 _logger.LogWarning(ex, "ArgumentOutOfRangeException in AI SDK. Treating as failure.");
                 throw new Exception("AI SDK error", ex);
            }
        }

        return string.Empty; // Should be unreachable given throws above
    }
}
