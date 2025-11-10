using System.Drawing;
using AIQ.Primitives.Extensions;
using ArchSitePlan.Core.Constants;
using ArchSitePlan.Core.Utils;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models;

public class StudyAreaMetrics
{
    public double BoundedArea { get; private set; }
    public double FAR { get; private set; }
    public double DevelopableArea { get; private set; }
    public double BuiltArea { get; private set; }
    public double BuiltCoverageArea { get; private set; }
    public double CoverageRatio { get; init; }
    public string ZoneName { get; init; }

    public IReadOnlyList<ParcelMetrics> ParcelMetrics { get; private set; }

    public IReadOnlyList<BuildingMetrics> BuildingMetrics { get; private set; }

    public IReadOnlyList<TransitMetrics> TransitMetrics { get; private set; }

    public StudyAreaMetrics(
        ICurve? boundary, 
        string zoneName,
        List<ParcelMetrics> parcelMetrics, 
        List<BuildingMetrics> buildingsMetrics,
        List<TransitMetrics> transitMetrics)
    {
        ParcelMetrics = parcelMetrics;
        BuildingMetrics = buildingsMetrics;
        TransitMetrics = transitMetrics;
        ZoneName = zoneName;
        BoundedArea = Math.Abs(boundary?.IArea()?? 0);
        DevelopableArea = ParcelMetrics.Sum(p => p.Area);
        BuiltArea = BuildingMetrics.Sum(b => b.BUA);
        FAR = BuiltArea / DevelopableArea;
        BuiltCoverageArea = BuildingMetrics.Sum(x => x.GroundCoverageArea);
        CoverageRatio = BuiltCoverageArea / DevelopableArea;
    }

    public List<GraphData> ExtractLandAreaGraphData()
    {
        var graphData = new List<GraphData>();

        var metricsByUse = ParcelMetrics.GroupBy(p => p.Use);

        foreach (var group in metricsByUse)
        {
            graphData.Add(
                new GraphData(
                    Label: group.Key.Name,
                    Data: group.Sum(u => u.Area),
                    Color: ColorToRGBA(group.Key.Color)
                    ));
        }

        return graphData;
    }

    public List<GraphData> ExtractBuiltUseGraphData()
    {
        var graphData = new List<GraphData>();

        var buaByUse = BuildingMetrics
            .SelectMany(x => x.BuaPerUse)
            .GroupBy(x => x.Use)
            .Select(x => (x.Key, x.Sum(x => x.BUA)))
            .ToList();

        foreach (var group in buaByUse)
        {
            graphData.Add(
                new GraphData(
                    Label: group.Key?.Name ?? "null",
                    Data: group.Item2,
                    Color: ColorToRGBA(group.Key?.Color ?? Color.Gray)
                    ));
        }

        return graphData;
    }

    public List<GraphData> ExtractRoadLengthGraphData()
    {
        var groupedByWidth = TransitMetrics.GroupBy(p => Math.Round(p.RoadWidth, 1));
        var graphData = new List<GraphData>();
        int colorIndex = 0;

        foreach (var transit in groupedByWidth)
        {
            if (colorIndex > AppColours.StandardColors.Count - 1)
                colorIndex = 0;

            var color = AppColours.StandardColors[colorIndex].ToRGB();
            colorIndex++;

            graphData.Add(
                new GraphData(
                    Label: transit.Key.ToString(),
                    Data: transit.Sum(u => u.RunningLength),
                    Color: ColorToRGBA(color)
                    ));

        }

        return graphData;
    }

    public List<GraphData> ExtractRoadAreaGraphData()
    {
        var groupedByWidth = TransitMetrics.GroupBy(p => Math.Round(p.RoadWidth, 1));
        var graphData = new List<GraphData>();
        int colorIndex = 0;

        foreach (var transit in groupedByWidth)
        {
            if (colorIndex > AppColours.StandardColors.Count - 1)
                colorIndex = 0;

            var color = AppColours.StandardColors[colorIndex].ToRGB();
            colorIndex++;

            graphData.Add(
                new GraphData(
                    Label: transit.Key.ToString(),
                    Data: transit.Sum(u => u.Area),
                    Color: ColorToRGBA(color)
                    ));
        }

        return graphData;
    }

    private static string ColorToRGBA(Color color)
    {
        return $"rgba({color.R}, {color.G}, {color.B}, {color.A})";
    }
}
