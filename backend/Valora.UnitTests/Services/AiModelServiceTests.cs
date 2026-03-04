using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Valora.Application.DTOs;
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
    public async Task GetModelForFeature_ReturnsConfiguredModel_WhenExists()
    {
        var config = new AiModelConfig
        {
            Feature = "chat",
            ModelId = "configured-model",
            IsEnabled = true
        };
        _context.AiModelConfigs.Add(config);
        await _context.SaveChangesAsync();

        var model = await _service.GetModelForFeatureAsync("chat");

        Assert.Equal("configured-model", model);
    }

    [Fact]
    public async Task GetModelForFeature_ReturnsDefaultModel_WhenNotExists()
    {
        var model = await _service.GetModelForFeatureAsync("chat");

        Assert.Equal("openai/gpt-4o-mini", model);
    }

    [Fact]
    public async Task GetModelForFeature_ReturnsDefaultModel_WhenDisabled()
    {
        var config = new AiModelConfig
        {
            Feature = "chat",
            ModelId = "configured-model",
            IsEnabled = false
        };
        _context.AiModelConfigs.Add(config);
        await _context.SaveChangesAsync();

        var model = await _service.GetModelForFeatureAsync("chat");

        Assert.Equal("openai/gpt-4o-mini", model);
    }

    [Fact]
    public async Task GetModelForFeature_ReturnsFallback_ForUnknownFeature()
    {
        var model = await _service.GetModelForFeatureAsync("unknown_feature");

        Assert.Equal("openai/gpt-4o-mini", model); // Hardcoded fallback in service
    }

    [Fact]
    public async Task CreateConfigAsync_AddsNewConfig()
    {
        var configDto = new AiModelConfigDto
        {
            Feature = "new-feature",
            ModelId = "new-model",
            IsEnabled = true
        };

        var result = await _service.CreateConfigAsync(configDto);

        Assert.NotNull(result);
        Assert.Equal("new-feature", result.Feature);

        var dbConfig = await _context.AiModelConfigs.FirstOrDefaultAsync(c => c.Feature == "new-feature");
        Assert.NotNull(dbConfig);
    }

    [Fact]
    public async Task UpdateConfigAsync_UpdatesExistingConfig()
    {
        var config = new AiModelConfig
        {
            Feature = "update-feature",
            ModelId = "old-model",
            IsEnabled = true
        };
        _context.AiModelConfigs.Add(config);
        await _context.SaveChangesAsync();

        var configDto = new AiModelConfigDto
        {
            Id = config.Id,
            Feature = "update-feature",
            ModelId = "updated-model",
            IsEnabled = true
        };

        var result = await _service.UpdateConfigAsync(configDto);

        Assert.NotNull(result);
        Assert.Equal("updated-model", result.ModelId);
        Assert.Equal(config.Id, result.Id);
        Assert.Equal("update-feature", result.Feature);

        var dbConfig = await _context.AiModelConfigs.FirstOrDefaultAsync(c => c.Feature == "update-feature");
        Assert.Equal("updated-model", dbConfig!.ModelId);
    }

    [Fact]
    public async Task DeleteConfigAsync_RemovesConfig()
    {
        var config = new AiModelConfig
        {
            Feature = "delete-feature",
            ModelId = "model",
            IsEnabled = true
        };
        _context.AiModelConfigs.Add(config);
        await _context.SaveChangesAsync();

        await _service.DeleteConfigAsync(config.Id);

        var dbConfig = await _context.AiModelConfigs.FirstOrDefaultAsync(c => c.Feature == "delete-feature");
        Assert.Null(dbConfig);
    }

    [Fact]
    public async Task GetAllConfigsAsync_ReturnsAllConfigs()
    {
        _context.AiModelConfigs.Add(new AiModelConfig { Feature = "feature1", ModelId = "m1" });
        _context.AiModelConfigs.Add(new AiModelConfig { Feature = "feature2", ModelId = "m2" });
        await _context.SaveChangesAsync();

        var configs = await _service.GetAllConfigsAsync();

        Assert.Equal(2, configs.Count());
    }
}
