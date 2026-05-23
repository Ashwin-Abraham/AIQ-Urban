using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface;

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
            foreach (var id in element.GetGeometryIds())
            {
                ObjectIdToElement[id] = element;
            }
        }
    }

    private void OnAddRhinoObject(object? sender, RhinoObjectEventArgs e)
    {
        if (_eventLock || e?.ObjectId == null) return;
        _queue.EnqueueAdd(e.ObjectId);
    }

    private void OnDeleteRhinoObject(object? sender, RhinoObjectEventArgs e)
    {
        if (_eventLock || e?.ObjectId == null) return;
        _queue.EnqueueDelete(e.ObjectId);
    }

    private void OnReplaceRhinoObject(object? sender, RhinoReplaceObjectEventArgs e)
    {
        if (_eventLock || e?.ObjectId == null) return;

        // ReplaceRhinoObject args give us the new object's ID.
        // The old object is already gone from the doc.
        // We generate a placeholder old ID — at processing time,
        // we look up by the new object's layer and current model state.
        _queue.EnqueueReplace(Guid.NewGuid(), e.ObjectId);
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
            if (!IsOnManagedLayer(id))
            {
                ObjectIdToElement.Remove(id);
                continue;
            }

            if (!ObjectIdToElement.TryGetValue(id, out var element))
                continue;

            ObjectIdToElement.Remove(id);

            switch (element)
            {
                case Transit transit:
                    HandleTransitDelete(transit);
                    break;
            }
        }

        // Process replacements
        foreach (var (oldId, newId) in batch.Replacements)
        {
            if (!IsOnManagedLayer(newId)) continue;

            // Try to find the element by scanning UrbanModel
            // (the old ID was a placeholder, not useful for lookup)
            var element = FindElementByLayer(newId);
            if (element == null) continue;

            // Remove old mapping for this element's previous geometry IDs
            var oldIds = element.GetGeometryIds();
            foreach (var oid in oldIds)
                ObjectIdToElement.Remove(oid);

            switch (element)
            {
                case Transit transit:
                    HandleTransitReplace(transit, newId);
                    break;
            }
        }
    }

    private bool IsOnManagedLayer(Guid objectId)
    {
        var rhinoObject = _doc.Objects.Find(objectId);
        if (rhinoObject == null) return false;

        var layerIndex = rhinoObject.Attributes.LayerIndex;
        var layer = _doc.Layers[layerIndex];
        if (layer == null) return false;

        var fullPath = layer.FullPath;
        return fullPath.StartsWith("AIQ Urban::", StringComparison.OrdinalIgnoreCase);
    }

    private IUrbanElement? FindElementByLayer(Guid objectId)
    {
        var rhinoObject = _doc.Objects.Find(objectId);
        if (rhinoObject == null) return null;

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

        return null;
    }

    private void HandleTransitDelete(Transit transit)
    {
        var centerlineIds = transit.GetGeometryIds().ToList();

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance);
        new PipelineConfigurations(pipeline)
            .WithTransitDeletionSteps(centerlineIds)
            .WithMetricsCalculation();

        RunPipeline(pipeline);
    }

    private void HandleTransitReplace(Transit transit, Guid newCenterlineId)
    {
        var newRhinoObject = _doc.Objects.Find(newCenterlineId);
        if (newRhinoObject == null) return;

        var newCurve = newRhinoObject.Geometry as Curve;
        if (newCurve == null) return;

        var oldCenterlineId = transit.GetGeometryIds().FirstOrDefault();
        if (oldCenterlineId == Guid.Empty) return;

        var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance);
        new PipelineConfigurations(pipeline)
            .WithTransitUpdateSteps(oldCenterlineId, newCurve)
            .WithMetricsCalculation();

        RunPipeline(pipeline);
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
