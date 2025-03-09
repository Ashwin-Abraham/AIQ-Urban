using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
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
        var buildingCLFragments = new List<PolyCurve>() { buildingCL! };

        // x
        var buildingFootprints = buildingCLFragments
            .Select(f => f.Thicken(_config.BuildingWidth, EndCapType.Butt))
            .Cast<PolyCurve>()
            .ToList();

        DEBUGMODEL.Reset();
        buildingFootprints.Debug();
        DEBUGMODEL.OpenAsGlb();

        var totalFootPrint = buildingFootprints.Sum(x => x.IArea());
        var requiredGfa = GetRequiredGFA(
            _config.TargetMode,
            _config.TargetGFA,
            _config.TargetFAR,
            _config.TargetHeight,
            site.Area());

        var floorCount = (int) Math.Floor(requiredGfa / site.Area());

        var buildings = buildingFootprints
            .Select(x => new BuildingCreationSpecs(x, x.IArea(), floorCount))
            .Select(spec => CreateBuilding(spec, _config.FloorHeight))
            .Where(x => x!=null)
            .Cast<Building>()
            .ToList();

        var buildingLayout = new BuildingLayout(site, setBackLine, buildings);

        return buildingLayout;
    }
}