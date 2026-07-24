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

    public double Area()
    {
        var outlineArea = Outline.Area();
        var holeArea = Holes.Sum(x => x.Area());

        return outlineArea - holeArea;
    }

    //public Brep GetBrep2(double tolerance = 1e-5)
    //{
    //    if (Holes.Count == 0)
    //        return Brep.CreatePlanarBreps(Outline.Geometry, tolerance).FirstOrDefault()!;

    //    var bounds = new BoundingBox(Outline.Points());
    //    Surface boundsSurface = new PlaneSurface(
    //        Plane.WorldXY,
    //        new Interval(bounds.Min.X, bounds.Max.X),
    //        new Interval(bounds.Min.Y, bounds.Max.Y)
    //    );

    //    var allCurves = Holes
    //        .Append(Outline)
    //        .Select(x => x.Geometry)
    //        .ToList();

    //    var result = Brep
    //        .CutUpSurface(boundsSurface, allCurves, useEdgeCurves: false, tolerance * 10)
    //        .MaxBy(x => x.GetArea());

    //    return result;
    //}

    public Brep GetBrep(double tolerance = 1e-5)
    {
        Brep[] profileBreps;
        List<Curve> curves = [Outline.Geometry];

        if (Holes.Count > 0)
        {
            var holeCurves = Holes.Select(x => x.Geometry).ToList();
            var holesInOutline = holeCurves
                .SelectMany(x => Curve.CreateBooleanIntersection(Outline.Geometry, x, tolerance))
                .Where(x => x.IsClosed)
                .ToArray();

            curves.AddRange(holesInOutline);
        }

        var crvsCopy = curves.Select(x => x.DuplicateCurve());
        profileBreps = Brep.CreatePlanarBreps(crvsCopy, tolerance);

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
