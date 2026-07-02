namespace AIQ.Urban.Core.GeometryOperations.Triangulation;

internal readonly struct TriangulationEdge : IEquatable<TriangulationEdge>
{
    internal TriangulationVertex BottomVertex { get; }
    internal TriangulationVertex TopVertex { get; }
    internal TriangulationVertex MidPoint => BottomVertex.MidPoint(TopVertex);

    internal TriangulationEdge(TriangulationVertex start, TriangulationVertex end)
    {
        if (start > end)
        {
            BottomVertex = end;
            TopVertex = start;
        }
        else
        {
            BottomVertex = start;
            TopVertex = end;
        }
    }

    internal TriangulationVertex GetVertex(bool top)
        => top ? TopVertex : BottomVertex;

    internal bool IsVertexAtTheRight(TriangulationVertex vertex)
    {
        var xEdge = GetXByY(vertex.Y);
        return vertex.X > xEdge;
    }

    internal double GetXByY(double y)
    {
        var bottomX = BottomVertex.X;
        var topX = TopVertex.X;
        var bottomY = BottomVertex.Y;
        var topY = TopVertex.Y;

        if (bottomY == topY)
            return (bottomX + topX) / 2;

        var t = (y - bottomY) / (topY - bottomY);
        return bottomX + t * (topX - bottomX);
    }

    internal static TriangulationVertex? GetEdgeVertex(TriangulationEdge? edge, bool top)
        => edge is null ? null : edge.Value.GetVertex(top);

    public bool Equals(TriangulationEdge other)
        => BottomVertex == other.BottomVertex && TopVertex == other.TopVertex;

    public override bool Equals(object? obj)
        => obj is TriangulationEdge other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(BottomVertex, TopVertex);

    public static bool operator ==(TriangulationEdge left, TriangulationEdge right)
        => left.Equals(right);

    public static bool operator !=(TriangulationEdge left, TriangulationEdge right)
        => !left.Equals(right);
}
