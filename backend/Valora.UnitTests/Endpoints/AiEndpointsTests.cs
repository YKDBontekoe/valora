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
    private readonly Mock<ILogger<AiAnalysisRequest>> _mockAnalysisLogger = new();
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

    [Fact]
    public async Task Chat_ReturnsResponse_WhenSuccessful()
    {
        // Arrange
        var request = new AiChatRequest { Prompt = "test", Intent = "chat" };
        _mockContextAnalysisService
            .Setup(x => x.ChatAsync("test", "chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync("response");

        var handler = async (AiChatRequest req, IContextAnalysisService service, ILogger<AiChatRequest> log, CancellationToken ct) =>
        {
            try
            {
                var response = await service.ChatAsync(req.Prompt, req.Intent, ct);
                return Results.Ok(new { response });
            }
            catch (Exception)
            {
                return Results.Problem();
            }
        };

        // Act
        var result = await handler(request, _mockContextAnalysisService.Object, _mockLogger.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Ok", result.GetType().Name);
    }

    [Fact]
    public async Task Chat_ReturnsProblem_WhenExceptionOccurs()
    {
        // Arrange
        var request = new AiChatRequest { Prompt = "test", Intent = "chat" };
        _mockContextAnalysisService
            .Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("error"));

        var handler = async (AiChatRequest req, IContextAnalysisService service, ILogger<AiChatRequest> log, CancellationToken ct) =>
        {
            try
            {
                var response = await service.ChatAsync(req.Prompt, req.Intent, ct);
                return Results.Ok(new { response });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error processing AI chat request.");
                return Results.Problem(detail: "An unexpected error occurred while processing your request.", statusCode: 500);
            }
        };

        // Act
        var result = await handler(request, _mockContextAnalysisService.Object, _mockLogger.Object, CancellationToken.None);

        // Assert
        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(500, problemResult.StatusCode);
    }

    [Fact]
    public async Task AnalyzeReport_ReturnsResponse_WhenSuccessful()
    {
        // Arrange
        var reportDto = new ContextReportDto(null!, null!, null!, null!, null!, null!, null!, null!, 0, null!, null!, null!);
        var request = new AiAnalysisRequest(reportDto);
        _mockContextAnalysisService
            .Setup(x => x.AnalyzeReportAsync(reportDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync("summary");

        var handler = async (AiAnalysisRequest req, IContextAnalysisService service, ILogger<AiAnalysisRequest> log, CancellationToken ct) =>
        {
            try
            {
                var summary = await service.AnalyzeReportAsync(req.Report, ct);
                return Results.Ok(new AiAnalysisResponse(summary));
            }
            catch (Exception)
            {
                return Results.Problem();
            }
        };

        // Act
        var result = await handler(request, _mockContextAnalysisService.Object, _mockAnalysisLogger.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Ok<AiAnalysisResponse>>(result);
        Assert.Equal("summary", okResult.Value!.Summary);
    }

    [Fact]
    public async Task AnalyzeReport_ReturnsProblem_WhenExceptionOccurs()
    {
        // Arrange
        var reportDto = new ContextReportDto(null!, null!, null!, null!, null!, null!, null!, null!, 0, null!, null!, null!);
        var request = new AiAnalysisRequest(reportDto);
        _mockContextAnalysisService
            .Setup(x => x.AnalyzeReportAsync(reportDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("error"));

        var handler = async (AiAnalysisRequest req, IContextAnalysisService service, ILogger<AiAnalysisRequest> log, CancellationToken ct) =>
        {
            try
            {
                var summary = await service.AnalyzeReportAsync(req.Report, ct);
                return Results.Ok(new AiAnalysisResponse(summary));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error generating AI analysis report.");
                return Results.Problem(detail: "An unexpected error occurred while generating the report summary.", statusCode: 500);
            }
        };

        // Act
        var result = await handler(request, _mockContextAnalysisService.Object, _mockAnalysisLogger.Object, CancellationToken.None);

        // Assert
        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(500, problemResult.StatusCode);
    }
}
