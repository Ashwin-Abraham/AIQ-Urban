using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoPlugin.Utils;
using AIQ.Urban.RhinoPlugin.Extensions;
using System.Linq;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoPlugin;

public class AddBuilding_AIQUrban : Command
{
    public AddBuilding_AIQUrban()
    {
        Instance = this;
    }

    public static AddBuilding_AIQUrban Instance { get; private set; }

    public override string EnglishName => "AddBuilding_AIQUrban";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var go = new GetObject();
        go.SetCommandPrompt("Select building footprint curve");
        go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;

        var floorHeightOption = new OptionDouble(3.0, 0.1, 100.0);
        var floorCountOption = new OptionInteger(1, 1, 1000);
        var useNames = DefaultUses.Names;
        var useListIndex = go.AddOptionList("Use", useNames, 0);
        int selectedUseIndex = 0;

        go.AddOptionDouble("Height", ref floorHeightOption);
        go.AddOptionInteger("Count", ref floorCountOption);

        if (!TryRunSelectionLoop(go, useListIndex, ref selectedUseIndex))
            return Result.Cancel;

        var selectedUse = DefaultUses.All[selectedUseIndex];
        var floorHeight = floorHeightOption.CurrentValue;
        var floorCount = floorCountOption.CurrentValue;
        var curveRef = go.Object(0);
        var footprint = curveRef.Curve();

        if (footprint == null)
        {
            RhinoApp.WriteLine("Selected object is not a curve.");
            return Result.Failure;
        }

        var building = new Building(footprint, floorHeight, floorCount, selectedUse);
        UrbanModel.Instance.AddBuilding(building);

        DeleteInputCurve(doc, curveRef);
        BakeNewBuilding(doc, building);

        doc.Views.Redraw();
        RhinoApp.WriteLine("Added building: {0} storeys, total height={1}, use={2}.",
            floorCount, floorHeight * floorCount, selectedUse.Name);
        return Result.Success;
    }

    private static bool TryRunSelectionLoop(GetObject go, int useListIndex, ref int selectedUseIndex)
    {
        while (true)
        {
            var rc = go.Get();

            if (rc == GetResult.Option)
            {
                if (go.Option().Index == useListIndex)
                    selectedUseIndex = go.Option().CurrentListOptionIndex;
                continue;
            }

            if (rc != GetResult.Object)
                return false;

            break;
        }

        return true;
    }

    private static void DeleteInputCurve(RhinoDoc doc, ObjRef curveRef)
    {
        doc.Objects.Delete(curveRef.ObjectId, true);
    }

    private void BakeNewBuilding(RhinoDoc doc, Building building)
    {
        var envelopeLayer = doc.Layers.EnsureLayerExists(LayerConstants.BuildingEnvelope).Index;
        var envelopeAttributes = new ObjectAttributes { LayerIndex = envelopeLayer };
        doc.Objects.AddBrep(building.Envelope.Geometry, envelopeAttributes);

        foreach (var plate in building.FloorPlates)
        {
            var plateLayer = doc.Layers.EnsureLayerExists(LayerConstants.BuildingFloorPlate(plate.Use)).Index;
            var plateAttributes = new ObjectAttributes { LayerIndex = plateLayer };
            doc.Objects.AddCurve(plate.Outline.Geometry, plateAttributes);
        }
    }
}
