using ArchSitePlan.Core.Extensions;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.UI.Actions;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.CoreModelInterface;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using static ArchSitePlan.UI.Constants.Enums;

namespace ArchSitePlan.UI.HTML_UI.MessagesFromUI
{
    public class ZoneMessage:IMessageFromUI<ZonesInModelMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.Zone;
        public string Name { get; init; }

        public ZonesInModelMessage HandleMessage()
    {
            var vm = RhinoEventHandler.HTMLViewModel;
            RhinoEventHandler.WPFViewModel.AppMode = ModellingTool.Zones;

            var rhinoObjs = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();

            if (rhinoObjs.Count != 1 || rhinoObjs[0].ObjectType != ObjectType.Curve)
                throw new Exception(UIMessages.AddStudyInstruction);

            var rhinoObj = rhinoObjs[0];
            var boundary = rhinoObj.Geometry as Curve;

            if (!boundary.IsClosed)
                throw new Exception("Error. Please select closed curve instead");

            if ( Name == string.Empty)
                throw new Exception("Error. Please enter a study name");

            if (boundary is null || boundary.Degree > 1)
                throw new Exception("Select closed polylines only.");

            var study = CoreModel.Studies.Find(s => s.Name == Name);

            if (study != null && study.Id != rhinoObj.Id)
                throw new Exception($"Another study named \"{Name}\" already exists.");

            var bhomCrv = boundary.ToBHoM2DegreeCrv();

            RhinoDoc doc2 = RhinoDoc.ActiveDoc;
            ModelBuilderPipelines.AddStudyAreaPipeline(doc: doc2,
                studyData: new StudyData(bhomCrv!, rhinoObj.Id, Name).ObjectToList(),
                ModellingTool.Zones);

            if (CoreModel.Studies.Exists(s => s.Name == Name))
                vm.UpdateZonesInModelMessage(Name);

            return vm.ZoneMessageData;
        }
    }
}
