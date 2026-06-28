using AIQ.Primitives.Extensions;
using AIQ.Urban.Core.GeometryOperations;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace AIQ.Urban.Core.Extensions;

public static class Profile2DExtensions
{
    public static Profile2D WithoutPeripheralVoids(this Profile2D profile, double tolerance = 1e-5)
    {
        var internalVoids = new List<Polygon2D>();
        var peripheralVoids = new List<Polygon2D>();
        var outline = profile.Outline.Geometry;

        foreach(var hole in profile.Holes)
        {
            int touchCount = hole.Points().Count(p => outline.ClosestPoint(p, out _, tolerance));
            if(touchCount > 1)
                peripheralVoids.Add(hole);
            else
                internalVoids.Add(hole);
        }

        var remainingOutline = Curve.CreateBooleanDifference(outline, peripheralVoids.Select(x => x.Geometry), tolerance);

        if (remainingOutline.Length != 1)
            return profile;

        var newProfile = Profile2D.Create(new Polygon2D(remainingOutline[0]), internalVoids, tolerance);
        return newProfile ?? profile;
    }

    public static Mesh Triangulate(this Profile2D profile, double tolerance = 1e-5)
        =>SweepLineTriangulator.Triangulate(profile, tolerance);

    public static bool Contains(this Profile2D profile, Line line, double tolerance = 1e-5)
    {
        var outlineContains = profile.Outline.Contains(line, tolerance);

        if (!outlineContains)
            return false;

        // Check if a part of the line strays into any hole
        foreach(var hole in profile.Holes)
        {
            if(hole.Contains(line, tolerance)) 
                return false;

            var intersections = Intersection.CurveLine(hole.Geometry, line, tolerance, tolerance);

            if (intersections.Count == 0)
                continue;

            if(intersections.Count == 1)
            {
                // Overlapping with a hole boundary is considered to be inside the profile
                if (intersections[0].IsOverlap)
                    continue;

                // The line's start or end point touches the hole only and the rest is within the polygon
                if (intersections[0].ParameterB.AlmostEquals(0) || intersections[0].ParameterB.AlmostEquals(1))
                    continue;
            }

            // intersections count > 2 means the line has gone into the hole
            return false;
        }

        return true;
    }

    public static bool Contains(this Polygon2D polygon, Line line, double tolerance = 1e-5)
    {
        var intersections = Intersection.CurveLine(polygon.Geometry, line, tolerance, tolerance);

        if (intersections.Count == 0)
        {
            // Entire line is either inside or outside
            var containment = polygon.Geometry.Contains(line.PointAt(0.5), Plane.WorldXY, tolerance);
            return containment == PointContainment.Inside;
        }

        // line has left the polygon while traversing it
        if (intersections.Count > 2)
            return false;

        // The mid point is either inside or outside the polygon, indicating the entire line is inside/ outside
        var containment2 = polygon.Geometry.Contains(line.PointAt(0.5), Plane.WorldXY, tolerance);
        return containment2 == PointContainment.Inside;
    }
}
