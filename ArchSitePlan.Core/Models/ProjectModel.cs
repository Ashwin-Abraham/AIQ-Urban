using BH.oM.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Models
{
    public class ProjectModel
    {
        public PolyCurve StudyBoundary { get; set; }
        public RoadNetwork RoadNetwork { get; set; }
        public List<Parcel> Parcels { get; set; }
    }
}
