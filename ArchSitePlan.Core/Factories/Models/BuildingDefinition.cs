using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories.Models;

public class BuildingDefinition(
    Building buildingNearOrigin,
    double frontageWidth)
{
    public Building Building { get; } = buildingNearOrigin;
    public double FrontageWidth { get; } = frontageWidth;
    public TransformMatrix Transform { get; } = TransformMatrix.Identity;
    public Polyline FootprintConvexHull {get;} = CalculateConvexHull(buildingNearOrigin);

    public BuildingInstance CreateInstance(TransformMatrix transform)
    {
        return new BuildingInstance(this, transform);
    }
    private static Polyline CalculateConvexHull(Building building)
    {
        var points = building.Envelope.Vertices
            .Select(x => x.ProjectToXY())
            .ToList();

        return Compute.ConvexHull(points);
    }

    public static BuildingDefinition FromFootprint(
        PolyCurve buildingFootprint,
        int floorCount,
        double floorHeight,
        double frontageWidth)
    {
        var mesh = BuildingLayoutFactoryBase.CreateEnvelopeMesh(
            buildingFootprint,
            floorCount,
            floorHeight);

        var typicalFloorplate = Create.PlanarSurface(buildingFootprint);
        List<PlanarSurface> floorPlates = new();

        for (double i = 0; i < floorCount; i++)
            floorPlates.Add(typicalFloorplate.Translate(Vector.ZAxis * floorHeight * i));

        var building = new Building(
            mesh,
            floorPlates,
            floorHeight);

        return new(building, frontageWidth);
    }
}
