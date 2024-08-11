using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.Engine.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Metrics
{
    public class ParcelMetrics
    {
        public double Area;
        public double Perimeter;
        public ParcelMetrics(Parcel parcel) 
        {
            CalculateParcelMetrics(parcel);
        }

        private void CalculateParcelMetrics(Parcel parcel)
        {
            Area = Math.Abs(parcel.Boundary.Area());
            Perimeter = Math.Abs(parcel.Boundary.ExternalBoundary.Length());
        }
    }
}
