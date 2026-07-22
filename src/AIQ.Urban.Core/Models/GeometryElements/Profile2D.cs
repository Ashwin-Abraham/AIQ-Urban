using AIQ.Urban.Core.Extensions;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.GeometryElements;
public class Profile2D : IGeometryElement<Brep>
{
    public Guid Id
    {
        get => Outline.Id;
        set => Outline.Id = value;
    }
    public Brep Geometry => GetBrep();
    public PolyCurve2D Outline { get; }
    public List<PolyCurve2D> Holes { get; }

    public Profile2D(Polygon2D outline, List<Polygon2D> holes)
    {
        Outline = new PolyCurve2D(outline.Geometry);
        Holes = holes.Select(x => new PolyCurve2D(x.Geometry)).ToList();
    }

    public Profile2D(PolyCurve2D outline, List<PolyCurve2D> holes)
    {
        Outline = outline;
        Holes = holes;
    }

    public Brep GetBrep(double tolerance = 1e-5)
    {
        Brep[] profileBreps;
        List<Curve> curves = [Outline.Geometry];
        if (Holes.Count > 0)
        {
            var holeCurves = Holes.Select(x => x.Geometry).ToList();
            var holesInOutline = holeCurves.SelectMany(x => Curve.CreateBooleanIntersection(Outline.Geometry, x, tolerance)).ToArray();
            curves.AddRange(holesInOutline);
        }
        
        profileBreps = Brep.CreatePlanarBreps(curves, tolerance);

        return profileBreps?.First();
    }

    public static Profile2D? Create(Brep profileBrep)
    {
        PolyCurve2D? outline = null;
        var holes = new List<PolyCurve2D>();

        profileBrep.Faces[0].TryGetPlane(out var plane);
        var faceTransform = Transform.PlaneToPlane(Plane.WorldXY, plane);

        foreach (var loop in profileBrep.Faces[0].Loops)
        {
            var polycurve = PolyCurve2D.Create(loop.To2dCurve());

            if (polycurve == null)
                continue;
            
            if (loop.LoopType == BrepLoopType.Outer)
            {
                polycurve.Geometry.Transform(faceTransform);
                outline = polycurve;
            }

            if (loop.LoopType == BrepLoopType.Inner)
            {
                polycurve.Geometry.Transform(faceTransform);
                holes.Add(polycurve);
            }
        }

        if (outline == null)
            return null;

        return new Profile2D(outline, holes);
    }

    public Point2d Centroid()
    {
        return AreaMassProperties.Compute(Geometry).Centroid.ToPoint2d();
    }
}
