using Rhino.Geometry;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanGraph;

namespace AIQ.Urban.Core.Models.UrbanElements;

public class Building : IUrbanElement, IGraphableAsNode
{
    public Guid Id => Envelope.Id;
    public BrepElement Envelope { get; }
    public IReadOnlyList<FloorPlate> FloorPlates { get; }
    public double TypicalFloorHeight { get; }
    public int FloorCount { get; }
    public Polygon2D Footprint { get; }
    public Point3d NodePoint { get; }

    public Building(Polygon2D footprint, double typicalFloorHeight, int floorCount, Use use)
    {
        ArgumentNullException.ThrowIfNull(footprint);

        TypicalFloorHeight = typicalFloorHeight;
        FloorCount = floorCount;

        var curve = footprint.Geometry;
        curve.TryGetPlane(out var plane);
        if (plane.Normal.Z < 0)
        {
            curve = curve.DuplicateCurve() as PolylineCurve;
            curve.Reverse();
        }

        Footprint = new Polygon2D(curve);
        Envelope = ComputeEnvelope(curve, typicalFloorHeight * floorCount);
        FloorPlates = ComputeOrderedFloorPlates(curve, typicalFloorHeight, floorCount, use);
        NodePoint = GetRepresentativeCenter();
    }

    public Building(Brep envelope, double typicalFloorHeight, Use use, double tolerance = 1e-5)
    {
        TypicalFloorHeight = typicalFloorHeight;
        FloorCount = ComputeFloorCount(envelope, typicalFloorHeight);
        var (contourPlates, footprint) = ComputeContours(envelope, typicalFloorHeight, use, tolerance);
        FloorPlates = contourPlates;
        Footprint = footprint;
        Envelope = new BrepElement(envelope.DuplicateBrep(), Guid.NewGuid());
        NodePoint = GetRepresentativeCenter();
    }

    private static BrepElement ComputeEnvelope(Curve footprint, double height, double tolerance = 1e-5)
    {
        var extrusion = Extrusion.CreateExtrusion(footprint, new Vector3d(0, 0, height));
        return new BrepElement(extrusion.ToBrep().CapPlanarHoles(tolerance), Guid.NewGuid());
    }

    private static int ComputeFloorCount(Brep envelope, double typicalFloorHeight)
    {
        var bbox = envelope.GetBoundingBox(true);
        var envelopeHeight = bbox.Max.Z - bbox.Min.Z;
        return (int)Math.Floor(envelopeHeight / typicalFloorHeight);
    }

    private static (IReadOnlyList<FloorPlate> Plates, Polygon2D Footprint) ComputeContours(
        Brep envelope, double floorHeight, Use use, double tolerance)
    {
        var bbox = envelope.GetBoundingBox(true);
        var startPt = new Point3d(0, 0, bbox.Min.Z + tolerance);
        var endPt = new Point3d(0, 0, bbox.Max.Z);
        var contours = Brep.CreateContourCurves(envelope, startPt, endPt, floorHeight);

        var plates = new List<FloorPlate>();
        Polygon2D? firstFootprint = null;

        if (contours != null)
        {
            foreach (var curve in contours)
            {
                if (curve.GetLength() < tolerance)
                    continue;

                var profile = new Polygon2D(curve);

                firstFootprint ??= profile;
                plates.Add(new FloorPlate(profile, use));
            }
        }

        var ordered = plates.OrderBy(x => x.Outline.Geometry.PointAtStart.Z).ToList().AsReadOnly();
        var footprint = firstFootprint ?? throw new InvalidOperationException("No valid contours found in envelope.");
        return (ordered, footprint);
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
