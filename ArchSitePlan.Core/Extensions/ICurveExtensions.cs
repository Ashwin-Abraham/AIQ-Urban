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
            switch(cap)
            {

                case EndCapType.Round:
                    throw new NotImplementedException();

                default:
                    // EndCapType.Butt and None:
                    var curve1 = curve.IOffset(offset, normal, tangentExtensions, tolerance);
                    var curve2 = curve
                        .IOffset(-offset, normal, tangentExtensions, tolerance)
                        .IFlip();

                    var cap1 = Create.Line(curve1.IEndPoint(), curve2.IStartPoint());
                    var cap2 = Create.Line(curve2.IEndPoint(), curve1.IStartPoint());
                    var combined = Create.PolyCurve(new ICurve[] { curve1, cap1, curve2, cap2 });
                    return combined;
            }
        }
        public static List<PolyCurve>? Split(this ICurve curve,
            List<ICurve> splitters,
            double tolerance = 1E-06)
        {
            splitters = splitters.Where(c => c != null).ToList();

            if (curve == null) return null;

            if (splitters.Count == 0)
                return new List<PolyCurve> { curve.ToPolycurve() };

            if (!curve.IIsPlanar(tolerance))
                throw new Exception("Non planar curves cannot be split");

            Vector normal = curve.INormal(tolerance);
            double toleranceEnhancer = 0.01;

            var splittersExploded = splitters
                .SelectMany(s => s.ToArcsLinesPolylines())
                .Where(x => x is not null)
                .Select(s => s.Thicken(
                    offset: tolerance,
                    cap: EndCapType.Butt,
                    tolerance:  tolerance * toleranceEnhancer,
                    normal: normal))
                .ToList();

            var fragments = curve.BooleanDifference(
                splittersExploded, 
                tolerance * toleranceEnhancer);

            return fragments;
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
