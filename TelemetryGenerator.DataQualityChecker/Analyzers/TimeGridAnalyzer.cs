using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class TimeGridAnalyzer
{
    private static readonly TimeSpan AllowedStepDeviation = TimeSpan.FromMinutes(2);

    public AnalyzerResult Analyze(IReadOnlyCollection<TelemetryRecord> records)
    {
        var result = new AnalyzerResult("TimeGridAnalyzer");

        if (!records.Any())
        {
            result.AddIssue(IssueSeverity.Critical, "Dataset is empty");
            return result;
        }

        var allSteps = new List<TimeSpan>();
        var largestGaps = new List<TimeSpan>();
        var excessiveDuplicates = 0;

        foreach (var group in records.GroupBy(r => r.WorkStationId))
        {
            var ordered = group.OrderBy(r => r.Timestamp).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var step = ordered[i].Timestamp - ordered[i - 1].Timestamp;
                allSteps.Add(step);
                if (step == TimeSpan.Zero)
                {
                    excessiveDuplicates++;
                }
                if (largestGaps.Count < 10 || step > largestGaps.Min())
                {
                    largestGaps.Add(step);
                    largestGaps = largestGaps
                        .OrderByDescending(gap => gap)
                        .Take(10)
                        .ToList();
                }
            }
        }

        if (allSteps.Count == 0)
        {
            result.AddIssue(IssueSeverity.Warning, "Only a single telemetry sample exists per workstation");
            return result;
        }

        var meanStep = TimeSpan.FromMilliseconds(allSteps.Average(ts => ts.TotalMilliseconds));
        var medianStep = Median(allSteps);
        var largeGaps = allSteps.Count(step => step > AllowedStepDeviation);

        if (largeGaps > 0)
        {
            result.AddIssue(IssueSeverity.Warning, $"Detected {largeGaps} intervals exceeding {AllowedStepDeviation.TotalMinutes} minutes");
        }

        if (excessiveDuplicates > records.Count / 10)
        {
            result.AddIssue(IssueSeverity.Warning, "Too many duplicate timestamps detected");
        }

        result.AddMetric("meanStepSeconds", Math.Round(meanStep.TotalSeconds, 2));
        result.AddMetric("medianStepSeconds", Math.Round(medianStep.TotalSeconds, 2));
        result.AddMetric("gapsOverThreshold", largeGaps);
        result.AddMetric("topGaps", largestGaps.Select(g => g.TotalSeconds).ToArray());

        return result;
    }

    private static TimeSpan Median(IReadOnlyList<TimeSpan> spans)
    {
        var ordered = spans.Select(s => s.TotalMilliseconds).OrderBy(x => x).ToList();
        if (ordered.Count == 0)
        {
            return TimeSpan.Zero;
        }

        var mid = ordered.Count / 2;
        if (ordered.Count % 2 == 0)
        {
            return TimeSpan.FromMilliseconds((ordered[mid - 1] + ordered[mid]) / 2);
        }

        return TimeSpan.FromMilliseconds(ordered[mid]);
    }
}
