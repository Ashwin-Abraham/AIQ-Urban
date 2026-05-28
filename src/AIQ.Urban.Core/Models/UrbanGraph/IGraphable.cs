using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanGraph;
public interface IGraphable
{
    public Curve Geometry { get; }
}
