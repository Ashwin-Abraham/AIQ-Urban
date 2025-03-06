namespace AIQ.Primitives.Extensions;
public static class DoubleExtensions
{
    public static bool AlmostZero(
        this double number,
        double tolerance = 1E-05)
        => Math.Abs(number) < Math.Abs(tolerance);

    public static bool AlmostEquals(this double number, double other, double tolerance = 1E-05)
        => Math.Abs(number - other) < tolerance;

    public static double ToRadians(this double degree)
        => degree * Math.PI / 180;
}
