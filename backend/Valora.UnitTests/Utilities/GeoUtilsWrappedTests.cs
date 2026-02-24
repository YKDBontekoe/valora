using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Utilities;
using Xunit;

namespace Valora.UnitTests.Utilities;

public class GeoUtilsWrappedTests
{
    [Fact]
    public void ValidateBoundingBox_ShouldPass_WhenCrossingDateLine()
    {
        // MinLon=179.9, MaxLon=-179.9. Span is 0.2 degrees.
        GeoUtils.ValidateBoundingBox(10, 179.9, 10.1, -179.9);
    }

    [Fact]
    public void ValidateBoundingBox_ShouldThrow_WhenWrappedWithInvalidValues()
    {
        // 359.9 is outside -180..180
        Assert.Throws<ValidationException>(() => GeoUtils.ValidateBoundingBox(10, 359.9, 10.1, 0.1));
    }

    [Fact]
    public void ValidateBoundingBox_ShouldThrow_WhenSpanTooLarge_CrossingDateLine()
    {
        // MinLon=179, MaxLon=-179. Span is 2 degrees. Max is 0.5.
        Assert.Throws<ValidationException>(() => GeoUtils.ValidateBoundingBox(10, 179, 10.1, -179));
    }
}
