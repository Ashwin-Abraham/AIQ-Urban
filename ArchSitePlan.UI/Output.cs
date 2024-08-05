using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI
{
    public static class Output
    {
        public static void OutputModelToRhino(RhinoDoc doc)
        {

        }

        public static void Bake()
        {

        }

        public static void OutputRoadsToRhino(RhinoDoc doc, RoadNetwork network)
        {
            // Get Attributes
            ObjectAttributes attributes = new ObjectAttributes()
            {
                LayerIndex = GetRoadLayerIndex(doc)
            };

            // Delete Existing Objects
            var guids = doc.Objects
                .Where(obj => obj.Attributes.LayerIndex == attributes.LayerIndex)
                .Select(obj => obj.Id)
                .ToList();

            doc.Objects.Delete(guids, true);

            // Bake model to rhino
            var rhinoSurfaces = network.ROWs
                .Select(row => row.ToRhino())
                .ToList();

            rhinoSurfaces
                .ToList()
                .ForEach(brep => doc.Objects.AddBrep(brep, attributes));
        }

        private static int GetRoadLayerIndex(RhinoDoc doc)
        {
            string layer = LayerConstants.RoadLayer;
            int index = doc.Layers.FindByFullPath(layer, LayerConstants.DefaultLayerIndex);

            if(index == LayerConstants.DefaultLayerIndex)
                return doc.Layers.AddPath(layer);

            return LayerConstants.DefaultLayerIndex;
        }
    }
}
