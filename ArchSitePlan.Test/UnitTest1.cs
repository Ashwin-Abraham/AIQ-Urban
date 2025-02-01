using ArchSitePlan.Core.Extensions;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Vector3 = Elements.Geometry.Vector3;

namespace ArchSitePlan.Test;
public class UnitTest1
{
    [Fact]
    public void CanOffsetLine()
    {
        Line line = Create.Line(Point.Origin, Create.Point(3,5));
        Line offsetLine = line.Offset(1, Vector.ZAxis);

        DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(line));
        DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(offsetLine), DebugMaterials.Red);
        DEBUGMODEL.OpenAsGlb();
    }

    [Fact]
    public void CanOffsetPolyLine()
    {
        var points = new List<Point>()
        {
            Point.Origin,
            Create.Point(12,5),
            Create.Point(-12,5),
            //Create.Point(0,15),
        };

        Polyline pline = Create.Polyline(points);
        //Polyline offset = pline.Offset(1, Vector.ZAxis);
        Polyline offset = pline.IOffset(1, Vector.ZAxis) as Polyline;

        DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(pline));
        DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(offset), DebugMaterials.Red);
        DEBUGMODEL.OpenAsGlb();
    }

    [Fact]
    public void CanCreateAndOffsetPolyCurve()
    {
        Point a = Create.Point(5,0);
        Point b = Create.Point(0, 5);
        Point c = Create.Point(-5, 5);

        Line line = Create.Line(b,c);
        Arc arc = Create.ArcByCentre(Point.Origin, a, b);

        PolyCurve pCurve = Create.PolyCurve(new ICurve[] {arc, line});
        PolyCurve offsetCrv = null;

        if (pCurve.ValidateContinuity())
        {
            offsetCrv = Modify.Offset(pCurve, 1, Vector.ZAxis);
            DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHypar(offsetCrv), DebugMaterials.Red);
        }

        DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHypar(pCurve));
        DEBUGMODEL.OpenAsGlb();
    }

    [Fact]
    public void CanOpenDebugModel()
    {
        var line = new Elements.Geometry.Line(Vector3.Origin, new Vector3(25,0));

        DEBUGMODEL.AddBoundedCurve(line);
        DEBUGMODEL.OpenAsGlb();
    }

    [Fact]
    public void Regressio_PolycurveDiscontinuityPoints()
    {
        //Point a = Create.Point(-9.9989226335647459, 27.740266505432324, 0);
        //Point b = Create.Point(0.2382492669088645, -24.814724558711305, 0);

        Point a = Create.Point(-9.99, 27.7402, 0);
        Point b = Create.Point(0.2382, -24.814, 0);
        Point c = Create.Point(20, -24.814, 0);

        Line line = Create.Line(a, b);
        Line line2 = Create.Line(b, c);

        PolyCurve pCrv = new PolyCurve();
        pCrv.Curves.Add(line);
        pCrv.Curves.Add(line2);

        var end = pCrv.EndPoint();
        var points = pCrv.DiscontinuityPoints();

        var offset = pCrv.Offset(1, Vector.ZAxis);
    }

    [Fact]
    public void CanFilletPolyLine()
    {
        var points = new List<Point>()
        {
            Point.Origin,
            Create.Point(2,5),
            Create.Point(12,8),
            Create.Point(12,15),
        };

        Polyline pline = Create.Polyline(points);
        PolyCurve filletted = pline.Fillet(2);

        //DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(pline));
        DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHypar(filletted), DebugMaterials.Red);
        DEBUGMODEL.OpenAsGlb();
    }

    [Fact]
    public void CanFillet2Lines()
    {
        //Line a = Create.Line(Point.Origin, Create.Point(1, 1));
        //Line b = Create.Line(Create.Point(1, 1), Create.Point(1,0));

        Line a = Create.Line(Point.Origin, Create.Point(5, 4));
        Line b = Create.Line(Create.Point(5, 4), Create.Point(12,0));

        var result = a.ArcBetweenLinesThatMeet(b, 0.3);

        //DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(pline));
        DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHypar(result), DebugMaterials.Red);
        DEBUGMODEL.OpenAsGlb();
    }

    [InlineData(0,0,0,0)]
    [InlineData(0,0,1)]
    [InlineData(33.0,3.0,1)]
    [InlineData(33.0,3.0,-100)]
    [Theory]
    public void CanUnitizeVector(double x, double y, double z, double result = 1)
    {
        Vector v = Create.Vector(x,y,z).Unitized();

        Assert.Equal(result, v.Length(), 1E-05);
    }

    [Fact]
    public void CanCreateArc()
    {
        var arc = Create.Arc(
            Point.Origin, 
            Create.Point(1),
            Create.Point(0,1));

        DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(arc));
        DEBUGMODEL.OpenAsGlb();
    }
}