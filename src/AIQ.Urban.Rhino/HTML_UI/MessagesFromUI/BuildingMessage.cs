using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public class BuildingMessage : IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType => TypeOfMessage.Building;

    public int FloorCount { get; init; }

    public double FloorHeight { get; init; }

    public string manual_generationType { get; init; } = "Footprint";

    public ModelStateMessage HandleMessage()
    {
        var doc = RhinoDoc.ActiveDoc;
        var use = DefaultUses.MixedUse;

        if (manual_generationType == "Mass")
            return HandleMassMode(doc, use);

        return HandleFootprintMode(doc, use);
    }

    private ModelStateMessage HandleFootprintMode(RhinoDoc doc, Use use)
    {
        var selected = doc.Objects.GetSelectedObjects(false, false)
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building footprint curves first.");

        List<BuildingData> buildingData = [];
        List<Guid> idsToDelete = [];

        foreach (var obj in selected)
        {
            if (obj.Geometry is not Curve curve)
                continue;

            var polygon = new Polygon2D(curve);
            buildingData.Add(new BuildingData(polygon, FloorHeight, FloorCount, use));
            idsToDelete.Add(obj.Id);
        }

        if (buildingData.Count == 0)
            throw new Exception("Selected objects must be curves.");

        return RunPipeline(doc, buildingData, idsToDelete);
    }

    private ModelStateMessage HandleMassMode(RhinoDoc doc, Use use)
    {
        var selected = doc.Objects.GetSelectedObjects(false, false)
            .Where(obj => obj.ObjectType == ObjectType.Brep)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building envelope Breps first.");

        List<BuildingData> buildingData = [];
        List<Guid> idsToDelete = [];

        foreach (var obj in selected)
        {
            if (obj.Geometry is not Brep brep)
                continue;

            var footprint = ExtractFootprintFromBrep(brep);
            buildingData.Add(new BuildingData(footprint, FloorHeight, FloorCount, use));
            idsToDelete.Add(obj.Id);
        }

        if (buildingData.Count == 0)
            throw new Exception("Selected objects must be Breps.");

        return RunPipeline(doc, buildingData, idsToDelete);
    }

    private ModelStateMessage RunPipeline(RhinoDoc doc, List<BuildingData> buildingData, List<Guid> idsToDelete)
    {
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithBuildingAdditionSteps(buildingData, idsToDelete)
            .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

        if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
            pipeline.WithEventHandler(handler);

        pipeline.Run();
        doc.Views.Redraw();

        return new ModelStateMessage(UrbanModel.Instance);
    }

    private static Polygon2D ExtractFootprintFromBrep(Brep brep)
    {
        var bottomFace = brep.Faces
            .OrderBy(f => f.GetBoundingBox(true).Center.Z)
            .First();

        var curve = bottomFace.OuterLoop.To3dCurve();
        return new Polygon2D(curve);
    }
}
