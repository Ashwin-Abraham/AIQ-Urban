using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Pipeline.CreationData;

public class AutoBuildingData
{
    public PolyCurve SiteBoundary { get; }
    public BuildingTargetMode TargetMode { get; }
    public double? TargetGFA { get; }
    public double? TargetFAR { get; }
    public double? TargetHeight { get; }
    public double Setback { get; }
    public double MaxHeight { get; }
    public double MinHeight { get; }
    public double FloorHeight { get; }
}

public class AutoRectangleBuildingData : AutoBuildingData
{
    public double MaxFloorPlateDepth { get; }
    public double MinFloorPlateDepth { get; }
}

public class AutoLShapedBuildingData :AutoBuildingData
{
    public double ArmRatio { get; }
    public double FloorPlateDepth { get; }
}

public class AutoOShapedBuildingData : AutoBuildingData
{
    public double MaxXYRatio { get; }
    public double FloorPlateDepth { get; }
    public double CourtyardDepth { get; }
}

public class AutoUShapedBuildingData : AutoBuildingData
{
    public double MaxXYRatio { get; }
    public double FloorPlateDepth { get; }
    public double CourtyardDepth { get; }
}

public class UnionedBoxesBuildingData : AutoBuildingData
{
    public double Length { get; }
    public double Width { get; }
    public double LengthWidthVariation { get; }
    public double HeightVariation { get; }
}
