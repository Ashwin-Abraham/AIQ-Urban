namespace AIQ.Urban.Core.GeometryOperations.Triangulation;

internal class MonotoneVertex
{
    internal TriangulationVertex Vertex { get; }
    internal MonotoneVertex? Above { get; set; }
    internal MonotoneVertex? Below { get; set; }

    internal MonotoneVertex(
        TriangulationVertex vertex,
        MonotoneVertex? below = null)
    {
        Vertex = vertex;
        Below = below;
    }

    internal bool IsBaseVertex => Above is null || Below is null;
}
