using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.GeometryElements;
public class CurveElement: IGeometryElement<Curve>
{
    public Curve Geometry { get; }
    public Guid Id { get; }

    public CurveElement(Curve geometry, Guid id)
    {
        Geometry = geometry; 
        Id = id;
    }
}