using Rhino.DocObjects;

namespace ArchSitePlan.UI.InputValidation
{
    public interface IRhinoInputValidation<TResult> where TResult  : IValidationResult
    {
        TResult ValidateInput(List<RhinoObject> rhinoObjs);
    }
}
