using ArchSitePlan.Core.Models.CreationData;
using BH.oM.Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Models
{
    public class ParcelDivider
    {
        public ICurve Curve;
        public Guid Id;

        [JsonConstructor]
        public ParcelDivider(ICurve curve, Guid id)
        {
            Curve = curve;
            Id = id;
        }

        public DividerData ToDividerData()=> new DividerData(Curve, Id);
    }
}
