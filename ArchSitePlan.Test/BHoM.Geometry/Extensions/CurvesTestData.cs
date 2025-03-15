using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Test.BHoM.Geometry.Extensions
{
    internal static class CurvesTestData
    {
        internal static Line _line1 = Create.Line(Point.Origin, Create.Point(0, 2, 0));
        internal static Line _line2 = Create.Line(Create.Point(2, 0, 0), Point.Origin);
        internal static Arc _arc = Create.ArcByCentre(Point.Origin, Create.Point(0, 2, 0), Create.Point(2, 0, 0));
        internal static List<ICurve> _pCrvList = new List<ICurve>() { _line2, _line1, _arc };

        internal static PolyCurve ArcPolyCurve = Create.PolyCurve(_pCrvList);
        internal static Polygon Rectangle1 = Create.Polygon(new List<Point>
        {
            Point.Origin,
            Create.Point(2,0),
            Create.Point(2,6),
            Create.Point(0,6),
        });
        internal static Polygon Rectangle2 = Create.Polygon(new List<Point>()
        {
            Create.Point(-1,2),
            Create.Point(5,2),
            Create.Point(5,4),
            Create.Point(-1,4),
        });
        internal static Polygon Rectangle3 = Create.Polygon(new List<Point>
        {
            Create.Point(0,2),
            Create.Point(2,2),
            Create.Point(2,4),
            Create.Point(0,4),
        });
        internal static Polygon Rectangle4 = Create.Polygon(new List<Point>
        {
            Create.Point(2,2),
            Create.Point(2,4),
            Create.Point(4,4),
            Create.Point(4,2),
        });

        internal static Polygon Hole = Create.Polygon(
            new() {
            Create.Point(0.5, 0.5),
            Create.Point(1.3, 0.5),
            Create.Point(1.3, 0.7),
            Create.Point(0.5, 0.7)
            });

        internal static Polygon DiffPolygon = Create.Polygon(
            new() {
            Create.Point(1, 1),
            Create.Point(5, 1),
            Create.Point(5, 5),
            Create.Point(1, 5)
            });

        internal static Polyline DiffPolyline = Create.Polyline(
        new List<Point>() {
                Create.Point(0.5, -1),
                Create.Point(0.5, 1),
                Create.Point(1, 1),
                Create.Point(1, -1),
                Create.Point(1.5, -1),
                Create.Point(1.5, 1),
        });

        internal static Polyline DiffPolyline2 = Create.Polyline(
        new List<Point>() {
                Create.Point(0.5, -1),
                Create.Point(0.5, 1),
                Create.Point(1, 1),
                Create.Point(1, -1),
                Create.Point(1.5, -1),
                Create.Point(1.5, 1),
                Create.Point(3, 1),
        });
    }
}
