using ArchSitePlan.Core.Extensions;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models
{
    public class Road
    {
        public Guid CenterLineId { get; init; }

        /// <summary>
        /// Contains a list of arcs and lines representing the centerline
        /// </summary>
        public PolyCurve Centerline {  get; init; }
        public PlanarSurface RoW {  get; private set; }

        public double Width { get; private set; }

        /// <summary>
        /// Makes a road using unbroken centerline
        /// </summary>
        /// <param name="centerline"></param>
        /// <param name="width"></param>
        Road(PolyCurve centerline, Guid centerLineId, double width)
        {
            Centerline = centerline;
            CenterLineId = centerLineId;
            Width = width;
            RoW = CalculateRoWs(centerline, width);
        }

        public Road(PolyCurve newCenterline, Road parentRoad)
            : this(newCenterline, parentRoad.CenterLineId, parentRoad.Width) 
        { }

        internal static bool TryCreateRoad(PolyCurve centerline, 
            Guid id,
            double width, 
            out Road road)
        {
            road = null;
            var segments = centerline.Curves;
            for (int i = 0; i < segments.Count - 1; i++) 
            {
                var currentCrvEnd = segments[i].IEndPoint();
                var nextCrvStart = segments[i].IStartPoint();

                if (!currentCrvEnd.IsEqual(nextCrvStart))
                    return false;
            }

            road = new Road(centerline, id, width);
            return true;
        }

        private static PlanarSurface CalculateRoWs(PolyCurve centerline, double width)
        {
            PolyCurve boundary = centerline
                .Extend(width/4, width/4)
                .OffsetBothSides(width/2, Vector.ZAxis);
            PlanarSurface surface = Create.PlanarSurface(boundary);
            return surface;
        }

        public  void ResetRoW()
        {
            RoW = null;
        }
    }
}
