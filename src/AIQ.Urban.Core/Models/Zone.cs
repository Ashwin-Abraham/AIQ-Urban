using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models;

public class Zone : IUrbanElement
{
    public Polygon2D Boundary { get; }
    public string Name { get; }

    public Guid Id => Boundary.Id;

    public Zone(Polygon2D boundary, string name)
    {
        Boundary = boundary;
        Name = name;
    }

    public Point3d GetRepresentativeCenter()
    {
        return AreaMassProperties.Compute(Boundary.Geometry)!.Centroid;
    }

    public List<Guid> GetGeometryIds() => [Boundary.Id];

    public List<IUrbanElement> GetContainedElements(UrbanModel model)
    {
        var results = new List<IUrbanElement>();
        var boundaryCurve = Boundary.Geometry;
        boundaryCurve.TryGetPlane(out var plane);
        const double tolerance = 0.001;

        foreach (var element in model.GetUrbanElements())
        {
            if (element is Zone)
                continue;

            var center = element.GetRepresentativeCenter();
            if (IsInside(boundaryCurve, center, plane, tolerance))
                results.Add(element);
        }

        return results;
    }

    private static bool IsInside(Curve boundary, Point3d point, Plane plane, double tolerance)
    {
        var containment = boundary.Contains(point, plane, tolerance);
        return containment == PointContainment.Inside
            || containment == PointContainment.Coincident;
    }
}
