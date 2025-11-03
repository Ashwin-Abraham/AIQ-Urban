using Rhino.DocObjects;

namespace ArchSitePlan.RhinoInterface.InputValidation.Results;
public class GeneralValidationResult
{
    public List<RhinoObject> ValidObjects { get; set; }
    public string Message { get; set; }
    public ValidationResult Result { get; set; }

    public GeneralValidationResult(
        List<RhinoObject> validObjects,
        ValidationResult result,
        string message)
    {
        ValidObjects = validObjects;
        Message = message;
        Result = result;
    }
}
