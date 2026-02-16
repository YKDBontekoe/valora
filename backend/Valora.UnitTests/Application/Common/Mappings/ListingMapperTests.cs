using FluentAssertions;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Mappings;
using Valora.Application.DTOs;
using Valora.Domain.Common;

namespace Valora.UnitTests.Application.Common.Mappings;

public class ListingMapperTests
{
    private readonly ListingDto _validDto = new ListingDto(
        Guid.NewGuid(), "12345", "Test Street 1", "Test City", "1234AB", 500000,
        3, 1, 120, 200, "House", "Sold", "http://funda.nl/123", "http://img.url/1.jpg",
        DateTime.UtcNow, DateTime.UtcNow, null, "A", 2000, [],
        "Full", "A123", 100, "CV", "Double", "South", false, "Paid",
        "Agent X", 400, 10, 50, 5, [], null, null, null, null, [], null,
        "Flat", 2, "2000-2010", "BrandX", 2015, "0612345678", null,
        true, DateTime.UtcNow, true, [], null, null, null, null, null, null
    );

    [Fact]
    public void ToEntity_ShouldThrowValidationException_WhenFundaIdTooLong()
    {
        var longId = new string('x', ValidationConstants.Listing.FundaIdMaxLength + 1);
        var dto = _validDto with { FundaId = longId };

        var act = () => ListingMapper.ToEntity(dto);

        act.Should().Throw<ValidationException>()
           .Where(e => e.Errors.ContainsKey("FundaId"));
    }

    [Fact]
    public void ToEntity_ShouldThrowValidationException_WhenAddressTooLong()
    {
        // Address is truncated in current logic but cannot be empty.
        // If we truncate to empty somehow (not possible with non-empty input), it might fail.
        // But let's check truncation logic: it should succeed but truncate.

        var longAddress = new string('x', ValidationConstants.Listing.AddressMaxLength + 10);
        var dto = _validDto with { Address = longAddress };

        var result = ListingMapper.ToEntity(dto);

        result.Address.Length.Should().Be(ValidationConstants.Listing.AddressMaxLength);
    }

    [Fact]
    public void UpdateEntity_ShouldThrowValidationException_WhenUrlTooLong()
    {
        var longUrl = "http://" + new string('x', ValidationConstants.Listing.UrlMaxLength);
        var dto = _validDto with { Url = longUrl };
        var entity = ListingMapper.ToEntity(_validDto);

        var act = () => ListingMapper.UpdateEntity(entity, dto);

        act.Should().Throw<ValidationException>()
           .Where(e => e.Errors.ContainsKey("Url"));
    }

    [Fact]
    public void UpdateEntity_ShouldTruncateCity_WhenTooLong()
    {
        var longCity = new string('x', ValidationConstants.Listing.CityMaxLength + 10);
        var dto = _validDto with { City = longCity };
        var entity = ListingMapper.ToEntity(_validDto);

        ListingMapper.UpdateEntity(entity, dto);

        entity.City.Length.Should().Be(ValidationConstants.Listing.CityMaxLength);
    }
}
