using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Models
{
    public class Road
    {
        public Guid CenterLineId { get; init; }
        public IndexedPolycurve Centerline {  get; init; }

        public double Width { get; private set; }
    }
}
