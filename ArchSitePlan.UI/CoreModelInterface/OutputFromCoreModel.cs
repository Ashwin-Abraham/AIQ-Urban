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
        private static void OutputParcelsToRhino(RhinoDoc doc)
        {
            // Get Attributes
            LayerHelpers.EnsureLayerExists(doc, LayerConstants.ParcelsLayer);

            ObjectAttributes parcelGeometryAttributes = new ObjectAttributes()
            {
                LayerIndex = LayerHelpers.GetLayerIndex(RhinoDoc.ActiveDoc, LayerConstants.ParcelsLayer)
            };

            // Delete Existing Objects
            var guids = doc.Objects
                .Where(obj => obj.Attributes.LayerIndex == parcelGeometryAttributes.LayerIndex)
                .Select(obj => obj.Id)
                .ToList();

            doc.Objects.Delete(guids, true);

            // Bake model to rhino
            var rhinoSurfaces = CoreModel.Parcels
                .Select(row => row.Boundary.ToRhino())
                .ToList();

            rhinoSurfaces
                .ToList()
                .ForEach(brep => doc.Objects.AddBrep(brep, parcelGeometryAttributes));

            doc.Views.Redraw();
        }

        public static void OutputCoreModelToRhino(RhinoDoc doc)
        {
            LayerHelpers.ToggleLockLayer(doc, LayerConstants.MainLayer, false);
            OutputRoadsToRhino(doc);
            OutputParcelsToRhino(doc);
            LayerHelpers.ToggleLockLayer(doc, LayerConstants.MainLayer, true);
        }

        private static void OutputRoadsToRhino(RhinoDoc doc)
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
            var rhinoSurfaces = CoreModel.ROWs
                .Select(row => row.ToRhino())
                .ToList();

            rhinoSurfaces
                .ToList()
                .ForEach(brep => doc.Objects.AddBrep(brep, roadGeometryAttributes));

            doc.Views.Redraw();
        }


    }
}
