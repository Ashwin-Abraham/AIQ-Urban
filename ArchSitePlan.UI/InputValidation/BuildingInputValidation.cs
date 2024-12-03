using ArchSitePlan.UI.Constants;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.InputValidation
{
    public class BuildingInputValidation 
        :IRhinoInputValidation<BuildingInputValidationResult>
    {
        readonly BuildingSelectorMode _inputType;

        public BuildingInputValidation(BuildingSelectorMode inputType)
        {
            _inputType = inputType;
        }

        public BuildingInputValidationResult ValidateInput(
            List<RhinoObject> rhinoObjs)
        {
            BuildingInputValidationResult result;
            string message = string.Empty;
            int removed = 0;

            if (rhinoObjs.Count == 0)
                return new(new(), ValidationResult.EmptyInput, "Nothing Selected");

            switch (_inputType)
            {
                case BuildingSelectorMode.Mass:
                    removed = rhinoObjs.RemoveAll(o => !RHGeneralConstants.BuildingEnvelopeObjectTypes.Contains(o.ObjectType));
                    break;

                case BuildingSelectorMode.Footprint:
                    
                    List<(RhinoObject Object, Curve Curve)> crvs = rhinoObjs
                        .Where(o => o.ObjectType == ObjectType.Curve)
                        .Select(o => (o, (o.Geometry as Curve)!))
                        .ToList();
                    
                    int wrongType = rhinoObjs.Count - crvs.Count;
                    if (wrongType > 0)
                        message += $"\nSkipped {wrongType} objects that are not curves";

                    int highDegree = crvs.RemoveAll(crv => crv.Curve.Degree > 2);
                    if (highDegree > 0)
                        message += $"\nSkipped {highDegree} objects that are NURBs splines";

                    int open = crvs.RemoveAll(crv => !crv.Curve.IsClosed);
                    if (open > 0)
                        message += $"\nSkipped {open} selected objects that are not closed";

                    int selfIntersecting = crvs.RemoveAll(crv => Intersection.CurveSelf(crv.Curve, 1E-06).Count > 0);
                    if (selfIntersecting > 0)
                        message += $"\nSkipped {selfIntersecting} selected objects that are self-intersecting";

                    rhinoObjs = crvs.Select(x => x.Object).ToList();
                    removed = wrongType + highDegree + open + selfIntersecting;
                    break;

                default:
                    removed = rhinoObjs.Count;
                    rhinoObjs = new();
                    break;
            }

            var validationResult = removed == 0 ?
                ValidationResult.All_Valid : ValidationResult.Some_Valid;

            if (rhinoObjs.Count == 0)
                validationResult = ValidationResult.All_Invalid;
            
            return new(rhinoObjs, validationResult, message);
        }
    }
}
