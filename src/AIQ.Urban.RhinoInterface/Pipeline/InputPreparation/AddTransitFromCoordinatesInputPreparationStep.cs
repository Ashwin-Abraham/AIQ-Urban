using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class AddTransitFromCoordinatesInputPreparationStep(
    IReadOnlyList<CurveCoordinates> centerlines,
    double width,
    TransitType transitType) : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        var result = new List<ICreationData>();
        for (var index = 0; index < centerlines.Count; index++)
        {
            try
            {
                var curve = Curve2D.Create(centerlines[index].ToCoreCoordinates());
                result.Add(new TransitData(curve, width, transitType));
            }
            catch (Exception ex)
            {
                result.Add(new InputRejectionData("MCP transit centerline", index, ex.Message));
            }
        }

        return result;
    }
}
