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
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.UI.Actions
{
    public static class OutputFromCoreModel
    {
        private static ObjectAttributes GetParcelAttributesByUse(RhinoDoc doc, Landuse use)
        {
            string layerName = LayerHelpers.ConstructParcelRhinoLayerName(use);
            LayerHelpers.EnsureLayerExists(doc, layerName);
            LayerHelpers.SetLayerColour(doc, layerName, Defaults.LandUseColours[use]);

            ObjectAttributes parcelGeometryAttributes = new ObjectAttributes()
            {
                LayerIndex = LayerHelpers.GetLayerIndex(doc, layerName)
            };

            return parcelGeometryAttributes;
        }
        private static Dictionary<Landuse, ObjectAttributes> GetParcelAttributesForAllUses(RhinoDoc doc)
        {
            var uses = Enum.GetValues(typeof(Landuse)).Cast<Landuse>().ToList();
            var dictionary = new Dictionary<Landuse, ObjectAttributes>();

            foreach ( var use in uses)
            {
                var attributes = GetParcelAttributesByUse(doc, use);
                dictionary.Add(use, attributes);
            }

            return dictionary;
        }
        public static void OutputParcelsToRhino(RhinoDoc doc)
        {
            // Get Attributes
            var parcelGeometryAttributes = GetParcelAttributesForAllUses(doc);
            var parcelLayers = parcelGeometryAttributes.Values.Select(attr => attr.LayerIndex).ToList();

            // Delete Existing Parcel Objects
            var guids = doc.Objects
                .Where(obj => parcelLayers.Contains(obj.Attributes.LayerIndex))
                .Select(obj => obj.Id)
                .ToList();

            doc.Objects.Delete(guids, true);

            // Bake model to rhino
            foreach(var parcel in CoreModel.Parcels)
            {
                var rhinoSrf = parcel.Boundary.ToRhino();
                parcel.Id = doc.Objects.AddBrep(rhinoSrf, parcelGeometryAttributes[parcel.LandUse]);
            }

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

        internal static void UpdateRhinoParcel(RhinoObject obj)
        {
            var parcels = CoreModel.Parcels.FindAll(p => p.Id == obj.Id);
            OutputParcelsToRhino(RhinoDoc.ActiveDoc);
        }

    }
}
