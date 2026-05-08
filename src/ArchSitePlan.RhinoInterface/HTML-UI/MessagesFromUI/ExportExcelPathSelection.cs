using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Eto.Forms;
using Rhino;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI
{
    public class ExportExcelPathSelection : IMessageFromUI<EmptyMessage>
    {
        public TypeOfMessage MessageType { get; init; } = TypeOfMessage.ExportExcelPathSelection;

        public EmptyMessage HandleMessage()
        {
            RhinoApp.InvokeOnUiThread(() =>
            {
                RhinoEventHandler.HTMLViewModel.AppMode = ModellingTool.Export;

                var dialog = new SelectFolderDialog
                {
                    Title = "Export File",
                };

                var result = dialog.ShowDialog(Rhino.UI.RhinoEtoApp.MainWindow);

                if (result == DialogResult.Ok || result == DialogResult.Yes)
                    RhinoEventHandler.HTMLViewModel.ExcelDirectory = dialog.Directory;
            });
            
            return new();
        }
    }
}
