using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Elements;
using static ArchSitePlan.Core.Extensions.Enums;

namespace ArchSitePlan.Core.Extensions
{
    public static class ICurveExtensions
    {
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

        public static ICurve Thicken(
            this ICurve curve,
            double offset,
            EndCapType cap,
            Vector? normal = null,
            bool tangentExtensions = false,
            double tolerance = 1E-06)
        {
            var type = curve.GetType();
            switch(cap)
            {

                case EndCapType.Round:
                    throw new NotImplementedException();

                // EndCapType.Butt and None:
                default:
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
                    else
                    {
                        curve1 = curve.IOffset(offset, normal, tangentExtensions, tolerance);
                        curve2 = curve
                            .IOffset(-offset, normal, tangentExtensions, tolerance)
                            .IFlip();
                    }
                    

                    var cap1 = Create.Line(curve1.IEndPoint(), curve2.IStartPoint());
                    var cap2 = Create.Line(curve2.IEndPoint(), curve1.IStartPoint());
                    var combined = Create.PolyCurve(new ICurve[] { curve1, cap1, curve2, cap2 });
                    return combined;
            }
        }
        public static List<ICurve> Split(this ICurve curve,
            IEnumerable<ICurve> splitters,
            double tolerance = 1E-06)
        {
            splitters = splitters.Where(c => c != null).ToList();

            if (splitters.Count() == 0 || !curve.IIsPlanar(tolerance))
                return curve.ObjectToList();
            
            var points = splitters
                    .SelectMany(crv => curve.ICurveIntersections(crv))
                    .ToList();

            var fragments = curve.ISplitAtPoints(points)
                .ToList();

            return fragments;
        }

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
            {
                if (bounds.IIsContaining(points[i].ObjectToList())) ;

            }

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

            if (splitters.Count == 0) return curve.ObjectToList();

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
                            && boundary.IIsContaining(crv.IPointAtParameter(0.5).ObjectToList()));

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

            return Create.PolyCurve(new ICurve[] { curve });
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
                return (polygon.IToPolyline() as ICurve).ObjectToList();

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
    }
}
