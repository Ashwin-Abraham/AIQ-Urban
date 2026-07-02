namespace AIQ.Urban.Core.GeometryOperations.Triangulation;

internal class MonotoneMountain
{
    internal MonotoneVertex BottomVertex { get; }
    internal TriangulationEdge Base { get; }

    internal MonotoneMountain(MonotoneVertex bottomVertex, TriangulationEdge @base)
    {
        BottomVertex = bottomVertex;
        Base = @base;
    }

    internal bool IsDegenerated
    {
        get
        {
            if (BottomVertex.Above is null)
                return true;
            if (BottomVertex.Above.Above is null)
                return true;
            return false;
        }
    }
}
