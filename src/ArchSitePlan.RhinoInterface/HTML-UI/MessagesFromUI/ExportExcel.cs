using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Eto.Forms;
using Rhino;
using static ArchSitePlan.RhinoInterface.Constants.Enums;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;

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
        ModelBuilderPipelines.ConstructReportPipeline(path);

        RhinoApp.InvokeOnUiThread(() =>
        {
            MessageBox.Show($"Exported to: {path}");
        });
        
        return new();
    }
}