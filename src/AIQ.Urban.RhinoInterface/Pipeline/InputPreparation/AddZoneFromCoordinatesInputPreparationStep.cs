using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class AddZoneFromCoordinatesInputPreparationStep(
    IReadOnlyList<CurveCoordinates> boundaries,
    string name) : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Contains("::", StringComparison.Ordinal))
            throw new ArgumentException("Zone name must not contain '::'.", nameof(name));

        if (context.UrbanModel.Zones.Any(
                zone => string.Equals(zone.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return [new InputRejectionData(
                "MCP zone boundary",
                0,
                $"A zone named '{name}' already exists.")];
        }

        if (boundaries.Count != 1)
            return [new InputRejectionData("MCP zone boundary", -1, "Exactly one boundary is required.")];

        var polygon = Polygon2D.Create(boundaries[0].ToCoreCoordinates());
        return polygon is null
            ? [new InputRejectionData("MCP zone boundary", 0, "Coordinates do not form a closed polygon.")]
            : [new ZoneData(polygon, name)];
    }
}
