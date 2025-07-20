using ArchSitePlan.BHoM.Geometry.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Rhino;

namespace ArchSitePlan.RhinoBhomInterop;

public static class ToRhinoInterop
{
    public static Rhino.Geometry.Brep ToRhino(this PlanarSurface bhomSurface)
    {
        List<ICurve> crvList = [bhomSurface.ExternalBoundary];
        crvList.AddRange(bhomSurface.InternalBoundaries);

        var brep = Rhino.Geometry.Brep.CreatePlanarBreps(crvList.ToRhino(), 1E-06).First();
        return brep;
    }

    public static Rhino.Geometry.Point3d ToRhino(this Point point)
    {
        return new Rhino.Geometry.Point3d(point.X, point.Y, point.Z);
    }

    public static IEnumerable<Rhino.Geometry.Point3d> ToRhino(this IEnumerable<Point> points)
    {
        return points.Select(pt => pt.ToRhino());
    }

    public static Rhino.Geometry.Arc ToRhinoArc(this Arc arc)
    {
        var rhinoArc = new Rhino.Geometry.Arc(arc.StartPoint().ToRhino(), 
            arc.Mid().ToRhino(), 
            arc.EndPoint().ToRhino());

        return rhinoArc;
    }
    public static Rhino.Geometry.PolyCurve BoundarycurveToRhino(this BoundaryCurve curve)
    {
        Rhino.Geometry.PolyCurve rhinoCrv = new();

        foreach (var bhomCrv in curve.Curves)
            rhinoCrv.Append(bhomCrv.ToRhino());

        return rhinoCrv;
    }
    public static Rhino.Geometry.PolyCurve PolycurveToRhino(this PolyCurve curve)
    {
        Rhino.Geometry.PolyCurve rhinoCrv = new();

        foreach (var bhomCrv in curve.Curves)
            rhinoCrv.Append(bhomCrv.ToRhino());

        return rhinoCrv;
    }
    public static Rhino.Geometry.PolylineCurve ToOSMRhino(this Polygon curve)
    {
        var rhPoints = curve.ControlPoints().Select(x => x.ToOSMRhino());
        var pline = new Rhino.Geometry.PolylineCurve(rhPoints);
        pline.MakeClosed(1e-03);

        var newCrv = new Rhino.Geometry.PolylineCurve(
            new List<Rhino.Geometry.Point3d>
            { rhPoints.ElementAt(0),
            rhPoints.ElementAt(1),
            rhPoints.ElementAt(2),
            rhPoints.ElementAt(0) });


        pline.SetEndPoint(pline.PointAtStart);
        //var rhCrv = Rhino.Geometry.Curve.CreateControlPointCurve(rhPoints, 1);
        //rhCrv.MakeClosed(1e-03);
        pline.MakeClosed(1e-03);
        var t = pline.IsClosed;

        return pline;
    }

    public static Rhino.Geometry.Curve ToOSMRhino(this Polyline curve)
    {
        var rhPoints = curve.ControlPoints.Select(x => x.ToOSMRhino());
        return Rhino.Geometry.Curve.CreateControlPointCurve(rhPoints, 1);
    }

    public static Rhino.Geometry.Point3d ToOSMRhino(this Point point)
    {
        return point.ToRhino();

        var doc = RhinoDoc.ActiveDoc;
        var xForm = doc.EarthAnchorPoint.GetModelToEarthTransform(doc.ModelUnitSystem);
        xForm.TryGetInverse(out var inverse);
        var rhPoint = point.ToRhino();
        rhPoint.Transform(inverse);

        return rhPoint;
    }

    public static Rhino.Geometry.Curve ToRhino(this ICurve curve)
    {
        var arcCrv = curve as Arc;
        if(arcCrv is not null)
        {
            Rhino.Geometry.Arc rhinoArc = arcCrv.ToRhinoArc();
            return rhinoArc.ToNurbsCurve();
        }
        else if (curve is PolyCurve pCrv)
        {
            return pCrv.PolycurveToRhino();
        }
        else if (curve is BoundaryCurve bCrv)
        {
            return bCrv.BoundarycurveToRhino();
        }
        else
        {
            var rhinoPts = curve.IControlPoints().ToRhino();
            var rhinoCrv = Rhino.Geometry.Curve.CreateControlPointCurve(rhinoPts, 1);

            return rhinoCrv;
        }
    }

    public static IEnumerable<Rhino.Geometry.Curve> ToRhino(this IEnumerable<ICurve> curve)
    {
        var result = curve.Select(c => c.ToRhino());
        return result;
    }

    public static Rhino.Geometry.Mesh ToRhino(this Mesh mesh)
    {
        List<Rhino.Geometry.MeshFace> rhFaces = new();
        foreach(var face in mesh.Faces)
        {
            if (face.D == -1)
                rhFaces.Add(new Rhino.Geometry.MeshFace(face.A, face.B, face.C));
            else
                rhFaces.Add(new Rhino.Geometry.MeshFace(face.A, face.B, face.C, face.D));
        }

        List<Rhino.Geometry.Point3d> rhVerts = mesh.Vertices
            .Select(pt => pt.ToRhino())
            .ToList();

        var rhinoMesh = new Rhino.Geometry.Mesh();
        rhinoMesh.Vertices.AddVertices(rhVerts);
        rhinoMesh.Faces.AddFaces(rhFaces);

        return rhinoMesh;
    }
}
