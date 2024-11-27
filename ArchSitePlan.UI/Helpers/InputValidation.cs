using ArchSitePlan.UI.Constants;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Helpers
{
    public static class InputValidation
    {
        public record RoadInputValidation(List<RhinoObject> ValidCenterlines, string ImprovementInstruction);

        public static RoadInputValidation ValidateRoadInput(List<RhinoObject> rhinoObjs)
        {
            List<RhinoObject> validCLs = new();

            if(rhinoObjs.Count == 0)
            {
                return new RoadInputValidation(
                    validCLs,
                    UIMessages.RoadInstruction);
            }

            validCLs = rhinoObjs
                .Where(obj => obj.Geometry is Curve rhinoCrv 
                && rhinoCrv.Degree <= 2 
                && Intersection.CurveSelf(rhinoCrv, 1E-06).Count == 0)
                .ToList();

            int removalCount = rhinoObjs.Count - validCLs.Count;

            string message = string.Empty;
            if (removalCount > 0)
            {
                message = "Removed " + removalCount + " objects";
                message = message + "\n\n" + UIMessages.RoadInstruction;
            }

            return new RoadInputValidation(
                    validCLs,
                    UIMessages.RoadInstruction);
        }
    }
}
