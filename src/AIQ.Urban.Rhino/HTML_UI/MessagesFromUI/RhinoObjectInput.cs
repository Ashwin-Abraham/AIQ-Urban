using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

internal static class RhinoObjectInput
{
    public static List<RhinoObject> Resolve(RhinoDoc doc, List<Guid>? objectIds, ObjectType allowedTypes)
    {
        if (objectIds is null)
        {
            return doc.Objects.GetSelectedObjects(false, false)
                .Where(obj => (obj.ObjectType & allowedTypes) != 0)
                .ToList();
        }

        if (objectIds.Count == 0)
            throw new ArgumentException("ObjectIds must contain at least one object ID.", nameof(objectIds));

        var result = new List<RhinoObject>(objectIds.Count);
        var seen = new HashSet<Guid>();
        foreach (var id in objectIds)
        {
            if (!seen.Add(id))
                continue;

            var obj = doc.Objects.FindId(id)
                ?? throw new ArgumentException($"Object {id} was not found in the active Rhino document.", nameof(objectIds));

            if ((obj.ObjectType & allowedTypes) == 0)
                throw new ArgumentException($"Object {id} has an unsupported geometry type: {obj.ObjectType}.", nameof(objectIds));

            result.Add(obj);
        }

        return result;
    }
}
