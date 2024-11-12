using BH.Engine.Geometry;
using BH.oM.Geometry;
using SixLabors.ImageSharp.Drawing;
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
        public static List<PolyCurve>? Split(this ICurve curve,
            IEnumerable<ICurve> splitters,
            double tolerance = 1E-06)
        {
            splitters = splitters.Where(c => c != null).ToList();

            if (curve == null) return null;

            if (splitters.Count() == 0)
                return new List<PolyCurve> { curve.ToPolycurve() };

            if (!curve.IIsPlanar(tolerance))
                return Create.PolyCurve(curve.ObjectToList()).ObjectToList();

            if (!curve.IIsClosed())
            {
                var points = splitters
                    .SelectMany(crv => curve.ICurveIntersections(crv))
                    .ToList();

                var fragments = curve.ISplitAtPoints(points)
                    .Select(crv => Create.PolyCurve(crv.ObjectToList()))
                    .ToList();

                return fragments;
            }

            Vector normal = curve.INormal(tolerance);
            double toleranceEnhancer = 0.01;

            var splittersAsArcsLinesAndPlines = splitters
                .SelectMany(s => s.ToArcsLinesPolylines())
                .Where(x => x is not null)
                .ToList();

            var openSplittersThickened = splittersAsArcsLinesAndPlines
                .Where(crv => crv is not null && !crv.IIsClosed())
                .Select(s => s.Thicken(
                    offset: tolerance,
                    cap: EndCapType.Butt,
                    tolerance:  tolerance * toleranceEnhancer,
                    normal: normal))
                .ToList();

            
            var fragments1 = curve.BooleanDifference(
                openSplittersThickened, 
                tolerance * toleranceEnhancer);

            var closedSplitters = splittersAsArcsLinesAndPlines
                .Where(crv => crv is not null && crv is Polyline && crv.IIsClosed())
                .ToList();

            //return fragments1;

///////////////////////////////////////////////////

            List<PolyCurve> fragments2 = new();
            foreach(var splitter in closedSplitters)
            {
                List<PolyCurve> ongoingSplit = new();
                for(int i = 0; i< fragments1.Count; i++)
                {
                    var p1 = fragments1[i].BooleanDifference(
                    splitter.ObjectToList(),
                    tolerance);

                    if (p1 != null) ongoingSplit.AddRange(p1);

                    var p2 = fragments1[i].BooleanIntersection(
                    splitter,
                    tolerance);

                    if (p2 != null) ongoingSplit.AddRange(p2);
                }

                fragments1 = ongoingSplit;
            }
            return fragments1;
        }
        public static List<ICurve> OffsetBothSides(this ICurve curve, double offset)
        {
            var c1 = curve.IOffset(offset);
            var c2 = curve.IOffset(-offset);

            return new List<ICurve>() { c1, c2 };
        }
        public static PolyCurve? ToPolycurve(this ICurve curve)
        {
            if (curve is null)
                return null;

            if (curve is PolyCurve)
                return curve as PolyCurve;

            return Create.PolyCurve(new ICurve[] { curve });
        }
        private static List<ICurve>? ToArcsLinesPolylines(this ICurve curve)
        {
            if (curve is null)
                return null;

            if (curve is Polyline 
                || curve is Line
                || curve is Arc) 
                return new() { curve };
            
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

            return arcsLinesPolylines;
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
