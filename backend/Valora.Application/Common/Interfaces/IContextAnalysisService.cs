using Valora.Application.DTOs;
using Valora.Application.DTOs.Map;

namespace Valora.Application.Common.Interfaces;

public interface IContextAnalysisService
{
    Task<string> ChatAsync(string prompt, string? intent, CancellationToken cancellationToken);
    Task<string> AnalyzeReportAsync(ContextReportDto report, CancellationToken cancellationToken);
    Task<MapQueryResultDto> PlanMapQueryAsync(string prompt, MapBoundsDto? currentBounds, CancellationToken cancellationToken);
}
