using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Components;
using AIQ.GH.Exports.Helpers;
using AIQ.GH.Exports.Models;
using AIQ.GH.Exports.Types;
using Rhino;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIQ.GH.Exports.Components;

public class PrintLayoutToPdfComponent : GH_Component
{
    private string _lastFolder;

    public PrintLayoutToPdfComponent()
        : base("Print Layout to PDF", "PrintPDF",
               "Exports a layout to a PDF file",
               "AIQ", "Export")
    {
        _lastFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    public override Guid ComponentGuid => ComponentGuids.PrintLayoutToPdf;

    protected override System.Drawing.Bitmap Icon => null;

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
        manager.AddTextParameter("Layout Name", "L", "Name of the layout to export", GH_ParamAccess.item);
        manager.AddTextParameter("Output Folder", "F", "Folder to save the PDF (leave empty for desktop)", GH_ParamAccess.item);
        manager.AddBooleanParameter("Open After", "O", "Open PDF after export", GH_ParamAccess.item, false);
        manager.AddGenericParameter("Doc Layer State", "D", "Layer state to apply", GH_ParamAccess.item);
        manager[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
        manager.AddTextParameter("PDF Path", "P", "Path to the exported PDF file", GH_ParamAccess.item);
        manager.AddBooleanParameter("Success", "S", "True if export was successful", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string layoutName = null;
        string outputFolder = null;
        bool openAfter = false;
        DocLayerStateGoo? docLayerStateGoo = null;

        if (!da.GetData(0, ref layoutName)) return;
        da.GetData(1, ref outputFolder);
        da.GetData(2, ref openAfter);
        da.GetData(3, ref docLayerStateGoo);

        if (string.IsNullOrWhiteSpace(layoutName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Layout name cannot be empty");
            da.SetData(1, false);
            return;
        }

        var folder = string.IsNullOrWhiteSpace(outputFolder) ? _lastFolder : outputFolder;
        if (!System.IO.Directory.Exists(folder))
        {
            try
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            catch
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Cannot access folder: {folder}");
                da.SetData(1, false);
                return;
            }
        }
        _lastFolder = folder;

        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            da.SetData(1, false);
            return;
        }

        var layout = LayoutHelpers.GetLayoutByName(doc, layoutName);
        if (layout is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Layout '{layoutName}' not found");
            da.SetData(1, false);
            return;
        }

        docLayerStateGoo?.Value?.ApplyTo(doc);

        string pdfPath = PdfHelpers.ExportLayoutToPdf(layout, folder, doc);

        if (pdfPath != null && openAfter)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        da.SetData(0, pdfPath ?? string.Empty);
        da.SetData(1, pdfPath != null);
    }
}
