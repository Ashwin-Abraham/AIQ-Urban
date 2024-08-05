using ArchSitePlan.DebugModel.BHoMExtensions;
using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.DebugModel.Extensions
{
    public static class ModelExtensions
    {
        public static void AddBHoMElement(this Model model, BH.oM.Geometry.PolyCurve crv)
        {
            List<BoundedCurve> crvs = ToHyparInterop.ToHypar(crv);

            DEBUGMODEL.AddBoundedCurves(crvs);
            DEBUGMODEL.OpenAsGlb();
        }
    }
}
