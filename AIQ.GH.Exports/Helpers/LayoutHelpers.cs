using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.GH.Exports.Extensions;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;


namespace AIQ.GH.Exports.Helpers;

public static class LayoutHelpers
{
    public static IEnumerable<Rhino.DocObjects.TextObject> GetTextObjectsInLayout(RhinoPageView layout, RhinoDoc doc)
    {
        var textObjects = new List<Rhino.DocObjects.TextObject>();

        var objects = layout.GetObjects(doc);
        foreach (var obj in objects)
        {
            if (obj is Rhino.DocObjects.TextObject textObj)
            {
                textObjects.Add(textObj);
            }
        }

        return textObjects;
    }

    public static bool SetTextObjectText(RhinoDoc doc, Rhino.DocObjects.TextObject textObject, string newText)
    {
        try
        {
            var textEntity = textObject.TextGeometry;
            if (textEntity is null)
                return false;

            textEntity.PlainText = newText;
            textObject.CommitChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static RhinoPageView? CreateLayoutFromTemplate(
        RhinoDoc doc,
        string templateLayoutName,
        string newLayoutName)
    {
        var template = GetLayoutByName(doc, templateLayoutName);

        if (template == null)
            return null;

        DeleteLayouts(newLayoutName, doc);
        var newLayout = CopyLayout(doc, template, newLayoutName);

        return newLayout;
    }

    private static void DeleteLayouts(string pageName, RhinoDoc doc)
    {
        doc.Views
            .Where(x => x is RhinoPageView page && page.PageName == pageName)
            .ToList()
            .ForEach(pv => pv.Close());
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

    public static RhinoPageView CopyLayout(
        RhinoDoc doc,
        RhinoPageView sourceView,
        string layoutName)
    {
        var newPage = doc.Views.AddPageView(layoutName, sourceView.PageWidth, sourceView.PageHeight);

        foreach(var sourceDetail in sourceView.GetDetailViews())
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
