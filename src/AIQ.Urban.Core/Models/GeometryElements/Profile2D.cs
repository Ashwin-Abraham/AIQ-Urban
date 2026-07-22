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
    public Polycurve2D Outline { get; }
    public List<Polycurve2D> Holes { get; }

    public Profile2D(Polycurve2D outline, List<Polycurve2D> holes, double tolerance = 1e-5)
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
        Polycurve2D? outline = null;
        var holes = new List<Polycurve2D>();

        profileBrep.Faces[0].TryGetPlane(out var plane);
        var faceTransform = Transform.PlaneToPlane(Plane.WorldXY, plane);

        foreach (var loop in profileBrep.Faces[0].Loops)
        {
            if(!loop.To2dCurve().TryGetPolyline(out var pline))
                return null;

            if (loop.LoopType == BrepLoopType.Outer)
            {
                pline.Transform(faceTransform);
                outline = new(pline.ToPolylineCurve());
            }

            if (loop.LoopType == BrepLoopType.Inner)
            {
                pline.Transform(faceTransform);
                holes.Add(new(pline.ToPolylineCurve()));
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
