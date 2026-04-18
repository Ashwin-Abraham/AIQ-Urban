using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.GH.Exports.Enums;
using AIQ.GH.Exports.Models;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.GH.Exports.Helpers;

public static class DetailHelpers
{
    public static DetailViewObject? GetDetailByName(
        RhinoPageView layout,
        string detailName)
    {
        return GetAllDetails(layout)
            .FirstOrDefault(d => d.Name == detailName);
    }

    public static List<DetailViewObject> GetAllDetails(RhinoPageView layout)
    {
        return layout.GetDetailViews().ToList();
    }

    public static List<DetailViewObject> FindDetails(
        List<RhinoPageView> layouts,
        string? detailName,
        TextMatchStrategy strategy)
    {
        var allDetails = layouts
            .SelectMany(l => l.GetDetailViews())
            .ToList();

        if (string.IsNullOrEmpty(detailName))
            return allDetails;

        return allDetails
            .Where(d => TextMatchHelpers.Matches(d.Name, detailName, strategy))
            .ToList();
    }

    public static void SetDetailTarget(
        DetailViewObject detail,
        Point3d worldTarget,
        double modelScaleRatio,
        RhinoDoc doc)
    {
        SetDetailScale(detail, modelScaleRatio, doc);
        detail.Viewport.SetCameraTarget(worldTarget, true);
        detail.CommitChanges();
    }

    public static void SetDetailTarget(
        DetailViewObject detail,
        Point3d worldTarget,
        double modelScaleRatio,
        AnchorPosition anchorPosition,
        RhinoDoc doc)
    {
        // Aligns the detail center to the target point
        SetDetailTarget(detail, worldTarget, modelScaleRatio, doc);
        

        var viewInfo = new ViewportInfo(detail.Viewport);
        viewInfo.GetFrustum(out double left, out double right, out double bottom, out double top, out _, out _);
        
        var worldWidth = right - left;
        var worldHeight = top - bottom;

        double offsetX, offsetY;
        switch (anchorPosition)
        {
            case AnchorPosition.TopLeft:
                offsetX = worldWidth / 2;
                offsetY = -worldHeight / 2;
                break;

            case AnchorPosition.TopRight:
                offsetX = -worldWidth / 2;
                offsetY = -worldHeight / 2;
                break;

            case AnchorPosition.BottomLeft:
                offsetX = worldWidth / 2;
                offsetY = worldHeight / 2;
                break;

            case AnchorPosition.BottomRight:
                offsetX = -worldWidth / 2;
                offsetY = worldHeight / 2;
                break;

            default:
                offsetX = 0; offsetY = 0; break;
        }

        var newCam = new Point3d(
            detail.Viewport.CameraTarget.X + offsetX, 
            detail.Viewport.CameraTarget.Y + offsetY,
            detail.Viewport.CameraTarget.Z);

        detail.Viewport.SetCameraTarget(newCam, true);
        detail.CommitViewportChanges();
        detail.CommitChanges();
    }


    public static void SetDetailScale(
        DetailViewObject detail,
        double modelUnitsPerPageUnit,
        RhinoDoc doc)
    {
        
        var detailGeometry = detail.DetailGeometry;
        if (detailGeometry == null) return;

        detailGeometry.SetScale(modelUnitsPerPageUnit, doc.ModelUnitSystem, 1, doc.PageUnitSystem);
        detail.CommitChanges();

        var vpId = detail.Viewport.Id;
        var layer = doc.Layers[3];
        layer.SetPerViewportVisible(vpId, true);

    }

    public static void SetDetailVisibility(
        DetailViewObject detail,
        List<(int LayerIndex, bool LayerOn)> layerVisibilityConfigs,
        RhinoDoc doc)
    {
        var vpId = detail.Viewport.Id;

        foreach (var config in layerVisibilityConfigs)
        {
            var layer = doc.Layers.ElementAtOrDefault(config.LayerIndex);
            if (layer == null) continue;

            layer.SetPerViewportVisible(vpId, config.LayerOn);
        }

        detail.CommitChanges();
    }

    public static void ApplyLayerStateToDetail(
        DetailViewObject detail,
        DocLayerState layerState,
        RhinoDoc doc)
    {
        var vpId = detail.Viewport.Id;

        foreach (var props in layerState.LayerState)
        {
            var layerIndex = doc.Layers.FindByFullPath(props.FullPath, -1);
            if (layerIndex < 0) continue;

            var layer = doc.Layers[layerIndex];
            ApplyLayerPropertyToViewport(layer, vpId, props);
        }

        detail.CommitChanges();
    }

    private static void ApplyLayerPropertyToViewport(
        Layer layer,
        Guid viewportId,
        LayerProperties props)
    {
        if (props.LayerOn.HasValue)
            SetLayerVisibilityForViewport(layer, viewportId, props.LayerOn.Value);

        if (props.LayerDisplayColor.HasValue)
            SetLayerColorForViewport(layer, viewportId, props.LayerDisplayColor.Value);

        if (props.LayerPlotColor.HasValue)
            SetLayerPlotColorForViewport(layer, viewportId, props.LayerPlotColor.Value);

        if (props.LayerPlotWeight.HasValue)
            SetLayerPlotWeightForViewport(layer, viewportId, props.LayerPlotWeight.Value);
    }

    public static void SetLayerVisibilityForViewport(
        Layer layer,
        Guid viewportId,
        bool isVisible)
    {
        layer.SetPerViewportVisible(viewportId, isVisible);
    }

    public static void SetLayerColorForViewport(
        Layer layer,
        Guid viewportId,
        Color color)
    {
        layer.SetPerViewportColor(viewportId, color);
    }

    public static void SetLayerPlotColorForViewport(
        Layer layer,
        Guid viewportId,
        Color plotColor)
    {
        layer.SetPerViewportPlotColor(viewportId, plotColor);
    }

    public static void SetLayerPlotWeightForViewport(
        Layer layer,
        Guid viewportId,
        double plotWeight)
    {
        layer.SetPerViewportPlotWeight(viewportId, plotWeight);
    }
}
