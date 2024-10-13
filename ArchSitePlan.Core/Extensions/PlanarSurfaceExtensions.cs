using BH.Engine.Geometry;
using BH.oM.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Extensions
{
    public static class PlanarSurfaceExtensions
    {
        public static Point ControlPointCentroid(this PlanarSurface surface)
        => surface.ExternalBoundary.ControlPointCentroid();

    }
}
