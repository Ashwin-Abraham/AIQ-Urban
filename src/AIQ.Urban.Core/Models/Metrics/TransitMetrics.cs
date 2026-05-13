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

    /// <param name="type">Transit type for the group.</param>
    /// <param name="width">Common right-of-way width.</param>
    /// <param name="transits">All transits in this (Type, Width) group.</param>
    public TransitMetrics(
        TransitType type,
        double width,
        IEnumerable<Transit> transits)
    {
        Type = type;
        Width = width;
        var transitList = transits.ToList();
        RunningLength = transitList.Sum(t => t.Centerline.Geometry.GetLength());
        Area = RunningLength * width;
        Count = transitList.Count;
    }
}
