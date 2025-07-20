using BH.Engine.Geometry;
using Rhino.Geometry;
using bhom = BH.oM.Geometry;

namespace ArchSitePlan.RhinoBhomInterop;
public static class ToBHoMExtensions
{
    public static bhom.TransformMatrix ToBHoM(this Transform rhinoTransform)
    {
        var transform = new bhom.TransformMatrix();
        transform.Matrix = new double[4, 4];

        transform.Matrix[0, 0] = rhinoTransform.M00;
        transform.Matrix[0, 1] = rhinoTransform.M01;
        transform.Matrix[0, 2] = rhinoTransform.M02;
        transform.Matrix[0, 3] = rhinoTransform.M03;
        transform.Matrix[1, 0] = rhinoTransform.M10;
        transform.Matrix[1, 1] = rhinoTransform.M11;
        transform.Matrix[1, 2] = rhinoTransform.M12;
        transform.Matrix[1, 3] = rhinoTransform.M13;
        transform.Matrix[2, 0] = rhinoTransform.M20;
        transform.Matrix[2, 1] = rhinoTransform.M21;
        transform.Matrix[2, 2] = rhinoTransform.M22;
        transform.Matrix[2, 3] = rhinoTransform.M23;
        transform.Matrix[3, 0] = rhinoTransform.M30;
        transform.Matrix[3, 1] = rhinoTransform.M31;
        transform.Matrix[3, 2] = rhinoTransform.M32;
        transform.Matrix[3, 3] = rhinoTransform.M33;
        return transform;
    }

    public static bhom.ICurve? ToBHoMXYOutline(this Brep rhinoSrf)
    {
        var boundaryCurves = rhinoSrf.Loops
            .Select(x => x.To3dCurve())
            .ToList();

        var boundary = Curve.JoinCurves(boundaryCurves, 1e-06).MaxBy(x => x.GetLength());
        if (boundary == null) return null;

        var isPlanar = boundary.TryGetPlane(out Plane plane);
        if (!isPlanar) return null;

        var isVertical = plane.Normal.IsPerpendicularTo(Vector3d.ZAxis);
        if (isVertical) return null;

        var crv = boundary.ToBHoM();
        return crv;
    }
    public static bhom.ICurve? ToBHoM2DegreeCrv(this Curve rhinoCrv)
    {
        if (rhinoCrv.TryGetPolyline(out Polyline pline))
        {
            if (pline.IsClosed)
                return pline.ToBHoMPolygon();
            else
                return pline.ToBHoMPolyline();
        }
        else if (rhinoCrv.TryGetArc(out Arc arc))
            return arc.ToBHoM();
        else if (rhinoCrv is PolyCurve polyCurve)
            return polyCurve.ToBHoM();
        else
            return null;
    }
    public static bhom.Arc ToBHoM(this Arc rhinoArc)
    {
           var bhomArc = Create.Arc(start: rhinoArc.StartPoint.ToBHoM(),
            middle: rhinoArc.MidPoint.ToBHoM(),
            end: rhinoArc.EndPoint.ToBHoM());

        return bhomArc;
    }

    public static bhom.Line ToBHoM(this Line rhinoLine)
    {
        return Create.Line(rhinoLine.From.ToBHoM(), rhinoLine.To.ToBHoM());
    }

    public static bhom.Point ToBHoM(this Point3d rhinoPoint)
    {
        return Create.Point(rhinoPoint.X, rhinoPoint.Y, rhinoPoint.Z);  
    }

    public static IEnumerable<bhom.Point> ToBHoM(this IEnumerable<Point3d> rhinoPoints)
    {
        return rhinoPoints.Select(pt => pt.ToBHoM());
    }

    public static bhom.Face ToBHoM(this MeshFace rhinoFace)
    {
        if(rhinoFace.C == rhinoFace.D)
            return Create.Face(rhinoFace.A, rhinoFace.B, rhinoFace.C);
        else
            return Create.Face(rhinoFace.A, rhinoFace.B, rhinoFace.C, rhinoFace.D);
    }

    public static bhom.Polyline ToBHoMPolyline(this Polyline rhinoPline)
    {
        List<Point3d> pointList = rhinoPline.ToList();
        List<bhom.Point> bhomPts = pointList.ToBHoM().ToList();
        return Create.Polyline(bhomPts);
    }
    public static bhom.Polygon ToBHoMPolygon(this Polyline rhinoPline)
    {
        List<Point3d> pointList = rhinoPline.ToList();
        List<bhom.Point> bhomPts = pointList.ToBHoM().ToList();
        return Create.Polygon(bhomPts);
    }

    public static bhom.Mesh ToBHoM(this Mesh rhinoMesh)
    {
        List<bhom.Point> bhomPts = rhinoMesh.Vertices.ToPoint3dArray().ToBHoM().ToList();
        List<bhom.Face> bhomFaces = rhinoMesh.Faces.Select(f => f.ToBHoM()).ToList();
        return Create.Mesh(bhomPts, bhomFaces);
    }

    public static bhom.PolyCurve ToBHoM(this Rhino.Geometry.PolyCurve polyCurve)
    {
        var segments = polyCurve.DuplicateSegments();
        List<bhom.ICurve> bhomSegments = new();

        foreach (var crv in segments)
        {
            if (crv != null)
                bhomSegments.Add(crv.ToBHoM());
        }
        return Create.PolyCurve(bhomSegments);
    }
    public static bhom.ICurve? ToBHoM(this Rhino.Geometry.Curve rhinoCrv)
    {
        if (rhinoCrv.TryGetPolyline(out Polyline pline))
            return rhinoCrv.IsClosed? pline.ToBHoMPolygon(): pline.ToBHoMPolyline();
        else if (rhinoCrv is PolyCurve pCrv)
            return pCrv.ToBHoM();
        else if (rhinoCrv.TryGetArc(out Arc arc))
            return arc.ToBHoM();
        else
            return null;
    }
}
