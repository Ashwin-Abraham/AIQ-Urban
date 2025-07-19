using AIQ.Primitives.Extensions;
using ArchSitePlan.BHoM.Geometry.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class ICurveExtensions
{
    public static Line ToLine(this ICurve curve)
    {
        if (curve is Line line)
            return line;

        return Create.Line(curve.IStartPoint(), curve.IEndPoint());
    }

    public static List<ICurve> BooleanIntersection(this ICurve curve, List<PlanarSurface> surfaces, double tolerance = 1e-06)
    {
        if (!curve.IIsClosed()) return [];
        //var externals = surfaces.Select(x => x.ExternalBoundary);
        //var internals = surfaces.SelectMany(x => x.InternalBoundaries);

        List<ICurve> result = new();
        foreach(var intersector in surfaces)
        {
            var externalOverlap = intersector.ExternalBoundary.BooleanIntersection(curve).ToList();
            var overlap = externalOverlap
                .SelectMany(x => x.BooleanDifference(intersector.InternalBoundaries))
                .ToList();
            
            
            result.AddRange(overlap);
        }

        //List<ICurve> result = externals
        //    .SelectMany(x => curve.BooleanIntersection(x))
        //    //.BooleanUnion()
        //    .SelectMany(x => x.BooleanDifference(internals).Cast<ICurve>())
        //    .ToList();

        return result;
    }

    public static List<ICurve> BooleanDifference(this ICurve curve, List<PlanarSurface> surfaces, double tolerance = 1e-06)
    {
        var overlap = curve.BooleanIntersection(surfaces);
        var curves = curve.BooleanDifference(overlap).Cast<ICurve>().ToList();

        return curves;
    }

    public static ICurve IOffsetSafe(
        this ICurve curve,
        double offset,
        Vector? normal = null,
        bool tangentExtensions = false,
        double distTol = 1e-06,
        double angleTol = 1e-06)
    {
        if(curve is Line line)
        {
            if (normal == null)
                normal = AssumeLineNormal(line);

            return line.Offset(offset, normal, tangentExtensions, distTol, angleTol);
        }

        else if( curve is PolyCurve pCrv && pCrv.Curves.All(x => x is Line))
        {
            if (normal == null)
            {
                var lineRepresentation = Create.Line(pCrv.StartPoint(), pCrv.EndPoint());
                normal = AssumeLineNormal(lineRepresentation);
            }
            
            return curve.IOffset(offset, normal, tangentExtensions, distTol, angleTol);
        }

        else
            return curve.IOffset(offset, normal, tangentExtensions, distTol, angleTol);
    }

    /// <summary>
    /// Assumes the normal of a line preferring to use the Z axis, unless the line is in Z.
    /// </summary>
    private static Vector AssumeLineNormal(Line line)
    {
        if (!line.Direction().IsEqual(Vector.ZAxis))
            return Vector.ZAxis;
        else
            return line.Direction().Rotate(Math.PI / 2, Vector.ZAxis);
    }


    // If BoundaryCurve or PolyCurve, this will divide each
    // curve within the ICurve into a number of fragments
    // Otherwise simply divides at relevant parameters
    public static List<ICurve> IDivideByCount(this ICurve curve, int count)
    {
        if (count < 2) return [curve];

        var divisionStepSize = 1.0 / (double)count;
        var divisionParams = new List<double>();

        for (double pos = divisionStepSize; pos < 1.0; pos += divisionStepSize)
            divisionParams.Add(pos);

        return IDivideAtParameters(curve, divisionParams);
    }

    //public static List<ICurve> IDivideAtParameters(this BoundaryCurve curve, List<double> divisionParams)
    //{
    //    List<ICurve> fragments = new();

    //    foreach(var subCurve in curve.Curves)
    //        fragments.AddRange(IDivideAtParameters((dynamic)subCurve, divisionParams));

    //    return fragments;
    //}

    //public static List<ICurve> IDivideAtParameters(this PolyCurve curve, List<double> divisionParams)
    //{
    //    List<ICurve> fragments = new();

    //    foreach (var subCurve in curve.Curves)
    //        fragments.AddRange(IDivideAtParameters((dynamic)subCurve, divisionParams));

    //    return fragments;
    //}

    public static List<ICurve> IDivideAtParameters(this ICurve curve, List<double> divisionParams)
    {
        List<Point> divisionPoints = GetDivisionPointsOnCurve((dynamic)curve, divisionParams); 
        return curve.ISplitAtPoints(divisionPoints);
    }

    private static List<Point> GetDivisionPointsOnCurve(Polygon curve, List<double> divisionParams)
    {
        return GetDivisionPointsOnCurve(curve as ICurve, divisionParams)
            .Prepend(curve.IStartPoint())
            .ToList();
    }

    private static List<Point> GetDivisionPointsOnCurve(BoundaryCurve curve, List<double> divisionParams)
    {
        return GetDivisionPointsOnCurve(curve as ICurve, divisionParams)
            .Prepend(curve.IStartPoint())
            .ToList();
    }

    private static List<Point> GetDivisionPointsOnCurve(ICurve curve, List<double> divisionParams)
    {
        List<Point> divisionPoints = divisionParams
            .Select(x => Clamp(x, 0 + 1e-05, 1 - 1e-05))
            .Select(param => curve.IPointAtParameter(param))
            .ToList();

        return divisionPoints;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static ICurve EnsureAntiClockwise(this ICurve curve, Vector? normal = null)
    {
        normal = normal ?? curve.INormal();
        return curve.IIsClockwise(normal) ? curve.IFlip() : curve;
    }

    public static ICurve EnsureClockwise(this ICurve curve, Vector? normal = null)
    {
        normal = normal ?? curve.INormal();
        return curve.IIsClockwise(normal) ? curve : curve.IFlip();
    }


    public static List<Point> ICurveIntersections(this ICurve curve, IEnumerable<ICurve> curves, double tolerance = 1e-06)
    {
        return curves.SelectMany(x => curve.ICurveIntersections(x, tolerance)).ToList();
    }

    public static ICurve OffsetAndMaintainClosure(
        this ICurve curve, 
        double offset,
        Vector? normal = null,
        bool tangentExtensions = false,
        double distTol = 1e-06,
        double angleTol = 1e-06 )
    {
        List<ICurve> subParts = new();
        bool isPolyCrv = false, isBoundaryCrv = false;

        if (curve is PolyCurve pCrv)
        {
            subParts = pCrv.Curves;
            isPolyCrv = true;
        }
        else if (curve is BoundaryCurve bCrv)
        {
            subParts = bCrv.Curves.ToList();
            isBoundaryCrv = true;
        }

        if (!isBoundaryCrv && !isPolyCrv)
            return curve.IOffsetSafe(offset, normal, tangentExtensions, distTol, angleTol);

        var areAllPolygons = subParts.All(x => x is Polygon);

        if(!areAllPolygons)
            return curve.IOffsetSafe(offset, normal, tangentExtensions, distTol, angleTol);

        var offsetPolygons = subParts
        .Select(polygon => polygon.IOffsetSafe(offset, normal, tangentExtensions, distTol, angleTol))
        .Select(x => x is Polyline pline? pline.ClosedPolylineToPolygon(): x)
        .ToList();

        return isPolyCrv ?
            Create.PolyCurve(offsetPolygons)
            : Create.BoundaryCurve(offsetPolygons);
    }

    private static void BooleanLine(
        Line line,
        ICurve boundary,
        out List<Line> linesInside,
        out List<Line> linesOutside)
    {
        linesInside = new();
        linesOutside = new();

        if (!boundary.IIsClosed()) return;

        var points = boundary.ICurveIntersections(line);

        if (!points.Exists(p => p.AlmostEquals(line.Start)))
            points.Add(line.Start);

        if (!points.Exists(p => p.AlmostEquals(line.End)))
            points.Add(line.End);

        List<Point> orderedPoints = points.OrderAlong(line).ToList();

        var fragments = Create.Polyline(orderedPoints).SubParts();

        linesInside = fragments
            .Where(x => boundary.IIsContaining([x.Mid()]))
            .ToList();

        var linesInside2 = linesInside;
        
        linesOutside = fragments
            .Where(x => !linesInside2.Contains(x))
            .ToList();
    }

    public static List<Line> LineBooleanDifference(this Line line, ICurve boundary)
    {
        BooleanLine(line, boundary, out var _, out var linesOutside);
        return linesOutside;
    }
    public static List<Line> LineBooleanIntersection(this Line line, ICurve boundary)
    {
        BooleanLine(line, boundary, out var linesInside, out var _);
        return linesInside;
    }

    public static Point ControlPointCentroid(this ICurve curve)
    => curve.IControlPoints().Average();
    public static ICurve IClose(this ICurve curve, double tolerance = 1E-06)
    {
        if (curve.IIsClosed(tolerance) || curve.IIsLinear()) return curve;

        Line closer = Create.Line(curve.IEndPoint(), curve.IStartPoint());
        var crvList = new List<ICurve> { closer as ICurve };
        crvList.AddRange(curve.ISubParts());

        ICurve polyCurve = Create.PolyCurve(crvList) as ICurve;
        return polyCurve;
    }

    public static bool IsPoint(this ICurve curve)
    {
        var points = curve.IControlPoints();
        var firstPoint = points[0];

        for (int i = 1; i < points.Count; i++) 
            if (!points[i].AlmostEquals(firstPoint))
                return false;

        return true;
    }

    public static ICurve? Thicken(
        this ICurve curve,
        double offset,
        EndCapType cap,
        Vector normal,
        bool tangentExtensions = false,
        double tolerance = 1E-06)
    {
        if (cap != EndCapType.Butt) throw new NotImplementedException();
        ICurve curve1, curve2;

        if(curve is Polyline && curve.IControlPoints().Count == 2)
        {
            var line = Create.Line(curve.IStartPoint(), curve.IEndPoint());

            // TODO: Wrap this in the IOffset method as an extension
            curve1 = line.Offset(offset, normal, tangentExtensions, tolerance);

            curve2 = line
                .Offset(-offset, normal, tangentExtensions, tolerance)
                .Flip();
        }
        if(curve.IIsClosed())
        {
            List<ICurve> parts = new();
            curve1 = curve.OffsetAndMaintainClosure(-offset, normal, tangentExtensions, tolerance);
            curve2 = curve.OffsetAndMaintainClosure(+offset, normal, tangentExtensions, tolerance);

            bool badCurve1 = curve1 == null || curve1.IsPoint() || curve1.IArea().AlmostZero();
            bool badCurve2 = curve2 == null || curve2.IsPoint() || curve2.IArea().AlmostZero();

            if (!badCurve1)
                parts.Add(curve1!);
            if (!badCurve2)
                parts.Add(curve2!);
            if (parts.Count == 0) 
                return null;

            // Prevent polygon to polyline conversion
            if (curve is Polygon)
                for(int i = 0; i< parts.Count; i++)
                    if (parts[i] is Polyline pline)
                        parts[i] = pline.ClosedPolylineToPolygon();

            return parts.Count == 1 ?
                parts[0]
                : Create.PolyCurve(parts);
        }
        else
        {
            curve1 = curve.IOffsetSafe(offset, normal, tangentExtensions, tolerance);
            curve2 = curve
                .IOffsetSafe(-offset, normal, tangentExtensions, tolerance)
                .IFlip();
        }
                
        var cap1 = Create.Line(curve1.IEndPoint(), curve2.IStartPoint());
        var cap2 = Create.Line(curve2.IEndPoint(), curve1.IStartPoint());
        var combined = Create.PolyCurve(new ICurve[] { curve1, cap1, curve2, cap2 });
        return combined;
    }

    public static List<ICurve> Split(this ICurve curve,
        IEnumerable<ICurve> splitters,
        double tolerance = 1E-06)
    {
        splitters = splitters.Where(c => c != null).ToList();

        if (splitters.Count() == 0 || !curve.IIsPlanar(tolerance))
            return [curve];
        
        var points = splitters
                .SelectMany(crv => curve.ICurveIntersections(crv))
                .ToList();

        var fragments = curve.ISplitAtPoints(points)
            .ToList();

        return fragments;
    }

    public static Point Mid(this ICurve curve)
        => curve.IPointAtParameter(0.5);

    /// <summary>
    /// Sequentially removes end points within a boundary. Useful for creating correct split results
    /// </summary>
    private static ICurve? RemoveTerminalPointsRecursive(
        ICurve curve, 
        BoundaryCurve bounds)
    {
        List<Point> points = curve.IControlPoints();
        
        if (curve.IIsClosed()) return curve;

        for(int i = 0; i< points.Count; i++)
            if (bounds.IIsContaining([points[i]])) ;

        return curve;
    }
    public static double DistanceToEnds(this Point point, ICurve curve)
    {
        double d1 = point.Distance(curve.IStartPoint());
        double d2 = point.Distance(curve.IEndPoint());

        if (d1 < d2)
            return d1;
        else
            return d2;
    }

    public static List<BoundaryCurve> SplitIntoClosedCurves2(this BoundaryCurve curve, 
        List<ICurve> splitters,
        double tolerance = 1E-06)
    {
        splitters = splitters
            .Where(splitter => !curve.IIsContaining(splitter, false)
            && !splitter.IIsContaining(curve))
            // Self intersecting splitters are an edge case that cant be handled
            .Where(splitter => !splitter.IIsSelfIntersecting())
            .Where(c => c != null)
            .ToList();

        if (splitters.Count == 0) return [curve];

        List<BoundaryCurve> input = new() { curve };
        List<BoundaryCurve> result = new() { curve };
        
        for (int i = 0; i < splitters.Count; i++)
        {
            var splitter = splitters[i];
            result = new();

            for (int j = 0; j < input.Count; j++)
            {
                var boundary = input[j];
                List<BoundaryCurve> boundaryFragments = new();

                var intersections = boundary.ICurveIntersections(splitter);

                var splitterFragments = splitter.ISplitAtPoints(intersections);
                var splittersToMerge = splitterFragments
                    .Where(crv => curve.IIsContaining(crv))
                    .ToList();

                var boundaryCurveFragments = boundary.ISplitAtPoints(intersections);

                if(boundaryCurveFragments.Count < 2)
                {
                    result.Add(boundary);
                    continue;
                }

                // Try creating a closed fragment out of each if possible
                foreach (var fragment in boundaryCurveFragments)
                {
                    ICurve? closurePiece = splittersToMerge
                        .Find(crv =>
                        crv.IStartPoint().DistanceToEnds(fragment).AlmostZero()
                        && crv.IEndPoint().DistanceToEnds(fragment).AlmostZero()
                        && boundary.IIsContaining([crv.IPointAtParameter(0.5)]));

                    if (closurePiece == null || closurePiece.SharesStartMidAndEnd(fragment)) continue;

                    if (!closurePiece.IStartPoint().Distance(fragment.IEndPoint())
                        .AlmostZero())
                        closurePiece = closurePiece.IFlip();

                    var closedFragment = new List<ICurve>()
                    {
                        fragment, closurePiece
                    };

                    boundaryFragments.Add(Create.BoundaryCurve(closedFragment));
                }

                // Use difference to find any remaining pieces
                var remaining = boundary.BooleanDifference(boundaryFragments)
                    .Select(pCrv => Create.BoundaryCurve(pCrv.Curves))
                    .ToList();

                boundaryFragments.AddRange(remaining);

                result.AddRange(boundaryFragments);
            }

            // After each splitter is used transfer the boundary pieces into the input
            input = result.Count > 0? result: input;
        }
        return result;
    }

    public static bool SharesStartMidAndEnd(
        this ICurve curve, 
        ICurve curve2,
        double tolerance = 1E-06)
    {
        if (!curve.IStartPoint().IsEqual(curve2.IStartPoint(), tolerance)
            && !curve.IStartPoint().IsEqual(curve2.IEndPoint(), tolerance))
            return false;

        if (!curve.IEndPoint().IsEqual(curve2.IStartPoint(), tolerance)
            && !curve.IEndPoint().IsEqual(curve2.IEndPoint(), tolerance))
            return false;

        return curve.IPointAtParameter(0.5).IsEqual(curve2.IPointAtParameter(0.5), tolerance);
    }
    
    public static BoundaryCurve? ToBoundaryCurve(this ICurve curve)
    {
        if (curve is BoundaryCurve bc)
            return bc;
        
        else if(curve is Line
            || !curve.IIsClosed() 
            || !curve.IIsPlanar())
            return null;

        else if (curve is PolyCurve pCrv
            && pCrv.IIsClosed()
            && pCrv.IsPlanar())
            return Create.BoundaryCurve(pCrv.Curves);

        else 
            return Create.BoundaryCurve(new() { curve});
    }
    public static PolyCurve ToPolycurve(this ICurve curve)
    {
        if (curve is PolyCurve pc)
            return pc;

        else if (curve is BoundaryCurve bc)
            return Create.PolyCurve(bc.Curves);

        return Create.PolyCurve([curve]);
    }
    private static List<ICurve>? ToArcsAndLines(this ICurve curve)
    {
        if (curve is NurbsCurve) return null;

        if (curve is Line
           || curve is Arc)
            return new() { curve };

        if (curve is Polyline polyline)
            return polyline.Segments().Cast<ICurve>().ToList();

        if (curve is Polygon polygon)
            return polygon.ISubParts().ToList();

        if(curve is not PolyCurve && curve is not BoundaryCurve) return null;

        List<ICurve> arcsAndLines = new();
        var subParts = curve.ISubParts();
        foreach (var subPart in subParts)
        {
            if (subPart is Line
                || subPart is Arc)
                arcsAndLines.Add(subPart);
            
            else if (subPart is PolyCurve
                    || subPart is Polygon
                    || subPart is Polyline)
            {
                var crvsWithin = subPart.ToArcsAndLines();
                
                if(crvsWithin is not null)
                    arcsAndLines.AddRange(crvsWithin);
            }
        }

        // Returns null if unsupported curves were only present
        return arcsAndLines.Count > 0 ?
            arcsAndLines : null;
    }
    private static List<ICurve>? ToArcsLinesPolylines(this ICurve curve)
    {
        if (curve is Polyline 
            || curve is Line
            || curve is Arc) 
            return new() { curve };

        if (curve is Polygon polygon)
            return [polygon.IToPolyline()];

        List<ICurve> arcsLinesPolylines = new();

        if (curve is not PolyCurve) return new();

        var subParts = curve.ISubParts();
        foreach (var subPart in subParts)
        {
            if(subPart is Line
                || subPart is Arc
                || subPart is Polyline)
                arcsLinesPolylines.Add(subPart);

            if(subPart is PolyCurve)
            {
                var crvsWithin = subPart.ToArcsLinesPolylines();
                arcsLinesPolylines.AddRange(crvsWithin);
            }
        }

        // Returns null if unsupported curves were only present
        return arcsLinesPolylines.Count > 0?
            arcsLinesPolylines:null;
    }


    //public static List<ICurve> Split(
    //    this ICurve curve,
    //    ICurve divider,
    //    bool closeFragments = true,
    //    double tolerance = 1E-06)
    //{
    //    List<ICurve> splitCurves = new();

    //    if (!divider.IIsClosed())
    //    {
    //        var thickened = divider.IOffset(tolerance * 4, EndCapType.Butt);

    //        var remaining = Compute.BooleanDifference(curve,
    //            thickened.ObjectToList());

    //        if (remaining.Count == 1)
    //            splitCurves.Add(curve);
    //        else
    //            splitCurves.AddRange(remaining);
    //    }
    //    else
    //    {
    //        var insideDivider = Compute.BooleanIntersection(curve, divider);
    //        if (!insideDivider.Any())
    //            splitCurves.Add(curve);
    //        else
    //        {
    //            var outsideDivider = Compute.BooleanDifference(curve, divider.ObjectToList());
    //            splitCurves.AddRange(insideDivider);
    //            splitCurves.AddRange(outsideDivider);
    //        }
    //    }

    //    return splitCurves;
    //}

    public static bool IIsContainingAny(this ICurve curve, List<Point> points, bool acceptOnEdge = true, double tolerance = 1E-06)
    {
        foreach (Point point in points)
        {
            if (curve.IIsContaining([point], acceptOnEdge, tolerance))
                return true;
        }

        return false;
    }

    public static List<ICurve> Trim(
        this ICurve curve,
        List<Polygon> splitters, 
        bool removeOnEdge = true)
    {
        var intersections = curve.ICurveIntersections(splitters);
        var fragments = curve.ISplitAtPoints(intersections);

        var outsideFragments = new List<ICurve>();
        foreach(var fragment in fragments)
        {
            bool valid = true;
            foreach(var splitter in splitters)
            {
                var isWithin = splitter.IIsContaining([fragment.Mid()], acceptOnEdge: removeOnEdge);
             
                if(isWithin)
                {
                    valid = false;
                    break;
                }

            }

            if(valid)
                outsideFragments.Add(fragment);
        }

        return outsideFragments;
    }
}
