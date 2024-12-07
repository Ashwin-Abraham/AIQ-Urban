using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.InputValidation.Results;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace ArchSitePlan.UI.InputValidation.Validators
{
    public class StudyInputValidator : IRhinoInputValidator<StudyInputValidationResult>
    {
        public StudyInputValidationResult ValidateInput(List<RhinoObject> rhinoObjs)
        {
            List<RhinoObject> validBoundaries = new();

            if (rhinoObjs.Count == 0)
            {
                return new StudyInputValidationResult(
                    validBoundaries,
                    ValidationResult.EmptyInput,
                    "Empty input\n" + UIMessages.AddStudyInstruction);
            }

            validBoundaries = rhinoObjs
                .Where(obj => obj.Geometry is Curve rhinoCrv
                && rhinoCrv.IsClosed
                && rhinoCrv.Degree <= 2
                && Intersection.CurveSelf(rhinoCrv, 1E-06).Count == 0)
                .ToList();

            int removalCount = rhinoObjs.Count - validBoundaries.Count;

            string message = string.Empty;
            ValidationResult result;

            if (removalCount > 0)
            {
                message = "Removed " + removalCount + " objects";
                message = message + "\n\n" + UIMessages.AddStudyInstruction;
                result = validBoundaries.Count == 0 ? ValidationResult.All_Invalid : ValidationResult.Some_Valid;
            }
            else
            {
                result = ValidationResult.All_Valid;
            }

            return new StudyInputValidationResult(
                validBoundaries,
                result,
                message);
        }
    }
}
