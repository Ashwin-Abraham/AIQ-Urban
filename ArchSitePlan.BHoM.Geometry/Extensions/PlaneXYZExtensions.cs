using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchSitePlan.BHoM.Geometry.Models;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;
public static class PlaneXYZExtensions
{
    public static IEnumerable<PlaneXYZ> RemoveSequentialDuplicateOrigins(this IEnumerable<PlaneXYZ> planes)
    {
        var list = planes.ToList();

        if (list.Count == 0)
            yield break;

        Point? previous = list[0].Origin;
        yield return list[0];

        for (int i = 1; i < list.Count; i++)
        {
            if (!list[i].Origin.AlmostEquals(previous))
                yield return list[i];
        }
    }
}
