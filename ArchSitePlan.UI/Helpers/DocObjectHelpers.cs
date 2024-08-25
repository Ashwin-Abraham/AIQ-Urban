using Rhino;
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
            var objs = ids.Where(id => id != null || id != Guid.Empty)
                .Select(id => doc.Objects.FindId(id))
                .Where(obj => obj != null)
                .ToList();

            if (!objs.Any()) return;

            objs.ForEach(obj => doc.Objects.Delete(obj, true, true));
        }
    }
}
