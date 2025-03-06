using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Core.Pipeline.CreationData;

public record AutoBuildingData(
    PolyCurve SiteBoundary,
    BuildingTargetMode TargetMode,
    double? TargetGFA,
    double? TargetFAR,
    double? TargetHeight,
    double Setback,
    double MaxHeight,
    double MinHeight,
    double FloorHeight);

public record AutoRectangleBuildingData(
    double MaxFloorPlateDepth,
    double MinFloorPlateDepth,
    PolyCurve SiteBoundary,
    BuildingTargetMode TargetMode,
    double? TargetGFA,
    double? TargetFAR,
    double? TargetHeight,
    double Setback,
    double MaxHeight,
    double MinHeight,
    double FloorHeight) : AutoBuildingData(
        SiteBoundary,
        TargetMode,
        TargetGFA,
        TargetFAR,
        TargetHeight,
        Setback,
        MaxHeight,
        MinHeight,
        FloorHeight);

public record AutoLShapedBuildingData :AutoBuildingData
{
    public double ArmRatio { get; }
    public double FloorPlateDepth { get; }
}

public record AutoOShapedBuildingData : AutoBuildingData
{
    public double MaxXYRatio { get; }
    public double FloorPlateDepth { get; }
    public double CourtyardDepth { get; }
}

public record AutoUShapedBuildingData : AutoBuildingData
{
    public double MaxXYRatio { get; }
    public double FloorPlateDepth { get; }
    public double CourtyardDepth { get; }
}

public record UnionedBoxesBuildingData : AutoBuildingData
{
    public double Length { get; }
    public double Width { get; }
    public double LengthWidthVariation { get; }
    public double HeightVariation { get; }
}
