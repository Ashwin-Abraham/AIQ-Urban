using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.EventHandler;

/// <summary>
/// Subscribes to Rhino document events, queues and deduplicates them via
/// <see cref="EventQueue"/>, filters by managed layer, and dispatches to
/// the appropriate pipeline configuration. Owns the reverse-lookup
/// dictionary mapping Rhino ObjectId → <see cref="IUrbanElement"/>.
/// </summary>
public class RhinoUrbanEventHandler : IDisposable
{
    private readonly RhinoDoc _doc;
    private readonly EventQueue _queue = new();
    private bool _eventLock;
    private bool _disposed;

    /// <summary>
    /// Reverse-lookup: Rhino ObjectId → owning IUrbanElement.
    /// Rebuilt after each pipeline run by scanning UrbanModel.
    /// </summary>
    public Dictionary<Guid, IUrbanElement> ObjectIdToElement { get; } = [];

    /// <summary>
    /// Creates the handler and subscribes to all Rhino document events.
    /// </summary>
    public RhinoUrbanEventHandler(RhinoDoc doc)
    {
        _doc = doc ?? throw new ArgumentNullException(nameof(doc));

        RhinoDoc.AddRhinoObject += OnAddRhinoObject;
        RhinoDoc.DeleteRhinoObject += OnDeleteRhinoObject;
        RhinoDoc.ReplaceRhinoObject += OnReplaceRhinoObject;
        RhinoDoc.BeginOpenDocument += OnBeginOpenDocument;
        RhinoDoc.EndOpenDocument += OnEndOpenDocument;
        RhinoApp.Idle += OnIdle;
    }

    /// <summary>
    /// Prevents event processing while the pipeline writes geometry back to Rhino.
    /// Set before batch operations, cleared after.
    /// </summary>
    public bool EventLock
    {
        get => _eventLock;
        set => _eventLock = value;
    }

    /// <summary>Rebuild the reverse-lookup dict from all current UrbanModel elements.</summary>
    public void RebuildLookup()
    {
        ObjectIdToElement.Clear();
        foreach (var element in UrbanModel.Instance.GetUrbanElements())
        {
            ObjectIdToElement[element.Id] = element;   
        }
    }

    private void OnAddRhinoObject(object? sender, RhinoObjectEventArgs e)
    {
        if (_eventLock || e?.TheObject == null) return;
        _queue.EnqueueAdd(e.ObjectId);
    }

    private void OnDeleteRhinoObject(object? sender, RhinoObjectEventArgs e)
    {
        if (_eventLock || e?.TheObject == null) return;
        _queue.EnqueueDelete(e.ObjectId);
    }

    private void OnReplaceRhinoObject(object? sender, RhinoReplaceObjectEventArgs e)
    {
        if (_eventLock || e?.NewRhinoObject == null) return;
        _queue.EnqueueReplace(e.OldRhinoObject.Id, e.NewRhinoObject.Id);
    }

    private void OnBeginOpenDocument(object? sender, EventArgs e)
    {
        _eventLock = true;
    }

    private void OnEndOpenDocument(object? sender, EventArgs e)
    {
        _eventLock = false;
        // No reconciliation: UrbanModel is empty after file open (accepted gap).
    }

    private void OnIdle(object? sender, EventArgs e)
    {
        if (_eventLock || _disposed) return;
        ProcessQueue();
    }

    private void ProcessQueue()
    {
        var batch = _queue.Drain();
        if (batch.IsEmpty) return;

        // Process deletions
        foreach (var id in batch.Deletes)
        {
            if (!ObjectIdToElement.TryGetValue(id, out var element))
                continue;

            ObjectIdToElement.Remove(id);

            switch (element)
            {
                case Transit transit:
                    HandleTransitDelete(transit);
                    break;
                case Building building:
                    HandleBuildingDelete(building);
                    break;
                case Zone zone:
                    HandleZoneDelete(zone);
                    break;
            }
        }

        // Process replacements
        foreach (var (oldId, newId) in batch.Replacements)
        {
            // Some cases of replacement have a Rhino bug. The newId is empty but replacement has occurred.
            var newId2 = newId == Guid.Empty ? oldId : newId;
            
            if (!ObjectIdToElement.TryGetValue(oldId, out var element))
                continue;

            //if (!IsOnManagedLayer(newObj)) continue;

            //element = ObjectIdToElement.TryGetValue(oldId, out var directLookup)
            //    ? directLookup
            //    : FindElementByLayer(newObj);

            if (element == null) continue;

            // Remove old mapping for this element's previous geometry IDs
            var oldIds = element.GetGeometryIds();
            foreach (var oid in oldIds)
                ObjectIdToElement.Remove(oid);

            switch (element)
            {
                case Transit transit:
                    HandleTransitReplace(transit, newId2);
                    break;
                
                case Parcel parcel:
                    HandleParcelReplace(parcel, newId2);
                    break;
                
                case Zone zone:
                    HandleZoneReplace(zone, newId2);
                    break;
                
                case Building building:
                    HandleBuildingReplace(building, newId2);
                    break;
                
                case FloorPlate floorPlate:
                    HandleFloorPlateReplace(floorPlate, newId2);
                    break;
            }
        }

        _doc.Views.Redraw();
    }

    private bool IsOnManagedLayer(RhinoObject rhinoObject)
    {
        var layerIndex = rhinoObject.Attributes.LayerIndex;
        var layer = _doc.Layers[layerIndex];
        if (layer == null) return false;

        var fullPath = layer.FullPath;
        return fullPath.StartsWith("AIQ Urban::", StringComparison.OrdinalIgnoreCase);
    }

    private IUrbanElement? FindElementByLayer(RhinoObject rhinoObject)
    {
        var objectId = rhinoObject.Id;
        var layerIndex = rhinoObject.Attributes.LayerIndex;
        var layer = _doc.Layers[layerIndex];
        if (layer == null) return null;

        var fullPath = layer.FullPath;

        // Match layer to element type and find the most recently modified element
        if (fullPath.Contains("::Transit::", StringComparison.OrdinalIgnoreCase))
        {
            // For transits, the centerline is on AIQ Urban::Transit::{Type}::Centerline
            // Since we're processing a replace on a centerline, find the transit
            // that has geometry on this layer. Use the objectId in the reverse-lookup.
            if (ObjectIdToElement.TryGetValue(objectId, out var existing))
                return existing;

            // Fallback: return first transit (will be refined in later slices)
            return UrbanModel.Instance.Transits.FirstOrDefault();
        }

        if (fullPath.Contains("::Building::", StringComparison.OrdinalIgnoreCase))
        {
            if (ObjectIdToElement.TryGetValue(objectId, out var existing))
                return existing;

            return UrbanModel.Instance.Buildings.FirstOrDefault();
        }

        if (fullPath.Contains("::Zone::", StringComparison.OrdinalIgnoreCase))
        {
            if (ObjectIdToElement.TryGetValue(objectId, out var existing))
                return existing;

            return UrbanModel.Instance.Zones.FirstOrDefault();
        }

        if (fullPath.Contains("::Parcel::", StringComparison.OrdinalIgnoreCase))
        {
            if (ObjectIdToElement.TryGetValue(objectId, out var existing))
                return existing;

            var useName = fullPath.Split("::Parcel::").Last();
            return UrbanModel.Instance.Parcels
                .FirstOrDefault(p => p.Use.Name.Equals(useName, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private void HandleTransitDelete(Transit transit)
    {
        var centerlineIds = transit.GetGeometryIds().ToList();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
            .WithTransitDeletionSteps(centerlineIds)
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    private void HandleTransitReplace(Transit transit, Guid newCenterlineId)
    {
        var newRhinoObject = _doc.Objects.FindId(newCenterlineId);
        if (newRhinoObject == null) return;

        var newCurve = newRhinoObject.Geometry as Curve;
        if (newCurve == null) return;

        var oldCenterlineId = transit.GetGeometryIds().FirstOrDefault();
        if (oldCenterlineId == Guid.Empty) return;

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
            .WithTransitUpdateSteps(oldCenterlineId, newCurve)
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    private void HandleBuildingDelete(Building building)
    {
        var geometryIds = building.GetGeometryIds();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
            .WithBuildingDeletionSteps(geometryIds)
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }


    private void HandleBuildingReplace(Building building, Guid newEnvelopeId)
    {
        var newRhinoObject = _doc.Objects.FindId(newEnvelopeId);
        if (newRhinoObject == null) return;

        var newBrep = newRhinoObject.Geometry as Brep;
        if (newBrep == null) return;

        var oldPlateUses = building.FloorPlates
            .OrderBy(p => p.Z())
            .Select(p => p.Use)
            .ToList();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
            .WithRhinoObjectDeletion([newEnvelopeId])
            .WithBuildingUpdateSteps(building.Envelope.Id, newBrep, building.TypicalFloorHeight, oldPlateUses)
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    private void HandleFloorPlateReplace(FloorPlate floorPlate, Guid newCurveId)
    {
        var newRhinoObject = _doc.Objects.FindId(newCurveId);
        if (newRhinoObject?.Geometry is not Curve newCurve) return;

        var newZ = newCurve.PointAtStart.Z;

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
            .WithRhinoObjectDeletion([newCurveId])
            .WithFloorPlateReplaceSteps(floorPlate, newZ)
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    private void HandleZoneDelete(Zone zone)
    {
        var geometryIds = zone.GetGeometryIds();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
            .WithZoneDeletionSteps(geometryIds)
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    private void HandleZoneReplace(Zone zone, Guid newBoundaryId)
    {
        var newRhinoObject = _doc.Objects.Find(newBoundaryId);
        if (newRhinoObject == null) return;

        var newCurve = newRhinoObject.Geometry as Curve;
        if (newCurve == null) return;

        var oldBoundaryId = zone.GetGeometryIds().FirstOrDefault();
        if (oldBoundaryId == Guid.Empty) return;

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
            .WithZoneUpdateSteps(oldBoundaryId, newCurve)
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    private void HandleParcelReplace(Parcel parcel, Guid newId)
    {
        try
        {
            _eventLock = true;
            ParcelOutputter outputter = new ParcelOutputter();
            outputter.Run(UrbanModel.Instance, _doc, [parcel]);
            _doc.Objects.Delete(newId, true);
            ObjectIdToElement[parcel.Outline.Id] = parcel;
        }
        finally
        {
            _eventLock = false;
        }
    }

    private void RunPipeline(RhinoUrbanModelPipeline pipeline)
    {
        _eventLock = true;
        try
        {
            pipeline.Run();
            RebuildLookup();
        }
        finally
        {
            _eventLock = false;
        }
    }

    /// <summary>
    /// Unsubscribes all events and clears state.
    /// Safe to call multiple times.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        RhinoDoc.AddRhinoObject -= OnAddRhinoObject;
        RhinoDoc.DeleteRhinoObject -= OnDeleteRhinoObject;
        RhinoDoc.ReplaceRhinoObject -= OnReplaceRhinoObject;
        RhinoDoc.BeginOpenDocument -= OnBeginOpenDocument;
        RhinoDoc.EndOpenDocument -= OnEndOpenDocument;
        RhinoApp.Idle -= OnIdle;

        ObjectIdToElement.Clear();
        _queue.Clear();
    }
}
