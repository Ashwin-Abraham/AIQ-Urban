using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.GH.Exports.Helpers;

public static class LayoutHelpers
{
    public static RhinoPageView CreateLayoutFromTemplate(
        RhinoDoc doc,
        string templateLayoutName,
        string newLayoutName)
    {
        var template = GetLayoutByName(doc, templateLayoutName);
        if (template == null)
        {
            return null!;
        }

        var newLayout = CreateEmptyLayout(
            doc,
            newLayoutName,
            template.PageWidth,
            template.PageHeight,
            doc.PageUnitSystem
        );

        if (newLayout != null)
        {
            CopyDetailsToLayout(template, newLayout);
        }

        return newLayout;
    }

    public static IEnumerable<string> GetAllLayoutNames(RhinoDoc doc)
    {
        return doc.Views
            .Where(v => v is RhinoPageView)
            .Cast<RhinoPageView>()
            .Select(pv => pv.PageName);
    }

    public static RhinoPageView? GetLayoutByName(RhinoDoc doc, string name)
    {
        return doc.Views
            .Where(v => v is RhinoPageView)
            .Cast<RhinoPageView>()
            .FirstOrDefault(pv => pv.PageName == name);
    }

    public static void CopyDetailsToLayout(
        RhinoPageView source,
        RhinoPageView destination)
    {
        var sourceDetails = source.GetDetailViews();
        foreach (var sourceDetail in sourceDetails)
        {
            var detailGeometry = sourceDetail.DetailGeometry;
            if (detailGeometry == null) continue;

            var topLeft = new Point2d(
                sourceDetail.Viewport.Bounds.Left,
                sourceDetail.Viewport.Bounds.Top
            );
            var bottomRight = new Point2d(
                sourceDetail.Viewport.Bounds.Right,
                sourceDetail.Viewport.Bounds.Bottom
            );

            var newDetail = destination.AddDetailView(
                sourceDetail.Name,
                topLeft,
                bottomRight,
                DefinedViewportProjection.Top
            );

            if (newDetail != null)
            {
                newDetail.Viewport.SetCameraTarget(
                    sourceDetail.Viewport.CameraTarget,
                    true
                );
                newDetail.Viewport.ZoomExtents();
                newDetail.CommitChanges();
            }
        }
    }

    public static RhinoPageView CreateEmptyLayout(
        RhinoDoc doc,
        string layoutName,
        double width,
        double height,
        UnitSystem units = UnitSystem.Millimeters)
    {
        doc.PageUnitSystem = units;
        var pageView = doc.Views.AddPageView(layoutName, width, height);
        return pageView;
    }
}
