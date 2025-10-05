using BH.Engine.Geometry;
using BH.oM.Geometry;
using Elements;
using BoundedCurve = Elements.Geometry.BoundedCurve;
using ColorSys = System.Drawing.Color;

namespace ArchSitePlan.DebugModel.BHoMExtensions
{
    public static class ToHyparInterop
    {
        public static Mass ToHyparMass(this PlanarSurface srf, double massHeight = 0.1, Material? material = null)
        {
            material ??= BuiltInMaterials.Default;
            return new Mass(srf.ToHyparProfile(), massHeight, material: material);
        }

        public static Elements.Geometry.Profile ToHyparProfile(this PlanarSurface srf)
        {
            var external = srf.ExternalBoundary.ToHyparPolygon();
            var internals = srf.InternalBoundaries.Select(v => v.ToHyparPolygon())
                .ToList();

            return new Elements.Geometry.Profile(external, internals);
        }

        public static Elements.Geometry.Polygon ToHyparPolygon(this ICurve curve)
        {
            var verts = curve.IControlPoints()
                .Select(pt => pt.ToHypar())
                .ToList();

            verts.RemoveAt(verts.Count - 1);
            return new Elements.Geometry.Polygon(verts);
        }

        public static Elements.Geometry.Polyline ToHyparPolyline(this ICurve curve)
        {
            var verts = curve.IControlPoints()
                .Select(pt => pt.ToHypar())
                .ToList();

            return new Elements.Geometry.Polyline(verts);
               
        }
        public static IEnumerable<Elements.Geometry.Polyline> ToHyparPolylines(
            this IEnumerable<ICurve> curves)
        {
            return curves.Select(crv => crv.ToHyparPolyline());
        }
        public static (List<BoundedCurve> Perimeter, List<List<BoundedCurve>> Voids) ToHypar(
            this PlanarSurface surface)
        {
            var perimeter = surface.ExternalBoundary.ToHypar();
            var voids = surface.InternalBoundaries.Select(v => v.ToHypar()).ToList();

            return (perimeter, voids);
        }

        public static List<BoundedCurve> ToHypar(this IEnumerable<ICurve> curves)
            => curves.SelectMany(x => x.ToHypar()).ToList();

        

       

       
        private static Elements.Geometry.Triangle CreateTriangle(Face f, List<Elements.Geometry.Vertex> vertices)
        {
            var vertexList = new List<Elements.Geometry.Vertex>()
            {
                vertices[f.A],
                vertices[f.B],
                vertices[f.C]
            };

            var AToB = vertices[f.B].Position - vertices[f.A].Position;
            var BToC = vertices[f.C].Position - vertices[f.B].Position;
            var normal = AToB.Cross(BToC).Unitized();

            normal = new Random().Next() % 2 == 0 ? normal : normal.Negate();

            return new Elements.Geometry.Triangle(vertexList, normal);
        }

        private static bool IsValid(this Elements.Geometry.Triangle triangle, double tolerance = 1e-06)
        {
            for(int i = 0; i < 2; i++)
            {
                for (int j = i + 1; j < 3; j++)
                    if (triangle.Vertices[i].Position.IsAlmostEqualTo(triangle.Vertices[j].Position, tolerance))
                        return false;
            }

            return true;
        }

        public static List<BoundedCurve> ToHypar(this ICurve crv)
        {
            List<BoundedCurve> hyparCrvs = new List<BoundedCurve>();

            if (crv is Line l)
            {
                var line = ToHypar(l);
                if(line is not null) hyparCrvs.Add(line);
            }

            else if (crv is Polyline pline)
                hyparCrvs.Add(ToHypar(pline));

            else if (crv is Arc arc)
                hyparCrvs.Add(ToHypar(arc));

            else if (crv is Circle circle)
                hyparCrvs.Add(ToHypar(circle));

            else if (crv is Polygon polygon)
                hyparCrvs.Add(ToHypar(polygon));

            else if (crv is Polyline polyline)
                hyparCrvs.Add(ToHypar(polyline));

            else if (crv is PolyCurve polyCurve)
                hyparCrvs.AddRange(ToHypar(polyCurve));

            else if (crv is BoundaryCurve boundary)
                hyparCrvs.AddRange(ToHypar(boundary));

            return hyparCrvs;
        }

        public static List<BoundedCurve> ToHypar(this BoundaryCurve boundaryCurve)
            => boundaryCurve.Curves.SelectMany(crv => crv.ToHypar()).ToList();

        public static List<BoundedCurve> ToHypar(this PolyCurve polycurve)
            => polycurve.Curves.SelectMany(crv => crv.ToHypar()).ToList();

        public static Elements.Geometry.Circle ToHypar(this Circle circle)
            =>new Elements.Geometry.Circle(circle.Centre.ToHypar(), circle.Radius);

        public static Elements.Geometry.Arc ToHypar(this Arc arc)
        {
            var arcTransform = new Elements.Geometry.Transform(
                origin: arc.CoordinateSystem.Origin.ToHypar(),
                xAxis: arc.CoordinateSystem.X.ToHypar(),
                yAxis: arc.CoordinateSystem.Y.ToHypar(),
                zAxis: arc.CoordinateSystem.Z.ToHypar());

            var hyparArc = new Elements.Geometry.Arc(
                arcTransform,
                arc.Radius,
                arc.StartAngle,
                arc.EndAngle);

            return hyparArc;
        }

        public static Elements.Geometry.Vector3 ToHypar(this Vector v)
        {
            return new Elements.Geometry.Vector3(v.X, v.Y, v.Z);
        }
        public static Elements.Geometry.Polyline ToHypar(this Polyline pline)
        {
            var points = pline.ControlPoints.Select(pt => ToHypar(pt)).ToList();

            return new Elements.Geometry.Polyline(points);
        }

        public static Elements.Geometry.Line? ToHypar(this Line line)
        {
            if(line.Start.Distance(line.End)< Elements.Geometry.Vector3.EPSILON) return null;

            return new Elements.Geometry.Line(ToHypar(line.Start), ToHypar(line.End));
        }

        public static Elements.Geometry.Vector3 ToHypar(this Point point)
        {
            return new Elements.Geometry.Vector3(point.X, point.Y, point.Z);
        }

        private static Elements.Geometry.Vertex GetVertex(this Point point)
        {
            return new Elements.Geometry.Vertex(point.ToHypar());
        }

        public static IEnumerable<Elements.Geometry.Triangle> ToHypar(
            this Face face,
            Mesh mesh,
            List<Elements.Geometry.Vertex> vertices)
        {
            if (!face.IsQuad())
            {
                var v1 = vertices[face.A];
                var v2 = vertices[face.B];
                var v3 = vertices[face.C];

                yield return new Elements.Geometry.Triangle(
                    vertices: [v1, v2, v3],
                    normal: face.Normal(mesh).ToHypar());
            }
            else
            {
                var v1 = vertices[face.A];
                var v2 = vertices[face.B];
                var v3 = vertices[face.C];
                var v4 = vertices[face.D];

                yield return new Elements.Geometry.Triangle(
                    vertices: [v1, v2, v4],
                    normal: face.Normal(mesh).ToHypar());

                yield return new Elements.Geometry.Triangle(
                    vertices: [v2, v3, v4],
                    normal: face.Normal(mesh).ToHypar());
            }
        }

        public static Elements.Geometry.Mesh ToHypar(
           this Mesh mesh,
           ColorSys? color = null)
        {
            color ??= ColorSys.Azure;

            var hyparVertices = mesh.Vertices
                       .Select(v => new Elements.Geometry.Vertex(v.ToHypar()))
                       .ToList();

            var triangles = new List<Elements.Geometry.Triangle>();

            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                var face = mesh.Faces[i];
                var faceTriangles = face.ToHypar(mesh, hyparVertices).ToList();
                triangles.AddRange(faceTriangles);
            }

            return new Elements.Geometry.Mesh(hyparVertices, triangles);
        }

        //public static Elements.Geometry.Mesh ToHypar(this Mesh mesh)
        //{
        //    var vertices = mesh.Vertices.Select(v => v.GetVertex()).ToList();
        //    var triangles = new List<Elements.Geometry.Triangle>();

        //    foreach (var face in mesh.Faces)
        //    {
        //        triangles.AddRange(face.ToHypar(mesh, vertices));
        //    }

        //    var hyparMesh = new Elements.Geometry.Mesh(vertices, triangles);

        //    return hyparMesh;
        //}
    }
}
