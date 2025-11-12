using ArchSitePlan.Core.Models;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.ModelEditActions.MiscActions;

public static class CalculateStudyBoundsAction
{
    public static List<(string Name, BoundingBox Bounds)> Run()
    {
        List < (string Name, BoundingBox bounds)> result = [];

        foreach(var zone in CoreModelSingelton.Instance.Zones)
        {
            var bounds = zone.Boundary == null ?
                CalculateEntireModelBounds()
                : zone.Boundary.IBounds();

            result.Add((zone.Name, bounds));
        }

        return result;
    }

    private static BoundingBox CalculateEntireModelBounds()
    {
        var bounds = CoreModelSingelton.Instance.ROWs.Select(x => x.IBounds())
            .Concat(CoreModelSingelton.Instance.Buildings.Select(x => x.Envelope.Bounds()))
            .Concat(CoreModelSingelton.Instance.Parcels.Select(x => x.Boundary.Bounds()))
            .Concat(CoreModelSingelton.Instance.Zones.Select(x => x.Boundary?.IBounds()))
            .OfType<BoundingBox>()
            .ToList();

        double minX = 0;
        double minY = 0;
        double minZ = 0;
        double maxX = 0;
        double maxY = 0;
        double maxZ = 0;

        foreach(var box in bounds)
        {
            minX = Math.Min(box.Min.X, minX);
            minY = Math.Min(box.Min.Y, minY);
            minZ = Math.Min(box.Min.Z, minZ);
            maxX = Math.Max(box.Max.X, maxX);
            maxY = Math.Max(box.Max.Y, maxY);
            maxZ = Math.Max(box.Max.Z, maxZ);
        }

        return Create.BoundingBox(
            min: Create.Point(minX, minY, minZ),
            max: Create.Point(maxX, maxY, maxZ));
    }
}
