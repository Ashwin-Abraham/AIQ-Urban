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
        public Guid Id { get; set; }
        public Parcel(PlanarSurface boundary)
        {
            Boundary = boundary;
        }
    }
}
