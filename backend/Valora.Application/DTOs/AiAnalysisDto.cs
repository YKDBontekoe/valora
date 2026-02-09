namespace Valora.Application.DTOs;

public sealed record AiAnalysisRequest(ContextReportDto Report);

public sealed record AiAnalysisResponse(string Summary);
