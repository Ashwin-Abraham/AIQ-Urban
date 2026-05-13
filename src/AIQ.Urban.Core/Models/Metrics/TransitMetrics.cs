using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.Models.Metrics;

/// <summary>Aggregate metrics for a group of transits sharing the same <see cref="TransitType"/> and width.</summary>
public class TransitMetrics : IMetrics
{
    /// <summary>Type of transit corridor for this group.</summary>
    public TransitType Type { get; }

    /// <summary>Right-of-way width shared by all transits in this group.</summary>
    public double Width { get; }

    /// <summary>Sum of centerline lengths of all transits in the group.</summary>
    public double RunningLength { get; }

    /// <summary>Total right-of-way area (= RunningLength × Width).</summary>
    public double Area { get; }

    /// <summary>Number of transit elements in this group.</summary>
    public int Count { get; }

    /// <summary>Create per-transit metrics from a single transit element.</summary>
    public TransitMetrics(Transit transit)
    {
        Type = transit.TransitType;
        Width = transit.Width;
        RunningLength = transit.Centerline.Geometry.GetLength();
        Area = RunningLength * transit.Width;
        Count = 1;
    }

    private TransitMetrics(
        TransitType type,
        double width,
        double runningLength,
        int count)
    {
        Type = type;
        Width = width;
        RunningLength = runningLength;
        Area = runningLength * width;
        Count = count;
    }

    /// <summary>Aggregate a group of per-transit metrics into a single group metric by summing RunningLength and Count.</summary>
    public static TransitMetrics Sum(IEnumerable<TransitMetrics> metrics)
    {
        var list = metrics.ToList();
        var first = list[0];
        return new TransitMetrics(
            first.Type,
            first.Width,
            list.Sum(m => m.RunningLength),
            list.Sum(m => m.Count));
    }
}
