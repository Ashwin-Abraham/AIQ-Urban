using AIQ.Primitives.Extensions;
using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using ArchSitePlan.DebugModel.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories;

public class RectangleBuildingFactory
{
    AutoRectangleBuildingData _config;

    public RectangleBuildingFactory(AutoRectangleBuildingData config)
    {
        _config = config;
    }

    private class BuildinsWIP(Polygon footPrint, double footprintArea, int floorCount)
    {
        public Polygon Footprint = footPrint;
        public double FootprintArea = footprintArea;
        public int FloorCount = floorCount;
    }

    public List<RectangleBuilding> Generate()
    {
        List<RectangleBuilding> buildings = new();

        var site = _config.SiteBoundary;
        Polyline primaryAxis = Building.GeneratePrimarySiteAxis(_config.SiteBoundary);
        var buildableZone = site.OffsetAndMaintainClosure(-_config.Setback);
        List<Polygon> footprints = GenerateFootprints(primaryAxis, buildableZone);

        DEBUGMODEL.AddBoundedCurves(footprints.ToHypar());
        DEBUGMODEL.AddBoundedCurves(_config.SiteBoundary.ToHypar());
        DEBUGMODEL.AddBoundedCurves(buildableZone.ToHypar());
        DEBUGMODEL.OpenAsGlb();

        return GenerateMassingForTargetGFA( footprints);
    }

    private List<Polygon> GenerateFootprints(Polyline primaryAxis, PolyCurve buildableZone)
    {
        List<Polygon> footprints = new();
        var primaryAxisLength = primaryAxis.Length();
        var widthParamSize = _config.BuildingWidth / primaryAxisLength;
        double stepSize = widthParamSize;
        var startParam = _config.Setback / primaryAxisLength;

        for (double i = startParam; i < 1 - startParam - stepSize; i += stepSize)
        {
            var pointA = primaryAxis.PointAtParameter(i);
            var pointB = primaryAxis.PointAtParameter(i + widthParamSize);
            var moveBtoA = Create.Vector(pointA) - Create.Vector(pointB);
            var perp = moveBtoA.Rotate(90d.ToRadians(), Vector.ZAxis).Unitized();

            // Check if point A is valid
            var pointAPlus = pointA.Translate(perp * _config.MaxLength);
            var lineAPlus = Create.Line(pointA, pointAPlus);
            var remainingAPlus = lineAPlus.LineBooleanIntersection(buildableZone).MaxBy(x => x.Length());
            var remainingAPlusLength = remainingAPlus?.Length()?? 0;

            var pointAMinus = pointA.Translate(-perp * _config.MaxLength);
            var lineAMinus = Create.Line(pointA, pointAMinus);
            var remainingAMinus = lineAMinus.LineBooleanIntersection(buildableZone).MaxBy(x => x.Length());
            var remainingAMinusLength = remainingAMinus?.Length()?? 0;

            var halfMaxLength = _config.MaxLength / 2;
            if (remainingAPlusLength / 2 > halfMaxLength
                && remainingAMinusLength / 2 > halfMaxLength)
                remainingAPlusLength = remainingAMinusLength = halfMaxLength;

            if (remainingAPlusLength + remainingAMinusLength < _config.MinLength)
                continue;

            // Check if point B is valid
            var pointBPlus = pointB.Translate(perp * remainingAPlusLength);
            var pointBMinus = pointB.Translate(-perp * remainingAMinusLength);
            var lineB = Create.Line(pointBPlus, pointBMinus);
            var remainingB = lineB.LineBooleanIntersection(buildableZone).MaxBy(x => x.Length());

            if ((remainingB?.Length()?? double.NegativeInfinity) < _config.MinLength) continue;

            var point3 = remainingB.End + moveBtoA;
            var point4 = remainingB.Start + moveBtoA;

            var footprint = Create.Polygon([remainingB.Start, remainingB.End, point3, point4]);
            footprints.Add(footprint);

            DEBUGMODEL.Reset();
            DEBUGMODEL.AddBoundedCurves(primaryAxis.ToHypar(), DebugMaterials.Red);
            DEBUGMODEL.AddBoundedCurves(footprints.ToHypar());
            DEBUGMODEL.AddBoundedCurves(_config.SiteBoundary.ToHypar());
            DEBUGMODEL.AddBoundedCurves(buildableZone.ToHypar());
            DEBUGMODEL.OpenAsGlb();

            i += widthParamSize;
        }

        return footprints;
    }

    private static Polygon CreateBuildingFootPrint(
        double buildingWidth,
        double buildingLength,
        Point center,
        Vector lengthDirection)
    {
        var widthDirection = lengthDirection.Rotate(90d.ToRadians(), Vector.ZAxis);
        var lengthMover = lengthDirection * buildingLength / 2;
        var widthMover = widthDirection * buildingWidth / 2;

        var pointA = center - widthMover - lengthMover;
        var pointB = center + widthMover - lengthMover;
        var pointC = center + widthMover + lengthMover;
        var pointD = center - widthMover + lengthMover;

        var footprint = Create.Polygon([pointA, pointB, pointC, pointD]);
        return footprint;
    }

    public List<RectangleBuilding> GenerateMassingForTargetGFA(List<Polygon> footprints)
    {
        if (footprints.Count == 0) return new();

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
