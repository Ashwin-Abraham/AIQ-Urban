using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class AddBuildingFromCoordinatesInputPreparationStep(
    IReadOnlyList<CurveCoordinates> footprints,
    double floorHeight,
    int floorCount,
    string useName) : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        var use = context.UrbanModel.GetUse(useName);
        var result = new List<ICreationData>();

        for (var index = 0; index < footprints.Count; index++)
        {
            var polygon = Polygon2D.Create(footprints[index].ToCoreCoordinates());
            if (polygon is null)
            {
                result.Add(new InputRejectionData("MCP building footprint", index, "Coordinates do not form a closed polygon."));
                continue;
            }

            result.Add(new BuildingFootprintData(polygon, floorHeight, floorCount, use));
        }

        return result;
    }
}
