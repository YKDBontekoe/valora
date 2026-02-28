using Microsoft.EntityFrameworkCore.ChangeTracking;
using Valora.Domain.Models;

namespace Valora.Infrastructure.Persistence;

public static class ValueComparers
{
    // Suppress null warnings with ! because these properties are initialized to empty collections
    // and JsonHelper ensures non-null returns from DB.

    public static readonly ValueComparer<List<string>> StringListComparer = new(
        (c1, c2) => c1!.SequenceEqual(c2!),
        c => c!.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
        c => c!.ToList());

    public static readonly ValueComparer<Dictionary<string, string>> DictionaryComparer = new(
        (c1, c2) => c1!.Count == c2!.Count && !c1.Except(c2).Any(),
        // Order by key to ensure GetHashCode is consistent regardless of insertion order
        c => c!.OrderBy(kv => kv.Key).Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
        c => c!.ToDictionary(entry => entry.Key, entry => entry.Value));

    public static readonly ValueComparer<Dictionary<string, List<string>>> DictionaryListComparer = new(
        (c1, c2) => c1!.Count == c2!.Count && c1.All(kv => c2.ContainsKey(kv.Key) && kv.Value.SequenceEqual(c2[kv.Key])),
        c => c!.OrderBy(kv => kv.Key).Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.Aggregate(0, (aa, vv) => HashCode.Combine(aa, vv.GetHashCode())))),
        c => c!.ToDictionary(entry => entry.Key, entry => entry.Value.ToList()));

    public static readonly ValueComparer<List<DateTime>> DateListComparer = new(
        (c1, c2) => c1!.SequenceEqual(c2!),
        c => c!.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
        c => c!.ToList());

    public static readonly ValueComparer<ContextReportModel?> ContextReportComparer = new(
        (c1, c2) => JsonHelper.Serialize(c1) == JsonHelper.Serialize(c2),
        c => c == null ? 0 : JsonHelper.Serialize(c).GetHashCode(),
        c => c == null ? null : JsonHelper.Deserialize<ContextReportModel>(JsonHelper.Serialize(c))!);
}
