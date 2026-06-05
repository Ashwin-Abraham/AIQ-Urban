using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoInterface.EventHandler;

/// <summary>
/// Pure queuing and deduplication of Rhino object events. No Rhino dependency —
/// operates on plain Guids. Testable in isolation.
/// </summary>
public class EventQueue
{
    private readonly HashSet<Guid> _adds = [];
    private readonly HashSet<Guid> _deletes = [];
    private readonly Dictionary<Guid, Guid> _replacements = new(); // oldId -> newId

    /// <summary>Queue an object for addition.</summary>
    public void EnqueueAdd(Guid obj)
    {
        _adds.Add(obj);
    }

    /// <summary>Queue an object for deletion.</summary>
    public void EnqueueDelete(Guid obj)
    {
        _deletes.Add(obj);
    }

    /// <summary>
    /// Queue an object replacement. If multiple replaces arrive for the same
    /// old ID, only the final new ID is kept.
    /// </summary>
    public void EnqueueReplace(Guid oldObject, Guid newObject)
    {
        _replacements[oldObject] = newObject;
        _adds.Remove(newObject);
        _deletes.Remove(newObject);
    }

    /// <summary>
    /// Drain all queued events, apply deduplication rules, and return the
    /// processed batch. Clears internal queues after drain.
    /// </summary>
    public EventQueueBatch Drain()
    {
        // Step 1: Resolve replacements against adds and deletes
        foreach (var (oldId, newId) in _replacements)
        {
            // Replace wins over an add of the same old ID
            _adds.Remove(oldId);

            // replacement wins over delete
            if (_replacements.Keys.Contains(oldId))
            {
                _deletes.Remove(oldId);
            }
        }

        // Step 2: Cancel add+delete of the same ID
        var cancelledByBoth = _adds.Intersect(_deletes).ToArray();
        foreach (var id in cancelledByBoth)
        {
            _adds.Remove(id);
            _deletes.Remove(id);
        }

        // Step 3: If a replacement produced a new ID that was also queued
        // for deletion, cancel the replacement (the new object was deleted).
        var replacedNewIds = _replacements.Values.ToHashSet();
        var deletedNewIds = replacedNewIds.Intersect(_deletes).ToArray();
        foreach (var newId in deletedNewIds)
        {
            var kvp = _replacements.First(x => x.Value == newId);
            _replacements.Remove(kvp.Key);
        }

        // Step 4: Build result
        var batch = new EventQueueBatch(
            Adds: [.. _adds],
            Deletes: [.. _deletes],
            Replacements: _replacements.Select(kvp => (kvp.Key, kvp.Value)).ToList());

        // Step 5: Clear queues
        Clear();

        return batch;
    }

    /// <summary>Clear all queues without processing.</summary>
    public void Clear()
    {
        _adds.Clear();
        _deletes.Clear();
        _replacements.Clear();
    }

    /// <summary>True when all queues are empty.</summary>
    public bool IsEmpty => _adds.Count == 0 && _deletes.Count == 0 && _replacements.Count == 0;
}

/// <summary>
/// Result of draining the event queue. All three lists are mutually exclusive:
/// an ID appears in at most one list.
/// </summary>
/// <param name="Adds">Confirmed additions.</param>
/// <param name="Deletes">Confirmed deletions (object IDs to remove from Rhino and UrbanModel).</param>
/// <param name="Replacements">Confirmed replacements (old ID to new ID).</param>
public record EventQueueBatch(
    List<Guid> Adds,
    List<Guid> Deletes,
    List<(Guid OldObject, Guid NewObject)> Replacements)
{
    /// <summary>True when all three lists are empty.</summary>
    public bool IsEmpty => Adds.Count == 0 && Deletes.Count == 0 && Replacements.Count == 0;
}
