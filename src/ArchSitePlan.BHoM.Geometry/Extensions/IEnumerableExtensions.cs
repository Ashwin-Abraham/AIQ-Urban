using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.BHoM.Geometry.Extensions;
public static class IEnumerableExtensions
{
    public static T? SelectByParameter<T>(this IEnumerable<T> list, double parameter, out int? index)
    {
        var indexedList = list.ToList();
        int count = indexedList.Count;

        index = null;
        if (count == 0)
            return default;

        parameter = Math.Clamp(parameter, 0, 1);
        index = (int)Math.Floor(parameter * (count - 1));
        return indexedList[index.Value];
    }

    public static IEnumerable<T> ShiftList<T>(this IEnumerable<T> list, int shiftAmount)
    {
        if (shiftAmount == 0)
            return list;

        var indexedList = list.ToList();
        int count = indexedList.Count;
       
        if (count == 0)
            return indexedList;

        if (shiftAmount < 0)
            shiftAmount = count - shiftAmount;

        shiftAmount = shiftAmount % count;

        return indexedList.Skip(shiftAmount).Concat(indexedList.Take(shiftAmount));
    }
}
