using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories.Models;

public class BuildingInstance(BuildingDefinition definition, TransformMatrix transform)
{
    public BuildingDefinition Definition { get; } = definition;
    public TransformMatrix Transform { get; } = transform;
    public PolyCurve GetBuildingFootprint()
        => Definition.Footprint.Transform(Transform);
}
