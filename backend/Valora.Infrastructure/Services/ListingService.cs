using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Services;

public class ListingService : IListingService
{
    private readonly IListingRepository _repository;

    public ListingService(IListingRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedList<ListingDto>> GetListingsAsync(ListingFilterDto filter, CancellationToken cancellationToken = default)
    {
        var paginatedList = await _repository.GetAllAsync(filter, cancellationToken);
        var dtos = paginatedList.Items.Select(MapToDto).ToList();

        return new PaginatedList<ListingDto>(dtos, paginatedList.TotalCount, paginatedList.PageIndex, filter.PageSize ?? 10);
    }

    public async Task<ListingDto?> GetListingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _repository.GetByIdAsync(id, cancellationToken);
        return listing is null ? null : MapToDto(listing);
    }

    private static ListingDto MapToDto(Listing l)
    {
        return new ListingDto(
            l.Id, l.FundaId, l.Address, l.City, l.PostalCode, l.Price,
            l.Bedrooms, l.Bathrooms, l.LivingAreaM2, l.PlotAreaM2,
            l.PropertyType, l.Status, l.Url, l.ImageUrl, l.ListedDate, l.CreatedAt
        );
    }
}
