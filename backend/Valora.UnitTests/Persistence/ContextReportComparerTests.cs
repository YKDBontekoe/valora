using Valora.Domain.Models;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.UnitTests.Persistence;

public class ContextReportComparerTests
{
    private ContextReportModel CreateSample(double score = 80.0)
    {
        return new ContextReportModel(
            new ResolvedLocationModel("Amsterdam", "Damrak 1", 52.37, 4.89, 121000, 487000, "0363", "Amsterdam", "d", "d", "n", "n", "1012LG"),
            [new ContextMetricModel("m1", "L1", 10, "u", 90, "S1")],
            [], [], [], [], [], [],
            score,
            new Dictionary<string, double> { { "K1", 90.0 } },
            [new SourceAttributionModel("S1", "url", "L", new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero))],
            ["W1"]
        );
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var report = CreateSample();
        Assert.True(ContextReportComparer.Equals(report, report));
    }

    [Fact]
    public void Equals_BothNull_ReturnsTrue()
    {
        Assert.True(ContextReportComparer.Equals(null, null));
    }

    [Fact]
    public void Equals_OneNull_ReturnsFalse()
    {
        Assert.False(ContextReportComparer.Equals(CreateSample(), null));
        Assert.False(ContextReportComparer.Equals(null, CreateSample()));
    }

    [Fact]
    public void Equals_IdenticalContent_ReturnsTrue()
    {
        var r1 = CreateSample();
        var r2 = CreateSample();
        Assert.True(ContextReportComparer.Equals(r1, r2));
    }

    [Fact]
    public void Equals_DifferentScore_ReturnsFalse()
    {
        Assert.False(ContextReportComparer.Equals(CreateSample(80.0), CreateSample(81.0)));
    }

    [Fact]
    public void Equals_DifferentLocation_ReturnsFalse()
    {
        var r1 = CreateSample();
        var r2 = r1 with { Location = r1.Location with { Query = "Rotterdam" } };
        Assert.False(ContextReportComparer.Equals(r1, r2));
    }

    [Fact]
    public void Equals_DifferentMetrics_ReturnsFalse()
    {
        var r1 = CreateSample();
        var r2 = r1 with { SocialMetrics = [new ContextMetricModel("m2", "L2", 10, "u", 90, "S1")] };
        Assert.False(ContextReportComparer.Equals(r1, r2));

        var r3 = r1 with { SocialMetrics = [] };
        Assert.False(ContextReportComparer.Equals(r1, r3));
    }

    [Fact]
    public void Equals_DifferentDictionary_ReturnsFalse()
    {
        var r1 = CreateSample();
        var r2 = r1 with { CategoryScores = new Dictionary<string, double> { { "K1", 91.0 } } };
        Assert.False(ContextReportComparer.Equals(r1, r2));

        var r3 = r1 with { CategoryScores = new Dictionary<string, double> { { "K2", 90.0 } } };
        Assert.False(ContextReportComparer.Equals(r1, r3));

        var r4 = r1 with { CategoryScores = new Dictionary<string, double>() };
        Assert.False(ContextReportComparer.Equals(r1, r4));
    }

    [Fact]
    public void Equals_DifferentSourcesOrWarnings_ReturnsFalse()
    {
        var r1 = CreateSample();
        var r2 = r1 with { Warnings = ["W2"] };
        Assert.False(ContextReportComparer.Equals(r1, r2));
    }

    [Fact]
    public void GetHashCode_IdenticalContent_ReturnsSameHash()
    {
        var r1 = CreateSample();
        var r2 = CreateSample();
        Assert.Equal(ContextReportComparer.GetHashCode(r1), ContextReportComparer.GetHashCode(r2));
    }

    [Fact]
    public void GetHashCode_Null_ReturnsZero()
    {
        Assert.Equal(0, ContextReportComparer.GetHashCode(null));
    }

    [Fact]
    public void GetHashCode_DictionaryOrderIndependent_ReturnsSameHash()
    {
        var r1 = CreateSample() with { CategoryScores = new Dictionary<string, double> { { "A", 1 }, { "B", 2 } } };
        var r2 = CreateSample() with { CategoryScores = new Dictionary<string, double> { { "B", 2 }, { "A", 1 } } };

        Assert.Equal(ContextReportComparer.GetHashCode(r1), ContextReportComparer.GetHashCode(r2));
    }

    [Fact]
    public void Clone_Null_ReturnsNull()
    {
        Assert.Null(ContextReportComparer.Clone(null));
    }

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        var original = CreateSample();
        var clone = ContextReportComparer.Clone(original);

        Assert.NotNull(clone);
        Assert.NotSame(original, clone);
        Assert.True(ContextReportComparer.Equals(original, clone));

        // Verify it is a deep copy of collections
        Assert.NotSame(original.SocialMetrics, clone.SocialMetrics);
        Assert.NotSame(original.CategoryScores, clone.CategoryScores);
        Assert.NotSame(original.Sources, clone.Sources);
        Assert.NotSame(original.Warnings, clone.Warnings);

        // Modifying clone shouldn't affect original
        clone.SocialMetrics.Add(new ContextMetricModel("new", "L", 1, "u", 1, "S"));
        Assert.NotEqual(original.SocialMetrics.Count, clone.SocialMetrics.Count);
    }

    [Fact]
    public void Equals_EmptyCollections_ReturnsTrue()
    {
        var r1 = new ContextReportModel { Location = new ResolvedLocationModel() };
        var r2 = new ContextReportModel { Location = new ResolvedLocationModel() };
        Assert.True(ContextReportComparer.Equals(r1, r2));
    }

    [Fact]
    public void GetHashCode_DifferentCollections_ReturnsDifferentHash()
    {
        var r1 = CreateSample();
        var r2 = r1 with { SocialMetrics = [] };
        Assert.NotEqual(ContextReportComparer.GetHashCode(r1), ContextReportComparer.GetHashCode(r2));
    }
}
