using AIQ.Primitives.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Comparers;

public class PointComparer : IEqualityComparer<Point>
{
    private int _digitTolerance;
    private double _tolerance;
    public PointComparer(int digitTolerance = 6)
    {
        digitTolerance = Math.Max(0, digitTolerance);
        _digitTolerance = digitTolerance;
        _tolerance = Math.Pow(10, -digitTolerance);
    }

    public static readonly PointComparer Instance = new PointComparer();
    public int Compare(Point p1, Point p2)
    {
        if (p1 == null || p2 == null)
            throw new ArgumentException("Points being compared cannot be null.");

        // Compare by X, then Y, then Z if previous values are equal
        int xComparison = p1.X.CompareTo(p2.X);
        if (xComparison != 0) return xComparison;

        int yComparison = p1.Y.CompareTo(p2.Y);
        if (yComparison != 0) return yComparison;

        return p1.Z.CompareTo(p2.Z);
    }

    public bool Equals(Point? p1, Point? p2)
    {
        if (ReferenceEquals(p1, p2)) return true;
        if (p1 == null || p2 == null) return false;
        return p1.Distance(p2).AlmostZero(_tolerance);
    }

    public int GetHashCode(Point? obj)
    {
        if (obj == null) return -1;

        int hash = 17;
        hash = hash * 23 + Math.Round(obj.X, _digitTolerance).GetHashCode();
        hash = hash * 31 + Math.Round(obj.Y, _digitTolerance).GetHashCode();
        hash = hash * 47 + Math.Round(obj.Z, _digitTolerance).GetHashCode();

        return hash;
    }
}
