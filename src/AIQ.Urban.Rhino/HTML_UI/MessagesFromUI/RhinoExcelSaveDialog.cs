using Eto.Forms;
using Rhino.UI;
using SaveFileDialog = Eto.Forms.SaveFileDialog;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

internal sealed class RhinoExcelSaveDialog : IExcelSaveDialog
{
    public string? Show(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Urban Metrics",
            FileName = suggestedFileName
        };
        dialog.Filters.Add(new FileFilter("Excel Workbook", ".xlsx"));

        return dialog.ShowDialog(RhinoEtoApp.MainWindow) == DialogResult.Ok
            ? dialog.FileName
            : null;
    }
}
