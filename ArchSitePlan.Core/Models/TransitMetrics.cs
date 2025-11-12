using BH.Engine.Geometry;

namespace ArchSitePlan.Core.Models;

public record TransitMetrics(
    double RoadWidth,
    double RunningLength,
    double Area,
    Guid Id);
