using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace AIQ.Urban.Core.ModelActions;

public class ReplaceFloorPlateStep(FloorPlate oldPlate, double newZ) : IEditUrbanElementsStep
{
    private const double Tolerance = 1e-5;

    public EditStepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var building = oldPlate.Parent
            ?? throw new InvalidOperationException("Floor plate has no parent building.");

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

        return new(newPlate.GetGeometryIds(), oldPlate.GetGeometryIds());
    }
}
