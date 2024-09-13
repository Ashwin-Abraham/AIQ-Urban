using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Extensions
{
    public static class DoubleExtensions
    {
        public static bool AlmostEquals(this double number, double other, double tolerance = 1E-05)
        {
            return Math.Abs(number - other) < tolerance;
        }
    }
}
