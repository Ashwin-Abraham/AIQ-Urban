using AIQ.GH.Exports.Enums;
using Rhino;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;

namespace AIQ.GH.Exports.Extensions;
public static class DetailExtensions
{
    public static bool ApplyLayerStateToDetail(this DetailViewObject detail,string layerStateName, RhinoDoc doc)
    {
        bool success = 
            doc.NamedLayerStates.Restore(layerStateName,RestoreLayerProperties.ViewportVisible, detail.Viewport.Id)
            && doc.NamedLayerStates.Restore(layerStateName, RestoreLayerProperties.ViewportColor, detail.Viewport.Id)
            ;
        
        if (success)
        {
            detail.CommitChanges();
        }

        return success;
    }

    public static void SetDetailTarget(
        this DetailViewObject detail,
        Point3d worldTarget,
        double modelScaleRatio,
        RhinoDoc doc)
    {
        SetDetailScale(detail, modelScaleRatio, doc);
        detail.Viewport.SetCameraTarget(worldTarget, true);
        detail.CommitChanges();
    }
    public static void SetDetailTarget(
        this DetailViewObject detail,
        Point3d worldTarget,
        double modelScaleRatio,
        AnchorPosition anchorPosition,
        RhinoDoc doc)
    {
        // Aligns the detail center to the target point
        SetDetailTarget(detail, worldTarget, modelScaleRatio, doc);


        var viewInfo = new ViewportInfo(detail.Viewport);
        viewInfo.GetFrustum(out double left, out double right, out double bottom, out double top, out _, out _);

        var worldWidth = right - left;
        var worldHeight = top - bottom;

        double offsetX, offsetY;
        switch (anchorPosition)
        {
            case AnchorPosition.TopLeft:
                offsetX = worldWidth / 2;
                offsetY = -worldHeight / 2;
                break;

            case AnchorPosition.TopRight:
                offsetX = -worldWidth / 2;
                offsetY = -worldHeight / 2;
                break;

            case AnchorPosition.BottomLeft:
                offsetX = worldWidth / 2;
                offsetY = worldHeight / 2;
                break;

            case AnchorPosition.BottomRight:
                offsetX = -worldWidth / 2;
                offsetY = worldHeight / 2;
                break;

            default:
                offsetX = 0; offsetY = 0; break;
        }

        var newCam = new Point3d(
            detail.Viewport.CameraTarget.X + offsetX,
            detail.Viewport.CameraTarget.Y + offsetY,
            detail.Viewport.CameraTarget.Z);

        detail.Viewport.SetCameraTarget(newCam, true);
        detail.CommitViewportChanges();
        detail.CommitChanges();
    }

    public static void SetDetailScale(
        this DetailViewObject detail,
        double modelUnitsPerPageUnit,
        RhinoDoc doc)
    {

        var detailGeometry = detail.DetailGeometry;
        if (detailGeometry == null) return;

        detailGeometry.SetScale(modelUnitsPerPageUnit, doc.ModelUnitSystem, 1, doc.PageUnitSystem);
        detail.CommitChanges();
    }
}
