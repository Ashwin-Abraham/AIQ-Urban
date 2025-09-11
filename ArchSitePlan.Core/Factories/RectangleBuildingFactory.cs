using AIQ.Primitives.Extensions;
using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories;

public class RectangleBuildingFactory:BuildingLayoutFactoryBase
{
    AutoRectangleBuildingData _config;

    public RectangleBuildingFactory(AutoRectangleBuildingData config)
    {
        _config = config;
    }

    public BuildingLayout? Generate(ICurve site, Guid? siteId)
    {
        var buildableZone = site.OffsetAndMaintainClosure(-_config.Setback);
        
        if (buildableZone == null) return null;

        Polyline? primaryAxis = GeneratePrimarySiteAxis(site);
        if (primaryAxis is null || buildableZone is null)
            return null;

        List<PolyCurve> footprints = GenerateFootprints(primaryAxis, buildableZone)
            .Select(x => Create.PolyCurve([x]))
            .ToList();

        var buildingCreationData = GenerateMassingForTargetGFA(
            footprints,
            (int)Math.Floor(_config.MaxHeight/ _config.FloorHeight),
            // TODO: Find target gfa
            _config.TargetGFA.Value);

        // Make buildings
        List<Building> buildings = buildingCreationData
            .Select(x => CreateBuilding(x, _config.FloorHeight))
            .ToList();

        var buildingLayout = new BuildingLayout(site, buildableZone, buildings, siteId);
        return buildingLayout;
    }

    private List<Polygon> GenerateFootprints(Polyline primaryAxis, ICurve buildableZone)
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
            var perp = moveBtoA.Rotate(90d.DegreesToRadians(), Vector.ZAxis).Unitized();

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

            if(!footprint.ICurveIntersections(footprints.Cast<ICurve>()).Any())
                footprints.Add(footprint);

            i += widthParamSize;
        }

        return footprints;
    }
}
