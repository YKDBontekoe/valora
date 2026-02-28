using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Moq;
using Valora.Api.Endpoints;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.UnitTests.Api.Endpoints;

public class ListingEndpointsTests
{
    private readonly Mock<IListingService> _serviceMock;

    public ListingEndpointsTests()
    {
        _serviceMock = new Mock<IListingService>();
    }

    [Fact]
    public async Task GetListingDetail_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ListingDetailDto(
            id, "Address", null, null, 100000, 2, 1, 100, null, null, null, null, null, null, null, null, null, [], null, null, 8.5, null, null, null, null, null
        );

        _serviceMock.Setup(s => s.GetListingDetailAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        // Invoke the private static method using reflection to test endpoint logic
        var method = typeof(ListingEndpoints).GetMethod("GetListingDetail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (IResult)await (Task<IResult>)method!.Invoke(null, new object[] { id, _serviceMock.Object, CancellationToken.None })!;

        // Assert
        var okResult = Assert.IsType<Ok<ListingDetailDto>>(result);
        okResult.Value.Should().BeEquivalentTo(dto);
    }
}
