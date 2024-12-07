using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Helpers
{
    public static class GeometryHelpers
    {
        public static Curve[] GetContours(Mesh mesh, double contourInterval)
        {
            var bounds = mesh.GetBoundingBox(false);
            var result = Mesh.CreateContourCurves(
                mesh, 
                new Point3d(0,0, bounds.Min.Z),
                new Point3d(0,0, bounds.Max.Z),
                contourInterval);

            return result;
        }

        internal static Mesh ConvertToMesh(Brep? brep)
        {
            MeshingParameters parameters = new MeshingParameters();
            parameters.ClosedObjectPostProcess = true;
            var meshes = Mesh.CreateFromBrep(brep, parameters);

            var mesh = Join(meshes.ToList());
            return mesh;
        }

        internal static Mesh ConvertToMesh(Extrusion brep)
        {
            MeshingParameters parameters = new MeshingParameters();
            parameters.ClosedObjectPostProcess = true;
            return Mesh.CreateFromExtrusion(brep, parameters);
        }

        internal static void RescaleMesh(Mesh rhinoMesh, double height)
        {
            var bounds = rhinoMesh.GetBoundingBox(false);
            double zMin = bounds.Min.Z;
            
            Point3d basePt = new Point3d(
                bounds.Center.X,
                bounds.Center.Y,
                zMin);

            double currentHeight = bounds.Max.Z - bounds.Min.Z;
            double scaleFactor = height / currentHeight;

            Transform scaleTransform = Transform.Scale(
                new Plane(basePt, Vector3d.ZAxis),
                1, 1, scaleFactor);

            rhinoMesh.Transform(scaleTransform);
        }

        private static Mesh Join(List<Mesh> meshes)
        {
            Mesh mesh = new Mesh();
            foreach (var m in meshes)
                mesh.Append(m);

            return mesh;
        }
    }
}
