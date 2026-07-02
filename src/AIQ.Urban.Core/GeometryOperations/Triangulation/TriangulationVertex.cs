namespace AIQ.Urban.Core.GeometryOperations.Triangulation;

internal readonly struct TriangulationVertex(double x, double y) : IComparable<TriangulationVertex>
{
    internal double X { get; } = x;
    internal double Y { get; } = y;

    public int CompareTo(TriangulationVertex other)
    {
        var yCmp = Y.CompareTo(other.Y);
        return yCmp != 0 ? yCmp : X.CompareTo(other.X);
    }

    internal TriangulationVertex MidPoint(TriangulationVertex other)
        => new((X + other.X) / 2, (Y + other.Y) / 2);

    internal double DistanceTo(TriangulationVertex other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    internal static bool CounterClockwise(TriangulationVertex a, TriangulationVertex b, TriangulationVertex c)
        => (c.Y - a.Y) * (b.X - a.X) > (b.Y - a.Y) * (c.X - a.X);

    internal static bool SegmentIntersect(
        TriangulationVertex a, TriangulationVertex b,
        TriangulationVertex c, TriangulationVertex d)
        => CounterClockwise(a, b, c) != CounterClockwise(a, b, d)
        && CounterClockwise(c, d, a) != CounterClockwise(c, d, b);

    internal static double Angle(TriangulationVertex v1, TriangulationVertex v2, TriangulationVertex v3)
    {
        var dxA = v1.X - v2.X;
        var dyA = v1.Y - v2.Y;
        var dxB = v3.X - v2.X;
        var dyB = v3.Y - v2.Y;
        var dot = dxA * dxB + dyA * dyB;
        var normA = Math.Sqrt(dxA * dxA + dyA * dyA);
        var normB = Math.Sqrt(dxB * dxB + dyB * dyB);
        var cos = Math.Clamp(dot / (normA * normB), -1.0, 1.0);
        return Math.Acos(cos) * 180.0 / Math.PI;
    }

    public override bool Equals(object? obj)
        => obj is TriangulationVertex other && X == other.X && Y == other.Y;

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(TriangulationVertex left, TriangulationVertex right)
        => left.X == right.X && left.Y == right.Y;

    public static bool operator !=(TriangulationVertex left, TriangulationVertex right)
        => !(left == right);

    public static bool operator >(TriangulationVertex left, TriangulationVertex right)
        => left.CompareTo(right) > 0;

    public static bool operator <(TriangulationVertex left, TriangulationVertex right)
        => left.CompareTo(right) < 0;
}
