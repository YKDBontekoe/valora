using System.Diagnostics;
using Valora.Application.DTOs;
using Xunit;


namespace Valora.UnitTests.Enrichment;

public class ContextScoreCalculatorBenchmarks
{



    [Fact]
    public void Compare_AverageScore_Performance()
    {
        var metrics = new List<ContextMetricDto>();
        for (int i = 0; i < 1000; i++)
        {
            metrics.Add(new ContextMetricDto(
                "key" + i,
                "label" + i,
                (double)i,
                "unit",
                i % 2 == 0 ? (double)i : null,
                "source"
            ));
        }

        const int iterations = 10000;

        // Warm up
        for (int i = 0; i < 100; i++)
        {
            OldAverageScore(metrics);
            NewAverageScore(metrics);
        }

        // Measure Old
        var swOld = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            OldAverageScore(metrics);
        }
        swOld.Stop();

        // Measure New
        var swNew = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            NewAverageScore(metrics);
        }
        swNew.Stop();

        Console.WriteLine($"Old Implementation: {swOld.Elapsed.TotalMilliseconds:F2}ms");
        Console.WriteLine($"New Implementation: {swNew.Elapsed.TotalMilliseconds:F2}ms");
        Console.WriteLine($"Improvement: {(1 - swNew.Elapsed.TotalMilliseconds / swOld.Elapsed.TotalMilliseconds) * 100:F2}%");
    }

    private static double? OldAverageScore(IReadOnlyList<ContextMetricDto> metrics)
    {
        var values = metrics.Where(m => m.Score.HasValue).Select(m => m.Score!.Value).ToList();
        if (values.Count == 0)
        {
            return null;
        }

        return values.Average();
    }

    private static double? NewAverageScore(IReadOnlyList<ContextMetricDto> metrics)
    {
        double sum = 0;
        int count = 0;
        foreach (var m in metrics)
        {
            if (m.Score.HasValue)
            {
                sum += m.Score.Value;
                count++;
            }
        }

        if (count == 0)
        {
            return null;
        }

        return sum / count;
    }
}
