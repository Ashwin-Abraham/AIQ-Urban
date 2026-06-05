using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    [Description("Adds transits to an urban model by specifying the centerline curve's coordinates, transit width and type of transit." +
        "The tool will automatically offset the centerline to create the transit geometry.")]
    public ProjectMetrics AddTransit(
        [Description("Width of transit in model units")]
        double width,

        [Description("Type of transit")]
        TransitType type,

        [Description("A list of XY coordinates for centerline curves. These may be lines or polylines. " +
        "If the first point in a curve is the same as the last, then it is considered closed.")]
        List<CurveCoordinates> curveCoordinates
    )
    {
        var curves = curveCoordinates.Select(x => Curve2D.Create(x.Coordinates)).ToList();
        var transitData = curves.Select(c => new TransitData(c, width, type)).ToList();

        Action<RhinoUrbanModelPipeline> pipelineAction = (pipeline) => PipelineConfigurations
        .WithTransitAdditionSteps(pipeline, transitData, [])
        .WithMetricsCalculation();

        return new RhinoPipelineExecutorForMcp(doc, urbanModel).Run(pipelineAction);
    }

    [Description("Lists all urban elements (transits, buildings, parcels, zones) in the current model. " +
        "Returns structured JSON summaries with GUIDs, types, uses, and type-specific details for each element.")]
    public ProjectMetrics AddBuilding(
        [Description("Floor to floor height between consecutive building floors")]
        double floorHeight,

        [Description("No of floors in the building")]
        int floorCount,

        [Description("Type of use for the building")]
        Use use,

        [Description("A list of XY coordinates for footprint curves. These may be lines or polylines. " +
        "If the first point in a curve is the same as the last, then it is considered closed. If not closed, the curve will be rejected.")]
        List<CurveCoordinates> footprintCurves
    )
    {
        var footprints = footprintCurves.Select(c => Polygon2D.Create(c.Coordinates)).ToList();
        var buildingData = footprints.Select(c => new BuildingData(c, floorHeight, floorCount, use)).ToList();

        Action<RhinoUrbanModelPipeline> pipelineAction = (pipeline)
            => PipelineConfigurations
            .WithBuildingAdditionSteps(pipeline, buildingData, [])
            .WithMetricsCalculation();

        return new RhinoPipelineExecutorForMcp(doc, urbanModel).Run(pipelineAction);
    }
}
