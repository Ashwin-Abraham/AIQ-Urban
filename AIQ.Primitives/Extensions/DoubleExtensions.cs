namespace AIQ.Primitives.Extensions;
public static class DoubleExtensions
{
    public static bool AlmostZero(
        this double number,
        double tolerance = 1E-05)
        => Math.Abs(number) < Math.Abs(tolerance);

    public static bool GreaterThanORAlmostEquals(this double number, double other, double tolerance = 1E-05)
    {
        if (number > other) return true;
        return number.AlmostEquals(other, tolerance);
    }

    public static bool GreaterThan(this double number, double other, double tolerance = 1E-05)
    {
        if(number.AlmostEquals(other, tolerance))
            return false;

        return number > other;
    }

    public static bool LessThan(this double number, double other, double tolerance = 1E-05)
    {
        if (number.AlmostEquals(other, tolerance))
            return false;

        return number < other;
    }
    
    public static bool LessThanORAlmostEquals(this double number, double other, double tolerance = 1E-05)
    {
        if (number < other) return true;
        return number.AlmostEquals(other, tolerance);
    }

    public static bool AlmostEquals(this double number, double other, double tolerance = 1E-05)
        => Math.Abs(number - other) < tolerance;

    public static double DegreesToRadians(this double degree)
        => degree * Math.PI / 180;
}
