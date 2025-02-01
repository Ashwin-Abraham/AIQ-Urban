
using ArchSitePlan.UI.Actions;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using ArchSitePlan.UI.InputValidation.Results;
using ArchSitePlan.UI.InputValidation.Validators;
using ArchSitePlan.UI.Models;
using Eto.Forms;
using Newtonsoft.Json;
using Rhino;
using Rhino.DocObjects;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.HTML_UI.MessagesFromUI
{
    public class BuildingMessage:IMessageFromUI<MetricsMessage>
    {

        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Building;
        
        [JsonProperty("floorCount")]
        public int FloorCount { get; init; }
        
        [JsonProperty("floorHeight")]
        public double FloorHeight { get; init; }

        [JsonProperty("manual_generationType")]
        public string GenerationType;

        public MetricsMessage HandleMessage()
        {
            RhinoEventHandler.WPFViewModel.AppMode = ModellingTool.Building;
            bool validMode = Enum.TryParse<BuildingSelectorMode>(GenerationType, out var buildingMode);
            
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

            var vm = RhinoEventHandler.HTMLViewModel;
            if (validation.ValidObjects.Count == 0) 
                return vm.MetricsMessageData;

            var rhinoBuildingData = validation.ValidObjects
                .Select(o => RhinoBuildingData.FromRhinoObject(o, FloorCount, FloorHeight))
                .ToList();

            DocObjectHelpers.DeleteRhinoObjectsIfPresent(doc, rhinoObjs.Select(r => r.Id));
            RhinoEventHandler.UpdateSelectedBuildingsPosition(rhinoBuildingData);

            vm.UpdateGraphMessage();
            return vm.MetricsMessageData;
        }
    }

   
}
