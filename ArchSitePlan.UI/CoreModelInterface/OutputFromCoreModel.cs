using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Actions
{
    public static class OutputFromCoreModel
    {
        public static void OutputParcelsToRhino(RhinoDoc doc)
        {
            //// Get Attributes
            //LayerHelpers.EnsureLayerExists(doc, LayerConstants.RoadLayer);

            //ObjectAttributes parcelGeometryAttributes = new ObjectAttributes()
            //{
            //    LayerIndex = LayerHelpers.GetLayerIndex(RhinoDoc.ActiveDoc, LayerConstants.RoadLayer)
            //};

            //// Delete Existing Objects
            //var guids = doc.Objects
            //    .Where(obj => obj.Attributes.LayerIndex == parcelGeometryAttributes.LayerIndex)
            //    .Select(obj => obj.Id)
            //    .ToList();

            //doc.Objects.Delete(guids, true);

            //// Bake model to rhino
            //var rhinoSurfaces = network.ROWs
            //    .Select(row => row.ToRhino())
            //    .ToList();

            //rhinoSurfaces
            //    .ToList()
            //    .ForEach(brep => doc.Objects.AddBrep(brep, parcelGeometryAttributes));

            //doc.Views.Redraw();
        }

        public static void Bake()
        {

        }

        public static void OutputRoadsToRhino(RhinoDoc doc, RoadNetwork network)
        {
            // Get Attributes
            LayerHelpers.EnsureLayerExists(doc, LayerConstants.RoadLayer);

            ObjectAttributes roadGeometryAttributes = new ObjectAttributes()
            {
                LayerIndex = LayerHelpers.GetLayerIndex(RhinoDoc.ActiveDoc, LayerConstants.RoadLayer)
            };

            // Delete Existing Objects
            var guids = doc.Objects
                .Where(obj => obj.Attributes.LayerIndex == roadGeometryAttributes.LayerIndex)
                .Select(obj => obj.Id)
                .ToList();

            doc.Objects.Delete(guids, true);

            // Bake model to rhino
            var rhinoSurfaces = network.ROWs
                .Select(row => row.ToRhino())
                .ToList();

            rhinoSurfaces
                .ToList()
                .ForEach(brep => doc.Objects.AddBrep(brep, roadGeometryAttributes));

            doc.Views.Redraw();
        }


    }
}
