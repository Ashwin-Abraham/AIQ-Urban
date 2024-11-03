using BH.Engine.Geometry;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BHoM = BH.oM.Geometry;

namespace ArchSitePlan.UI.RhinoBHoMInterop
{
    public static class ToBHoMExtensions
    {
        public static BHoM.ICurve? ToBHoM2DegreeCrv(this Curve rhinoCrv)
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
        public static BHoM.Arc ToBHoM(this Arc rhinoArc)
        {
            var bhomArc = Create.Arc(start: rhinoArc.StartPoint.ToBHoM(),
                middle: rhinoArc.MidPoint.ToBHoM(),
                end: rhinoArc.EndPoint.ToBHoM());

            return bhomArc;
        }

        public static BHoM.Line ToBHoM(this Line rhinoLine)
        {
            return Create.Line(rhinoLine.From.ToBHoM(), rhinoLine.To.ToBHoM());
        }

        public static BHoM.Point ToBHoM(this Point3d rhinoPoint)
        {
            return Create.Point(rhinoPoint.X, rhinoPoint.Y, rhinoPoint.Z);  
        }

        public static IEnumerable<BHoM.Point> ToBHoM(this IEnumerable<Point3d> rhinoPoints)
        {
            return rhinoPoints.Select(pt => pt.ToBHoM());
        }

        public static BHoM.Face ToBHoM(this MeshFace rhinoFace)
        {
            if(rhinoFace.C == rhinoFace.D)
                return Create.Face(rhinoFace.A, rhinoFace.B, rhinoFace.C);
            else
                return Create.Face(rhinoFace.A, rhinoFace.B, rhinoFace.C, rhinoFace.D);
        }

        public static BHoM.Polyline ToBHoMPolyline(this Polyline rhinoPline)
        {
            List<Point3d> pointList = rhinoPline.ToList();
            List<BHoM.Point> bhomPts = pointList.ToBHoM().ToList();
            return Create.Polyline(bhomPts);
        }
        public static BHoM.Polygon ToBHoMPolygon(this Polyline rhinoPline)
        {
            List<Point3d> pointList = rhinoPline.ToList();
            List<BHoM.Point> bhomPts = pointList.ToBHoM().ToList();
            return Create.Polygon(bhomPts);
        }

        public static BHoM.Mesh ToBHoM(this Mesh rhinoMesh)
        {
            List<BHoM.Point> bhomPts = rhinoMesh.Vertices.ToPoint3dArray().ToBHoM().ToList();
            List<BHoM.Face> bhomFaces = rhinoMesh.Faces.Select(f => f.ToBHoM()).ToList();
            return Create.Mesh(bhomPts, bhomFaces);
        }

        public static BHoM.PolyCurve ToBHoM(this Rhino.Geometry.PolyCurve polyCurve)
        {
            var segments = polyCurve.DuplicateSegments();
            List<BHoM.ICurve> bhomSegments = new();

            foreach (var crv in segments)
            {
                if (crv != null)
                    bhomSegments.Add(crv.ToBHoM());
            }
            return Create.PolyCurve(bhomSegments);
        }
        public static BHoM.ICurve? ToBHoM(this Rhino.Geometry.Curve rhinoCrv)
        {
            if (rhinoCrv.TryGetPolyline(out Polyline pline))
                return pline.ToBHoMPolyline();
            else if (rhinoCrv is PolyCurve pCrv)
                return pCrv.ToBHoM();
            else if (rhinoCrv.TryGetArc(out Arc arc))
                return arc.ToBHoM();
            else
                return null;
        }
    }
}
