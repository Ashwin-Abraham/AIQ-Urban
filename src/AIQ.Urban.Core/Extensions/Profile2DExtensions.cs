using AIQ.Primitives.Extensions;
using AIQ.Urban.Core.GeometryOperations;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanGraph;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace AIQ.Urban.Core.Extensions;

public static class Profile2DExtensions
{
    public static Profile2D WithoutPeripheralVoids(this Profile2D profile, double tolerance = 1e-5)
    {
        var internalVoids = new List<Polycurve2D>();
        var peripheralVoids = new List<Polycurve2D>();
        var outline = profile.Outline.Geometry;

        foreach (var hole in profile.Holes)
        {
            int touchCount = hole.Points().Count(p => outline.ClosestPoint(p, out _, tolerance));
            if (touchCount > 1)
                peripheralVoids.Add(hole);
            else
                internalVoids.Add(hole);
        }

        var remainingOutlines = peripheralVoids.Count == 0?
            [outline]
            :Curve.CreateBooleanDifference(outline, peripheralVoids.Select(x => x.Geometry), tolerance);

        if (remainingOutlines.Length != 1)
            return profile;

        var newProfile = new Profile2D(new Polycurve2D(remainingOutlines[0]), internalVoids, tolerance);
        return newProfile ?? profile;
    }

    public static bool Contains(this Profile2D profile, Line line, double tolerance = 1e-5)
    {
        var outlineContains = profile.Outline.ContainsOrCoincides(line, tolerance);

        if (!outlineContains)
            return false;

        var lineCurve = line.ToNurbsCurve();

        // Check if a part of the line strays into any hole
        foreach (var hole in profile.Holes)
        {
            if (hole.ContainsOrCoincides(line, tolerance))
                return false;

            var intersections = Intersection.CurveCurve(hole.Geometry, lineCurve, tolerance, tolerance);

            if (intersections.Count == 0)
                continue;

            if (intersections.Count == 1)
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

}