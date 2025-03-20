using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.RhinoInterface.Helpers
{
    public static class DocObjectHelpers
    {
        public static void DeleteRhinoObjectsIfPresent(RhinoDoc doc, IEnumerable<Guid> ids)
        {
            var objIds = ids.Where(id => id != null && id != Guid.Empty).ToArray();

            if (!objIds.Any()) return;

            doc.Objects.Delete(objIds, true);
        }
        public static void DeleteRhinoObjectsIfPresent(RhinoDoc doc, RhinoObject obj)
        {
            doc.Objects.Delete(obj, true);
        }
        public static Polyline? TryExtractPolyline(RhinoObject obj)
        {
            var crv = obj.Geometry as Curve;
            
            if (crv != null && crv.TryGetPolyline(out Polyline pline))
                return pline;
            else
                return null;
        }
    }
}
