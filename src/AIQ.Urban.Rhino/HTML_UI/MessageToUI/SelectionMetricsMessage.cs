using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.Metrics;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

public class SelectionMetricsMessage : IMessageToUI
{
    public string Type => nameof(SelectionMetricsMessage);

    public BuildingSelectionMetrics Buildings { get; }

    public ParcelSelectionMetrics Parcels { get; }

    public TransitSelectionMetrics Transits { get; }

    private SelectionMetricsMessage(
        BuildingSelectionMetrics buildings,
        ParcelSelectionMetrics parcels,
        TransitSelectionMetrics transits)
    {
        Buildings = buildings;
        Parcels = parcels;
        Transits = transits;
    }

    public static SelectionMetricsMessage Empty { get; } = new(
        BuildingSelectionMetrics.Empty,
        ParcelSelectionMetrics.Empty,
        TransitSelectionMetrics.Empty);

    public static SelectionMetricsMessage FromSelection(
        UrbanModel urbanModel,
        IEnumerable<Guid> selectedRhinoObjectIds)
    {
        var projectMetrics = urbanModel.ProjectMetrics;
        if (projectMetrics is null)
            return Empty;

        var selected = ResolveSelection(urbanModel, selectedRhinoObjectIds);

        var buildings = projectMetrics.BuildingMetrics
            .Where(metrics => selected.Buildings.Contains(metrics.SourceElementId))
            .ToList();

        var parcels = projectMetrics.ParcelMetrics
            .Where(metrics => selected.Parcels.Contains(metrics.SourceElementId))
            .ToList();

        var transits = projectMetrics.TransitMetrics
            .Where(metrics => selected.Transits.Contains(metrics.SourceElementId))
            .ToList();

        return new(
            BuildBuildingMetrics(buildings),
            BuildParcelMetrics(parcels),
            BuildTransitMetrics(transits));
    }

    private static BuildingSelectionMetrics BuildBuildingMetrics(
        List<BuildingMetrics> metrics)
    {
        if (metrics.Count == 0)
            return BuildingSelectionMetrics.Empty;

        return new(
            Count: metrics.Count,
            BuiltUpArea: metrics.Sum(item => item.BuiltUpArea),
            FootprintArea: metrics.Sum(item => item.FootprintArea),
            Volume: metrics.Sum(item => item.Volume));
    }

    private static ParcelSelectionMetrics BuildParcelMetrics(
        List<ParcelMetrics> metrics)
    {
        if (metrics.Count == 0)
            return ParcelSelectionMetrics.Empty;

        var landArea = metrics.Sum(item => item.LandArea);
        var builtUpArea = metrics.Sum(item => item.BuiltUpArea);
        var builtFootprint = metrics.Sum(item => item.BuiltFootprint);

        return new(
            Count: metrics.Count,
            LandArea: landArea,
            Perimeter: metrics.Sum(item => item.Perimeter),
            BuiltUpArea: builtUpArea,
            BuiltFootprint: builtFootprint,
            FAR: landArea > 0 ? builtUpArea / landArea : 0,
            CoveragePercentage: landArea > 0 ? builtFootprint / landArea * 100 : 0);
    }

    private static TransitSelectionMetrics BuildTransitMetrics(
        List<TransitMetrics> metrics)
    {
        if (metrics.Count == 0)
            return TransitSelectionMetrics.Empty;

        return new(
            Count: metrics.Count,
            RunningLength: metrics.Sum(item => item.RunningLength),
            Area: metrics.Sum(item => item.Area));
    }

    private static (
        HashSet<Guid> Buildings,
        HashSet<Guid> Parcels,
        HashSet<Guid> Transits) ResolveSelection(
            UrbanModel urbanModel,
            IEnumerable<Guid> selectedRhinoObjectIds)
    {
        var selectedIds = selectedRhinoObjectIds.ToHashSet();

        if (selectedIds.Count == 0)
            return ([], [], []);

        var buildings = urbanModel.Buildings
            .Where(building => building.GetGeometryIds().Any(selectedIds.Contains))
            .Select(building => building.Id)
            .ToHashSet();

        var parcels = urbanModel.Parcels
            .Where(parcel => parcel.GetGeometryIds().Any(selectedIds.Contains))
            .Select(parcel => parcel.Id)
            .ToHashSet();

        var transits = urbanModel.Transits
            .Where(transit => transit.GetGeometryIds().Any(selectedIds.Contains))
            .Select(transit => transit.Id)
            .ToHashSet();

        return (buildings, parcels, transits);
    }
}
