using ArchSitePlan.RhinoInterface.InputValidation.Results;
using Rhino.DocObjects;

namespace ArchSitePlan.RhinoInterface.InputValidation.Validators
{
    public interface IRhinoInputValidator<TResult> where TResult : IValidationResult
    {
        TResult ValidateInput(List<RhinoObject> rhinoObjs);
    }
}
