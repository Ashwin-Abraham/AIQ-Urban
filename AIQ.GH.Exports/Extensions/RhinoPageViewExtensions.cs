using System.Collections.Generic;
using System.Linq;
using AIQ.GH.Exports.Enums;
using AIQ.GH.Exports.Helpers;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;


namespace AIQ.GH.Exports.Extensions;
public static class RhinoPageViewExtensions
{
    public static bool Delete(this RhinoPageView layout) => layout.Close();

    public static IEnumerable<DetailViewObject> FindDetails(
        this RhinoPageView layout,
        string? detailName,
        TextMatchStrategy strategy = TextMatchStrategy.ExactMatch)
    {
        var allDetails = layout.GetDetailViews().ToList();

        if (string.IsNullOrEmpty(detailName))
            return allDetails;

        return allDetails.Where(d => TextMatchHelpers.Matches(d.Name, detailName, strategy)).ToList();
    }

    public static RhinoPageView CopyLayout(
        this RhinoPageView sourceView,
        RhinoDoc doc,
        string layoutName)
    {
        var newPage = doc.Views.AddPageView(layoutName, sourceView.PageWidth, sourceView.PageHeight);

        foreach (var sourceDetail in sourceView.GetDetailViews())
        {
            var bounds = sourceDetail.Geometry.GetBoundingBox(true);

            var detail = newPage.AddDetailView(
                title: sourceDetail.DescriptiveTitle,
                corner0: new Point2d(bounds.Min),
                corner1: new Point2d(bounds.Max),
                initialProjection: DefinedViewportProjection.Top
                );

            detail.Name = sourceDetail.Name;
            detail.DetailGeometry.SetScale(1, doc.ModelUnitSystem, sourceDetail.DetailGeometry.PageToModelRatio, doc.PageUnitSystem);
            detail.Viewport.DisplayMode = sourceDetail.Viewport.DisplayMode;
            detail.Viewport.SetCameraLocations(sourceDetail.Viewport.CameraTarget, sourceDetail.Viewport.CameraLocation);
            detail.CommitChanges();
            detail.CommitViewportChanges();
        }

        var objectsInSource = sourceView.GetObjects(doc).ToList();
        newPage.CopyObjects(doc, objectsInSource);

        return newPage;
    }
}
