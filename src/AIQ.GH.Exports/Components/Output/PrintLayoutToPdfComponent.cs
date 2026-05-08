using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.GH.Exports.Extensions;
using AIQ.GH.Exports.Models;
using AIQ.GH.Exports.Types;
using AIQ.GH.Exports.Utils;
using Grasshopper.Kernel;
using Rhino;

namespace AIQ.GH.Exports.Components.Output;

public class PrintLayoutToPdfComponent : GH_Component
{
    public PrintLayoutToPdfComponent()
        : base("Print Layout to PDF", "PrintPDF",
               "Exports a layout to a PDF file",
               "AIQ", "Export")
    {  }

    public override Guid ComponentGuid => ComponentGuids.PrintLayoutToPdf;

    protected override Bitmap Icon => IconLoader.LoadIcon("Print PDF.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Layout Names", "L", "Name of the layout to export. If multiple are provided, then it will export all of them into a single file.", GH_ParamAccess.list);
        pManager.AddTextParameter("Output Folder", "F", "Folder to save the PDF (leave empty for desktop)", GH_ParamAccess.item);
        pManager.AddTextParameter("File Name", "N", "The name of the pdf file. The folder location and file extension will be added automatically", GH_ParamAccess.item, "AIQ Export");

        pManager.AddGenericParameter("Doc Layer States", "D", "Layer state to apply for each layout", GH_ParamAccess.list);
        pManager[3].Optional = true;

        pManager.AddBooleanParameter("Run", "R", "Print the layouts", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("PDF Path", "P", "Path to the exported PDF file", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Success", "S", "True if export was successful", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<string> layoutNames = [];
        string outputFolder = "";
        string fileName = "";
        bool run = false;
        List<DocLayerStateGoo> docLayerStateGoos = [];
        var logger = new Logger();

        if (!DA.GetDataList(0, layoutNames)) return;
        DA.GetData(1, ref outputFolder);
        DA.GetData(2, ref fileName);
        DA.GetDataList(3, docLayerStateGoos);
        DA.GetData(4, ref run);

        if (!run)
            return;

        var emitLogToComponent = (Logger log, bool success) =>
        {
            logger.LogMessages(this);
            DA.SetData(1, success);
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

        DA.SetData(0, filePath);
        emitLogToComponent(logger, success);
        return;
    }
}
