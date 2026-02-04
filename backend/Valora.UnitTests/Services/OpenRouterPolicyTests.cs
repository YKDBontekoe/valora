using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using Valora.Infrastructure.Services;

namespace Valora.UnitTests.Services;

public class OpenRouterPolicyTests
{
    private class TestPipelineMessage : PipelineMessage
    {
        public TestPipelineMessage(PipelineRequest request) : base(request)
        {
        }

        // No need to override Response or Request if we pass request to base constructor
        // Base constructor sets the Request property.
    }

    private class TestPipelineRequest : PipelineRequest
    {
        private readonly PipelineRequestHeaders _headers = new TestPipelineRequestHeaders();
        private BinaryContent? _content;
        private string _method = "POST";
        private Uri? _uri;

        protected override BinaryContent? ContentCore
        {
            get => _content;
            set => _content = value;
        }

        protected override PipelineRequestHeaders HeadersCore => _headers;

        protected override string MethodCore
        {
            get => _method;
            set => _method = value;
        }

        protected override Uri? UriCore
        {
            get => _uri;
            set => _uri = value;
        }

        public override void Dispose() { }

        private class TestPipelineRequestHeaders : PipelineRequestHeaders
        {
            private readonly Dictionary<string, string> _headers = new();

            public override void Add(string name, string value) => _headers[name] = value;
            public override void Set(string name, string value) => _headers[name] = value;
            public override bool Remove(string name) => _headers.Remove(name);
            public override bool TryGetValue(string name, out string? value) => _headers.TryGetValue(name, out value);
            public override bool TryGetValues(string name, out IEnumerable<string>? values)
            {
                if (_headers.TryGetValue(name, out var val))
                {
                    values = new[] { val };
                    return true;
                }
                values = null;
                return false;
            }
            public override IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _headers.GetEnumerator();
        }
    }

    private class MockNextPolicy : PipelinePolicy
    {
        public bool ProcessCalled { get; private set; }
        public bool ProcessAsyncCalled { get; private set; }

        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            ProcessCalled = true;
        }

        public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            ProcessAsyncCalled = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void HeadersPolicy_Process_ShouldAddHeaders()
    {
        var policy = new OpenRouterHeadersPolicy("https://test.com", "TestApp");
        var request = new TestPipelineRequest();
        var message = new TestPipelineMessage(request);
        var nextPolicy = new MockNextPolicy();
        var pipeline = new List<PipelinePolicy> { policy, nextPolicy };

        policy.Process(message, pipeline, 0);

        Assert.True(nextPolicy.ProcessCalled);
        Assert.True(request.Headers.TryGetValue("HTTP-Referer", out var referer));
        Assert.Equal("https://test.com", referer);
        Assert.True(request.Headers.TryGetValue("X-Title", out var title));
        Assert.Equal("TestApp", title);
    }

    [Fact]
    public async Task HeadersPolicy_ProcessAsync_ShouldAddHeaders()
    {
        var policy = new OpenRouterHeadersPolicy("https://test.com", "TestApp");
        var request = new TestPipelineRequest();
        var message = new TestPipelineMessage(request);
        var nextPolicy = new MockNextPolicy();
        var pipeline = new List<PipelinePolicy> { policy, nextPolicy };

        await policy.ProcessAsync(message, pipeline, 0);

        Assert.True(nextPolicy.ProcessAsyncCalled);
        Assert.True(request.Headers.TryGetValue("HTTP-Referer", out var referer));
        Assert.Equal("https://test.com", referer);
        Assert.True(request.Headers.TryGetValue("X-Title", out var title));
        Assert.Equal("TestApp", title);
    }

    [Fact]
    public void DefaultModelPolicy_Process_ShouldRemovePlaceholderModel()
    {
        var policy = new OpenRouterDefaultModelPolicy();
        var json = "{\"model\":\"openrouter/default\",\"messages\":[]}";
        var request = new TestPipelineRequest { Content = BinaryContent.Create(BinaryData.FromString(json)) };
        var message = new TestPipelineMessage(request);
        var nextPolicy = new MockNextPolicy();
        var pipeline = new List<PipelinePolicy> { policy, nextPolicy };

        policy.Process(message, pipeline, 0);

        Assert.True(nextPolicy.ProcessCalled);
        var newJson = GetContentAsString(request.Content);
        Assert.DoesNotContain("model", newJson);
    }

    [Fact]
    public async Task DefaultModelPolicy_ProcessAsync_ShouldRemovePlaceholderModel()
    {
        var policy = new OpenRouterDefaultModelPolicy();
        var json = "{\"model\":\"openrouter/default\",\"messages\":[]}";
        var request = new TestPipelineRequest { Content = BinaryContent.Create(BinaryData.FromString(json)) };
        var message = new TestPipelineMessage(request);
        var nextPolicy = new MockNextPolicy();
        var pipeline = new List<PipelinePolicy> { policy, nextPolicy };

        await policy.ProcessAsync(message, pipeline, 0);

        Assert.True(nextPolicy.ProcessAsyncCalled);
        var newJson = GetContentAsString(request.Content);
        Assert.DoesNotContain("model", newJson);
    }

    [Fact]
    public void DefaultModelPolicy_Process_ShouldKeepSpecificModel()
    {
        var policy = new OpenRouterDefaultModelPolicy();
        var json = "{\"model\":\"gpt-4\",\"messages\":[]}";
        var request = new TestPipelineRequest { Content = BinaryContent.Create(BinaryData.FromString(json)) };
        var message = new TestPipelineMessage(request);
        var nextPolicy = new MockNextPolicy();
        var pipeline = new List<PipelinePolicy> { policy, nextPolicy };

        policy.Process(message, pipeline, 0);

        var newJson = GetContentAsString(request.Content);
        Assert.Contains("gpt-4", newJson);
    }

    [Fact]
    public void DefaultModelPolicy_Process_ShouldHandleInvalidJsonGracefully()
    {
        var policy = new OpenRouterDefaultModelPolicy();
        var invalidJson = "{ invalid json }";
        var request = new TestPipelineRequest { Content = BinaryContent.Create(BinaryData.FromString(invalidJson)) };
        var message = new TestPipelineMessage(request);
        var nextPolicy = new MockNextPolicy();
        var pipeline = new List<PipelinePolicy> { policy, nextPolicy };

        // Should not throw, catch block handles exception
        policy.Process(message, pipeline, 0);

        Assert.True(nextPolicy.ProcessCalled);
        var content = GetContentAsString(request.Content);
        Assert.Equal(invalidJson, content);
    }

    [Fact]
    public async Task DefaultModelPolicy_ProcessAsync_ShouldHandleInvalidJsonGracefully()
    {
        var policy = new OpenRouterDefaultModelPolicy();
        var invalidJson = "{ invalid json }";
        var request = new TestPipelineRequest { Content = BinaryContent.Create(BinaryData.FromString(invalidJson)) };
        var message = new TestPipelineMessage(request);
        var nextPolicy = new MockNextPolicy();
        var pipeline = new List<PipelinePolicy> { policy, nextPolicy };

        await policy.ProcessAsync(message, pipeline, 0);

        Assert.True(nextPolicy.ProcessAsyncCalled);
        var content = GetContentAsString(request.Content);
        Assert.Equal(invalidJson, content);
    }

    private string GetContentAsString(BinaryContent? content)
    {
        if (content == null) return string.Empty;
        using var stream = new MemoryStream();
        content.WriteTo(stream, CancellationToken.None);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
