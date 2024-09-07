using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Constants
{
    public static class GeneralConstants
    {
        public static int MaxGraphBarSize = 200;

        public static List<ObjectType> BuildingFootprintObjectTypes = new List<ObjectType>(){ ObjectType.Curve};

        public static List<ObjectType> BuildingEnvelopeObjectTypes = new List<ObjectType>()
        { 
            ObjectType.Extrusion,
            ObjectType.Brep,
            ObjectType.Mesh
        };
}
}
