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
        public static ObjectAttributes ParcelDividerGeometryAttributes = new ObjectAttributes()
        {
            LayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                RhinoDoc.ActiveDoc,
                LayerConstants.ParcelDividerLayer,
                LayerConstants.ParcelDividerColour)
        };

        public static ObjectAttributes RoadGeometryAttributes = new ObjectAttributes()
        {
            LayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                RhinoDoc.ActiveDoc, 
                LayerConstants.RoadLayer,
                LayerConstants.RoadColour)
        };

        public static ObjectAttributes BuildingGeometryAttributes = new ObjectAttributes()
        {
            LayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                RhinoDoc.ActiveDoc, 
                LayerConstants.BuildingMassLayer,
                LayerConstants.BuildingMassColour)
        };

        public static ObjectAttributes BuildingFloorPlateAttributes = new ObjectAttributes()
        {
            LayerIndex = LayerHelpers.CreateOrGetLayerIndex(
                RhinoDoc.ActiveDoc, 
                LayerConstants.BuildingFloorOutlineLayer, 
                LayerConstants.BuildingFloorOutlineColour)
        };


    }
}
