using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanElements;

/// <summary>Base type for all urban model elements.</summary>
public interface IUrbanElement
{
    public List<Guid> GetGeometryIds();
    public Point3d GetRepresentativeCenter();
}
