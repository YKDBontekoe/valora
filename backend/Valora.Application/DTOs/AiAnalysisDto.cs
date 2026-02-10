using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public sealed record AiAnalysisRequest(
    [property: Required] ContextReportDto Report);

public sealed record AiAnalysisResponse(string Summary);
