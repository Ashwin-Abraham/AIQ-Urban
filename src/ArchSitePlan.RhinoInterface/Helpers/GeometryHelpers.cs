using AIQ.Primitives.Extensions;
using Rhino.Geometry;

namespace ArchSitePlan.RhinoInterface.Helpers
{
    public static class GeometryHelpers
    {
        public static Curve[] GetContours(Mesh mesh, double contourInterval)
        {
            var bounds = mesh.GetBoundingBox(false);
            var contourOffsetTolerance = 1e-05;

            var result = Mesh.CreateContourCurves(
                mesh, 
                new Point3d(0,0, bounds.Min.Z + contourOffsetTolerance),
                new Point3d(0,0, bounds.Max.Z - contourOffsetTolerance),
                contourInterval,
                1e-06);

            var groups = result.GroupBy(x => x.PointAtStart.Z);
            var joined = groups.SelectMany(x => Curve.JoinCurves(x, 1e-01, true)).ToArray();
            joined = VerifyContours(joined, contourInterval);
            var lengths = joined.Select(x => x.GetLength()).ToList();

            return joined;
        }

        private static Curve[] VerifyContours(Curve[] contours, double interval)
        {
            if (contours.Length == 0 || interval.AlmostZero())
                return contours;

            contours = contours.OrderBy(x => x.PointAtStart.Z).ToArray();
            double startZ = contours[0].PointAtStart.Z;
            double maxZ = contours[^1].PointAtStart.Z;

            Dictionary<int, List<Curve>> contoursPerLevel = new();
            double zHeight = startZ;

            for(int i = 0; zHeight.LessThanORAlmostEquals(maxZ); i++)
            {
                var curves = contours
                    .Where(x => x.PointAtStart.Z.AlmostEquals(zHeight))
                    .ToList();


                if(curves.Count > 0)
                {
                    contoursPerLevel.Add(i, curves);
                }
                else
                // This means the contour function failed for buildings 
                // and we must insert floor curves here
                {
                    int previousLevel = i - 1;
                    double previousZ = zHeight - interval;
                    var mover = new Vector3d(0,0, -interval);

                    curves = contoursPerLevel[previousLevel]
                        .Select(x => x.DuplicateCurve())
                        .ToList();

                    curves.ForEach(x => x.Translate(mover));

                    contoursPerLevel.Add(i, curves);
                }

                zHeight += interval;
            }

            return contoursPerLevel.Values.SelectMany(x => x).ToArray();
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
