using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class ListingEndpoints
{
    public static void MapListingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/listings").RequireAuthorization();

        group.MapGet("/", async ([AsParameters] ListingFilterDto filter, IListingService service, CancellationToken ct) =>
        {
            var paginatedList = await service.GetAllAsync(filter, ct);

            return Results.Ok(new
            {
                paginatedList.Items,
                paginatedList.PageIndex,
                paginatedList.TotalPages,
                paginatedList.TotalCount,
                paginatedList.HasNextPage,
                paginatedList.HasPreviousPage
            });
        });

        group.MapGet("/{id:guid}", async (Guid id, IListingService service, CancellationToken ct) =>
        {
            var listing = await service.GetByIdAsync(id, ct);
            if (listing is null) return Results.NotFound();
            return Results.Ok(listing);
        });
    }
}
