using ArchSitePlan.Core.Models;
using ArchSitePlan.RhinoInterface.Actions;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Eto.Forms;
using Rhino;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI
{
    public class ExportExcel : IMessageFromUI<EmptyMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.ExportExcel;

        public EmptyMessage HandleMessage()
        {
            RhinoEventHandler.HTMLViewModel.AppMode = ModellingTool.Export;
            
            var directory = RhinoEventHandler.HTMLViewModel.ExcelDirectory;

            if(!Directory.Exists(directory))
                throw new Exception($"Directory {directory} does not exist. Please select a valid folder.");
           
            string rhinoFileName = RhinoDoc.ActiveDoc.Name;
            var path = Path.Combine(directory!, $"{rhinoFileName}_Excel Export.xlsx");


            var pr = new ProjectReport(path, CoreModel.ProjectWideStudy().Metrics, CoreModel.Studies);
            pr.WriteExcelFile();

            RhinoApp.InvokeOnUiThread(() =>
            {
                MessageBox.Show($"Exported to: {path}");
            });
            
            return new();
        }
    }
}
