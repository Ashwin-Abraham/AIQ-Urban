using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.Helpers;
using Rhino;
using Rhino.DocObjects;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.CoreModelInterface;


public static class LayerManager
{
    public static void ToggleLayerStateBasedOnAppMode(RhinoDoc doc, ModellingTool appMode)
    {
        List<string> layersToUnlock = new();
        List<string> additionalLayersToLock = new();

        switch (appMode)
        {
            case ModellingTool.Transit:
                layersToUnlock.Add(LayerConstants.RoadLayer);
                additionalLayersToLock.Add(LayerConstants.RoadOutlineLayer);
                break;

            case ModellingTool.LandUse:
                layersToUnlock.AddRange(new string[] { LayerConstants.ParcelsLayer, LayerConstants.ParcelDividerLayer });
                break;

            case ModellingTool.AutoBuilding:
            case ModellingTool.Building:
                {
                    layersToUnlock.Add(LayerConstants.BuildingLayer);
                    additionalLayersToLock.Add(LayerConstants.BuildingFloorOutlineLayer);
                }
                break;

            case ModellingTool.Zones:
                layersToUnlock.Add(LayerConstants.StudyAreaLayer);
                break;

            case ModellingTool.Dividers:
                layersToUnlock.Add(LayerConstants.ParcelDividerLayer);
                layersToUnlock.Add(LayerConstants.ParcelsLayer);
                break;

            default:
                layersToUnlock.Add(LayerConstants.MainModellingLayer);
                break;
        }

        // Manipulate layer lock states
        LockModelLayersExcept(doc, layersToUnlock);
        additionalLayersToLock.ForEach(layer => LayerHelpers.ToggleLockLayer(doc, layer, true));

        // Deselects selected objects
        doc.Views.Redraw();
    }

    public static void ToggleLockModelLayers(RhinoDoc doc, bool lockLayers)
    {
        var mainLayer = doc.Layers.FindByFullPath(LayerConstants.MainModellingLayer, -1);
        if (mainLayer == -1) return;

        var appLayers = doc.Layers[mainLayer].GetChildren(true);

        // Unlock app layers
        doc.Layers[mainLayer].IsLocked = false;
        foreach (var layer in appLayers)
            layer.IsLocked = lockLayers;
    }

    internal static void LockModelLayersExcept(RhinoDoc doc, List<string> layersToUnlock, bool unlockChildren = true)
    {
        ToggleLockModelLayers(doc, true);

        // Unlock layer and children
        List<Layer> unlockedLayers = new List<Layer>();
        foreach (var layer in layersToUnlock)
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
}
