using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Helpers;
using Rhino;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace AIQ.GH.Exports.Components.Output;

public class MergePdfsComponent : GH_Component
{
    private string _lastFolder;

    public MergePdfsComponent()
        : base("Merge PDFs", "MergePDF",
               "Combines multiple PDF files into a single PDF",
               "AIQ", "Export")
    {
        _lastFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }

    public override Guid ComponentGuid => ComponentGuids.MergePdfs;

    protected override Bitmap Icon => IconLoader.LoadIcon("Print PDF.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("PDF Paths", "P", "Paths to PDF files to merge", GH_ParamAccess.list);
        pManager.AddTextParameter("Output Path", "O", "Path for the merged PDF output", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Output Path", "P", "Path to the merged PDF file", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Success", "S", "True if merge was successful", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var pdfPaths = new System.Collections.Generic.List<string>();
        string? outputPath = null;

        if (!DA.GetDataList(0, pdfPaths)) return;
        DA.GetData(1, ref outputPath);

        if (pdfPaths.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No PDF paths provided");
            DA.SetData(1, false);
            return;
        }

        if(!Directory.CreateDirectory(outputPath?? "").Exists)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "PDF path cannot be empty");
            DA.SetData(1, false);
            return;
        }

        foreach (var path in pdfPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "PDF path cannot be empty");
                DA.SetData(1, false);
                return;
            }

            if (!File.Exists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"File not found: {path}");
                DA.SetData(1, false);
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine(_lastFolder, "merged.pdf");
        }
        else
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                _lastFolder = dir;
            }
        }

        try
        {
            var fileName = Path.GetFileNameWithoutExtension(outputPath);
            var directory = Path.GetDirectoryName(outputPath);

            if (fileName.Length == 0)
                fileName = RhinoFileName() + "-Export";

            fileName += ".pdf";
            var path = Path.Combine(directory?? "", fileName);

            PdfHelpers.MergePdfFiles(pdfPaths, path);
            DA.SetData(0, outputPath);
            DA.SetData(1, true);
        }
        catch (Exception ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            DA.SetData(1, false);
        }
    }

    private static string RhinoFileName()
    {
        var name = RhinoDoc.ActiveDoc.Name;
        return name.Replace(".3dm", "");
    }
}
