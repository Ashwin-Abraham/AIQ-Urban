using ArchSitePlan.DebugModel.Extensions;
using Elements;
using Elements.Geometry;
using Elements.Geometry.Solids;
using System.Diagnostics;

namespace ArchSitePlan.DebugModel
{
    public static class DEBUGMODEL
    {
        private const string glbExtension = ".glb";
        private const double defaultTextSize = 0.5;
        private const string defaultPath = "%temp%";
        private static bool _executingScript = false;
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

        public static string SaveAsGlb(string path = defaultPath)
        {
            path += glbExtension;
            Elements.Serialization.glTF.GltfExtensions.ToGlTF(_hyparModel, path);

            return path;
        }

        public static void OpenAsGlb(this IEnumerable<Element?> elements)
        {
            _hyparModel = new();
            _hyparModel.AddElements(elements.Where(x => x != null));
            OpenAsGlb();
        }

        public static void OpenAsGlb(string path = defaultPath)
        {
            if (_executingScript) return;
#if DEBUG_VIZ || DEBUG
            _executingScript = true;
            path = SaveAsGlb(path);
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
            _executingScript = false;
#endif
        }

        public static List<Element> Debug(this IEnumerable<Extrude> extrudes, Material? material = null, double zMovt = 0)
        {
            return extrudes.Select(x => x.Debug(material, zMovt)).ToList();
        }

        public static Element Debug(this Extrude extrude, Material? material = null, double zMovt = 0)
        {
            var transform = extrude.LocalTransform?? new();

            if(!zMovt.ApproximatelyEquals(0))
                transform = transform.Concatenated(new Transform(new Vector3(0, 0, zMovt)));

            var mass = new Mass(extrude.Profile, extrude.Height, transform: transform);
            return mass.Debug(material);
        }

        public static Element Debug(this Mass mass, Material? material = null)
        {
            var massElement = new Mass(
                mass.Profile, 
                mass.Height, 
                mass.Material, 
                mass.Transform, 
                mass.Representation,
                mass.IsElementDefinition,
                mass.Id, 
                mass.Name);

            if (material != null)
                massElement.Material = material;

            return massElement;
        }

        public static List<Element> Debug(this IEnumerable<Mass> masses, Material? material = null, double zMovt = 0)
        {
            return masses.Debug(material, zMovt).ToList();
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


        public static Element Debug(this BoundedCurve crv,
                                Material? material = null,
                                double zMovt = 0)
        {
            crv = crv.Transformed(new Transform(new Vector3(0, 0, zMovt))) as BoundedCurve;
            var element = new ModelCurve(crv, material);

            _hyparModel.AddElement(element);
            return element;
        }

        public static List<Element> Debug(this IEnumerable<BoundedCurve> boundedCrvList,
                                Material? material = null,
                                double zMovt = 0)
        {
            return boundedCrvList
                .Select(crv => Debug(crv, material, zMovt))
                .ToList();
        }

        public static List<Element> Debug(this IEnumerable<string> text,
            Vector3 location,
            Vector3? facingDirection = null,
            Vector3? lineDirection = null,
            FontSize fontSize = FontSize.PT60,
            Color? color = null)
        {
            facingDirection ??= Vector3.ZAxis;
            lineDirection ??= Vector3.XAxis;

            return text
                .Select(x => x.Debug(location, facingDirection, lineDirection, fontSize, color))
                .ToList();
        }

        public static Element Debug(this string text,
            Vector3 location,
            Vector3? facingDirection = null,
            Vector3? lineDirection = null,
            FontSize fontSize = FontSize.PT60,
            Color? color = null)
        {
            facingDirection??= Vector3.ZAxis;
            lineDirection ??= Vector3.XAxis;

            var modelText = new ModelText(
                texts: new List<(Vector3, Vector3, Vector3, string, Color?)>
                {
                    (location, facingDirection.Value, lineDirection.Value, text, color)
                },
                fontSize: fontSize);

            return modelText;
        }

        public static void DebugAsPolygons(this IEnumerable<Circle> circleList,
                                Material? material = null,
                                double zMovt = 0)
        {
            circleList
                .ToList()
                .ForEach(circ => DebugAsPolygons(circ, material, zMovt));
        }

        public static Element DebugAsPolygons(this Circle circle, Material? material = null, double zMovt = 0)
        {
            return Debug((BoundedCurve)circle.ToPolygonalCircle(), material, zMovt);
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

        public static Element Debug(this Vector3 point, 
            double radius = 0.15, 
            Material? material = null,
            double zMovt = 0)
        {
            return DebugAsPolygons(new Circle(point, radius), material, zMovt);
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

        public static void Debug(this IEnumerable<Transform> planes, double length = 0.3, Material? colour = null)
        {
            planes.ToList().ForEach(t => Debug(t, length, colour));
        }

        public static void Debug(this IEnumerable<Vector3> XYplaneOrigins, double length = 0.3, Material? colour = null)
        {
            IEnumerable<Transform> planes = XYplaneOrigins.Select(o => new Transform(o));
            planes.ToList().ForEach(t => Debug(t, length, colour));
        }

        public static IEnumerable<Element> Debug(this Transform plane, double length = 0.5, Material? colour = null)
        {
            Line xAxis = new Line(plane.Origin, plane.XAxis, length);
            Line yAxis = new Line(plane.Origin, plane.YAxis, length);
            Line zAxis = new Line(plane.Origin, plane.ZAxis, length);

            yield return Debug((BoundedCurve)HyparExtensions.CreateRectangle(plane, length / 3, length / 3), colour);
            yield return Debug(xAxis, DebugMaterials.Red);
            yield return Debug(yAxis, DebugMaterials.Green);
            yield return Debug(zAxis, DebugMaterials.Blue);
        }

        public static Element Debug(this Mesh hyparMesh, Material? material = null)
        {
            material ??= DebugMaterials.Red;
            return new MeshElement(hyparMesh, material: material);
        }

        public static Element DebugWithColours(this Triangle face, Material material)
        {
            (var polygonXY, var toGlobal) = face.ToPolygonXY();

            var mass = new Mass(polygonXY.EnsureClockwise(), 1e-3, material);
            mass.Transform = toGlobal;

            return mass;
        }

        //public static List<Element> DebugWithColours(this Mesh hyparMesh)
        //{
        //    var elements = new List<Element>();

        //    foreach (var face in hyparMesh.Triangles)
        //    {
        //        var material = new Material("", face.GetColor());
        //        elements.Add(face.DebugWithColours(material));
        //    }

        //    return elements;
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
