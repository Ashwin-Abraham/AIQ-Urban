using AIQ.Urban.Core;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Logging;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using Microsoft.Extensions.Logging;
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
    private bool _modelStateChanged;

    public event System.EventHandler? SelectionChanged;

    /// <summary>
    /// Fired at the end of each ProcessQueue batch when a pipeline mutation occurred.
    /// </summary>
    public event System.EventHandler? ModelChanged;

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
        RhinoDoc.SelectObjects += OnSelectionChanged;
        RhinoDoc.DeselectObjects += OnSelectionChanged;
        RhinoDoc.DeselectAllObjects += OnDeselectAllObjects;
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

    private void OnSelectionChanged(object? sender, RhinoObjectSelectionEventArgs e)
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnDeselectAllObjects(object? sender, RhinoDeselectAllObjectsEventArgs e)
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
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

        _modelStateChanged = false;

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
                case ParcelBoundary boundary:
                    HandleParcelBoundaryDelete(boundary);
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

                case ParcelBoundary boundary:
                    HandleParcelBoundaryReplace(boundary, newId2);
                    break;
            }
        }

        _doc.Views.Redraw();

        if (_modelStateChanged)
        {
            _modelStateChanged = false;
            ModelChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void HandleTransitDelete(Transit transit)
    {
        var centerlineIds = transit.GetGeometryIds().ToList();

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new TransitDeletionGatherStep(centerlineIds))
            .WithTransitDeletionSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    internal void HandleTransitReplace(Transit transit, Guid newCenterlineId)
    {
        var newRhinoObject = _doc.Objects.FindId(newCenterlineId);
        if (newRhinoObject == null) return;

        var newCurve = newRhinoObject.Geometry as Curve;
        if (newCurve == null) return;

        var oldCenterlineId = transit.GetGeometryIds().FirstOrDefault();
        if (oldCenterlineId == Guid.Empty) return;

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        if (!newCurve.TryNormalizeTransitCenterline(out var normalizedCurve))
        {
            var bounds = newCurve.GetBoundingBox(true);
            loggerFactory.CreateLogger<RhinoUrbanEventHandler>().LogWarning(
                "Rejected replacement for transit centerline {CenterlineId}: Rhino object {ObjectId} has Z range [{MinZ}, {MaxZ}], outside tolerance {Tolerance}",
                oldCenterlineId,
                newCenterlineId,
                bounds.Min.Z,
                bounds.Max.Z,
                AIQ.Primitives.NumericTolerance.Default);

            RestoreTransit(oldCenterlineId, newCenterlineId, loggerFactory);
            return;
        }

        if (normalizedCurve.IsSelfIntersecting())
        {
            loggerFactory.CreateLogger<RhinoUrbanEventHandler>().LogWarning(
                "Rejected replacement for transit centerline {CenterlineId}: Rhino object {ObjectId} self-intersects within tolerance {Tolerance}",
                oldCenterlineId,
                newCenterlineId,
                AIQ.Primitives.NumericTolerance.Default);

            RestoreTransit(oldCenterlineId, newCenterlineId, loggerFactory);
            return;
        }

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new TransitUpdateGatherStep(oldCenterlineId, newCenterlineId, normalizedCurve))
            .WithTransitUpdateSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    private void RestoreTransit(Guid oldCenterlineId, Guid rejectedRhinoObjectId, ILoggerFactory loggerFactory)
    {
        var restorePipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new TransitRestoreGatherStep(oldCenterlineId, rejectedRhinoObjectId))
            .WithTransitRestoreSteps()
            .WithRhinoOutputter(new TransitOutputter(loggerFactory));

        RunPipeline(restorePipeline);
    }

    private void HandleBuildingDelete(Building building)
    {
        var geometryIds = building.GetGeometryIds();

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new BuildingDeletionGatherStep(geometryIds))
            .WithBuildingDeletionSteps()
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

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new BuildingUpdateGatherStep(
                building.Envelope.Id,
                newEnvelopeId,
                newBrep,
                building.TypicalFloorHeight,
                oldPlateUses))
            .WithBuildingUpdateSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    private void HandleFloorPlateReplace(FloorPlate floorPlate, Guid newCurveId)
    {
        var newRhinoObject = _doc.Objects.FindId(newCurveId);
        if (newRhinoObject?.Geometry is not Curve newCurve) return;

        var newZ = newCurve.PointAtStart.Z;

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new FloorPlateReplaceGatherStep(floorPlate, newCurveId, newZ))
            .WithFloorPlateReplaceSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    private void HandleZoneDelete(Zone zone)
    {
        var geometryIds = zone.GetGeometryIds();

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new ZoneDeletionGatherStep(geometryIds))
            .WithZoneDeletionSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    internal void HandleParcelBoundaryDelete(ParcelBoundary boundary)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new ParcelBoundaryDeletionGatherStep([boundary.Id]))
            .WithParcelBoundaryDeletionSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    internal void HandleParcelBoundaryReplace(ParcelBoundary boundary, Guid newBoundaryId)
    {
        var newRhinoObject = _doc.Objects.FindId(newBoundaryId);
        if (newRhinoObject?.Geometry is not Curve newCurve)
            return;

        if (!newCurve.TryNormalizeParcelBoundary(out var normalized))
        {
            _eventLock = true;
            try
            {
                _doc.Objects.Delete(newBoundaryId, true);
                new ParcelBoundaryOutputter().Run(UrbanModel.Instance, _doc, [boundary]);
                RebuildLookup();
            }
            finally
            {
                _eventLock = false;
            }
            return;
        }

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new ParcelBoundaryUpdateGatherStep(boundary.Id, newBoundaryId, normalized))
            .WithParcelBoundaryUpdateSteps()
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

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new ZoneUpdateGatherStep(oldBoundaryId, newCurve))
            .WithZoneUpdateSteps()
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
            int mutationCount = pipeline.Run();
            _modelStateChanged |= mutationCount > 0;
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
        RhinoDoc.SelectObjects -= OnSelectionChanged;
        RhinoDoc.DeselectObjects -= OnSelectionChanged;
        RhinoDoc.DeselectAllObjects -= OnDeselectAllObjects;
        RhinoDoc.BeginOpenDocument -= OnBeginOpenDocument;
        RhinoDoc.EndOpenDocument -= OnEndOpenDocument;
        RhinoApp.Idle -= OnIdle;

        ObjectIdToElement.Clear();
        _queue.Clear();
        SelectionChanged = null;
        ModelChanged = null;
    }
}
