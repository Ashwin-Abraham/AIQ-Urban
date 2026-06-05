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
    
    //TODO: Use Polygon not Polygon2D because we dont want to track the ID
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

        var length = profileBreps.Count();

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
            if (loop.LoopType == BrepLoopType.Outer)
            {
                var curve = loop.To2dCurve();
                curve.Transform(faceTransform);
                outline = new(curve);
            }

            if (loop.LoopType == BrepLoopType.Inner)
            {
                var curve = loop.To2dCurve();
                curve.Transform(faceTransform);
                holes.Add(new(curve));
            }
        }

        if (outline == null)
            return null;

        return new Profile2D(profileBrep, outline, holes);
    }
}
