using System.Collections.Generic;
using System.Linq;
using AIQ.GH.Exports.Enums;
using AIQ.GH.Exports.Utils;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;

namespace AIQ.GH.Exports.Extensions;
public static class RhinoDocExtensions
{
    public static IEnumerable<DetailViewObject> GetDetailViews(
        this RhinoDoc doc,
        string? detailName = null,
        string? layoutName = null,
        TextMatchStrategy textMatchStrategy = TextMatchStrategy.ExactMatch)
    {
        var layout = FindLayouts(doc, layoutName, textMatchStrategy);

        var details = layout?.FindDetails(detailName, textMatchStrategy)?? [];

        return details;
    }

    public static RhinoPageView? FindLayouts(this RhinoDoc doc, string? layoutName, TextMatchStrategy strategy = TextMatchStrategy.ExactMatch)
    {
        return doc.Views
            .Where(v => v is RhinoPageView)
            .Cast<RhinoPageView>()
            .FirstOrDefault(l => TextMatchHelpers.Matches(l.PageName, layoutName, strategy));
    }

    public static IEnumerable<TextObject> GetTextObjectsInLayout(this RhinoDoc doc, RhinoPageView layout)
    {
        var objects = layout.GetObjects(doc);
        foreach (var obj in objects)
        {
            if (obj is TextObject textObj)
                yield return textObj;
        }
    }
}
