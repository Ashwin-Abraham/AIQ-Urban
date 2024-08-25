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
            LayerIndex = LayerHelpers.CreateOrGetLayerIndex(RhinoDoc.ActiveDoc, LayerConstants.RoadLayer)
        };

        public static ObjectAttributes BuildingGeometryAttributes = new ObjectAttributes()
        {
            LayerIndex = LayerHelpers.CreateOrGetLayerIndex(RhinoDoc.ActiveDoc, LayerConstants.BuildingMassLayer)
        };

        public static ObjectAttributes BuildingFloorPlateAttributes = new ObjectAttributes()
        {
            LayerIndex = LayerHelpers.CreateOrGetLayerIndex(RhinoDoc.ActiveDoc, LayerConstants.BuildingFloorOutlineLayer)
        };


    }
}
