using ArchSitePlan.RhinoInterface.Constants;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.RhinoInterface.Helpers;
public static class ViewHelpers
{
    public static int CreatePlanNamedView(RhinoDoc doc, BoundingBox objectBounds, string zoneName)
    {
        var originalView = doc.Views.ActiveView;

        // Set up new view
        var viewInfo = new ViewInfo();
        viewInfo.Viewport.SetCameraDirection(-Vector3d.ZAxis);
        viewInfo.Viewport.ChangeToParallelProjection(true);
        viewInfo.Viewport.SetCameraLocation(new Point3d(objectBounds.Center.X, objectBounds.Center.Y, objectBounds.Max.Z));

        // Create new view
        string viewName = RHGeneralConstants.NamedViewPrefix + zoneName;

        var newView = doc.Views.Add(
            viewName,
            Rhino.Display.DefinedViewportProjection.Top,
            new System.Drawing.Rectangle(0, 0, 1200, 850),
            false);

        newView.ActiveViewport.ZoomBoundingBox(objectBounds);

        // Update Named Views
        doc.NamedViews.Delete(viewName);
        int result = doc.NamedViews.Add(viewName, newView.ActiveViewportID);

        // Cleanup doc
        doc.Views.ActiveView = originalView;
        newView.Close();
        doc.Views.Redraw();

        return result;
    }
}
