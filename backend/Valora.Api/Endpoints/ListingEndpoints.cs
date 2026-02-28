using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;

namespace Valora.Api.Endpoints;

public static class ListingEndpoints
{
    public static RouteGroupBuilder MapListingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/listings")
            .RequireAuthorization()
            .WithTags("Listings");

        group.MapGet("/{id}", GetListingDetail);

        return group;
    }

    private static async Task<IResult> GetListingDetail(
        Guid id,
        IListingService service,
        CancellationToken ct)
    {
        var result = await service.GetListingDetailAsync(id, ct);
        return Results.Ok(result);
    }
}
