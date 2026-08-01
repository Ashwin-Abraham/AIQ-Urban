namespace AIQ.Primitives.Extensions;
public static class DoubleExtensions
{
    public static bool AlmostZero(
        this double number,
        double tolerance = NumericTolerance.Default)
        => Math.Abs(number) < Math.Abs(tolerance);

    public static bool GreaterThanORAlmostEquals(this double number, double other, double tolerance = NumericTolerance.Default)
    {
        if (number > other) return true;
        return number.AlmostEquals(other, tolerance);
    }

    public static bool GreaterThan(this double number, double other, double tolerance = NumericTolerance.Default)
    {
        if(number.AlmostEquals(other, tolerance))
            return false;

        return number > other;
    }

    public static bool LessThan(this double number, double other, double tolerance = NumericTolerance.Default)
    {
        if (number.AlmostEquals(other, tolerance))
            return false;

        return number < other;
    }
    
    public static bool LessThanORAlmostEquals(this double number, double other, double tolerance = NumericTolerance.Default)
    {
        if (number < other) return true;
        return number.AlmostEquals(other, tolerance);
    }

    public static bool AlmostEquals(this double number, double other, double tolerance = NumericTolerance.Default)
        => Math.Abs(number - other) < tolerance;

    public static double RadiansToDegrees(this double radian)
        => radian * 180 / Math.PI;

    public static double DegreesToRadians(this double degree)
        => degree * Math.PI / 180;

    public static string ToFormattedText(this double number, bool withCommas = true, int decimals = 2)
    {
        string format = withCommas
            ? "N" + decimals   // e.g. "N2" → 1,234.56
            : "F" + decimals;  // e.g. "F2" → 1234.56

        return number.ToString(format);
    }
}
