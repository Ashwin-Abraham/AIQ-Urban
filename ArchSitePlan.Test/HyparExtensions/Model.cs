using ArchSitePlan.DebugModel;
using ArchSitePlan.Test.BHoMExtensions;
using BH.oM.Geometry;
using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Test.HyparExtensions
{
    public static class ModelExtension
    {
        public static void AddBHoMElement(this Model model, PolyCurve crv)
        {
            List<BoundedCurve> crvs = ToHyparInterop.ToHypar(crv);

            DEBUGMODEL.AddBoundedCurves(crvs);
            DEBUGMODEL.OpenAsGlb();
        }
    }
}
