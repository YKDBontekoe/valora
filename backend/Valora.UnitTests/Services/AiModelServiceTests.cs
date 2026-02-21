using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.UnitTests.Services;

public class AiModelServiceTests : IDisposable
{
    private readonly ValoraDbContext _context;
    private readonly AiModelService _service;
    private readonly IConfiguration _configuration;

    public AiModelServiceTests()
    {
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ValoraDbContext(options);

        var inMemorySettings = new Dictionary<string, string?> {
            {"TopLevelKey", "TopLevelValue"},
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _service = new AiModelService(_context, _configuration);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetModelsForIntent_ReturnsConfiguredModel_WhenExists()
    {
        var config = new AiModelConfig
        {
            Intent = "chat",
            PrimaryModel = "configured-model",
            FallbackModels = new List<string> { "fallback-1" },
            IsEnabled = true
        };
        _context.AiModelConfigs.Add(config);
        await _context.SaveChangesAsync();

        var (primary, fallbacks) = await _service.GetModelsForIntentAsync("chat");

        Assert.Equal("configured-model", primary);
        Assert.Single(fallbacks);
        Assert.Equal("fallback-1", fallbacks[0]);
    }

    [Fact]
    public async Task GetModelsForIntent_ReturnsDefaultModel_WhenNotExists()
    {
        var (primary, fallbacks) = await _service.GetModelsForIntentAsync("chat");

        Assert.Equal("openai/gpt-4o-mini", primary);
        Assert.Empty(fallbacks);
    }

    [Fact]
    public async Task GetModelsForIntent_ReturnsDefaultModel_WhenDisabled()
    {
        var config = new AiModelConfig
        {
            Intent = "chat",
            PrimaryModel = "configured-model",
            IsEnabled = false
        };
        _context.AiModelConfigs.Add(config);
        await _context.SaveChangesAsync();

        var (primary, fallbacks) = await _service.GetModelsForIntentAsync("chat");

        Assert.Equal("openai/gpt-4o-mini", primary);
        Assert.Empty(fallbacks);
    }

    [Fact]
    public async Task CreateConfigAsync_AddsNewConfig()
    {
        var config = new AiModelConfig
        {
            Intent = "new-intent",
            PrimaryModel = "new-model",
            FallbackModels = new List<string>(),
            IsEnabled = true
        };

        var result = await _service.CreateConfigAsync(config);

        Assert.NotNull(result);
        Assert.Equal("new-intent", result.Intent);

        var dbConfig = await _context.AiModelConfigs.FirstOrDefaultAsync(c => c.Intent == "new-intent");
        Assert.NotNull(dbConfig);
    }

    [Fact]
    public async Task UpdateConfigAsync_UpdatesExistingConfig()
    {
        var config = new AiModelConfig
        {
            Intent = "update-intent",
            PrimaryModel = "old-model",
            FallbackModels = new List<string>(),
            IsEnabled = true
        };
        _context.AiModelConfigs.Add(config);
        await _context.SaveChangesAsync();

        config.PrimaryModel = "updated-model";
        await _service.UpdateConfigAsync(config);

        var dbConfig = await _context.AiModelConfigs.FirstOrDefaultAsync(c => c.Intent == "update-intent");
        Assert.Equal("updated-model", dbConfig!.PrimaryModel);
    }

    [Fact]
    public async Task DeleteConfigAsync_RemovesConfig()
    {
        var config = new AiModelConfig
        {
            Intent = "delete-intent",
            PrimaryModel = "model",
            FallbackModels = new List<string>(),
            IsEnabled = true
        };
        _context.AiModelConfigs.Add(config);
        await _context.SaveChangesAsync();

        await _service.DeleteConfigAsync(config.Id);

        var dbConfig = await _context.AiModelConfigs.FirstOrDefaultAsync(c => c.Intent == "delete-intent");
        Assert.Null(dbConfig);
    }

    [Fact]
    public async Task GetAllConfigsAsync_ReturnsAllConfigs()
    {
        _context.AiModelConfigs.Add(new AiModelConfig { Intent = "intent1", PrimaryModel = "m1" });
        _context.AiModelConfigs.Add(new AiModelConfig { Intent = "intent2", PrimaryModel = "m2" });
        await _context.SaveChangesAsync();

        var configs = await _service.GetAllConfigsAsync();

        Assert.Equal(2, ((List<AiModelConfig>)configs).Count);
    }
}
