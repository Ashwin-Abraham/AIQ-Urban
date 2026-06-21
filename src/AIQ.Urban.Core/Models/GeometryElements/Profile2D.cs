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
    public Brep Geometry { get; }
    public Polygon2D Outline { get; }
    public List<Polygon2D> Holes { get; }

    public Profile2D(Brep geometry, Polygon2D outline, List<Polygon2D> holes, double tolerance = 1e-5)
    {
        Geometry = geometry;
        Outline = outline;
        Holes = holes;
    }

    public static Profile2D? Create(Polygon2D outline, List<Polygon2D> holes, double tolerance = 1e-5)
    {
        var curves = holes.Select(x => x.Geometry).Append(outline.Geometry).ToList();
        var profileBreps = Brep.CreatePlanarBreps(curves, tolerance);

        if (profileBreps.Length != 1 || profileBreps[0].Faces.Count != 1)
            throw new ArgumentException("Invalid Profile2D");

        var profileBrep = profileBreps[0];

        return Create(profileBrep);
    }

    public static Profile2D? Create(Brep profileBrep)
    {
        Polygon2D? outline = null;
        var holes = new List<Polygon2D>();

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

        return new Profile2D(profileBrep, outline, holes);
    }

    public Point2d Centroid()
    {
        return AreaMassProperties.Compute(Geometry).Centroid.ToPoint2d();
    }
}
