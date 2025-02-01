using System.Drawing;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Constants;
using Rhino;
using Rhino.DocObjects;

namespace ArchSitePlan.UI.Helpers
{
    public static class LayerHelpers
    {
        public static int CreateOrGetLayerIndex(RhinoDoc doc,
            string layerName,
            Color? layerColor = null)
        {
            layerColor = layerColor ?? Color.Black;
            string layer = layerName;
            int index = doc.Layers.FindByFullPath(layer, LayerConstants.DefaultLayerIndex);

            if (index == LayerConstants.DefaultLayerIndex)
                index = doc.Layers.AddPath(layer);

            doc.Layers[index].Color = (Color)layerColor;
            return index;
        }

        public static void ToggleLockLayer(RhinoDoc doc, string layerName, bool lockLayer)
        {
            string layer = layerName;
            int index = doc.Layers.FindByFullPath(layer, LayerConstants.DefaultLayerIndex);

            if (index != LayerConstants.DefaultLayerIndex)
                doc.Layers[index].IsLocked = lockLayer;
        }
        public static string GetParcelRhinoLayerName(Landuse use)
        {
            return LayerConstants.ParcelsLayer + "::" + use.Name;
        }

        public static List<string> GetParcelRhinoLayerNames()
        {
            var parcelLayerNames = AppLanduses.Uses
                .Select(use => LayerHelpers.GetParcelRhinoLayerName(use))
                .ToList();

            return parcelLayerNames;
        }

        internal static void SetLayerColour(RhinoDoc doc, int layerIndex, Color color)
        {
            doc.Layers[layerIndex].Color = color;
        }

        public static void ChangeLayer(RhinoObject obj, int layerIndex)
        {
            obj.Attributes.LayerIndex = layerIndex;
            obj.CommitChanges();
        }

        internal static object GetTransitLayerNames()
        {
            throw new NotImplementedException();
        }

        internal static string GenerateRoadCLLayer(double width)
        {
            return LayerConstants.RoadCLLayer + LayerConstants._Sep_ + width.ToString();
        }

        internal static void DeleteLayerAndChildrenIfEmpty(RhinoDoc doc, Layer layer)
        {
            var children = layer.GetChildren(false);

            if (children is not null && children.Length > 0)
            {
                foreach (var childLayer in children)
                    DeleteLayerAndChildrenIfEmpty(doc, childLayer);
            }

            var remainingChildren = layer.GetChildren(false);
            int objectCount = doc.Objects.Count(obj => obj.Attributes.LayerIndex == layer.Index);

            if (remainingChildren is null && objectCount == 0)
                doc.Layers.Delete(layer, true);
        }

        internal static void DeleteLayerAndChildren(RhinoDoc doc, string layerName)
        {
            int index = doc.Layers.FindByFullPath(layerName, -1);
            if (index != -1)
                DeleteLayerAndChildren(doc, doc.Layers[index]);
        }
        internal static void DeleteLayerAndChildren(RhinoDoc doc, Layer layer)
        {
            var layerIndicesToDelete = layer.Index.ObjectToList();
            layer.GetChildren(true)
                .ToList()
                .ForEach(lay => layerIndicesToDelete.Add(lay.Index));

            var objectsToDelete = doc.Objects
                .Where(o => layerIndicesToDelete.Contains(o.Attributes.LayerIndex))
                .ToList();

            foreach (var obj in objectsToDelete)
            {
                doc.Objects.Delete(obj, true, true);
            }
        }
    }

}
