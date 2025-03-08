using ArchSitePlan.BHoM.Geometry;
using ArchSitePlan.Core.Factories;
using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Test.Core.Models;

public class BuildingTests
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
        var axis = Building.GeneratePrimarySiteAxis(
            Create.PolyCurve([siteBoundary]));

#if DEBUG
        DEBUGMODEL.Reset();
        DEBUGMODEL.AddBoundedCurves(axis.ToHypar(), DebugMaterials.Red);
        DEBUGMODEL.AddBoundedCurves(siteBoundary.ToHypar());
        DEBUGMODEL.OpenAsGlb();
#endif
    }

    [Fact]
    public void GenerateRectangleBuilding()
    {
        var siteBoundary = Create.Polygon([
            Create.Point(0,0),
            Create.Point(200,0),
            Create.Point(225, 300),
            Create.Point(0,100)
        ]);

        var config = new AutoRectangleBuildingData(
            BuildingWidth: 15,
            MinFloorPlateDepth: 5,
            SiteBoundary: Create.PolyCurve([siteBoundary]),
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: null,
            TargetHeight: null,
            Setback: 6,
            MaxHeight: 24,
            MinHeight: 6,
            MaxLength: 50,
            MinLength: 10,
            FloorHeight: 3.6);

        // Test Line Boolean Intersections
        // Then test building

        var building = new RectangleBuildingFactory(config).Generate();

        var gfa = building.Sum(x => x.BUA());
        Assert.Equal(50000, gfa, 1000.0);
    }

    [Fact]
    public void GenerateRectangleBuilding_OddSite()
    {
        var siteBoundary = Create.Polygon([
            Create.Point(0,0),
            Create.Point(200,0),
            Create.Point(400, 300),
            Create.Point(225, 150),
            Create.Point(0, 100),
            Create.Point(-150, 150),
            Create.Point(-50,20)
        ]);

        var config = new AutoRectangleBuildingData(
            BuildingWidth: 15,
            MinFloorPlateDepth: 5,
            SiteBoundary: Create.PolyCurve([siteBoundary]),
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: null,
            TargetHeight: null,
            Setback: 6,
            MaxHeight: 24,
            MinHeight: 6,
            MaxLength: 50,
            MinLength: 10,
            FloorHeight: 3.6);

        // Test Line Boolean Intersections
        // Then test building

        var building = new RectangleBuildingFactory(config).Generate();

        var gfa = building.Sum(x => x.BUA());
        Assert.Equal(50000, gfa, 1000.0);
    }
}
