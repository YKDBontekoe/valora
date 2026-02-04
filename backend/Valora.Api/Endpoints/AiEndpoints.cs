using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class AiEndpoints
{
    public static void MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai").RequireAuthorization();

        group.MapPost("/chat", async (
            [FromBody] AiChatRequest request,
            IAiService aiService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return Results.BadRequest(new { error = "Prompt is required" });
            }

            try
            {
                var response = await aiService.ChatAsync(request.Prompt, request.Model, ct);
                return Results.Ok(new { response });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
    }
}
