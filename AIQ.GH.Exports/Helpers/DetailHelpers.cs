using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.GH.Exports.Helpers;

public static class DetailHelpers
{
    public static DetailViewObject? GetDetailByName(
        RhinoPageView layout,
        string detailName)
    {
        return GetAllDetails(layout)
            .FirstOrDefault(d => d.Name == detailName);
    }

    public static List<DetailViewObject> GetAllDetails(RhinoPageView layout)
    {
        return layout.GetDetailViews().ToList();
    }

    public static void SetDetailTarget(
        DetailViewObject detail,
        Point3d worldTarget,
        double modelScaleRatio,
        RhinoDoc doc)
    {
        detail.Viewport.SetCameraTarget(worldTarget, true);
        SetDetailScale(detail, modelScaleRatio, doc);
        detail.CommitChanges();
    }

    public static void SetDetailScale(
        DetailViewObject detail,
        double modelUnitsPerPageUnit,
        RhinoDoc doc)
    {
        var detailGeometry = detail.DetailGeometry;
        if (detailGeometry == null) return;

        detailGeometry.SetScale(1, doc.ModelUnitSystem, modelUnitsPerPageUnit, doc.PageUnitSystem);
        detail.CommitChanges();
    }
}
