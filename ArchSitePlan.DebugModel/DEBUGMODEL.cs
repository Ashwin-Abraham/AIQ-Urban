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
            path += glbExtension;
            Elements.Serialization.glTF.GltfExtensions.ToGlTF(_hyparModel, path);
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
        }

        public static void AddMass(Mass testMass)
        {
            _hyparModel.AddElement(testMass);
        }

        public static void AddMasses(IEnumerable<Mass> masses, Material material = null)
        {
            if (material != null)
                masses.ToList().ForEach(mass => mass.Material = material);

            IEnumerable<Element> massElements = masses.Select(m => m as Element);
            _hyparModel.AddElements(massElements);
        }

        public static void AddPolygon(Polygon p,
                                Material material = null)
        {
            _hyparModel.AddElement(new ModelCurve(p, material));
        }

        public static void AddPolygons(IEnumerable<Polygon> polygonList,
                                Material material = null)
        {
            polygonList.ToList().ForEach(polygon => AddPolygon(polygon, material));
        }

        public static void AddBoundedCurve(BoundedCurve crv,
                                Material material = null,
                                double zMovt = 0)
        {
            crv = crv.Transformed(new Transform(new Vector3(0, 0, zMovt))) as BoundedCurve;
            _hyparModel.AddElement(new ModelCurve(crv, material));
        }

        public static void AddBoundedCurves(IEnumerable<BoundedCurve> boundedCrvList,
                                Material material = null,
                                double zMovt = 0)
        {
            boundedCrvList.ToList().ForEach(crv => AddBoundedCurve(crv, material, zMovt));
        }

        public static void AddPolygonalCircles(IEnumerable<Circle> circleList,
                                Material material = null)
        {
            circleList
                .ToList()
                .ForEach(circ => AddPolygonalCircle(circ, material));
        }

        public static void AddPolygonalCircle(Circle circle, Material material = null)
        {
            AddBoundedCurve(circle.ToPolygonalCircle(), material);
        }

        public static void MoveModelInZ(double distance)
        {
            _hyparModel.Transform.Move(0, 0, distance);
        }

        public static void AddPoints(List<Vector3> points, Material material = null)
        {
            _hyparModel.AddElements(new ModelPoints(points, material));
        }

        public static void AddPointsAsCircles(List<Vector3> points, double radius = 0.15, Material material = null)
        {
            points.ForEach(pt => AddPointAsCircle(pt, radius, material));
        }

        public static void AddPointAsCircle(Vector3 point, double radius = 0.15, Material material = null)
        {
            AddPolygonalCircle(new Circle(point, radius), material);
        }

        public static void AddModelText(List<Vector3> placementPoints,
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

        public static void AddModelPoint(Vector3 point, Material material = null)
        {
            ModelPoints points = new ModelPoints(new List<Vector3>() { point }, material);
            _hyparModel.AddElement(points);
        }

        public static void AddModelPoints(IEnumerable<Vector3> points, Material material = null)
        {
            _hyparModel.AddElements(new ModelPoints(points.ToList()));
        }

        public static void AddPlanes(IEnumerable<Transform> planes, double length = 0.3, Material colour = null)
        {
            planes.ToList().ForEach(t => AddPlane(t, length, colour));
        }

        public static void AddPlanes(IEnumerable<Vector3> XYplaneOrigins, double length = 0.3, Material colour = null)
        {
            IEnumerable<Transform> planes = XYplaneOrigins.Select(o => new Transform(o));
            planes.ToList().ForEach(t => AddPlane(t, length, colour));
        }

        public static void AddPlane(Transform plane, double length = 0.5, Material colour = null)
        {
            Line xAxis = new Line(plane.Origin, plane.XAxis, length);
            Line yAxis = new Line(plane.Origin, plane.YAxis, length);
            Line zAxis = new Line(plane.Origin, plane.ZAxis, length);

            AddBoundedCurve(HyparDebugExtensions.CreateRectangle(plane, length / 3, length / 3), colour);
            AddBoundedCurve(xAxis, DebugMaterials.Red);
            AddBoundedCurve(yAxis, DebugMaterials.Green);
            AddBoundedCurve(zAxis, DebugMaterials.Blue);
        }

        public static void AddMesh(Mesh hyparMesh, Material? material = null)
        {
            material ??= DebugMaterials.Gray;
            _hyparModel.AddElement(new MeshElement(hyparMesh, material: material));
        }

        public static void AddColoredMesh(Mesh hyparMesh)
        {
            List<Mass> masses = new List<Mass>();
            int count = 0;
            foreach (Triangle tri in hyparMesh.Triangles)
            {
                Mass m = new Mass(profile: new Profile(tri.ToPolygon()),
                                    material: new Material("x", tri.Vertices.First().Color),
                                    height: 0.1
                                );

                masses.Add(m);

                count++;
                if (count > 25) break;
            }
            _hyparModel.AddElements(masses);
        }

        public static void AddMeshes(IEnumerable<Mesh> meshes)
        {
            meshes.ToList().ForEach(m => AddMesh(m));
        }

        public static void AddElements(IEnumerable<Element> elements)
        {
            _hyparModel.AddElements(elements);
        }

        public static void AddProfiles(IEnumerable<Profile> profiles)
        {
            profiles.ToList().ForEach(p => AddProfile(p));
        }

        public static void AddProfile(Profile profile)
        {
            AddBoundedCurve(profile.Perimeter);
            AddBoundedCurves(profile.Voids);
        }

        public static void AddPointDataField(List<(Vector3, double)> field, double radius = 0.15)
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

                AddPointAsCircle(fieldPt.Item1, radius: radius, material);
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
