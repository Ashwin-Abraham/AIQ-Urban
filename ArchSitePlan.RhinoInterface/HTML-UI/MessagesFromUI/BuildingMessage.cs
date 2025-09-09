using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.Helpers;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using ArchSitePlan.RhinoInterface.InputValidation.Results;
using ArchSitePlan.RhinoInterface.InputValidation.Validators;
using ArchSitePlan.RhinoInterface.Models;
using Newtonsoft.Json;
using Rhino;
using Rhino.DocObjects;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;

public class BuildingMessage:IMessageFromUI<StudyAreaMetricsMessage>
{

    public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Building;
    
    [JsonProperty("floorCount")]
    public int FloorCount { get; init; }
    
    [JsonProperty("floorHeight")]
    public double FloorHeight { get; init; }

    [JsonProperty("manual_generationType")]
    public string GenerationType;

    public StudyAreaMetricsMessage HandleMessage()
    {
        RhinoEventHandler.HTMLViewModel.AppMode = ModellingTool.Building;
        bool validMode = Enum.TryParse<BuildingGeneratorStartPoint>(GenerationType, out var buildingMode);
        
        if (!validMode)
            throw new Exception("Invalid building generation mode");
        
        RhinoDoc doc = RhinoDoc.ActiveDoc;

        var allowedTypes = new List<ObjectType>
            {
                ObjectType.Curve, ObjectType.Brep, ObjectType.Mesh, ObjectType.Extrusion
            };

        var rhinoObjs = doc.Objects.GetSelectedObjects(false, false)
            .Where(obj => allowedTypes.Contains(obj.ObjectType))
            .ToList();

        var validation = new BuildingInputValidator(buildingMode)
            .ValidateInput(rhinoObjs);

        if (validation.Result != ValidationResult.All_Valid)
            throw new Exception(validation.Message);

        if (validation.ValidObjects.Count == 0)
            return new();

        var rhinoBuildingData = validation.ValidObjects
            .Select(o => RhinoBuildingData.FromRhinoObject(o, FloorCount, FloorHeight))
            .ToList();

        DocObjectHelpers.DeleteRhinoObjectsIfPresent(doc, rhinoObjs.Select(r => r.Id));
        RhinoEventHandler.UpdateSelectedBuildingsPosition(rhinoBuildingData);

        return new();
    }
}


