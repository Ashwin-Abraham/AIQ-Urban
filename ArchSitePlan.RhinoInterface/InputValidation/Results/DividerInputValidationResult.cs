using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.RhinoInterface.InputValidation.Results
{
    public class DividerInputValidationResult : IValidationResult
    {
        public List<RhinoObject> ValidObjects { get; set ; }
        public string Message { get ; set ; }
        public ValidationResult Result { get ; set ; }
        public DividerInputValidationResult(
        List<RhinoObject> validObjects,
        ValidationResult result,
        string message)
        {
            ValidObjects = validObjects;
            Message = message;
            Result = result;
        }
    }
}
