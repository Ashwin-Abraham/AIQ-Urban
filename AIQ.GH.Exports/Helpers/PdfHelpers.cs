using Rhino;
using Rhino.Display;
using Rhino.FileIO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AIQ.GH.Exports.Models;
using ArchSitePlan.RhinoInterface.Helpers;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;

namespace AIQ.GH.Exports.Helpers;

public static class PdfHelpers
{
    public static string ExportLayoutToPdf(
        RhinoPageView layout,
        string outputFolder,
        RhinoDoc doc)
    {
        var pdfPath = Path.Combine(outputFolder, $"{layout.PageName}.pdf");

        var viewCaptureSettings = new ViewCaptureSettings(layout, 300);
        var pdf = FilePdf.Create();
        pdf.AddPage(viewCaptureSettings);
        pdf.Write(pdfPath);

        return pdfPath;
    }

    public static void MergePdfFiles(
        IEnumerable<string> inputPaths,
        string outputPath)
    {
        var paths = inputPaths.ToList();
        if (paths.Count < 2)
        {
            throw new System.ArgumentException("Need at least 2 PDF files to merge");
        }

        using var writer = new PdfWriter(outputPath);
        using var mergedPdf = new PdfDocument(writer);
        var merger = new PdfMerger(mergedPdf);

        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"PDF file not found: {path}");
            }

            using var pdf = new PdfDocument(new PdfReader(path));
            merger.Merge(pdf, 1, pdf.GetNumberOfPages());
        }
    }
}
