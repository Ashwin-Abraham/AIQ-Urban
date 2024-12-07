using Rhino.DocObjects;

namespace ArchSitePlan.UI.InputValidation.Results;

public interface IValidationResult
{
    public List<RhinoObject> ValidObjects { get; set; }
    public string Message { get; set; }
    public ValidationResult Result { get; set; }
}
