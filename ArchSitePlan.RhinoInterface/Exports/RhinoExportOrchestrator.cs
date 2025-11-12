
using ArchSitePlan.RhinoInterface.Constants;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.RhinoInterface.Exports;
public static class RhinoExportOrchestrator
{
    /// <summary>
    /// Creates/updates a named Top/plan view for the given objects and exports a JPG of just those objects.
    /// </summary>
    /// <param name="doc">Rhino document</param>
    /// <param name="outputPath">Destination JPG path</param>
    internal static bool ExportPlanImageForObjects(
        RhinoDoc doc,
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

        for (int i = 0; i < doc.NamedViews.Count - 1; i++)
        {
            // Only export AIQ views
            if (!doc.NamedViews[i].Name.Contains(RHGeneralConstants.NamedViewPrefix))
                continue;
            
            var success = doc.Views.ActiveView.ActiveViewport.PushViewInfo(doc.NamedViews[i], false);
            RhinoApp.RunScript($"_-SetDisplayMode Viewport=All Mode=Shaded ", false);

            string viewName = doc.Views.ActiveView.MainViewport.Name;
            viewName = viewName.Replace(' ', '_');

            // Capture to file
            if (success)
            {
                string viewPath = outputPath + viewName;

                // Set name based on named view 
                // " " encloses file name to prevent rhino interpretting spaces in file names as enter
                 RhinoApp.RunScript($"_-ViewCaptureToFile Unit millimeters LockAspectRatio=No Width 297 Height 210 Dpi 300 Autoname \"{viewPath}\" _Enter _Enter", false);
            }
        }

        RhinoApp.RunScript("_Enter", true);
        int lastViewIndex = doc.NamedViews.Count - 1;
        doc.NamedViews.Restore(lastViewIndex, doc.Views.ActiveView.ActiveViewport);
        doc.Views.Redraw();

        doc.NamedViews.Delete(originalNamedView);
        doc.Views.Redraw();
        
        return true;
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