using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.GeometryElements;
public interface IGeometryElement<T> where T : GeometryBase
{
    public T Geometry { get; }
    Guid Id { get; set; }
}
