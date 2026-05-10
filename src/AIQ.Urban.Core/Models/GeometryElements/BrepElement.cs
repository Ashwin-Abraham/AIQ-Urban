using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.GeometryElements;
public class BrepElement: IGeometryElement<Brep>
{
    public Brep Geometry { get; }
    public Guid Id { get; set; }

    public BrepElement(Brep geometry, Guid id)
    {
        Geometry = geometry; 
        Id = id;
    }
}