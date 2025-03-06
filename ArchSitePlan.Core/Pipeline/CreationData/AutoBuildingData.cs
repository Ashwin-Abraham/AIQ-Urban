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
    double BuildingWidth,
    double MinFloorPlateDepth,
    PolyCurve SiteBoundary,
    BuildingTargetMode TargetMode,
    double? TargetGFA,
    double? TargetFAR,
    double? TargetHeight,
    double Setback,
    double MaxHeight,
    double MaxLength,
    double MinLength,
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

public record AutoLShapedBuildingData 
{
    public double ArmRatio { get; }
    public double FloorPlateDepth { get; }
}

public record AutoOShapedBuildingData 
{
    public double MaxXYRatio { get; }
    public double FloorPlateDepth { get; }
    public double CourtyardDepth { get; }
}

public record AutoUShapedBuildingData
{
    public double MaxXYRatio { get; }
    public double FloorPlateDepth { get; }
    public double CourtyardDepth { get; }
}

public record UnionedBoxesBuildingData 
{
    public double Length { get; }
    public double Width { get; }
    public double LengthWidthVariation { get; }
    public double HeightVariation { get; }
}
