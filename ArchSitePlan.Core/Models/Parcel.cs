using BH.Engine.Geometry;
using BH.oM.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Models
{
    public class Parcel
    {
        public PlanarSurface Boundary {  get; set; }
        public Landuse LandUse { get; set; }
        public Guid OriginalObjectID { get; set; }
        internal Parcel(PlanarSurface boundary, Landuse use)
        {
            Boundary = boundary;
            LandUse = use;
        }
        internal Parcel(Parcel parcel, PlanarSurface boundary):this(boundary, parcel.LandUse)
        {
            OriginalObjectID = parcel.OriginalObjectID;
        }

        public double Area()
        {
            return Math.Abs(Boundary.Area());
        }
    }
}
