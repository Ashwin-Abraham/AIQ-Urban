using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.Models.Metrics;

/// <summary>Metrics for one source <see cref="Transit"/>.</summary>
public class TransitMetrics : IMetrics
{
    /// <summary>ID of the source Transit.</summary>
    public Guid SourceElementId { get; }

    /// <summary>Type of the source transit corridor.</summary>
    public TransitType Type { get; }

    /// <summary>Right-of-way width of the source transit corridor.</summary>
    public double Width { get; }

    /// <summary>Centerline length of the source transit corridor.</summary>
    public double RunningLength { get; }

    /// <summary>Total right-of-way area (= RunningLength × Width).</summary>
    public double Area { get; }

    /// <summary>Create metrics from a single transit element.</summary>
    public TransitMetrics(Transit transit)
    {
        SourceElementId = transit.Id;
        Type = transit.TransitType;
        Width = transit.Width;
        RunningLength = transit.Centerline.Geometry.GetLength();
        Area = RunningLength * transit.Width;
    }
}
