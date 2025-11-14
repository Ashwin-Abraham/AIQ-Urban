using System.Drawing;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;

namespace ArchSitePlan.RhinoInterface.Exports;
public static class RhinoImageExporter
{
    /// <summary>
    /// Creates/updates a named Top/plan view for the given objects and exports a JPG of just those objects.
    /// </summary>
    /// <param name="doc">Rhino document</param>
    /// <param name="outputPath">Destination JPG path</param>
    internal static string? ExportPlanImageForObjects(
        RhinoDoc doc,
        string namedView,
        string outputPath,
        List<Layer> layersToShow)
    {
        // Set layer visibility
        foreach(var layer in doc.Layers)
        {
            layer.IsVisible = layersToShow.Contains(layer);
        }

        string originalNamedView = "AIQ_OriginalView";
        doc.NamedViews.Add(originalNamedView, doc.Views.ActiveView.ActiveViewportID);

        var viewToExport = doc.NamedViews.FirstOrDefault(x => x.Name.Contains(namedView));
        var viewToExportIndex = doc.NamedViews.FindByName(namedView);
        if (viewToExport == null)
            return null;

        //var success = doc.Views.ActiveView.ActiveViewport.PushViewInfo(viewToExport, false);
        //if (!success)
        //    return null;

        // Set required display mode
        RhinoApp.RunScript($"_-SetDisplayMode Viewport=All Mode=Shaded ", false);

        // Capture to file

        // Set name based on named view 
        // " " encloses file name to prevent rhino interpretting spaces in file names as enter
        //RhinoApp.RunScript($"_-ViewCaptureToFile Unit millimeters LockAspectRatio=No Width 297 Height 210 Dpi 300 Autoname \"{viewPath}\" _Enter _Enter", false);
        //RhinoApp.RunScript("_Enter", true);

        ///// Capture View 2
        doc.NamedViews.Restore(viewToExportIndex, doc.Views.ActiveView.ActiveViewport);
        string viewName = doc.Views.ActiveView.MainViewport.Name;
        viewName = viewName.Replace(' ', '_');
        string viewPath = outputPath + viewName + ".png";

        RhinoViewCapture(doc.Views.ActiveView, viewPath);

        // Reset original view
        int lastViewIndex = doc.NamedViews.Count - 1;
        doc.NamedViews.Restore(lastViewIndex, doc.Views.ActiveView.ActiveViewport);
        doc.NamedViews.Delete(originalNamedView);
        
        return viewPath;
    }

    private static void RhinoViewCapture(RhinoView view, string savePath)
    {
        string displayModeName = "AIQ_PlanExport";
        var displayId = DisplayModeDescription
            .GetDisplayModes()
            .FirstOrDefault(x => x.EnglishName == displayModeName || x.LocalName == displayModeName)?
            .Id;
        
        if(displayId == null)
            displayId = DisplayModeDescription.AddDisplayMode(displayModeName);

        var displayMode = DisplayModeDescription.GetDisplayMode(displayId.Value);
        
        displayMode.DisplayAttributes.CurveThickness = 110;
        displayMode.SupportsShading = true;
        displayMode.DisplayAttributes.LockedColor = Color.IndianRed;

        var bmp2 = view.CaptureToBitmap(displayMode);
        bmp2.Save(savePath);
    }

    // --- helpers ---

    //private static BoundingBox GetBoundingBoxWorld(List<RhinoObject> objs)
    //{
    //    var bb = BoundingBox.Empty;
    //    foreach (var obj in objs)
    //    {
    //        var g = obj.Geometry;
    //        if (g == null) continue;

    //        var bbox = g.GetBoundingBox(false);
    //        bb.Union(bbox);
    //    }

    //    return bb;
    //}

    //private static ViewportInfo ModifyViewportInfo(BoundingBox bboxWorld, ViewportInfo vpInfo)
    //{
    //    var target = new Point3d(bboxWorld.Center.X, bboxWorld.Center.Y, bboxWorld.Min.Z);
    //    var dir = -Vector3d.ZAxis;

    //    vpInfo.ChangeToParallelProjection(true);

    //    vpInfo.SetCameraDirection(dir);

    //    var camPoint = new Point3d(target.X, target.Y, bboxWorld.Max.Z);
    //    vpInfo.SetCameraLocation(camPoint);

    //    return vpInfo;
    //}
}