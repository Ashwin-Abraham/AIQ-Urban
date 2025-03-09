using Elements;
using Elements.Geometry;
using System.Diagnostics;

namespace ArchSitePlan.DebugModel
{
    public static class DEBUGMODEL
    {
        private const string glbExtension = ".glb";
        private const double defaultTextSize = 0.5;
        private const string defaultPath = "%temp%";
        private static Model _hyparModel = new Model();
        public static Model HyparModel
        {
            get => _hyparModel;
            set => _hyparModel = value;
        }

        public static void Reset()
        {
            _hyparModel = new Model();
        }

        public static void OpenAsGlb(string path = defaultPath)
        {
#if DEBUG
            path += glbExtension;
            Elements.Serialization.glTF.GltfExtensions.ToGlTF(_hyparModel, path);
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
#endif
        }

        public static void Debug(this Mass testMass)
        {
            _hyparModel.AddElement(testMass);
        }

        public static void Debug(this IEnumerable<Mass> masses, Material? material = null)
        {
            if (material != null)
                masses.ToList().ForEach(mass => mass.Material = material);

            IEnumerable<Element> massElements = masses.Select(m => m as Element);
            _hyparModel.AddElements(massElements);
        }

        public static void Debug(this Polygon p,
                                Material? material = null)
        {
            _hyparModel.AddElement(new ModelCurve(p, material));
        }

        public static void Debug(this IEnumerable<Polygon> polygonList,
                                Material? material = null)
        {
            polygonList.ToList().ForEach(polygon => Debug(polygon, material));
        }


        public static void Debug(this BoundedCurve crv,
                                Material? material = null,
                                double zMovt = 0)
        {
            crv = crv.Transformed(new Transform(new Vector3(0, 0, zMovt))) as BoundedCurve;
            _hyparModel.AddElement(new ModelCurve(crv, material));
        }

        public static void Debug(this IEnumerable<BoundedCurve> boundedCrvList,
                                Material? material = null,
                                double zMovt = 0)
        {
            boundedCrvList.ToList().ForEach(crv => Debug(crv, material, zMovt));
        }

        public static void DebugAsPolygons(this IEnumerable<Circle> circleList,
                                Material? material = null,
                                double zMovt = 0)
        {
            circleList
                .ToList()
                .ForEach(circ => DebugAsPolygons(circ, material, zMovt));
        }

        public static void DebugAsPolygons(this Circle circle, Material? material = null, double zMovt = 0)
        {
            Debug((BoundedCurve)circle.ToPolygonalCircle(), material, zMovt);
        }

        public static void Debug(this double distance)
        {
            _hyparModel.Transform.Move(0, 0, distance);
        }

        public static void Debug(this List<Vector3> points, Material? material = null)
        {
            _hyparModel.AddElements(new ModelPoints(points, material));
        }

        public static void Debug(this List<Vector3> points, double radius = 0.15, Material? material = null)
        {
            points.ForEach(pt => Debug(pt, radius, material));
        }

        public static void Debug(this Vector3 point, 
            double radius = 0.15, 
            Material? material = null,
            double zMovt = 0)
        {
            DebugAsPolygons(new Circle(point, radius), material, zMovt);
        }

        public static void Debug(this List<Vector3> placementPoints,
                                        List<string> text,
                                        Color? color = null)
        {
            var texts = new List<(Vector3 location,
                                               Vector3 facingDirection,
                                               Vector3 lineDirection,
                                               string text,
                                               Color? color)>();

            var dir = Vector3.YAxis.Negate();

            for (int i = 0; i < placementPoints.Count; i++)
            {
                texts.Add((placementPoints[i], Vector3.ZAxis, Vector3.XAxis, text[i], color));
            }

            ModelText txt = new ModelText(
                   texts: texts,
                   fontSize: FontSize.PT60);

            _hyparModel.AddElement(txt);
        }

        public static void Debug(this Vector3 point, Material? material = null)
        {
            ModelPoints points = new ModelPoints(new List<Vector3>() { point }, material);
            _hyparModel.AddElement(points);
        }

        public static void Debug(this IEnumerable<Vector3> points, Material? material = null)
        {
            _hyparModel.AddElements(new ModelPoints(points.ToList()));
        }

        public static void Debug(this IEnumerable<Transform> planes, double length = 0.3, Material colour = null)
        {
            planes.ToList().ForEach(t => Debug(t, length, colour));
        }

        public static void Debug(this IEnumerable<Vector3> XYplaneOrigins, double length = 0.3, Material colour = null)
        {
            IEnumerable<Transform> planes = XYplaneOrigins.Select(o => new Transform(o));
            planes.ToList().ForEach(t => Debug(t, length, colour));
        }

        public static void Debug(this Transform plane, double length = 0.5, Material colour = null)
        {
            Line xAxis = new Line(plane.Origin, plane.XAxis, length);
            Line yAxis = new Line(plane.Origin, plane.YAxis, length);
            Line zAxis = new Line(plane.Origin, plane.ZAxis, length);

            Debug((BoundedCurve)HyparExtensions.CreateRectangle(plane, length / 3, length / 3), colour);
            Debug(xAxis, DebugMaterials.Red);
            Debug(yAxis, DebugMaterials.Green);
            Debug(zAxis, DebugMaterials.Blue);
        }

        public static void Debug(this Mesh hyparMesh, Material? material = null)
        {
            material ??= DebugMaterials.Gray;
            _hyparModel.AddElement(new MeshElement(hyparMesh, material: material));
        }

        //public static void Debug(this Mesh hyparMesh)
        //{
        //    List<Mass> masses = new List<Mass>();
        //    int count = 0;
        //    foreach (Triangle tri in hyparMesh.Triangles)
        //    {
        //        Mass m = new Mass(profile: new Profile(tri.ToPolygon()),
        //                            material: new Material("x", tri.Vertices.First().Color),
        //                            height: 0.1
        //                        );

        //        masses.Add(m);

        //        count++;
        //        if (count > 25) break;
        //    }
        //    _hyparModel.AddElements(masses);
        //}

        public static void Debug(this IEnumerable<Mesh> meshes)
        {
            meshes.ToList().ForEach(m => Debug(m));
        }

        public static void Debug(this IEnumerable<Element> elements)
        {
            _hyparModel.AddElements(elements);
        }

        public static void Debug(this IEnumerable<Profile> profiles)
        {
            profiles.ToList().ForEach(p => Debug(p));
        }

        public static void Debug(this Profile profile)
        {
            Debug((BoundedCurve)profile.Perimeter);
            Debug((IEnumerable<BoundedCurve>)profile.Voids);
        }

        public static void DebugPointField(this List<(Vector3, double)> field, double radius = 0.15)
        {
            if (!field.Any()) return;

            var points = field.Select(x => x.Item1).ToList();
            var data = field.Select(x => x.Item2).ToList();

            var highMaterial = DebugMaterials.Red;
            var lowMaterial = DebugMaterials.Blue;

            Domain1d redDomain = new Domain1d(highMaterial.Color.Red, lowMaterial.Color.Red);
            Domain1d greenDomain = new Domain1d(highMaterial.Color.Green, lowMaterial.Color.Green);
            Domain1d blueDomain = new Domain1d(highMaterial.Color.Blue, lowMaterial.Color.Blue);
            Domain1d dataDomain = new Domain1d(data.Max(), data.Min());

            foreach (var fieldPt in field)
            {
                double r = dataDomain.RemapToOtherDomain(redDomain, fieldPt.Item2);
                double g = dataDomain.RemapToOtherDomain(greenDomain, fieldPt.Item2);
                double b = dataDomain.RemapToOtherDomain(blueDomain, fieldPt.Item2);

                var material = new Material("", new Color(r, g, b, 1));

                Debug(fieldPt.Item1, radius: radius, material);
            }
        }

        private static double RemapToOtherDomain(this Domain1d domain1, Domain1d domain2, double numberOnDom1)
        {
            double parameterOnDomain1 = domain1.GetParameterAt(numberOnDom1);
            double numberOnDom2 = domain2.GetNumberAt(parameterOnDomain1);
            return numberOnDom2;
        }

        private static double GetParameterAt(this Domain1d domain, double position)
        {
            double parameter = (position - domain.Min) / domain.Length;
            return parameter;
        }

        private static double GetNumberAt(this Domain1d domain, double parameter)
        {
            double number = domain.Length * parameter + domain.Min;
            return number;
        }
    }
}
