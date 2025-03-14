using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Factories;

public class PeripheralBuildingFactory:BuildingFactoryBase
{
    AutoPeripheralBuildingData _config;
    public PeripheralBuildingFactory(AutoPeripheralBuildingData config)
    {
        _config = config;
    }

    public BuildingLayout Generate(PolyCurve site)
    {
        var setBackLine = site.OffsetAndMaintainClosure(-(_config.Setback)) as PolyCurve;

        if (setBackLine == null) return null;

        var buildingCL = site
            .OffsetAndMaintainClosure(-(_config.Setback + _config.BuildingWidth / 2))
            as PolyCurve;

        // Split up based on max length constraints
        var buildingCLFragments = ApplySolidVoidPattern(buildingCL, _config.MaxLength, 10);
        
        var buildingFootprints = buildingCLFragments
            .Select(f => f.Thicken(_config.BuildingWidth, EndCapType.Butt))
            .Cast<PolyCurve>()
            .ToList();

        buildingFootprints = RemoveClashes(buildingFootprints);

        var totalFootPrint = buildingFootprints.Sum(x => x.IArea());
        var requiredGfa = GetRequiredGFA(
            _config.TargetMode,
            _config.TargetGFA,
            _config.TargetFAR,
            _config.TargetHeight,
            site.Area());

        var floorCount = (int) Math.Floor(requiredGfa / site.Area());

        var buildingSpecs = GenerateMassingForTargetGFA(
            buildingFootprints,
            (int)Math.Floor(_config.MaxHeight / _config.FloorHeight),
            _config.TargetGFA.Value);

        var buildings = buildingSpecs
            //.Select(x => new BuildingCreationSpecs(x, x.IArea(), floorCount))
            .Select(spec => CreateBuilding(spec, _config.FloorHeight))
            .Where(x => x!=null)
            .Cast<Building>()
            .ToList();

        var buildingLayout = new BuildingLayout(site, setBackLine, buildings);

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

        List<PolyCurve> validFootprints = new() { buildingFootprints[0] };

        for(int i = 1; i < buildingFootprints.Count; i ++)
        {
            var footprint = buildingFootprints[i];
            var points = footprint.ControlPoints();

            if (validFootprints[^1].IsContainingAny(points))
                continue;

            if (i == buildingFootprints.Count - 1
                && buildingFootprints[0].IsContainingAny(points))
                continue;

            validFootprints.Add(footprint);
        }

        return validFootprints;
    }

    public static List<PolyCurve> ApplySolidVoidPattern(
        PolyCurve curve,
        double solidsLength,
        double gapLength)
    {
        solidsLength = Math.Abs(solidsLength);
        gapLength = Math.Abs(gapLength);
        var curveLength = curve.Length();

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
            .Select(pos => curve.PointAtLength(pos)).ToList();

        var fragments = curve.SplitAtPoints(splitPoints);
        var solids = fragments
            .Where((x, i) => int.IsOddInteger(i))
            .Select(x => x.ToPolycurve())
            .ToList();

        return solids;
    }
}