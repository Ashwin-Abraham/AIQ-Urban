using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories.Models;

public class BuildingInstance(BuildingDefinition definition, TransformMatrix transform)
{
    public BuildingDefinition Definition { get; } = definition;
    public TransformMatrix Transform { get; } = transform;
    public Building GetBuilding()
        => Definition.Building.Transformed(Transform);

    public Polygon GetConvexHull()
    {
        var curve = Definition.FootprintConvexHull.Transform(Transform);
        return Create.Polygon(curve.Vertices());
    }
        
}
