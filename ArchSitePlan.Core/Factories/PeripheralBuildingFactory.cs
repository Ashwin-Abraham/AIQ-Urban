using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories;

public class PeripheralBuildingFactory:BuildingLayoutFactoryBase
{
    AutoPeripheralBuildingData _config;
    public PeripheralBuildingFactory(AutoPeripheralBuildingData config)
    {
        _config = config;
    }

    public BuildingLayout? Generate(ICurve site, Guid? siteId = null)
    {
        var setBackLine = site.OffsetAndMaintainClosure(-(_config.Setback));

        if (setBackLine == null) return null;

        var buildingCL = site
            .OffsetAndMaintainClosure(-(_config.Setback + _config.BuildingWidth / 2));

        // Split up based on max length constraints
        var buildingCLFragments = ApplySolidVoidPattern(buildingCL, _config.MaxLength, 10);

        var buildingFootprints = buildingCLFragments
            .Select(f => f.Thicken(_config.BuildingWidth/2, EndCapType.Butt, Vector.ZAxis))
            .Cast<PolyCurve>()
            .ToList();

        buildingFootprints = RemoveClashes(buildingFootprints);

        var totalFootPrint = buildingFootprints.Sum(x => x.IArea());
        var requiredGfa = GetRequiredGFA(
            _config.TargetMode,
            _config.TargetGFA,
            _config.TargetFAR,
            site.IArea());

        var buildingSpecs = GenerateMassingForTargetGFA(
            buildingFootprints,
            (int)Math.Floor(_config.MaxHeight / _config.FloorHeight),
            requiredGfa);

        var buildings = buildingSpecs
            //.Select(x => new BuildingCreationSpecs(x, x.IArea(), floorCount))
            .Select(spec => CreateBuilding(spec, _config.FloorHeight))
            .Where(x => x!=null)
            .Cast<Building>()
            .ToList();

        var buildingLayout = new BuildingLayout(site, setBackLine, buildings, siteId);

        return buildingLayout;
    }

    /// <summary>
    /// Checks building footprints sequentially for clashes with footprints just
    /// before or just after
    /// </summary>
    private List<PolyCurve> RemoveClashes(List<PolyCurve> buildingFootprints)
    {
        if (buildingFootprints.Count == 0)
            return buildingFootprints;

        List<PolyCurve> validFootprints = new();

        for(int i = 0; i < buildingFootprints.Count; i ++)
        {
            var footprint = buildingFootprints[i];
            var points = footprint.ControlPoints();

            if (footprint.ICurveIntersections(validFootprints).Count > 0)
                continue;

            validFootprints.Add(footprint);


        }

        return validFootprints;
    }

    public static List<ICurve> ApplySolidVoidPattern(
        ICurve curve,
        double solidsLength,
        double gapLength)
    {
        solidsLength = Math.Abs(solidsLength);
        gapLength = Math.Abs(gapLength);
        var curveLength = curve.ILength();

        if (curveLength <= solidsLength)
            return [curve];

        var pattern = new List<double>() { 0 };

        for(double pos = 0; pos < curveLength; pos += solidsLength + gapLength)
        {
            pattern.Add(pos + gapLength);
            pattern.Add(pos + gapLength + solidsLength);
        }

        var splitPoints = pattern
            .Where(pos => pos < curveLength)
            .Select(pos => curve.IPointAtLength(pos)).ToList();

        var fragments = curve.ISplitAtPoints(splitPoints);
        var solids = fragments
            .Where((x, i) => int.IsOddInteger(i))
            .ToList();

        return solids;
    }
}