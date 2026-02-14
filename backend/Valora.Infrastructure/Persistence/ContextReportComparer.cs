using System.Collections.Generic;
using System.Linq;
using Valora.Domain.Models;

namespace Valora.Infrastructure.Persistence;

/// <summary>
/// Provides high-performance deep equality, hashing, and cloning for ContextReportModel.
/// This avoids expensive JSON serialization during EF Core change tracking.
/// </summary>
public static class ContextReportComparer
{
    public static bool Equals(ContextReportModel? r1, ContextReportModel? r2)
    {
        if (ReferenceEquals(r1, r2)) return true;
        if (r1 is null || r2 is null) return false;

        return r1.CompositeScore == r2.CompositeScore &&
               object.Equals(r1.Location, r2.Location) &&
               SequenceEqual(r1.SocialMetrics, r2.SocialMetrics) &&
               SequenceEqual(r1.CrimeMetrics, r2.CrimeMetrics) &&
               SequenceEqual(r1.DemographicsMetrics, r2.DemographicsMetrics) &&
               SequenceEqual(r1.HousingMetrics, r2.HousingMetrics) &&
               SequenceEqual(r1.MobilityMetrics, r2.MobilityMetrics) &&
               SequenceEqual(r1.AmenityMetrics, r2.AmenityMetrics) &&
               SequenceEqual(r1.EnvironmentMetrics, r2.EnvironmentMetrics) &&
               DictionaryEqual(r1.CategoryScores, r2.CategoryScores) &&
               SequenceEqual(r1.Sources, r2.Sources) &&
               SequenceEqual(r1.Warnings, r2.Warnings);
    }

    public static int GetHashCode(ContextReportModel? r)
    {
        if (r is null) return 0;

        var hash = new HashCode();
        hash.Add(r.CompositeScore);
        hash.Add(r.Location);

        AddSequenceHash(ref hash, r.SocialMetrics);
        AddSequenceHash(ref hash, r.CrimeMetrics);
        AddSequenceHash(ref hash, r.DemographicsMetrics);
        AddSequenceHash(ref hash, r.HousingMetrics);
        AddSequenceHash(ref hash, r.MobilityMetrics);
        AddSequenceHash(ref hash, r.AmenityMetrics);
        AddSequenceHash(ref hash, r.EnvironmentMetrics);

        // Use order-independent hash for dictionary
        int dictHash = 0;
        if (r.CategoryScores != null)
        {
            foreach (var kvp in r.CategoryScores)
            {
                dictHash ^= HashCode.Combine(kvp.Key, kvp.Value);
            }
        }
        hash.Add(dictHash);

        AddSequenceHash(ref hash, r.Sources);
        AddSequenceHash(ref hash, r.Warnings);

        return hash.ToHashCode();
    }

    public static ContextReportModel? Clone(ContextReportModel? r)
    {
        if (r is null) return null;

        // Records have a shallow copy with 'with', but we need deep copies for lists and dictionaries
        // Since the items are themselves immutable records or strings, ToList() and new Dictionary are sufficient.
        return r with
        {
            SocialMetrics = r.SocialMetrics?.ToList() ?? [],
            CrimeMetrics = r.CrimeMetrics?.ToList() ?? [],
            DemographicsMetrics = r.DemographicsMetrics?.ToList() ?? [],
            HousingMetrics = r.HousingMetrics?.ToList() ?? [],
            MobilityMetrics = r.MobilityMetrics?.ToList() ?? [],
            AmenityMetrics = r.AmenityMetrics?.ToList() ?? [],
            EnvironmentMetrics = r.EnvironmentMetrics?.ToList() ?? [],
            CategoryScores = r.CategoryScores != null ? new Dictionary<string, double>(r.CategoryScores) : [],
            Sources = r.Sources?.ToList() ?? [],
            Warnings = r.Warnings?.ToList() ?? []
        };
    }

    private static bool SequenceEqual<T>(List<T>? l1, List<T>? l2)
    {
        if (ReferenceEquals(l1, l2)) return true;
        if (l1 is null || l2 is null) return false;
        if (l1.Count != l2.Count) return false;
        return l1.SequenceEqual(l2);
    }

    private static bool DictionaryEqual(Dictionary<string, double>? d1, Dictionary<string, double>? d2)
    {
        if (ReferenceEquals(d1, d2)) return true;
        if (d1 is null || d2 is null) return false;
        if (d1.Count != d2.Count) return false;

        foreach (var kvp in d1)
        {
            if (!d2.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                return false;
        }
        return true;
    }

    private static void AddSequenceHash<T>(ref HashCode hash, List<T>? list)
    {
        if (list == null) return;
        foreach (var item in list)
        {
            hash.Add(item);
        }
    }
}
