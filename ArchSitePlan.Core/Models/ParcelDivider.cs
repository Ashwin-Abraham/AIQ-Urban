using BH.oM.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Models
{
    public class ParcelDivider
    {
        public Polyline Curve;
        public Guid Id;

        public ParcelDivider(Polyline curve, Guid id)
        {
            Curve = curve;
            Id = id;
        }
    }
}
