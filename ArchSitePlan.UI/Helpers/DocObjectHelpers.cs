using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Helpers
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
    }
}
