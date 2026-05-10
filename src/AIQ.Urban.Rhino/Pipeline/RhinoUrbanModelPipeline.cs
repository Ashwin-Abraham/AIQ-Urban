using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoPlugin.Extensions;
using AIQ.Urban.RhinoPlugin.Pipeline.ModelActions;
using AIQ.Urban.RhinoPlugin.Utils;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoPlugin;

public class RhinoUrbanModelPipeline(RhinoDoc _rhinoDoc, UrbanModel _urbanModel)
{
    //private readonly List<Transit> _pendingTransits = [];

    //public RhinoUrbanModelPipeline RemoveMatchingElements(
    //    List<Guid> matchIds,
    //    List<Guid> exemptFromRhinoDelete)
    //{
    //    var toRemove = new List<IUrbanElement>();

    //    toRemove.AddRange(UrbanModel.Instance.Transits
    //        .Where(t => t.GetGeometryIds().Any(id => matchIds.Contains(id))));
    //    toRemove.AddRange(UrbanModel.Instance.Buildings
    //        .Where(b => b.GetGeometryIds().Any(id => matchIds.Contains(id))));

    //    foreach (var element in toRemove)
    //    {
    //        var deleteIds = element.GetGeometryIds()
    //            .Where(id => !exemptFromRhinoDelete.Contains(id))
    //            .ToList();
    //        _rhinoDoc.Objects.Delete(deleteIds, true);

    //        if (element is Transit transit)
    //            UrbanModel.Instance.Transits.Remove(transit);
    //        else if (element is Building building)
    //            UrbanModel.Instance.Buildings.Remove(building);
    //    }

    //    return this;
    //}

    //public RhinoUrbanModelPipeline ChangeInputCurvesLayer(
    //    List<Guid> curveIds,
    //    TransitType transitType,
    //    double width)
    //{
    //    var layerPath = LayerConstants.TransitCenterline(transitType, width);
    //    var layer = _rhinoDoc.Layers.EnsureLayerExists(layerPath);
    //    var layerIndex = layer.Index;

    //    foreach (var id in curveIds)
    //    {
    //        var rhinoObj = _rhinoDoc.Objects.Find(id);
    //        if (rhinoObj == null) continue;

    //        var attrs = rhinoObj.Attributes.Duplicate() as ObjectAttributes
    //            ?? new ObjectAttributes();
    //        attrs.LayerIndex = layerIndex;
    //        _rhinoDoc.Objects.ModifyAttributes(rhinoObj, attrs, quiet: true);
    //    }

    //    return this;
    //}

    //public RhinoUrbanModelPipeline AddTransit(List<Transit> transits)
    //{
    //    foreach (var transit in transits)
    //    {
    //        UrbanModel.Instance.AddTransit(transit);
    //        _pendingTransits.Add(transit);
    //    }

    //    return this;
    //}

    //public void BakeNewUrbanElements()
    //{
    //    foreach (var transit in _pendingTransits)
    //    {
    //        var outlineLayer = LayerConstants.TransitOutline(transit.TransitType, transit.Width);
    //        var outlineLayerIndex = _rhinoDoc.Layers.EnsureLayerExists(outlineLayer).Index;
    //        var outlineAttributes = new ObjectAttributes { LayerIndex = outlineLayerIndex };

    //        var outlineId = _rhinoDoc.Objects.AddCurve(transit.Outline.Geometry, outlineAttributes);
    //        transit.Outline.Id = outlineId;
    //    }

    //    _pendingTransits.Clear();
    //}

    private readonly List<IAddUrbanElementsStep> _addUrbanElementSteps = [];
    private readonly List<IDeleteUrbanElementsStep> _deleteUrbanElementsSteps = [];
    private readonly List<IEditUrbanElementsStep> _editUrbanElementsSteps = [];
    private readonly List<Guid> _directDeleteIds = [];

    internal RhinoUrbanModelPipeline WithRhinoObjectDeletion(List<Guid> ids)
    {
        _directDeleteIds.AddRange(ids);
        return this;
    }

    internal RhinoUrbanModelPipeline WithUrbanElementAddition(IAddUrbanElementsStep step)
    {
        _addUrbanElementSteps.Add(step);
        return this;
    }

    internal RhinoUrbanModelPipeline WithUrbanElementDeletion(IDeleteUrbanElementsStep step)
    {
        _deleteUrbanElementsSteps.Add(step);
        return this;
    }

    internal void Run()
    {
        var deletedIds = _deleteUrbanElementsSteps.SelectMany(x => x.Run(_urbanModel));

        var editedIds = _editUrbanElementsSteps.SelectMany(x => x.Run(_urbanModel));

        var addedIds = _addUrbanElementSteps.SelectMany(x => x.Run(_urbanModel)).ToList();

        var idsToDelete = editedIds
            .Concat(deletedIds)
            .Concat(_directDeleteIds)
            .ToList();

        var idsToAdd = editedIds.Concat(addedIds).ToList();

        DeleteRhinoObjects(idsToDelete);
        AddRhinoObjects(idsToAdd);
    }

    private void AddRhinoObjects(List<Guid> addedIds)
    {
        var elementsToAdd = FilterElementsToAdd(addedIds);
        
        var transit = elementsToAdd.OfType<Transit>().ToList();
        var buildings = elementsToAdd.OfType<Building>().ToList();

        OutputTransits(transit);
        OutputBuildings(buildings);

    }

    private List<IUrbanElement> FilterElementsToAdd(List<Guid> addedIds)
    {
        var elementsToAdd = new List<IUrbanElement>();
        var allElements = _urbanModel.GetUrbanElements();

        foreach(var element in allElements)
        {
            var ids = element.GetGeometryIds();

            if (!ids.Any(addedIds.Contains))
                continue;

            elementsToAdd.Add(element);
        }

        return elementsToAdd;
    }

    private void OutputBuildings(List<Building> buildings)
    {
        var envelopeLayer = _rhinoDoc.Layers.EnsureLayerExists(LayerConstants.BuildingEnvelope).Index;

        foreach (var building in buildings)
        {
            var envelopeAttributes = new ObjectAttributes { LayerIndex = envelopeLayer };
            var newEnvelopeId = _rhinoDoc.Objects.AddBrep(building.Envelope.Geometry, envelopeAttributes);
            building.Envelope.Id = newEnvelopeId;

            foreach (var plate in building.FloorPlates)
            {
                var plateLayer = _rhinoDoc.Layers.EnsureLayerExists(LayerConstants.BuildingFloorPlate(plate.Use)).Index;
                var plateAttributes = new ObjectAttributes { LayerIndex = plateLayer };
                var newId = _rhinoDoc.Objects.AddCurve(plate.Outline.Geometry, plateAttributes);

                plate.Outline.Id = newId;
            }
        }
        
    }

    private void OutputTransits(List<Transit> transits)
    {
        foreach (var transit in transits)
        {
            var outlineLayer = LayerConstants.TransitOutline(transit.TransitType, transit.Width);
            var outlineLayerIndex = _rhinoDoc.Layers.EnsureLayerExists(outlineLayer).Index;
            var outlineAttributes = new ObjectAttributes { LayerIndex = outlineLayerIndex };

            var outlineId = _rhinoDoc.Objects.AddCurve(transit.Outline.Geometry, outlineAttributes);
            transit.Outline.Id = outlineId;
        }
    }

    private void DeleteRhinoObjects(List<Guid> idsToDelete)
    {
        _rhinoDoc.Objects.Delete(idsToDelete, true);
    }
}
