using Microsoft.AspNetCore.Mvc;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class SupportEndpoints
{
    public static void MapSupportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/support")
            .RequireRateLimiting("fixed");

        group.MapGet("/status", (IConfiguration config) =>
        {
            var isSupportActive = config.GetValue<bool>("Support:IsActive", true);
            var supportMessage = config.GetValue<string>("Support:Message", "Our support team is available 24/7");
            var statusPageUrl = config.GetValue<string>("Support:StatusPageUrl", "https://status.valora.nl");
            var contactEmail = config.GetValue<string>("Support:ContactEmail", "support@valora.nl");

            return Results.Ok(new SupportStatusDto(
                isSupportActive,
                supportMessage,
                statusPageUrl,
                contactEmail
            ));
        });
    }
}
