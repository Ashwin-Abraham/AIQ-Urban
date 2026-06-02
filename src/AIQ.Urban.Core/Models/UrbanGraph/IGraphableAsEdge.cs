using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanGraph;
public interface IGraphableAsEdge
{
    /// <summary>
    /// The geometry that will be used to calculate the graph representation.
    /// </summary>
    public Curve GraphableGeometry { get; }
}
