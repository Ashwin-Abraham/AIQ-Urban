using ArchSitePlan.Core.Extensions;
using ArchSitePlan.DebugModel;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.UI.RhinoBHoMInterop
{
    public static class ToRhinoInterop
    {
        public static Rhino.Geometry.Brep ToRhino(this PlanarSurface bhomSurface)
        {
            var rhinoPoints = bhomSurface.ExternalBoundary.IControlPoints().ToRhino();

            var crvList = bhomSurface.ExternalBoundary.ObjectToList();
            crvList.AddRange(bhomSurface.InternalBoundaries);

            var brep = Rhino.Geometry.Brep.CreatePlanarBreps(crvList.ToRhino()).First();
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

        public static Rhino.Geometry.Curve ToRhino(this ICurve curve)
        {
            var rhinoPts = curve.IControlPoints().ToRhino();
            var rhinoCrv = Rhino.Geometry.Curve.CreateControlPointCurve(rhinoPts, 1);

            return rhinoCrv;
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
}
