using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.RhinoInterface;
using Rhino;

namespace AIQ.Urban.RhinoPlugin.MCP_Bridge;

public record CurveCoordinates(List<(double X, double Y)> Coordinates);

public class UrbanModelTools(RhinoDoc doc, UrbanModel urbanModel)
{
    private const string  _success = "Succeeded";

    /// <summary>Adds transits by centerline coordinates, width, and transit type. Automatically offsets the centerline to create the transit geometry.</summary>
    /// <param name="width">Width of transit in model units.</param>
    /// <param name="type">Type of transit.</param>
    /// <param name="curveCoordinates">A list of XY coordinates for centerline curves. These may be lines or polylines. If the first point in a curve is the same as the last, then it is considered closed.</param>
    public string AddTransit(
        double width,
        TransitType type,
        List<CurveCoordinates> curveCoordinates
    )
    {
        try
        {

            var curves = curveCoordinates.Select(x => Curve2D.Create(x.Coordinates)).ToList();
            var transitData = curves.Select(c => new TransitData(c, width, type)).ToList();

            Action<RhinoUrbanModelPipeline> pipelineAction = (pipeline) => PipelineConfigurations
            .WithTransitAdditionSteps(pipeline, transitData, []);

            var metrics = new RhinoPipelineExecutorForMcp(doc, urbanModel).Run(pipelineAction);
            return _success;
        }
        catch(Exception ex)
        {
            return FailureMessage(ex);
        }
    }

    /// <summary>Adds buildings by footprint coordinates, floor height, floor count, and use type.</summary>
    /// <param name="floorHeight">Floor to floor height between consecutive building floors.</param>
    /// <param name="floorCount">Number of floors in the building.</param>
    /// <param name="use">Type of use for the building.</param>
    /// <param name="footprintCurves">A list of XY coordinates for footprint curves. These must be closed rings. If not closed, the curve will be rejected.</param>
    public string AddBuilding(
        double floorHeight,
        int floorCount,
        Use use,
        List<CurveCoordinates> footprintCurves
    )
    {
        try
        {
            var footprints = footprintCurves.Select(c => Polygon2D.Create(c.Coordinates)).ToList();
            var buildingData = footprints.Select(c => new BuildingData(c, floorHeight, floorCount, use)).ToList();

            Action<RhinoUrbanModelPipeline> pipelineAction = (pipeline)
                => PipelineConfigurations
                .WithBuildingAdditionSteps(pipeline, buildingData, []);

            new RhinoPipelineExecutorForMcp(doc, urbanModel).Run(pipelineAction);

            return _success;
        }
        catch (Exception ex)
        {
            return FailureMessage(ex);
        }
    }

    /// <summary>Adds a zone by a named boundary polygon.</summary>
    /// <param name="name">Name for the zone.</param>
    /// <param name="boundaryCoordinates">A list of XY coordinates forming the zone boundary (closed ring).</param>
    public string AddZone(
        string name,
        List<CurveCoordinates> boundaryCoordinates)
    {
        try
        {
            if (boundaryCoordinates.Count == 0)
                return "Failed. No boundary coordinates provided.";

            var boundary = Polygon2D.Create(boundaryCoordinates[0].Coordinates);
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

    /// <summary>Deletes transits by their centerline geometry IDs.</summary>
    /// <param name="centerlineIds">List of centerline geometry IDs to delete.</param>
    public string DeleteTransit(
        List<Guid> centerlineIds)
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

    /// <summary>Deletes buildings by their geometry IDs.</summary>
    /// <param name="geometryIds">List of building geometry IDs to delete.</param>
    public string DeleteBuilding(
        List<Guid> geometryIds)
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

    /// <summary>Deletes zones by their boundary geometry IDs.</summary>
    /// <param name="geometryIds">List of zone boundary geometry IDs to delete.</param>
    public string DeleteZone(
        List<Guid> geometryIds)
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

    /// <summary>Computes project metrics (FAR, coverage, built-up area, transit lengths, zone metrics) from the current model state and returns them as JSON.</summary>
    /// <param name="tolerance">Containment tolerance for point-in-polygon checks (default 0.001).</param>
    public string ComputeMetrics(
        double tolerance = 0.001)
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

    /// <summary>Changes the use of one or more buildings or floor plates by their geometry IDs.</summary>
    /// <param name="geometryIds">List of geometry IDs (envelope or floor plate outlines) to change.</param>
    /// <param name="useName">New use type name (e.g. Residential, Commercial).</param>
    public string ChangeBuildingUse(
        List<Guid> geometryIds,
        string useName)
    {
        try
        {
            var use = DefaultUses.Resolve(useName);

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

    private static string FailureMessage(Exception ex)
    {
        return "Failed. Exception - " + ex.Message;
    }
}
