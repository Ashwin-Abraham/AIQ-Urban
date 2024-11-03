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
            out Road? road)
        {
            road = null;
            var segments = centerline.Curves;
            
            if(centerline.ValidateContinuity())
            {
                road = new Road(centerline, id, width);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static PlanarSurface CalculateRoWs(PolyCurve centerline, double width)
        {
            var boundaries = centerline
                //.Extend(width/4, width/4)
                .OffsetBothSides(width / 2, Vector.ZAxis)
                .OrderByDescending(c => c.Length())
                .ToList();

            if (centerline.IsClosed())
            {

                var outer = boundaries[0];
                var inner = boundaries.Skip(1);
                return Create.PlanarSurface(outer, inner.Cast<ICurve>().ToList());
            }
            else
            {
                return Create.PlanarSurface(boundaries[0], new List<ICurve>());
            }
        }

        public  void ResetRoW()
        {
            RoW = null;
        }
    }
}
