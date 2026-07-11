using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.RhinoInterface;
using Rhino;

namespace AIQ.Urban.RhinoPlugin.MCP_Bridge;

public class UrbanModelTools(RhinoDoc doc, UrbanModel urbanModel)
{
    private const string _success = "Succeeded";

    [McpTool("add_transit", "Adds transits by centerline coordinates, width, and transit type. Automatically offsets the centerline to create the transit geometry.")]
    public string AddTransit(
        double width,
        TransitType type,
        List<CurveCoordinates> centerlineCurves)
    {
        try
        {
            var curves = centerlineCurves.Select(x => Curve2D.Create(x.ToCoreTupleList())).ToList();
            var transitData = curves.Select(c => new TransitData(c, width, type)).ToList();

            Action<RhinoUrbanModelPipeline> pipelineAction = (pipeline) => PipelineConfigurations
            .WithTransitAdditionSteps(pipeline, transitData, []);

            new RhinoPipelineExecutorForMcp(doc, urbanModel).Run(pipelineAction);
            return _success;
        }
        catch (Exception ex)
        {
            return FailureMessage(ex);
        }
    }

    [McpTool("add_building", "Adds buildings by footprint coordinates, floor height, floor count, and use type.")]
    public string AddBuilding(
        double floorHeight,
        int floorCount,
        string useName,
        List<CurveCoordinates> footprintCurves)
    {
        try
        {
            var use = urbanModel.GetUse(useName);
            var footprints = footprintCurves.Select(c => Polygon2D.Create(c.ToCoreTupleList())).ToList();
            var buildingData = footprints.Select(c => new BuildingFootprintData(c, floorHeight, floorCount, use)).ToList();

            Action<RhinoUrbanModelPipeline> pipelineAction = (pipeline)
                => PipelineConfigurations
                .WithBuildingAdditionSteps(pipeline, buildingData.Cast<ICreationData>().ToList(), []);

            new RhinoPipelineExecutorForMcp(doc, urbanModel).Run(pipelineAction);

            return _success;
        }
        catch (Exception ex)
        {
            return FailureMessage(ex);
        }
    }

    [McpTool("add_zone", "Adds a zone boundary with a name.")]
    public string AddZone(
        string name,
        List<CurveCoordinates> boundaryCoordinates)
    {
        try
        {
            if (boundaryCoordinates.Count == 0)
                return "Failed. No boundary coordinates provided.";

            var boundary = Polygon2D.Create(boundaryCoordinates[0].ToCoreTupleList());
            var zoneData = new List<ZoneData> { new(boundary, name) };

            Action<RhinoUrbanModelPipeline> pipelineAction = (pipeline) => PipelineConfigurations
                .WithZoneAdditionSteps(pipeline, zoneData, []);

            new RhinoPipelineExecutorForMcp(doc, urbanModel).Run(pipelineAction);
            return _success;
        }
        catch (Exception ex)
        {
            return FailureMessage(ex);
        }
    }

    [McpTool("delete_transit", "Deletes transits by their centerline IDs.")]
    public string DeleteTransit(List<Guid> centerlineIds)
    {
        try
        {
            Action<RhinoUrbanModelPipeline> pipelineAction = (pipeline) => PipelineConfigurations
                .WithTransitDeletionSteps(pipeline, centerlineIds);

            new RhinoPipelineExecutorForMcp(doc, urbanModel).Run(pipelineAction);
            return _success;
        }
        catch (Exception ex)
        {
            return FailureMessage(ex);
        }
    }

    [McpTool("delete_building", "Deletes buildings by their geometry IDs.")]
    public string DeleteBuilding(List<Guid> geometryIds)
    {
        try
        {
            Action<RhinoUrbanModelPipeline> pipelineAction = (pipeline) => PipelineConfigurations
                .WithBuildingDeletionSteps(pipeline, geometryIds);

            new RhinoPipelineExecutorForMcp(doc, urbanModel).Run(pipelineAction);
            return _success;
        }
        catch (Exception ex)
        {
            return FailureMessage(ex);
        }
    }

    [McpTool("delete_zone", "Deletes zones by their boundary geometry IDs.")]
    public string DeleteZone(List<Guid> geometryIds)
    {
        try
        {
            Action<RhinoUrbanModelPipeline> pipelineAction = (pipeline) => PipelineConfigurations
                .WithZoneDeletionSteps(pipeline, geometryIds);

            new RhinoPipelineExecutorForMcp(doc, urbanModel).Run(pipelineAction);
            return _success;
        }
        catch (Exception ex)
        {
            return FailureMessage(ex);
        }
    }

    [McpTool("compute_metrics", "Recomputes project metrics (FAR, coverage, area per use, transit lengths, zone metrics) from current model state.")]
    public string ComputeMetrics(double tolerance = 0.001)
    {
        try
        {
            ProjectMetricsCalculator.CalculateAndStore(urbanModel, tolerance);
            return JsonSerializer.Serialize(urbanModel.ProjectMetrics, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception ex)
        {
            return FailureMessage(ex);
        }
    }

    [McpTool("change_building_use", "Changes the use of buildings or individual floor plates by their geometry IDs.")]
    public string ChangeBuildingUse(List<Guid> geometryIds, string useName)
    {
        try
        {
            var use = urbanModel.GetUse(useName);

            Action<RhinoUrbanModelPipeline> pipelineAction = (pipeline) => PipelineConfigurations
                .WithBuildingUseChangeSteps(pipeline, geometryIds, use);

            new RhinoPipelineExecutorForMcp(doc, urbanModel).Run(pipelineAction);
            return _success;
        }
        catch (Exception ex)
        {
            return FailureMessage(ex);
        }
    }

    [McpTool("list_elements", "Lists all urban elements (transits, buildings, parcels, zones) in the current model.")]
    public string ListElements()
    {
        try
        {
            var response = new ListElementsResponse(
                urbanModel.Transits.Select(t => new ElementSummary(t.GetGeometryIds()[0], "Transit", null, t.ToString() ?? "", null)).ToList(),
                urbanModel.Buildings.Select(b => new ElementSummary(b.GetGeometryIds()[0], "Building", b.FloorPlates.FirstOrDefault()?.Use?.Name, b.ToString() ?? "", null)).ToList(),
                urbanModel.Parcels.Select(p => new ElementSummary(p.GetGeometryIds()[0], "Parcel", p.Use?.Name, p.ToString() ?? "", null)).ToList(),
                urbanModel.Zones.Select(z => new ElementSummary(z.GetGeometryIds()[0], "Zone", z.Name, z.ToString() ?? "", null)).ToList());

            return ElementSummarySerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            return FailureMessage(ex);
        }
    }

    [McpTool("list_available_uses", "Returns all available use types that can be assigned to buildings or floor plates.")]
    public string ListAvailableUses()
    {
        return JsonSerializer.Serialize(urbanModel.Uses.Select(u => u.Name).ToArray(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static string FailureMessage(Exception ex)
    {
        return "Failed. Exception - " + ex.Message;
    }
}
