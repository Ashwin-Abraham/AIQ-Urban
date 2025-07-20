using ArchSitePlan.Core.Factories;
using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.RhinoBhomInterop;
using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using ArchSitePlan.RhinoInterface.InputValidation.Results;
using ArchSitePlan.RhinoInterface.InputValidation.Validators;
using BH.oM.Geometry;
using Newtonsoft.Json;
using Rhino;
using static ArchSitePlan.Core.Constants.Enums;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;

public enum AutoGenerationType { Row, Peripheral}
class AutoBuildingMessage : IMessageFromUI<StudyAreaMetricsMessage>
{
    public TypeOfMessage MessageType { get; init; }

    [JsonProperty("autoGenerationType")]
    public AutoGenerationType AutoGenerationType { get; init; }

    [JsonProperty("setback")]
    public double Setback { get; init; }

    [JsonProperty("floorHeight")]
    public double FloorHeight { get; init; }

    [JsonProperty("maxHeight")]
    public double MaxHeight { get; init; }

    [JsonProperty("maxLength")]
    public double MaxLength { get; init; }

    [JsonProperty("floorDepth")]
    public double FloorDepth { get; init; }

    [JsonProperty("gfa")]
    public double GFA { get; init; }

    [JsonProperty("far")]
    public double FAR { get; init; }

    [JsonProperty("modeIsGFA")]
    public bool ModeIsGFA { get; init; }

    public StudyAreaMetricsMessage HandleMessage()
    {
        RhinoDoc doc = RhinoDoc.ActiveDoc;
        var rhinoObjs = doc.Objects.GetSelectedObjects(false, false).ToList();

        var validation = new BuildingInputValidator(BuildingGeneratorStartPoint.WithinParcel)
                .ValidateInput(rhinoObjs);

        if (validation.Result != ValidationResult.All_Valid)
            throw new Exception(validation.Message);

        var siteBoundaries = validation.ValidObjects
            .Select(x => x.Geometry as Rhino.Geometry.Brep)
            .Select(x => x.ToBHoMXYOutline())
            .ToList();

        var layouts = new List<BuildingLayout>();

        switch (AutoGenerationType)
        {
            case AutoGenerationType.Row: 
                layouts = ComputeAutoRectangleLayouts(siteBoundaries);
                break;

            case AutoGenerationType.Peripheral: 
            default: 
                layouts = ComputeAutoPeripheralLayouts(siteBoundaries);
                break;

        }

        ModelBuilderPipelines.AutoGenerateBuildingsOnParcel( doc, layouts);

        return new();
    }

    private List<BuildingLayout> ComputeAutoRectangleLayouts(List<ICurve> sites)
    {
        var rowConfig = new AutoRectangleBuildingData(
                    BuildingWidth: FloorDepth,
                    TargetMode: ModeIsGFA ? BuildingTargetMode.GFA : BuildingTargetMode.FAR,
                    TargetGFA: GFA,
                    TargetFAR: FAR,
                    Setback: Setback,
                    MaxHeight: MaxHeight,
                    MaxLength: MaxLength,
                    MinLength: FloorDepth,
                    FloorHeight: FloorHeight);

        List<BuildingLayout> layouts = new();

        foreach (var site in sites)
        {
            var layout = new RectangleBuildingFactory(rowConfig).Generate(site);
            if (layout != null)
                layouts.Add(layout);
        }

        return layouts;
    }

    private List<BuildingLayout> ComputeAutoPeripheralLayouts(List<ICurve> sites)
    {
        var peripheralConfig = new AutoPeripheralBuildingData(
                   BuildingWidth: FloorDepth,
                   MinFloorPlateDepth: FloorDepth,
                   TargetMode: ModeIsGFA ? BuildingTargetMode.GFA : BuildingTargetMode.FAR,
                   TargetGFA: GFA,
                   TargetFAR: FAR,
                   Setback: Setback,
                   MaxHeight: MaxHeight,
                   MaxLength: MaxLength,
                   MinLength: FloorDepth,
                   FloorHeight: FloorHeight);

        List<BuildingLayout> layouts = new();

        foreach (var site in sites)
        {
            var layout = new PeripheralBuildingFactory(peripheralConfig).Generate(site);
            if (layout != null)
                layouts.Add(layout);
        }

        return layouts;
    }
}
