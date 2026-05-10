using Rhino.Geometry;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models.UrbanElements;

public class Building : IUrbanElement
{
    public BrepElement Envelope { get; }
    public List<FloorPlate> FloorPlates { get; }
    public Use Use { get; }
    public double TypicalFloorHeight { get; }
    public int FloorCount { get; }

    public Building(Curve footprint, double typicalFloorHeight, int floorCount, Use use)
    {
        Use = use;
        TypicalFloorHeight = typicalFloorHeight;
        FloorCount = floorCount;

        footprint = ValidateFootprint(footprint);

        Envelope = ComputeEnvelope(footprint, typicalFloorHeight * floorCount);
        FloorPlates = ComputeFloorPlates(footprint, typicalFloorHeight, floorCount, use);
    }

    private static Curve ValidateFootprint(Curve footprint)
    {
        var validated = footprint.DuplicateCurve();
        if (!validated.TryGetPlane(out var plane))
            throw new InvalidOperationException("Footprint must be planar.");
        if (plane.Normal.Z < 0)
            validated.Reverse();
        return validated;
    }

    private static BrepElement ComputeEnvelope(Curve footprint, double height)
    {
        var extrusion = Extrusion.CreateExtrusion(footprint, new Vector3d(0, 0, height));
        return new BrepElement(extrusion.ToBrep(), Guid.Empty);
    }

    private static List<FloorPlate> ComputeFloorPlates(Curve footprint, double floorHeight, int floorCount, Use use)
    {
        var plates = new List<FloorPlate>(floorCount);

        for (int i = 0; i < floorCount; i++)
        {
            var curve = footprint.DuplicateCurve();
            var translate = Transform.Translation(0, 0, i * floorHeight);
            curve.Transform(translate);
            var profile = new Profile2D(curve);
            plates.Add(new FloorPlate(profile, use));
        }

        return plates;
    }

    public List<Guid> GetGeometryIds()
    {
        return FloorPlates
            .SelectMany(x => x.GetGeometryIds())
            .Append(Envelope.Id)
            .ToList();
    }
}
