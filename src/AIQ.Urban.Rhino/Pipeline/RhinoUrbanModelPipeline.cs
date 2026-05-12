using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoPlugin.Extensions;
using AIQ.Urban.RhinoPlugin.Pipeline.ModelActions;
using AIQ.Urban.RhinoPlugin.Utils;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoPlugin;

public class RhinoUrbanModelPipeline(RhinoDoc _rhinoDoc, UrbanModel _urbanModel)
{
    private readonly List<IUrbanModelStep> _steps = [];
    private readonly List<Guid> _directDeleteIds = [];
    private readonly List<Action<UrbanModel>> _postActions = [];

    internal RhinoUrbanModelPipeline WithRhinoObjectDeletion(List<Guid> ids)
    {
        _directDeleteIds.AddRange(ids);
        return this;
    }

    internal RhinoUrbanModelPipeline WithUrbanElementAddition(IAddUrbanElementsStep step)
    {
        _steps.Add(step);
        return this;
    }

    internal RhinoUrbanModelPipeline WithUrbanElementDeletion(IDeleteUrbanElementsStep step)
    {
        _steps.Add(step);
        return this;
    }

    /// <summary>Register an action to execute after all steps, deletions, and baking complete.</summary>
    internal RhinoUrbanModelPipeline WithUrbanModelAction(Action<UrbanModel> action)
    {
        _postActions.Add(action);
        return this;
    }

    internal void Run()
    {
        var deletedIds = new List<Guid>();
        var editedIds = new List<Guid>();
        var addedIds = new List<Guid>();

        foreach (var step in _steps)
        {
            var result = step.Run(_urbanModel);

            if (step is IDeleteUrbanElementsStep)
                deletedIds.AddRange(result);
            if (step is IEditUrbanElementsStep)
                editedIds.AddRange(result);
            if (step is IAddUrbanElementsStep)
                addedIds.AddRange(result);
        }

        var idsToDelete = editedIds
            .Concat(deletedIds)
            .Concat(_directDeleteIds)
            .ToList();

        var idsToAdd = editedIds.Concat(addedIds).ToList();

        DeleteRhinoObjects(idsToDelete);
        AddRhinoObjects(idsToAdd);

        foreach (var action in _postActions)
            action(_urbanModel);
    }

    private void AddRhinoObjects(List<Guid> addedIds)
    {
        var elementsToAdd = FilterElementsToAdd(addedIds);
        
        var transit = elementsToAdd.OfType<Transit>().ToList();
        var buildings = elementsToAdd.OfType<Building>().ToList();

        OutputTransits(transit);
        OutputBuildings(buildings);
        OutputTransitSurfaces();
        OutputParcels(addedIds);
        OutputZones(addedIds);
    }

    private ObjectAttributes MakeAttributes(string layerPath)
    {
        var layerIndex = _rhinoDoc.Layers.EnsureLayerExists(layerPath).Index;
        return new ObjectAttributes { LayerIndex = layerIndex };
    }

    private void OutputTransitSurfaces()
    {
        foreach (var (profile, type) in _urbanModel.TransitSurfaces)
            OutputGeometry(profile, MakeAttributes(LayerConstants.TransitSurface(type)));
    }

    private void OutputParcels(List<Guid> addedIds)
    {
        foreach (var parcel in _urbanModel.Parcels)
        {
            if (!addedIds.Contains(parcel.Outline.Id))
                continue;

            OutputGeometry(parcel.Outline, MakeAttributes(LayerConstants.ParcelLayer(parcel.Use)));
        }
    }

    private void OutputZones(List<Guid> addedIds)
    {
        foreach (var zone in _urbanModel.Zones)
        {
            if (!addedIds.Contains(zone.Boundary.Id))
                continue;

            OutputGeometry(zone.Boundary, MakeAttributes(LayerConstants.ZoneLayer(zone.Name)));
        }
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
        var envelopeAttributes = MakeAttributes(LayerConstants.BuildingEnvelope);

        foreach (var building in buildings)
        {
            var newEnvelopeId = _rhinoDoc.Objects.AddBrep(building.Envelope.Geometry, envelopeAttributes);
            building.Envelope.Id = newEnvelopeId;

            foreach (var plate in building.FloorPlates)
            {
                var plateAttributes = MakeAttributes(LayerConstants.BuildingFloorPlate(plate.Use));
                var newId = _rhinoDoc.Objects.AddCurve(plate.Outline.Geometry, plateAttributes);
                plate.Outline.Id = newId;
            }
        }
    }

    private void OutputTransits(List<Transit> transits)
    {
        foreach (var transit in transits)
        {
            OutputGeometry(transit.Outline, MakeAttributes(LayerConstants.TransitOutline(transit.TransitType, transit.Width)));
            OutputGeometry(transit.Centerline, MakeAttributes(LayerConstants.TransitCenterline(transit.TransitType, transit.Width)));
        }
    }

    private void OutputGeometry(Curve2D geometry, ObjectAttributes attributes)
    {
        geometry.Id = _rhinoDoc.Objects.AddCurve(geometry.Geometry, attributes);
    }

    private void OutputGeometry(Profile2D geometry, ObjectAttributes attributes)
    {
        OutputGeometry(geometry.Outline, attributes);

        foreach (var hole in geometry.Holes)
            OutputGeometry(hole, attributes);
    }

    private void OutputGeometry(Polygon2D geometry, ObjectAttributes attributes)
    {
        geometry.Id = _rhinoDoc.Objects.AddCurve(geometry.Geometry, attributes);
    }

    private void DeleteRhinoObjects(List<Guid> idsToDelete)
    {
        _rhinoDoc.Objects.Delete(idsToDelete, true);
    }
}
