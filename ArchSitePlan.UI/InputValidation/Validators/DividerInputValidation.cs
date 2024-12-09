using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.InputValidation.Results;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace ArchSitePlan.UI.InputValidation.Validators
{
    public class DividerInputValidator : IRhinoInputValidator<DividerInputValidationResult>
    {
        bool _checkCorrectLayer;
        public DividerInputValidator(bool checkCorrectLayer = false)
        {
            _checkCorrectLayer = checkCorrectLayer;
        }

        public DividerInputValidationResult ValidateInput(List<RhinoObject> rhinoObjs)
        {
            RhinoDoc doc = RhinoDoc.ActiveDoc;
            List<RhinoObject> validCLs = new();

            if (rhinoObjs.Count == 0)
            {
                return new DividerInputValidationResult(
                    validCLs,
                    ValidationResult.EmptyInput,
                    "Empty input\n" + UIMessages.AddDividerInstruction);
            }

            if(_checkCorrectLayer)
            {
                var index = doc.Layers.FindByFullPath(LayerConstants.ParcelDividerLayer, -1);

                if (index == -1)
                    rhinoObjs.Clear();
                else
                    rhinoObjs = rhinoObjs.Where(obj => obj.Attributes.LayerIndex == index).ToList();
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
                message = message + "\n\n" + UIMessages.AddDividerInstruction;
                result = validCLs.Count == 0 ? ValidationResult.All_Invalid : ValidationResult.Some_Valid;
            }
            else
            {
                result = ValidationResult.All_Valid;
            }

            return new DividerInputValidationResult(
                validCLs,
                result,
                message);
        }
    }
}
