using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.GH.Exports.Enums;
using AIQ.GH.Exports.Extensions;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;


namespace AIQ.GH.Exports.Helpers;

public static class LayoutHelpers
{


    //public static RhinoPageView? CreateLayoutFromTemplate(
    //    RhinoDoc doc,
    //    string templateLayoutName,
    //    string newLayoutName)
    //{
    //    var template = doc.FindLayouts(templateLayoutName);

    //    if (template == null)
    //        return null;

    //    doc.FindLayouts(newLayoutName)?.Delete();

    //    var newLayout = template?.CopyLayout(doc, newLayoutName);

    //    return newLayout;
    //}

    //public static void CopyDetailsToLayout(
    //    RhinoPageView source,
    //    RhinoPageView destination)
    //{
    //    var sourceDetails = source.GetDetailViews();
    //    foreach (var sourceDetail in sourceDetails)
    //    {
    //        var detailGeometry = sourceDetail.DetailGeometry;
    //        if (detailGeometry == null) continue;

    //        var topLeft = new Point2d(
    //            sourceDetail.Viewport.Bounds.Left,
    //            sourceDetail.Viewport.Bounds.Top
    //        );
    //        var bottomRight = new Point2d(
    //            sourceDetail.Viewport.Bounds.Right,
    //            sourceDetail.Viewport.Bounds.Bottom
    //        );

    //        var newDetail = destination.AddDetailView(
    //            sourceDetail.Name,
    //            topLeft,
    //            bottomRight,
    //            DefinedViewportProjection.Top
    //        );

    //        if (newDetail != null)
    //        {
    //            newDetail.Viewport.SetCameraTarget(
    //                sourceDetail.Viewport.CameraTarget,
    //                true
    //            );
    //            newDetail.Viewport.ZoomExtents();
    //            newDetail.CommitChanges();
    //        }
    //    }
    //}

    //public static RhinoPageView CreateEmptyLayout(
    //    RhinoDoc doc,
    //    string layoutName,
    //    double width,
    //    double height,
    //    UnitSystem units = UnitSystem.Millimeters)
    //{
    //    doc.PageUnitSystem = units;
    //    var pageView = doc.Views.AddPageView(layoutName, width, height);
        
    //    return pageView;
    //}
}
