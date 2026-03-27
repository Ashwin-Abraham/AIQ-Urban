using System.Drawing;
using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.RhinoInterface.Constants;
using Rhino;
using Rhino.DocObjects;

namespace ArchSitePlan.RhinoInterface.Helpers
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
                index = doc.Layers.AddPath(layer, (Color) layerColor);
            
            return index;
        }

        public static void ToggleLockLayer(RhinoDoc doc, string layerName, bool lockLayer)
        {
            string layer = layerName;
            int index = doc.Layers.FindByFullPath(layer, LayerConstants.DefaultLayerIndex);

            if (index != LayerConstants.DefaultLayerIndex)
                doc.Layers[index].IsLocked = lockLayer;
        }

        public static string ConstructParcelRhinoLayerName(Landuse use)
        {
            return LayerConstants.ParcelsLayer + "::" + use.Name;
        }

        public static string ConstructFloorplateRhinoLayerName(Landuse use)
        {
            return LayerConstants.BuildingFloorOutlineLayer + "::" + use.Name;
        }

        public static List<Landuse> GetLandusesInDocument(RhinoDoc doc)
        {
            var parcelLayers = GetParcelRhinoLayers(doc);
            var usesInModel = parcelLayers.Select(x => new Landuse(x.Name, x.Color, Guid.NewGuid())).ToList();

            return usesInModel;
        }

        public static List<int> GetBuildingOutlineRhinoLayerIndices(RhinoDoc doc)
            => GetBuildingOutlineRhinoLayers(doc).Select(x => x.Index).ToList();

        public static List<Layer> GetBuildingOutlineRhinoLayers(RhinoDoc doc)
        {
            var buildingLayerIndex = doc.Layers.FindByFullPath(LayerConstants.BuildingFloorOutlineLayer, LayerConstants.DefaultLayerIndex);
            if (buildingLayerIndex == LayerConstants.DefaultLayerIndex)
                return [];

            var buildingOutlineLayer = doc.Layers[buildingLayerIndex];
            var children = buildingOutlineLayer.GetChildren().ToList();

            return children;
        }

        public static List<int> GetParcelRhinoLayerIndices(RhinoDoc doc)
            => GetParcelRhinoLayers(doc).Select(x => x.Index).ToList();

        public static List<Layer> GetParcelRhinoLayers(RhinoDoc doc)
        {
            var parcelLayerIndex = doc.Layers.FindByFullPath(LayerConstants.ParcelsLayer, LayerConstants.DefaultLayerIndex);
            if (parcelLayerIndex == LayerConstants.DefaultLayerIndex)
                return [];

            var parcelLayer = doc.Layers[parcelLayerIndex];
            var children = parcelLayer.GetChildren().ToList();

            return children;
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
            List<int> layerIndicesToDelete = [layer.Index];
            layer.GetChildren(true)
                .ToList()
                .ForEach(lay => layerIndicesToDelete.Add(lay.Index));

            var objectsToDelete = doc.Objects
                .Where(o => layerIndicesToDelete.Contains(o.Attributes.LayerIndex))
                .ToList();

            foreach (var obj in objectsToDelete)
            {
                bool result = doc.Objects.Delete(obj, true, true);
            }
        }

        public static bool TryGetRoadDetailsFromObjectLayer(RhinoObject obj, out double? width)
        {
            width = null;

            var layerName = RhinoDoc.ActiveDoc.Layers[obj.Attributes.LayerIndex].FullPath;
            var roadLayerStart = LayerConstants.RoadCLLayer + LayerConstants._Sep_;

            if (layerName == null || layerName.Length <= roadLayerStart.Length)
                return false;

            var widthPart = layerName.Substring(roadLayerStart.Length);

            if(!Double.TryParse(widthPart, out var roadWidth))
                return false;

            width = roadWidth;
            return true;
        }

        public static void ApplyLayerProperties(RhinoDoc doc, Layer layer, bool isVisible, Color displayColor, double plotWeight, string lineTypeName)
        {
            layer.IsVisible = isVisible;
            layer.Color = displayColor;
            layer.PlotWeight = plotWeight;

            var lineTypeIndex = doc.Linetypes.Find(lineTypeName);
            if (lineTypeIndex >= 0)
                layer.LinetypeIndex = lineTypeIndex;
        }
    }

}
