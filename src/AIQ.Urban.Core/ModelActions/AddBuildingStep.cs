using AIQ.Urban.Core.ModelActions.EditData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class AddBuildingStep(double _tolerance = 1e-5, ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AddBuildingStep>();

    public StepResult Run(UrbanModel model, List<IModelEditData> creationData)
    {
        var buildings = new List<Models.UrbanElements.Building>();

        foreach (var data in creationData)
        {
            if (data is BuildingFootprintData fpData)
            {
                buildings.Add(new Models.UrbanElements.Building(fpData.Footprint, fpData.TypicalFloorHeight, fpData.FloorCount, fpData.Use));
            }
            else if (data is BuildingMassData massData)
            {
                buildings.Add(new Models.UrbanElements.Building(massData.Envelope.Geometry, massData.FloorHeight, massData.Use, _tolerance));
            }
            else if (data is BuildingMassAndFloorsData mfData)
            {
                buildings.Add(new Models.UrbanElements.Building(mfData.Envelope.Geometry, mfData.AssociatedPlates));
            }
        }

        foreach (var building in buildings)
            model.AddBuilding(building);

        _logger.LogInformation("Added {Count} buildings", buildings.Count);

        return new(buildings.SelectMany(x => x.GetGeometryIds()).ToList(), []);
    }
}
