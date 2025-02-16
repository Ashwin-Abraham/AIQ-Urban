using ArchSitePlan.OSM.Models;
using Rhino;
using Rhino.DocObjects;

namespace ArchSitePlan.UI.RhinoOSM;

public interface IRhinoOSM<T> where T:IOsmObject
{
    public Guid RhinoId { get; }
    public bool AddToDocument();
}
