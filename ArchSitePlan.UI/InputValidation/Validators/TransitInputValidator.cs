using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.InputValidation.Results;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace ArchSitePlan.UI.InputValidation.Validators
{
    public class TransitInputValidator : IRhinoInputValidator<TransitInputValidationResult>
    {
        public TransitInputValidationResult ValidateInput(List<RhinoObject> rhinoObjs)
        {
            List<RhinoObject> validCLs = new();

            if (rhinoObjs.Count == 0)
            {
                return new TransitInputValidationResult(
                    validCLs,
                    ValidationResult.EmptyInput,
                    "Empty input\n" + UIMessages.RoadInstruction);
            }

            validCLs = rhinoObjs
                .Where(obj => obj.Geometry is Curve rhinoCrv
                && rhinoCrv.Degree <= 2
                && Intersection.CurveSelf(rhinoCrv, 1E-06).Count == 0)
                .ToList();

            int removalCount = rhinoObjs.Count - validCLs.Count;

            string message = string.Empty;
            ValidationResult result;

            if (removalCount > 0)
            {
                message = "Removed " + removalCount + " objects";
                message = message + "\n\n" + UIMessages.RoadInstruction;
                result = validCLs.Count == 0 ? ValidationResult.All_Invalid : ValidationResult.Some_Valid;
            }
            else
            {
                result = ValidationResult.All_Valid;
            }

            return new TransitInputValidationResult(
                validCLs,
                result,
                message);
        }
    }
}
