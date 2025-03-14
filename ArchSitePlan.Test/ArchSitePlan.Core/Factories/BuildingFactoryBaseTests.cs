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
        var axis = BuildingFactoryBase.GeneratePrimarySiteAxis(
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
            .ApplySolidVoidPattern(Create.PolyCurve([curve]), 180, 5)
            .ToList();

        // Visualize
        DEBUGMODEL.Reset();
        DEBUGMODEL.Debug(fragments.ToHypar());
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Equal(50000, 1000.0);
    }
}