using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
