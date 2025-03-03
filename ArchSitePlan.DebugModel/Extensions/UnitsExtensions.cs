namespace ArchSitePlan.DebugModel.Extensions
{
    public static class UnitsExtensions
    {
        public static double RadiansToDegrees(this double radianAngle)
        {
            return 180 / Math.PI * radianAngle;
        }
    }
}
