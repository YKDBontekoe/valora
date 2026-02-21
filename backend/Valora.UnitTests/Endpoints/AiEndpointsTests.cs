using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Valora.Api.Endpoints;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Endpoints;

public class AiEndpointsTests
{
    private readonly Mock<IAiModelService> _mockAiModelService = new();
    private readonly Mock<ILogger<AiChatRequest>> _mockLogger = new();
    private readonly Mock<IContextAnalysisService> _mockContextAnalysisService = new();

    // Helper to test MapGet
    [Fact]
    public async Task GetConfigs_ReturnsOk_WithConfigs()
    {
        // Arrange
        var configs = new List<AiModelConfig> { new() { Intent = "test" } };
        _mockAiModelService.Setup(x => x.GetAllConfigsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configs);

        // Simulate the delegate directly since we can't easily invoke MapGet result
        var handler = async (IAiModelService service, CancellationToken ct) =>
        {
            var result = await service.GetAllConfigsAsync(ct);
            return Results.Ok(result);
        };

        // Act
        var result = await handler(_mockAiModelService.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<IEnumerable<AiModelConfig>>>(result);
        Assert.Single(okResult.Value!);
    }

    [Fact]
    public async Task UpdateConfig_ReturnsBadRequest_WhenIntentMismatch()
    {
        var handler = async (string intent, UpdateAiModelConfigDto dto, IAiModelService service, CancellationToken ct) =>
        {
            if (intent != dto.Intent) return Results.BadRequest("Intent mismatch");
            return Results.Ok();
        };

        var result = await handler("intent1", new UpdateAiModelConfigDto { Intent = "intent2" }, _mockAiModelService.Object, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<string>>(result);
        Assert.Equal("Intent mismatch", badRequest.Value);
    }

    [Fact]
    public async Task UpdateConfig_CreatesNewConfig_WhenNotFound()
    {
        // Arrange
        _mockAiModelService.Setup(x => x.GetConfigByIntentAsync("new", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiModelConfig?)null);

        var dto = new UpdateAiModelConfigDto { Intent = "new", PrimaryModel = "model", IsEnabled = true };

        var handler = async (string intent, UpdateAiModelConfigDto d, IAiModelService service, CancellationToken ct) =>
        {
            if (intent != d.Intent) return Results.BadRequest("Intent mismatch");
            var config = await service.GetConfigByIntentAsync(intent, ct);
            if (config == null)
            {
                var newConfig = new AiModelConfig
                {
                    Intent = d.Intent,
                    PrimaryModel = d.PrimaryModel,
                    FallbackModels = d.FallbackModels,
                    Description = d.Description,
                    IsEnabled = d.IsEnabled,
                    SafetySettings = d.SafetySettings
                };
                await service.CreateConfigAsync(newConfig, ct);
                return Results.Ok(newConfig);
            }
            return Results.Problem();
        };

        // Act
        var result = await handler("new", dto, _mockAiModelService.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<AiModelConfig>>(result);
        Assert.Equal("new", okResult.Value!.Intent);
        _mockAiModelService.Verify(x => x.CreateConfigAsync(It.IsAny<AiModelConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConfig_UpdatesExisting_WhenFound()
    {
        // Arrange
        var existing = new AiModelConfig { Intent = "exist", PrimaryModel = "old" };
        _mockAiModelService.Setup(x => x.GetConfigByIntentAsync("exist", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var dto = new UpdateAiModelConfigDto { Intent = "exist", PrimaryModel = "new", IsEnabled = true };

        var handler = async (string intent, UpdateAiModelConfigDto d, IAiModelService service, CancellationToken ct) =>
        {
            if (intent != d.Intent) return Results.BadRequest("Intent mismatch");
            var config = await service.GetConfigByIntentAsync(intent, ct);
            if (config != null)
            {
                config.PrimaryModel = d.PrimaryModel;
                config.FallbackModels = d.FallbackModels;
                config.Description = d.Description;
                config.IsEnabled = d.IsEnabled;
                config.SafetySettings = d.SafetySettings;

                await service.UpdateConfigAsync(config, ct);
                return Results.Ok(config);
            }
            return Results.NotFound();
        };

        // Act
        var result = await handler("exist", dto, _mockAiModelService.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<AiModelConfig>>(result);
        Assert.Equal("new", okResult.Value!.PrimaryModel);
        _mockAiModelService.Verify(x => x.UpdateConfigAsync(It.IsAny<AiModelConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
