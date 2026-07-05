using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;

public class SelectBuildingFootprintsStep(
    IEnumerable<RhinoObject> selectedObjects,
    double floorHeight,
    int floorCount,
    Use use,
    string generationType) : IRhinoDataGatherStep
{
    public ICreationData Run(RhinoDoc doc)
    {
        if (generationType == "Mass")
            return GatherFromMass();

        return GatherFromFootprints();
    }

    private CreationDataCollection<BuildingData> GatherFromFootprints()
    {
        var selected = selectedObjects
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building footprint curves first.");

        var buildingData = new List<BuildingData>();

        foreach (var obj in selected)
        {
            if (obj.Geometry is not Curve curve)
                continue;

            var polygon = new Polygon2D(curve);
            buildingData.Add(new BuildingData(polygon, floorHeight, floorCount, use));
        }

        if (buildingData.Count == 0)
            throw new Exception("Selected objects must be curves.");

        return new CreationDataCollection<BuildingData>(buildingData);
    }

    private CreationDataCollection<BuildingData> GatherFromMass()
    {
        var selected = selectedObjects
            .Where(obj => obj.ObjectType == ObjectType.Brep)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building envelope Breps first.");

        var buildingData = new List<BuildingData>();

        foreach (var obj in selected)
        {
            if (obj.Geometry is not Brep brep)
                continue;

            var footprint = ExtractFootprintFromBrep(brep);
            buildingData.Add(new BuildingData(footprint, floorHeight, floorCount, use));
        }

        if (buildingData.Count == 0)
            throw new Exception("Selected objects must be Breps.");

        return new CreationDataCollection<BuildingData>(buildingData);
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
