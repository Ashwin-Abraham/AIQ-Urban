using ArchSitePlan.Core.Factories;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Test.ArchSitePlan.Core.Factories;

public class BuildingFactoryBaseTests
{
    public static IEnumerable<object[]> GetSiteBoundaries()
    {
        yield return new object[]
         {
             "Rectangle-WorldOriented",
            new Polygon(new List<Point>
            {
                Create.Point(0, 0),
                Create.Point(5, 0),
                Create.Point(5, 2),
                Create.Point(0, 2),
                Create.Point(0, 0)
            }),
            Point.Origin,
            Point.Origin,
            Point.Origin
         };

        yield return new object[]
         {
            "Rectangle-Angled",
            new Polygon(new List<Point>
            {
                Create.Point(2, 0),
                Create.Point(6, 4),
                Create.Point(4, 6),
                Create.Point(0, 2),
                Create.Point(2, 0)
            }),
            Point.Origin,
            Point.Origin,
            Point.Origin
         };

        yield return new object[]
        {
            "RandomSite",
            new Polygon(new List<Point>
            {
                Create.Point(2, 0),
                Create.Point(10, 4),
                Create.Point(7, 6),
                Create.Point(3, 10),
                Create.Point(-3, -6),
                Create.Point(2, 0)
            }),
            Point.Origin,
            Point.Origin,
            Point.Origin
        };

        yield return new object[]
         {
            "LShape",
            new Polygon(new List<Point>
            {
                Create.Point(2, 0),
                Create.Point(10, 0),
                Create.Point(10, 5),
                Create.Point(8, 5),
                Create.Point(8, 2),
                Create.Point(0, 2)
            }),
            Point.Origin,
            Point.Origin,
            Point.Origin
         };

        yield return new object[]
         {
            "3 Sided Star",
            new Polygon(new List<Point>
            {
                Create.Point(2, 0),
                Create.Point(10, 0),
                Create.Point(10, 5),
                Create.Point(8, 5),
                Create.Point(4, 7),
                Create.Point(3, 4),
                Create.Point(-4, 1)
            }),
            Point.Origin,
            Point.Origin,
            Point.Origin
         };
    }

    [MemberData(nameof(GetSiteBoundaries))]
    [Theory]
    public void CanGenerateSiteAxis(
            string caseName, 
            Polygon siteBoundary, 
            Point pointAtParam25,
            Point mid,
            Point pointAtParam75)
    {
        var axis = BuildingLayoutFactoryBase.GeneratePrimarySiteAxis(
            Create.PolyCurve([siteBoundary]));

#if DEBUG
        DEBUGMODEL.Reset();
        axis.Debug(DebugMaterials.Red);
        DEBUGMODEL.Debug(siteBoundary.ToHypar());
        DEBUGMODEL.OpenAsGlb();
#endif
    }

    [Fact]
    public void SplitUpCurveIntoFragments()
    {
        // Arrange
        var curve = Create.PolyCurve([
            Create.Polygon([
                Create.Point(0,0),
                Create.Point(200,0),
                Create.Point(225, 300),
                Create.Point(0,100)
            ])
            ]);

        //Act
        var fragments = PeripheralBuildingFactory
            .ApplySolidVoidPattern(Create.PolyCurve([curve]), 180, 25)
            .ToList();

        // Visualize
        DEBUGMODEL.Reset();
        DEBUGMODEL.Debug(fragments.ToHypar());
        DEBUGMODEL.OpenAsGlb();

        // Assert
        var solidTotal = fragments.Sum(x => x.Length());
        Assert.Equal(777, solidTotal, 1.0);
    }

    [Fact]
    public void SplitUpCurveIntoFragments_ArcPolycurve()
    {
        // Arrange
        var polyline = Create.Polyline([
                Create.Point(0,0),
                Create.Point(200,0),
                Create.Point(225, 300),
                Create.Point(0,100)
            ]);

        var arc = Create.Arc(
            start: Create.Point(0, 100),
            middle: Create.Point(-50, 50),
            end: Create.Point(0, 0));

        var curve = Create.PolyCurve([polyline, arc]);

        //Act
        var fragments = PeripheralBuildingFactory
            .ApplySolidVoidPattern(Create.PolyCurve([curve]), 180, 25)
            .ToList();

        // Visualize
        DEBUGMODEL.Reset();
        DEBUGMODEL.Debug(fragments.ToHypar());
        DEBUGMODEL.OpenAsGlb();

        // Assert
        var solidTotal = fragments.Sum(x => x.Length());
        Assert.Equal(834.15, solidTotal, 0.1);
    }

    [Fact]
    public void SplitUpCurveIntoFragments_Circle()
    {
        // Arrange
        var circle = Create.Circle(Create.Point(10,10), 200 );

        var curve = Create.PolyCurve([circle]);

        //Act
        var fragments = PeripheralBuildingFactory
            .ApplySolidVoidPattern(Create.PolyCurve([curve]), 180, 25)
            .ToList();

        // Visualize
        DEBUGMODEL.Reset();
        DEBUGMODEL.Debug(fragments.ToHypar());
        DEBUGMODEL.OpenAsGlb();

        // Assert
        var solidTotal = fragments.Sum(x => x.Length());
        Assert.Equal(1081.637, solidTotal, 0.1);
    }
}