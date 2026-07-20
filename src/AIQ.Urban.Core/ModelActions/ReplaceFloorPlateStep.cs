using AIQ.Primitives.Extensions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace AIQ.Urban.Core.ModelActions;

public class ReplaceFloorPlateStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ReplaceFloorPlateStep>();
    private const double Tolerance = 1e-5;

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var data = creationData.OfType<FloorPlateReplaceData>().First();
        var oldPlate = data.OldPlate;
        var newZ = data.NewZ;

        var building = oldPlate.Parent
            ?? throw new InvalidOperationException("Floor plate has no parent building.");

        var zIsOccupied = building.FloorPlates
            .Any(p => p != oldPlate && p.Z().AlmostEquals(newZ, Tolerance));

        if (zIsOccupied)
            return new ([], oldPlate.GetGeometryIds());

        var plane = new Plane(new Point3d(0, 0, newZ), Vector3d.ZAxis);
        var success = Intersection.BrepPlane(
            building.Envelope.Geometry, 
            plane,
            Tolerance,
            out var intersectionCurves,
            out _);

        if (!success || intersectionCurves is null || intersectionCurves.Length == 0)
            return new([], []);

        var contour = intersectionCurves.FirstOrDefault(c => c.GetLength() > Tolerance);
        if (contour is null)
            return new([], []);

        var newProfile = new Polygon2D(contour);
        var newPlate = new FloorPlate(newProfile, oldPlate.Use);
        newPlate.Parent = building;

        var index = building.FloorPlates.IndexOf(oldPlate);
        building.FloorPlates[index] = newPlate;

        building.FloorPlates.Sort((a, b) => a.Z().CompareTo(b.Z()));

        _logger.LogInformation("Replaced floor plate at Z={Z:F2}", newZ);

        return new(newPlate.GetGeometryIds(), oldPlate.GetGeometryIds());
    }
}
