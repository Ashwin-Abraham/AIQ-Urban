using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.GeometryElements;
public class Profile2D : IGeometryElement<Brep>
{
    public Guid Id { get; set; }
    public Brep Geometry { get; }
    public Polygon2D Outline { get; }
    public List<Polygon2D> Holes { get; }

    public Profile2D(Polygon2D outline, List<Polygon2D> holes, double tolerance = 1e-5)
    {
        var curves = holes.Select(x => x.Geometry).Append(outline.Geometry).ToList();

        var profileBreps = Brep.CreatePlanarBreps(curves, tolerance);
        
        if (profileBreps.Length != 1)
            throw new ArgumentException("Invalid Profile2D");

        var profileBrep = profileBreps[0];
        Geometry = profileBrep;
        Holes = [];
        Outline = null!;

        foreach (var loop in profileBrep.Faces[0].Loops)
        {
            if (loop.LoopType == BrepLoopType.Outer)
            {
                Outline = new(loop.To2dCurve());
            }

            if (loop.LoopType == BrepLoopType.Inner)
            {
                Holes.Add(new(loop.To2dCurve()));
            }
        }
    }
}
