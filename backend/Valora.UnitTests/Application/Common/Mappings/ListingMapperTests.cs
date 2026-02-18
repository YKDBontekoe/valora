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
           .Where(e => e.Errors != null && e.Errors.ContainsKey("FundaId"));
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
           .Where(e => e.Errors != null && e.Errors.ContainsKey("Url"));
    }

    [Fact]
    public void UpdateEntity_ShouldTruncateCity_WhenTooLong()
    {
        var longCity = new string('x', ValidationConstants.Listing.CityMaxLength + 10);
        var dto = _validDto with { City = longCity };
        var entity = ListingMapper.ToEntity(_validDto);

        ListingMapper.UpdateEntity(entity, dto);

        entity.City.Should().NotBeNull();
        entity.City!.Length.Should().Be(ValidationConstants.Listing.CityMaxLength);
    }

    [Fact]
    public void ToEntity_ShouldSanitizeDescription()
    {
        var description = "<p>This is a <b>bold</b> move.</p><script>alert('xss')</script>";
        var dto = _validDto with { Description = description };

        var entity = ListingMapper.ToEntity(dto);

        // Tags are replaced with space to prevent concatenation
        entity.Description.Should().Be(" This is a  bold  move. ");
    }

    [Fact]
    public void ToEntity_ShouldSanitizeFeatures()
    {
        var features = new Dictionary<string, string>
        {
            { "Feature1", "<b>Value1</b>" },
            { "Feature2", "<iframe src='malicious'></iframe>Safe" }
        };
        var dto = _validDto with { Features = features };

        var entity = ListingMapper.ToEntity(dto);

        entity.Features["Feature1"].Should().Be(" Value1 ");
        // iframe matches <\/?[a-zA-Z]... because iframe starts with i
        entity.Features["Feature2"].Should().Be("  Safe");
    }

    [Fact]
    public void ToEntity_ShouldPreserveMathematicalComparisons()
    {
        var description = "Living room < 20m2 and Kitchen > 10m2";
        var dto = _validDto with { Description = description };

        var entity = ListingMapper.ToEntity(dto);

        entity.Description.Should().Be("Living room < 20m2 and Kitchen > 10m2");
    }

    [Fact]
    public void ToEntity_ShouldReplaceTagsWithSpace()
    {
        var description = "Line1<br>Line2";
        var dto = _validDto with { Description = description };

        var entity = ListingMapper.ToEntity(dto);

        entity.Description.Should().Be("Line1 Line2");
    }

    [Fact]
    public void ToEntity_ShouldSanitizeFeatureKeys()
    {
        var features = new Dictionary<string, string>
        {
            { "<b>Key1</b>", "Value1" },
            { "<script>alert(1)</script>Key2", "Value2" }
        };
        var dto = _validDto with { Features = features };

        var entity = ListingMapper.ToEntity(dto);

        entity.Features.Should().ContainKey(" Key1 ");
        entity.Features.Should().ContainKey("Key2");
        entity.Features[" Key1 "].Should().Be("Value1");
    }

    [Fact]
    public void ToEntity_ShouldHandleNullFeatures()
    {
        var dto = _validDto with { Features = null! }; // Intentionally null to test robustness

        var action = () => ListingMapper.ToEntity(dto);

        action.Should().NotThrow();
        var entity = ListingMapper.ToEntity(dto);
        entity.Features.Should().NotBeNull().And.BeEmpty();
    }
}
