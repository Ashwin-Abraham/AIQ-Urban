using ArchSitePlan.RhinoInterface.InputValidation.Results;
using Rhino.DocObjects;

namespace ArchSitePlan.RhinoInterface.InputValidation.Validators;

public interface IRhinoInputValidator
{
    GeneralValidationResult ValidateInput(List<RhinoObject> rhinoObjs);

    static ValidationResult GetValidationResult(int originalCount, int removedCount)
    {
        if (removedCount == originalCount)
            return ValidationResult.All_Invalid;

        if (removedCount == 0)
            return ValidationResult.All_Valid;

        return ValidationResult.Some_Valid;
    }
}
