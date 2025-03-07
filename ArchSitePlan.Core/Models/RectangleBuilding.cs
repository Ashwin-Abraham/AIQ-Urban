using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models;

public class RectangleBuilding : Building
{
    public RectangleBuilding(
        Mesh envelope, 
        List<PlanarSurface> floorPlates, 
        double floorToFloorHeight)
        : base(envelope, floorPlates, floorToFloorHeight)
    {
    }

    //public static List<RectangleBuilding> Generate( AutoRectangleBuildingData config)
    //{
    //    List<RectangleBuilding> buildings = new();

    //    var site = config.SiteBoundary;
    //    Polyline primaryAxis = Building.GeneratePrimarySiteAxis(config.SiteBoundary);

    //    var widthParamSize = config.BuildingWidth/ primaryAxis.Length();
    //    var buildableZone = site.Offset(-config.Setback);

    //    double stepSize = widthParamSize / 2;

    //    List<Polygon> footprints = new();
    //    for(double i = config.Setback; i < 1; i += stepSize)
    //    {
    //        var pointA = primaryAxis.PointAtParameter(i);
    //        var pointB = primaryAxis.PointAtParameter(i + widthParamSize);
    //        var perp = primaryAxis.TangentAtParameter((i + widthParamSize)/2);

    //        var pointAPlus = pointA.Translate(perp * config.MaxLength);
    //        var pointAMinus = pointA.Translate(-perp * config.MaxLength);
    //        var lineA = Create.Line(pointAPlus, pointAMinus);

    //        var pointBPlus = pointB.Translate(perp * config.MaxLength);
    //        var pointBMinus = pointB.Translate(-perp * config.MaxLength);
    //        var lineB = Create.Line(pointBPlus, pointBMinus);

    //        var remainingA = lineA.LineBooleanIntersection(buildableZone).MaxBy(x => x.Length());
    //        var remainingB = lineB.LineBooleanIntersection(buildableZone).MaxBy(x => x.Length());

    //        var length = Math.Max(remainingA.Length(), remainingB.Length());

    //        if (length < config.MinLength) continue;

    //        footprints.Add(CreateBuildingFootPrint(
    //            config.BuildingWidth,
    //            length,
    //            (pointA + pointB) / 2,
    //            perp));
    //    }

    //    GenerateMassingForTargetGFA(
    //        footprints,
    //        config.TargetGFA.Value,
    //        config.MaxHeight,
    //        config.FloorHeight,
    //        config.HeightVariance);

    //    return new();
    //}

    //private class BuildingConstructionProcessData(
    //    Polygon Footprint, 
    //    double FootprintArea, 
    //    int FloorCount);

    //private static List<RectangleBuilding> GenerateMassingForTargetGFA(
    //    List<Polygon> footprints,
    //    double targetGFA, 
    //    double maxHeight, 
    //    double floorHeight, 
    //    double heightVariance)
    //{
    //    int maxFloorCount = (int)Math.Floor(maxHeight / floorHeight);
    //    double totalGFA = 0;
    //    var footPrintFloors = footprints.ToDictionary(x => (x, 0));
    //    var buildingWIP = footprints
    //        .Select(x => new BuildingConstructionProcessData(x, x.IArea(), 0))
    //        .ToList();

    //    while(totalGFA < targetGFA)
    //    {
    //        foreach(var building in buildingWIP)
    //        {
    //            totalGFA += building.FootprintArea;
    //            if (totalGFA > targetGFA) break;

    //            building.FloorCount++;
    //        }

    //        if (totalGFA > targetGFA) break;
    //    }
    //}

    //private static Polygon CreateBuildingFootPrint(
    //    double buildingWidth,
    //    double buildingLength, 
    //    Point center, 
    //    Vector lengthDirection)
    //{
    //    var widthDirection = lengthDirection.Rotate(90d.RadiansToDegrees(), Vector.ZAxis);
    //    var lengthMover = lengthDirection * buildingWidth / 2;
    //    var widthMover = widthDirection * buildingWidth / 2;

    //    var pointA = center - widthMover - lengthMover;
    //    var pointB = center + widthMover - lengthMover;
    //    var pointC = center + widthMover + lengthMover;
    //    var pointD = center + widthMover + lengthMover;

    //    var footprint = Create.Polygon([pointA, pointB, pointC, pointD]);
    //    return footprint;
    //}
}
