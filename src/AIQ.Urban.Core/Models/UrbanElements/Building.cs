using Rhino.Geometry;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models.UrbanElements;

public class Building : IUrbanElement
{
    public BrepElement Envelope { get; }
    public IReadOnlyList<FloorPlate> FloorPlates { get; }
    public Use Use { get; }
    public double TypicalFloorHeight { get; }
    public int FloorCount { get; }
    public Polygon2D Footprint { get; }

    public Building(Polygon2D footprint, double typicalFloorHeight, int floorCount, Use use)
    {
        ArgumentNullException.ThrowIfNull(footprint);

        Use = use;
        TypicalFloorHeight = typicalFloorHeight;
        FloorCount = floorCount;

        var curve = footprint.Geometry;
        curve.TryGetPlane(out var plane);
        if (plane.Normal.Z < 0)
        {
            curve = curve.DuplicateCurve();
            curve.Reverse();
        }

        Footprint = new Polygon2D(curve);
        Envelope = ComputeEnvelope(curve, typicalFloorHeight * floorCount);
        FloorPlates = ComputeOrderedFloorPlates(curve, typicalFloorHeight, floorCount, use);
    }

    private static BrepElement ComputeEnvelope(Curve footprint, double height, double tolerance = 1e-5)
    {
        var extrusion = Extrusion.CreateExtrusion(footprint, new Vector3d(0, 0, height));
        return new BrepElement(extrusion.ToBrep().CapPlanarHoles(tolerance), Guid.NewGuid());
    }

    private static List<FloorPlate> ComputeOrderedFloorPlates(Curve footprint, double floorHeight, int floorCount, Use use)
    {
        var plates = new List<FloorPlate>(floorCount);

        for (int i = 0; i < floorCount; i++)
        {
            var curve = footprint.DuplicateCurve();
            var translate = Transform.Translation(0, 0, i * floorHeight);
            curve.Transform(translate);
            var profile = new Polygon2D(curve);
            plates.Add(new FloorPlate(profile, use));
        }

        plates = plates.OrderBy(x => x.Outline.Geometry.PointAtStart.Z).ToList();

        return plates;
    }

    public Point3d GetRepresentativeCenter()
    {
        var lowestPlate = FloorPlates[0].Outline.Geometry;
        return AreaMassProperties.Compute(lowestPlate)!.Centroid;
    }

    public List<Guid> GetGeometryIds()
    {
        return FloorPlates
            .SelectMany(x => x.GetGeometryIds())
            .Append(Envelope.Id)
            .ToList();
    }

    internal FloorPlate GroundFloor()
    {
        return FloorPlates[0];
    }

    internal double Height()
    {
        return FloorPlates[^1].Z() - FloorPlates[0].Z();
    }
}
