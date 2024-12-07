using ArchSitePlan.UI.InputValidation.Results;
using Rhino.DocObjects;

namespace ArchSitePlan.UI.InputValidation.Validators
{
    public interface IRhinoInputValidator<TResult> where TResult : IValidationResult
    {
        TResult ValidateInput(List<RhinoObject> rhinoObjs);
    }
}
