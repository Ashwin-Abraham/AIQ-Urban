using Rhino.DocObjects;

namespace ArchSitePlan.UI.InputValidation;

public class BuildingInputValidationResult : IValidationResult
{
    public List<RhinoObject> ValidObjects { get; set; }
    public string Message { get ; set ; }
    public ValidationResult Result { get ; set ; }

    public BuildingInputValidationResult(
        List<RhinoObject> validObjects,
        ValidationResult result,
        string message)
    {
        ValidObjects = validObjects;
        Message = message;
        Result = result;
    }
}
