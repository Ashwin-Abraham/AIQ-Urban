using AIQ.Urban.Core;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Logging;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
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
                case Transit:
                    HandleTransitDelete(id);
                    break;
                case Building:
                    HandleBuildingDelete(id);
                    break;
                case Zone:
                    HandleZoneDelete(id);
                    break;
                case ParcelBoundary:
                    HandleParcelBoundaryDelete(id);
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
                case Transit:
                    HandleTransitReplace(oldId, newId2);
                    break;
                
                case Parcel:
                    HandleParcelReplace(oldId, newId2);
                    break;
                
                case Zone:
                    HandleZoneReplace(oldId, newId2);
                    break;
                
                case Building:
                    HandleBuildingReplace(oldId, newId2);
                    break;
                
                case FloorPlate:
                    HandleFloorPlateReplace(oldId, newId2);
                    break;

                case ParcelBoundary:
                    HandleParcelBoundaryReplace(oldId, newId2);
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

    private void HandleTransitDelete(Guid geometryId)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();
        var preparationStep = new DeleteTransitInputPreparationStep([geometryId]);
        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithTransitDeletionSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();
        RunPipeline(pipeline);
    }

    internal void HandleTransitReplace(Guid oldGeometryId, Guid newCenterlineId)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();
        var preparationStep = new ReplaceTransitInputPreparationStep(oldGeometryId, newCenterlineId);
        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithTransitUpdateSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();
        RunPipeline(pipeline);
    }

    private void HandleBuildingDelete(Guid geometryId)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();
        var preparationStep = new DeleteBuildingInputPreparationStep([geometryId]);
        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithBuildingDeletionSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();
        RunPipeline(pipeline);
    }

    private void HandleBuildingReplace(Guid oldGeometryId, Guid newEnvelopeId)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();
        var preparationStep = new ReplaceBuildingInputPreparationStep(oldGeometryId, newEnvelopeId);
        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithBuildingUpdateSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();
        RunPipeline(pipeline);
    }

    private void HandleFloorPlateReplace(Guid oldGeometryId, Guid newCurveId)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();
        var preparationStep = new ReplaceFloorPlateInputPreparationStep(oldGeometryId, newCurveId);
        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithFloorPlateReplaceSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();
        RunPipeline(pipeline);
    }

    private void HandleZoneDelete(Guid geometryId)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();
        var preparationStep = new DeleteZoneInputPreparationStep([geometryId]);
        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithZoneDeletionSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();
        RunPipeline(pipeline);
    }

    internal void HandleParcelBoundaryDelete(Guid geometryId)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();
        var preparationStep = new DeleteParcelBoundaryInputPreparationStep([geometryId]);
        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithParcelBoundaryDeletionSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();
        RunPipeline(pipeline);
    }

    internal void HandleParcelBoundaryReplace(Guid oldGeometryId, Guid newBoundaryId)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();
        var preparationStep = new ReplaceParcelBoundaryInputPreparationStep(oldGeometryId, newBoundaryId);
        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithParcelBoundaryUpdateSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();
        RunPipeline(pipeline);
    }

    private void HandleZoneReplace(Guid oldGeometryId, Guid newBoundaryId)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();
        var preparationStep = new ReplaceZoneInputPreparationStep(oldGeometryId, newBoundaryId);
        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithZoneUpdateSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation();
        RunPipeline(pipeline);
    }

    private void HandleParcelReplace(Guid oldGeometryId, Guid newId)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();
        var preparationStep = new RejectParcelReplacementInputPreparationStep(oldGeometryId, newId);
        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithRhinoOutputter<ParcelOutputter>();
        RunPipeline(pipeline);
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
