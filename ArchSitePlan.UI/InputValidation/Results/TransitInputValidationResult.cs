using Rhino.DocObjects;

namespace ArchSitePlan.UI.InputValidation.Results;

public class TransitInputValidationResult : IValidationResult
{
    public List<RhinoObject> ValidObjects { get; set; }
    public string Message { get; set; }
    public ValidationResult Result { get; set; }

    public TransitInputValidationResult(
        List<RhinoObject> validObjects,
        ValidationResult result,
        string message)
    {
        ValidObjects = validObjects;
        Message = message;
        Result = result;
    }
}
