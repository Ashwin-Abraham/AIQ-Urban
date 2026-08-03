using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

internal static class RhinoObjectResolver
{
    internal static List<RhinoObject> Resolve(RhinoDoc doc, IEnumerable<Guid> objectIds)
    {
        return objectIds
            .Distinct()
            .Select(doc.Objects.FindId)
            .Where(x => x is not null)
            .Cast<RhinoObject>()
            .ToList();
    }
}
