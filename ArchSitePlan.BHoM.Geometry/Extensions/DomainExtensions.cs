using AIQ.Primitives.Extensions;
using BH.Engine.Data;
using BH.oM.Data.Collections;

namespace ArchSitePlan.BHoM.Geometry.Extensions;
public static class DomainExtensions
{
    public static Domain ExtendByRatio(this Domain domain, double extensionRatio)
    {
        var size = domain.Length();
        var extensionSize = size * extensionRatio - size;
        
        return new Domain(domain.Min - extensionSize, domain.Max + extensionSize);
    }

    /// <summary>
    /// Remap numbers from one domain to another. If a (0 +or- 1E-05) length domain is input, the result will be the target.Min
    /// </summary>
    public static double Remap(this Domain original, double number, Domain target)
    {
        if (original.IsDecreasing())
            original = original.Flip();

        if (target.IsDecreasing())
            target = target.Flip();

        if (original.Length().AlmostZero() || target.Length().AlmostZero())
            return target.Min;

        double paramPosition = (number - original.Min) / original.Length();
        double remappedNumber = paramPosition * target.Length() + target.Min;

        return remappedNumber;
    }

    public static bool IsDecreasing(this Domain domain) => domain.Min > domain.Max;
    public static bool IsIncreasing(this Domain domain) => domain.Min < domain.Max;
    public static Domain Flip(this Domain domain) => Create.Domain(domain.Max, domain.Min);
    public static double Length(this Domain domain) => Math.Abs(domain.Max - domain.Min);
}
