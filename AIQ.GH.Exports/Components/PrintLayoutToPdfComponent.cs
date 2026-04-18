using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.GH.Exports.Extensions;
using AIQ.GH.Exports.Helpers;
using AIQ.GH.Exports.Models;
using AIQ.GH.Exports.Types;
using Grasshopper.Kernel;
using Rhino;

namespace AIQ.GH.Exports.Components;

public class PrintLayoutToPdfComponent : GH_Component
{
    public PrintLayoutToPdfComponent()
        : base("Print Layout to PDF", "PrintPDF",
               "Exports a layout to a PDF file",
               "AIQ", "Export")
    {  }

    public override Guid ComponentGuid => ComponentGuids.PrintLayoutToPdf;

    protected override Bitmap Icon => IconLoader.LoadIcon("Print PDF.png");

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
        manager.AddTextParameter("Layout Names", "L", "Name of the layout to export. If multiple are provided, then it will export all of them into a single file.", GH_ParamAccess.list);
        manager.AddTextParameter("Output Folder", "F", "Folder to save the PDF (leave empty for desktop)", GH_ParamAccess.item);
        manager.AddTextParameter("File Name", "N", "The name of the pdf file. The folder location and file extension will be added automatically", GH_ParamAccess.item, "AIQ Export");

        manager.AddGenericParameter("Doc Layer States", "D", "Layer state to apply for each layout", GH_ParamAccess.list);
        manager[3].Optional = true;

        manager.AddBooleanParameter("Run", "R", "Print the layouts", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
        manager.AddTextParameter("PDF Path", "P", "Path to the exported PDF file", GH_ParamAccess.item);
        manager.AddBooleanParameter("Success", "S", "True if export was successful", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        List<string> layoutNames = [];
        string outputFolder = "";
        string fileName = "";
        bool run = false;
        List<DocLayerStateGoo> docLayerStateGoos = [];
        var logger = new Logger();

        if (!da.GetDataList(0, layoutNames)) return;
        da.GetData(1, ref outputFolder);
        da.GetData(2, ref fileName);
        da.GetDataList(3, docLayerStateGoos);
        da.GetData(4, ref run);

        if (!run)
            return;

        var emitLogToComponent = (Logger log, bool success) =>
        {
            logger.LogMessages(this);
            da.SetData(1, success);
        };

        if (!FileHelpers.TryGetFilePath(outputFolder, fileName, ".pdf", ref logger, out string filePath))
        {
            emitLogToComponent(logger, false);
            return;
        }

        var doc = RhinoDoc.ActiveDoc;
        if (!logger.ValidateRhinoDoc(doc, this))
        {
            emitLogToComponent(logger, false);
            return;
        }

        var docLayerStates = docLayerStateGoos.Select(x => x.Value).ToList();
        bool success = false;

        try
        {
            success = PdfHelpers.ExportMultipleLayoutsToPdf(doc, filePath, layoutNames, docLayerStates, ref logger);
        }
        catch (Exception ex)
        {
            logger.AddError($"Failed to export PDF: {ex.Message}");
            emitLogToComponent(logger, false);
            return;
        }

        da.SetData(0, filePath);
        emitLogToComponent(logger, success);
        return;
    }
}
