using AIQ.Urban.Core;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Pipeline.Configs;
using AIQ.MCP.Models;

namespace AIQ.MCP.Services;

/// <summary>Data-only coordinator that translates MCP request records into
/// pipeline configurations and step objects. Does not own pipeline execution
/// or RhinoDoc access.</summary>
public class UrbanModelService
{
    /// <summary>Creates the pipeline configuration for adding one or more transits.</summary>
    public TransitPipelineConfiguration CreateTransitSteps(AddTransitRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Items);

        if (request.Items.Count == 0)
            throw new ArgumentException("At least one transit item is required.");

        var transitData = new List<TransitData>(request.Items.Count);
        var centerlineIds = new List<Guid>(request.Items.Count);

        foreach (var item in request.Items)
        {
            if (item.Width <= 0)
                throw new ArgumentException($"Transit width must be positive. Got: {item.Width}.");

            var centerline = Curve2D.Create(item.CenterlineCoords);
            var transitType = item.TransitType.ParseToTransitType();

            centerlineIds.Add(centerline.Id);
            transitData.Add(new TransitData(centerline, item.Width, transitType));
        }

        return new TransitPipelineConfiguration(transitData, centerlineIds);
    }

    /// <summary>Creates the pipeline configuration for adding one or more buildings.</summary>
    public BuildingPipelineConfiguration CreateBuildingSteps(AddBuildingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Items);

        if (request.Items.Count == 0)
            throw new ArgumentException("At least one building item is required.");

        var buildingData = new List<BuildingData>(request.Items.Count);
        var inputCurveIds = new List<Guid>(request.Items.Count);

        foreach (var item in request.Items)
        {
            if (item.TypicalFloorHeight <= 0)
                throw new ArgumentException($"Building floor height must be positive. Got: {item.TypicalFloorHeight}.");

            if (item.FloorCount <= 0)
                throw new ArgumentException($"Building floor count must be positive. Got: {item.FloorCount}.");

            var footprint = Polygon2D.Create(item.FootprintCoords);
            var use = DefaultUses.Resolve(item.UseName);

            inputCurveIds.Add(footprint.Id);
            buildingData.Add(new BuildingData(footprint, item.TypicalFloorHeight, item.FloorCount, use));
        }

        return new BuildingPipelineConfiguration(buildingData, inputCurveIds);
    }

    /// <summary>Creates ChangeBuildingUseStep objects for changing one or more buildings' uses.
    /// Items are grouped by new use name, producing one step per distinct use.</summary>
    public List<ChangeBuildingUseStep> CreateChangeUseSteps(ChangeBuildingUseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Items);

        if (request.Items.Count == 0)
            throw new ArgumentException("At least one change item is required.");

        // Group by use name to create one step per distinct use
        var groups = request.Items
            .GroupBy(i => i.NewUseName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var steps = new List<ChangeBuildingUseStep>(groups.Count);

        foreach (var group in groups)
        {
            var use = DefaultUses.Resolve(group.Key);
            var geometryIds = group.Select(i => i.BuildingGeometryId).ToList();

            // Validate GUIDs
            foreach (var id in geometryIds)
            {
                if (id == Guid.Empty)
                    throw new ArgumentException("Building geometry ID cannot be empty.");
            }

            steps.Add(new ChangeBuildingUseStep(geometryIds, use));
        }

        return steps;
    }

    /// <summary>Groups change items by new use name and validates GUIDs.
    /// Returns one BuildingUseChangeGroup per distinct use, for direct consumption
    /// by PipelineConfigurations.WithBuildingUseChangeSteps.</summary>
    public List<BuildingUseChangeGroup> CreateChangeUseGroups(ChangeBuildingUseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Items);

        if (request.Items.Count == 0)
            throw new ArgumentException("At least one change item is required.");

        var groups = request.Items
            .GroupBy(i => i.NewUseName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new List<BuildingUseChangeGroup>(groups.Count);

        foreach (var group in groups)
        {
            var use = DefaultUses.Resolve(group.Key);
            var geometryIds = group.Select(i => i.BuildingGeometryId).ToList();

            foreach (var id in geometryIds)
            {
                if (id == Guid.Empty)
                    throw new ArgumentException("Building geometry ID cannot be empty.");
            }

            result.Add(new BuildingUseChangeGroup(geometryIds, use));
        }

        return result;
    }

    /// <summary>Builds element summaries from the current UrbanModel state.</summary>
    public ListElementsResponse GetElementSummaries()
    {
        var model = UrbanModel.Instance;

        var transits = model.Transits.Select(t => new ElementSummary(
            Id: t.Centerline.Id,
            Type: "Transit",
            Use: null,
            Summary: $"{t.TransitType} corridor, width={t.Width}",
            Details: new Dictionary<string, object>
            {
                ["transitType"] = t.TransitType.ToString(),
                ["width"] = t.Width,
                ["centerlineMidpoint"] = new[] { t.GetRepresentativeCenter().X, t.GetRepresentativeCenter().Y }
            })).ToList();

        var buildings = model.Buildings.Select(b =>
        {
            var centroid = b.GetRepresentativeCenter();
            var use = b.FloorPlates.FirstOrDefault()?.Use?.Name;
            return new ElementSummary(
                Id: b.Envelope.Id,
                Type: "Building",
                Use: use,
                Summary: $"{b.FloorCount}-storey building, use={use ?? "unknown"}",
                Details: new Dictionary<string, object>
                {
                    ["floorCount"] = b.FloorCount,
                    ["totalHeight"] = b.TypicalFloorHeight * b.FloorCount,
                    ["footprintCenter"] = new[] { centroid.X, centroid.Y }
                });
        }).ToList();

        var parcels = model.Parcels.Select(p =>
        {
            var centroid = p.GetRepresentativeCenter();
            return new ElementSummary(
                Id: p.Outline.Id,
                Type: "Parcel",
                Use: p.Use.Name,
                Summary: $"Parcel, use={p.Use.Name}",
                Details: new Dictionary<string, object>
                {
                    ["use"] = p.Use.Name,
                    ["centroid"] = new[] { centroid.X, centroid.Y }
                });
        }).ToList();

        var zones = model.Zones.Select(z => new ElementSummary(
            Id: z.Boundary.Id,
            Type: "Zone",
            Use: null,
            Summary: $"Zone: {z.Name}",
            Details: new Dictionary<string, object>
            {
                ["name"] = z.Name
            })).ToList();

        return new ListElementsResponse(transits, buildings, parcels, zones);
    }
}
