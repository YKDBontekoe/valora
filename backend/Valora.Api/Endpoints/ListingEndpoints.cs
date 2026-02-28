using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Valora.Application.Common.Interfaces.Listings;
using Valora.Application.DTOs.Listings;
using Valora.Api.Filters;

namespace Valora.Api.Endpoints;

public static class ListingEndpoints
{
    public static RouteGroupBuilder MapListingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/listings")
            .WithTags("Listings");

        group.MapGet("/search", SearchListingsHandler)
            .RequireAuthorization()
            .WithName("SearchListings");

        return group;
    }

    public static async Task<IResult> SearchListingsHandler(
        [AsParameters] ListingSearchRequest request,
        IListingService listingService,
        CancellationToken ct)
    {
        var listings = await listingService.SearchListingsAsync(request, ct);
        return Results.Ok(listings);
    }
}
