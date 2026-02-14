using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Valora.UnitTests.Persistence;

public class ValoraDbContextComparerTests
{
    [Fact]
    public void DictionaryComparer_Equality_Works()
    {
        var d1 = new Dictionary<string, string> { { "A", "1" }, { "B", "2" } };
        var d2 = new Dictionary<string, string> { { "B", "2" }, { "A", "1" } };
        var d3 = new Dictionary<string, string> { { "A", "1" }, { "B", "3" } };
        var d4 = new Dictionary<string, string> { { "A", "1" } };

        // Test logic from ValoraDbContext
        bool Equals(Dictionary<string, string>? c1, Dictionary<string, string>? c2) =>
            c1!.Count == c2!.Count && c1.All(kv => c2.ContainsKey(kv.Key) && c2[kv.Key] == kv.Value);

        Assert.True(Equals(d1, d2));
        Assert.False(Equals(d1, d3));
        Assert.False(Equals(d1, d4));
    }

    [Fact]
    public void DictionaryComparer_HashCode_IsOrderIndependent()
    {
        var d1 = new Dictionary<string, string> { { "A", "1" }, { "B", "2" } };
        var d2 = new Dictionary<string, string> { { "B", "2" }, { "A", "1" } };

        int GetHashCode(Dictionary<string, string>? c) =>
            c!.Aggregate(0, (a, v) => a ^ HashCode.Combine(v.Key, v.Value));

        Assert.Equal(GetHashCode(d1), GetHashCode(d2));
    }

    [Fact]
    public void DictionaryComparer_Clone_Works()
    {
        var original = new Dictionary<string, string> { { "A", "1" } };
        var clone = original.ToDictionary(entry => entry.Key, entry => entry.Value);

        Assert.NotSame(original, clone);
        Assert.Equal(original, clone);
    }
}
