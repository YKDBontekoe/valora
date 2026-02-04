using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;

namespace Valora.Infrastructure.Services;

public class OpenRouterHeadersPolicy : PipelinePolicy
{
    private readonly string _siteUrl;
    private readonly string _siteName;

    public OpenRouterHeadersPolicy(string siteUrl = "https://valora.app", string siteName = "Valora")
    {
        _siteUrl = siteUrl;
        _siteName = siteName;
    }

    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        AddHeaders(message.Request);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        AddHeaders(message.Request);
        await ProcessNextAsync(message, pipeline, currentIndex);
    }

    private void AddHeaders(PipelineRequest request)
    {
        request.Headers.Set("HTTP-Referer", _siteUrl);
        request.Headers.Set("X-Title", _siteName);
    }
}

public class OpenRouterDefaultModelPolicy : PipelinePolicy
{
    private const string PlaceholderModel = "openrouter/default";

    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        // Sync version of modifying request
        ModifyRequest(message);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        // Async version
        await ModifyRequestAsync(message);
        await ProcessNextAsync(message, pipeline, currentIndex);
    }

    private void ModifyRequest(PipelineMessage message)
    {
        if (message.Request.Content != null)
        {
            using var stream = new MemoryStream();
            message.Request.Content.WriteTo(stream, message.CancellationToken);
            stream.Position = 0;

            if (TryRemovePlaceholderModel(stream, out var newContent))
            {
                message.Request.Content = BinaryContent.Create(BinaryData.FromString(newContent));
            }
        }
    }

    private async Task ModifyRequestAsync(PipelineMessage message)
    {
        if (message.Request.Content != null)
        {
            using var stream = new MemoryStream();
            await message.Request.Content.WriteToAsync(stream, message.CancellationToken);
            stream.Position = 0;

            if (TryRemovePlaceholderModel(stream, out var newContent))
            {
                message.Request.Content = BinaryContent.Create(BinaryData.FromString(newContent));
            }
        }
    }

    private bool TryRemovePlaceholderModel(Stream stream, out string newJson)
    {
        newJson = string.Empty;
        try
        {
            var node = JsonNode.Parse(stream);
            if (node is JsonObject obj && obj.ContainsKey("model"))
            {
                var model = obj["model"]?.GetValue<string>();
                if (model == PlaceholderModel)
                {
                    obj.Remove("model");
                    newJson = obj.ToJsonString();
                    return true;
                }
            }
        }
        catch
        {
            // Ignore parse errors, just pass through
        }
        return false;
    }
}
