using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.DebugModel.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories;

public class RectangleBuildginFactory
{
    AutoRectangleBuildingData _config;

    public RectangleBuildginFactory(AutoRectangleBuildingData config)
    {
        _config = config;
    }

    private class BuildinsWIP(Polygon footPrint, double footprintArea, int floorCount)
    {
        public Polygon Footprint = footPrint;
        public double FootprintArea = footprintArea;
        public int FloorCount = floorCount;
    }

    public List<RectangleBuilding> Generate(AutoRectangleBuildingData config)
    {
        List<RectangleBuilding> buildings = new();

        var site = config.SiteBoundary;
        Polyline primaryAxis = Building.GeneratePrimarySiteAxis(config.SiteBoundary);

        var widthParamSize = config.BuildingWidth / primaryAxis.Length();
        var buildableZone = site.Offset(-config.Setback);

        double stepSize = widthParamSize / 2;

        List<Polygon> footprints = new();
        for (double i = config.Setback; i < 1; i += stepSize)
        {
            var pointA = primaryAxis.PointAtParameter(i);
            var pointB = primaryAxis.PointAtParameter(i + widthParamSize);
            var perp = primaryAxis.TangentAtParameter((i + widthParamSize) / 2);

            var pointAPlus = pointA.Translate(perp * config.MaxLength);
            var pointAMinus = pointA.Translate(-perp * config.MaxLength);
            var lineA = Create.Line(pointAPlus, pointAMinus);

            var pointBPlus = pointB.Translate(perp * config.MaxLength);
            var pointBMinus = pointB.Translate(-perp * config.MaxLength);
            var lineB = Create.Line(pointBPlus, pointBMinus);

            var remainingA = lineA.LineBooleanIntersection(buildableZone).MaxBy(x => x.Length());
            var remainingB = lineB.LineBooleanIntersection(buildableZone).MaxBy(x => x.Length());

            var length = Math.Max(remainingA.Length(), remainingB.Length());

            if (length < config.MinLength) continue;

            footprints.Add(CreateBuildingFootPrint(
                _config.BuildingWidth,
                length,
                (pointA + pointB) / 2,
                perp));
        }

        GenerateMassingForTargetGFA( footprints);

        return new();
    }
    private static Polygon CreateBuildingFootPrint(
        double buildingWidth,
        double buildingLength,
        Point center,
        Vector lengthDirection)
    {
        var widthDirection = lengthDirection.Rotate(90d.RadiansToDegrees(), Vector.ZAxis);
        var lengthMover = lengthDirection * buildingWidth / 2;
        var widthMover = widthDirection * buildingWidth / 2;

        var pointA = center - widthMover - lengthMover;
        var pointB = center + widthMover - lengthMover;
        var pointC = center + widthMover + lengthMover;
        var pointD = center + widthMover + lengthMover;

        var footprint = Create.Polygon([pointA, pointB, pointC, pointD]);
        return footprint;
    }

    public List<RectangleBuilding> GenerateMassingForTargetGFA(List<Polygon> footprints)
    {
        int maxFloorCount = (int)Math.Floor(_config.MaxHeight / _config.FloorHeight);
        double totalGFA = 0;

        List<BuildinsWIP> buildingsWIP  =
            footprints
            .Select(x => new BuildinsWIP(x, x.IArea(), 0))
            .ToList();

        // First pack the buildings to the brim
        while (totalGFA < _config.TargetGFA)
        {
            foreach(var buildingWIP in buildingsWIP)
            {
                totalGFA += buildingWIP.FootprintArea;
                if (totalGFA > _config.TargetGFA || buildingWIP.FloorCount >= maxFloorCount) break;

                buildingWIP.FloorCount++;
            }

            if (totalGFA > _config.TargetGFA) break;
        }

        // Next Apply variance

        // Make buildings
        List<RectangleBuilding> buildings = new();
        foreach(var buildingData in buildingsWIP)
            buildings.Add(CreateBuilding(buildingData));

        return buildings;
    }

    private RectangleBuilding CreateBuilding(BuildinsWIP buildingData)
    {
        Mesh envelope = CreateEnvelopeMesh(buildingData.Footprint,  buildingData.FloorCount);
        List<PlanarSurface> floorPlates = new();
        var typicalFloorPlate = Create.PlanarSurface(buildingData.Footprint);

        for(int i = 0; i < buildingData.FloorCount; i ++)
        {
            var mover = Vector.ZAxis * i * _config.FloorHeight;
            floorPlates.Add(typicalFloorPlate.Translate(mover));
        }

        return new RectangleBuilding(envelope, floorPlates, _config.FloorHeight);
    }

    private Mesh CreateEnvelopeMesh( Polygon footprint, int floorCount)
    {
        var mover = Vector.ZAxis * (double)floorCount * _config.FloorHeight;
        Extrusion extrude = Create.Extrusion(footprint, mover, true);
        var renderMesh = BH.Engine.Representation.Compute.RenderMesh(extrude);
        var meshEnvelope = Create.Mesh(renderMesh.Vertices.Select(x => x.Point).ToList(), renderMesh.Faces);
        return meshEnvelope;
    }
}
