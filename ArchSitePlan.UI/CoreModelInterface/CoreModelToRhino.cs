using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using Rhino;
using Rhino.DocObjects;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.Actions
{
    public static class CoreModelToRhino
    {
        internal static void LockModelLayersExcept(RhinoDoc doc, List<string> layersToUnlock, bool unlockChildren=true)
        {
            ToggleLockModelLayers(doc, true);

            // Unlock layer and children
            List<Layer> unlockedLayers = new List<Layer>();
            foreach(var layer in layersToUnlock)
            {
                int layerIndex = doc.Layers.FindByFullPath(layer, -1);
                if (layerIndex == -1) continue;

                Layer parent = doc.Layers[layerIndex];
                var children = parent.GetChildren(true);

                LayerHelpers.ToggleLockLayer(doc, layer, false);
                unlockedLayers.Add(parent);
                parent.IsLocked = false;

                if (children != null)
                {
                    foreach (var child in children)
                        child.IsLocked = false;

                    unlockedLayers.AddRange(children);
                }
            }
        }

        internal static void ToggleLockModelLayers(RhinoDoc doc, bool lockLayers)
        {
            var mainLayer = doc.Layers.FindByFullPath(LayerConstants.MainLayer, -1);
            if (mainLayer == -1) return;

            var appLayers = doc.Layers[mainLayer].GetChildren(true);

            // Unlock app layers
            doc.Layers[mainLayer].IsLocked = false;
            foreach (var layer in appLayers)
                layer.IsLocked = lockLayers; 
        }

        internal static void ToggleLayerStateBasedOnAppMode(RhinoDoc doc, AppMode appMode)
        {
            List<string> layersToUnlock = new();
            List<string> additionalLayersToLock = new();
            int layerIndex = 0;

            switch (appMode)
            {
                case AppMode.Road:  
                    layersToUnlock.Add(LayerConstants.RoadLayer);
                    additionalLayersToLock.Add(LayerConstants.RoadOutlineLayer);
                    layerIndex = LayerHelpers.CreateOrGetLayerIndex(doc, LayerConstants.RoadCLLayer);
                    doc.Layers.SetCurrentLayerIndex(layerIndex, true);
                    break;
                
                case AppMode.LandUse:  
                    layersToUnlock.AddRange(new string[] { LayerConstants.ParcelsLayer, LayerConstants.ParcelDividerLayer });
                    layerIndex = LayerHelpers.CreateOrGetLayerIndex(doc, LayerConstants.ParcelsLayer);
                    doc.Layers.SetCurrentLayerIndex(layerIndex, true);
                    break;
                
                case AppMode.Building:
                    {
                        layersToUnlock.Add(LayerConstants.BuildingLayer);
                        additionalLayersToLock.Add(LayerConstants.BuildingFloorOutlineLayer);
                        layerIndex = LayerHelpers.CreateOrGetLayerIndex(doc, LayerConstants.BuildingLayer);
                        doc.Layers.SetCurrentLayerIndex(layerIndex, true);
                    }
                    break;

                case AppMode.StudyArea:  
                    layersToUnlock.Add(LayerConstants.StudyAreaLayer);
                    layerIndex = LayerHelpers.CreateOrGetLayerIndex(doc, LayerConstants.StudyAreaLayer);
                    doc.Layers.SetCurrentLayerIndex(layerIndex, true);
                    break;

                default:
                    layersToUnlock.Add(LayerConstants.MainLayer);
                    layerIndex = LayerHelpers.CreateOrGetLayerIndex(doc, LayerConstants.MainLayer);
                    doc.Layers.SetCurrentLayerIndex(layerIndex, true);
                    break;
            }

            // Manipulate layer lock states
            LockModelLayersExcept(doc, layersToUnlock);
            additionalLayersToLock.ForEach(layer => LayerHelpers.ToggleLockLayer(doc, layer, true));

            // Deselects selected objects
            doc.Views.Redraw();
        }
    }
}
