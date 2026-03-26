using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;

namespace AIQ.GH.Exports.Extensions;
public static class ViewExtensions
{
    public static void CopyObjects(this RhinoPageView pageView, RhinoDoc doc, IEnumerable<RhinoObject> sourceObjects)
    {
        sourceObjects.ToList().ForEach(obj => CopyObjects(pageView, doc, obj));
    }

    public static void CopyObjects(this RhinoPageView pageView, RhinoDoc doc, RhinoObject sourceObject)
    {
        var geometryCopy = sourceObject.Geometry?.Duplicate();
        
        if (geometryCopy == null)
            return;

        // Detail type not supported by Rhino Common
        if (sourceObject.ObjectType == ObjectType.Detail)
            return;

        var attributesCopy = sourceObject.Attributes.Duplicate();

        attributesCopy.Space = ActiveSpace.PageSpace;
        attributesCopy.ViewportId = pageView.MainViewport.Id;

        doc.Objects.Add(geometryCopy, attributesCopy);
    }

    public static IEnumerable<RhinoObject> GetObjects(this RhinoPageView pageView, RhinoDoc doc)
    {
        return doc.Objects.Where(x => x.Attributes.Space == ActiveSpace.PageSpace && x.Attributes.ViewportId == pageView.MainViewport.Id);
    }
}
