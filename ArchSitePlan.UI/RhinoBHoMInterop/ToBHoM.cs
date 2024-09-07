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
        public static BHoM.Arc ToBHoM(this Arc rhinoArc)
        {
            return null;
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

        public static BHoM.Polyline ToBHoM(this Polyline rhinoPline)
        {
            List<Point3d> pointList = rhinoPline.ToList();
            List<BHoM.Point> bhomPts = pointList.ToBHoM().ToList();
            return Create.Polyline(bhomPts);
        }

        public static BHoM.Mesh ToBHoM(this Mesh rhinoMesh)
        {
            List<BHoM.Point> bhomPts = rhinoMesh.Vertices.ToPoint3dArray().ToBHoM().ToList();
            List<BHoM.Face> bhomFaces = rhinoMesh.Faces.Select(f => f.ToBHoM()).ToList();
            return Create.Mesh(bhomPts, bhomFaces);
        }

        public static BHoM.ICurve ToBHoM(this Rhino.Geometry.Curve rhinoCrv)
        {
            if (rhinoCrv.TryGetPolyline(out Polyline pline))
                return pline.ToBHoM();
            else
                return null;
        }
    }
}
