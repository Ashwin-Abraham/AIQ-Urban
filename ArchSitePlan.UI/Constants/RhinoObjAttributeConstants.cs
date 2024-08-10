using ArchSitePlan.UI.Helpers;
using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Constants
{
    public static class RhinoObjAttributeConstants
    {
        public static ObjectAttributes RoadGeometryAttributes = new ObjectAttributes()
        {
            LayerIndex = LayerHelpers.GetLayerIndex(RhinoDoc.ActiveDoc, LayerConstants.RoadLayer)
        };

        
    }
}
