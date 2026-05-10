using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoPlugin.Extensions;
using AIQ.Urban.RhinoPlugin.Utils;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoPlugin;

public class RhinoUrbanModelPipeline(RhinoDoc doc)
{
    private readonly List<Transit> _pendingTransits = [];

    public RhinoUrbanModelPipeline RemoveMatchingElements(
        List<Guid> matchIds,
        List<Guid> exemptFromRhinoDelete)
    {
        var toRemove = new List<IUrbanElement>();

        toRemove.AddRange(UrbanModel.Instance.Transits
            .Where(t => t.GetGeometryIds().Any(id => matchIds.Contains(id))));
        toRemove.AddRange(UrbanModel.Instance.Buildings
            .Where(b => b.GetGeometryIds().Any(id => matchIds.Contains(id))));

        foreach (var element in toRemove)
        {
            var deleteIds = element.GetGeometryIds()
                .Where(id => !exemptFromRhinoDelete.Contains(id))
                .ToList();
            doc.Objects.Delete(deleteIds, true);

            if (element is Transit transit)
                UrbanModel.Instance.Transits.Remove(transit);
            else if (element is Building building)
                UrbanModel.Instance.Buildings.Remove(building);
        }

        return this;
    }

    public RhinoUrbanModelPipeline ChangeInputCurvesLayer(
        List<Guid> curveIds,
        TransitType transitType,
        double width)
    {
        var layerPath = LayerConstants.TransitCenterline(transitType, width);
        var layer = doc.Layers.EnsureLayerExists(layerPath);
        var layerIndex = layer.Index;

        foreach (var id in curveIds)
        {
            var rhinoObj = doc.Objects.Find(id);
            if (rhinoObj == null) continue;

            var attrs = rhinoObj.Attributes.Duplicate() as ObjectAttributes
                ?? new ObjectAttributes();
            attrs.LayerIndex = layerIndex;
            doc.Objects.ModifyAttributes(rhinoObj, attrs, quiet: true);
        }

        return this;
    }

    public RhinoUrbanModelPipeline AddTransit(List<Transit> transits)
    {
        foreach (var transit in transits)
        {
            UrbanModel.Instance.AddTransit(transit);
            _pendingTransits.Add(transit);
        }

        return this;
    }

    public void BakeNewUrbanElements()
    {
        foreach (var transit in _pendingTransits)
        {
            var outlineLayer = LayerConstants.TransitOutline(transit.TransitType, transit.Width);
            var outlineLayerIndex = doc.Layers.EnsureLayerExists(outlineLayer).Index;
            var outlineAttributes = new ObjectAttributes { LayerIndex = outlineLayerIndex };

            var outlineId = doc.Objects.AddCurve(transit.Outline.Geometry, outlineAttributes);
            transit.Outline.Id = outlineId;
        }

        _pendingTransits.Clear();
    }
}
