using ArchSitePlan.Core.Models;
using ArchSitePlan.Core.Pipeline;
using ArchSitePlan.DebugModel;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Elements;

namespace ArchSitePlan.Test.ArchSitePlan.Core;
public class CoreModelEditStepLibraryTests
{
    private static List<Road> GetRoads(List<ICurve> curves)
    {
        return curves
            .Select(x => Create.PolyCurve([x]))
            .Select(x =>
            {
                if (Road.TryCreateRoad(x, Guid.Empty, 2, out var road))
                    return road;
                else
                    return null;
            })
            .Where(x => x!= null)
            .Cast<Road>()
            .ToList();
    }

    [Fact]
    public void CanBuildRoadsAndParcels_4Roads()
    {
        // Arrange
        var lines = new List<ICurve>()
        {
            Create.Line(Create.Point(-2, 0), Create.Point(12, 0)),
            Create.Line(Create.Point(0, -2), Create.Point(0, 12)),
            Create.Line(Create.Point(-2, 10), Create.Point(12, 10)),
            Create.Line(Create.Point(10, 12), Create.Point(10, -2)),
        };

        var roads = GetRoads(lines);

        // Act
        CoreModelEditStepLibrary.CalculateParcelAndRoadBoundaries(
            roads, out var roadSrfs, out var parcelSrfs);

        // Visualize
        roadSrfs.ForEach(x => x.Debug(BuiltInMaterials.Concrete));
        parcelSrfs.ForEach(x => x.Debug(BuiltInMaterials.Topography));
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Single(roadSrfs);
        Assert.Single(parcelSrfs);
    }

    [Fact]
    public void CanBuildRoadsAndParcels_SingleClosedCurve()
    {
        // Arrange
        var curve = new Polygon(
            new List<Point>()
            {
                Create.Point(0, 0),
                Create.Point(10, 0),
                Create.Point(10, 10),
                Create.Point(0, 10),
            });
        
        var roads = GetRoads([curve]);

        // Act
        CoreModelEditStepLibrary.CalculateParcelAndRoadBoundaries(
            roads, out var roadSrfs, out var parcelSrfs);

        // Visualize
        roadSrfs.ForEach(x => x.Debug(BuiltInMaterials.Concrete));
        parcelSrfs.ForEach(x => x.Debug(BuiltInMaterials.Topography));
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Single(roadSrfs);
        Assert.Single(parcelSrfs);
    }

    [Fact]
    public void CanBuildRoadsAndParcels_2OverlappingClosedCurves()
    {
        // Arrange
        var curve1 = new Polygon(
            new List<Point>()
            {
                Create.Point(0, 0),
                Create.Point(10, 0),
                Create.Point(10, 10),
                Create.Point(0, 10),
            });

        var curve2 = new Polygon(
            curve1.Vertices.Select(x => x.Translate(Create.Vector(5, 5))));

        var roads = GetRoads([curve1, curve2]);

        // Act
        CoreModelEditStepLibrary.CalculateParcelAndRoadBoundaries(
            roads, out var roadSrfs, out var parcelSrfs);

        // Visualize
        roadSrfs.ForEach(x => x.Debug(BuiltInMaterials.Concrete));
        parcelSrfs
            .Select((x,i) => x.Translate(Create.Vector(0,0,i*0.5)))
            .ToList()
            .ForEach(x => x.Debug(BuiltInMaterials.Topography));
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Single(roadSrfs);
        Assert.Equal(3, parcelSrfs.Count);
    }

    [Fact]
    public void CanBuildRoadsAndParcels_RoadWithinRoad()
    {
        // Arrange
        var curve1 = new Polygon(
            new List<Point>()
            {
                Create.Point(0, 0),
                Create.Point(30, 0),
                Create.Point(30, 30),
                Create.Point(0, 30),
            });

        var curve2 = Create.Line(
            Create.Point(15, 10),
            Create.Point(25, 10));

        var roads = GetRoads([curve1, curve2]);

        // Act
        CoreModelEditStepLibrary.CalculateParcelAndRoadBoundaries(
            roads, out var roadSrfs, out var parcelSrfs);

        // Visualize
        curve1.Debug(zMovt: .1);
        curve2.Debug(zMovt: .1);
        roadSrfs.ForEach(x => x.Debug(BuiltInMaterials.Concrete));
        parcelSrfs
            .Select((x, i) => x.Translate(Create.Vector(0, 0, i * 0.5)))
            .ToList()
            .ForEach(x => x.Debug(BuiltInMaterials.Topography));
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Equal(2, roadSrfs.Count);
        Assert.Equal(1, parcelSrfs.Count);
    }

    [Fact]
    public void CanBuildRoadsAndParcels_PolygonRoadWithinRoad()
    {
        // Arrange
        var curve1 = new Polygon(
            new List<Point>()
            {
                Create.Point(0, 0),
                Create.Point(50, 0),
                Create.Point(50, 50),
                Create.Point(0, 50),
            });

        var curve2 = new Polygon(
             new List<Point>()
             {
                Create.Point(15, 15),
                Create.Point(35, 15),
                Create.Point(35, 35),
                Create.Point(15, 35),
             });

        var roads = GetRoads([curve1, curve2]);

        // Act
        CoreModelEditStepLibrary.CalculateParcelAndRoadBoundaries(
            roads, out var roadSrfs, out var parcelSrfs);

        // Visualize
        curve1.Debug(zMovt: .1);
        curve2.Debug(zMovt: .1);
        roadSrfs.ForEach(x => x.Debug(BuiltInMaterials.Concrete));
        parcelSrfs
            .Select((x, i) => x.Translate(Create.Vector(0, 0, i * 0.5)))
            .ToList()
            .ForEach(x => x.Debug(BuiltInMaterials.Topography));
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Equal(2, roadSrfs.Count);
        Assert.Equal(2, parcelSrfs.Count);
    }

    [Fact]
    public void CanBuildRoadsAndParcels_PolygonRoadWithin4LinearRoads()
    {
        // Arrange
        var curve1 = Create.Line(Create.Point(0, 0), Create.Point(50, 0));
        var curve2 = Create.Line(Create.Point(50, 0), Create.Point(50, 50));
        var curve3 = Create.Line(Create.Point(50, 50), Create.Point(0, 50));
        var curve4 = Create.Line(Create.Point(0, 50), Create.Point(0, 0));

        var curve5 = new Polygon(
             new List<Point>()
             {
                Create.Point(15, 15),
                Create.Point(35, 15),
                Create.Point(35, 35),
                Create.Point(15, 35),
             });

        var curves = new List<ICurve>() { curve1, curve2, curve3, curve4, curve5};
        var roads = GetRoads(curves);

        // Act
        CoreModelEditStepLibrary.CalculateParcelAndRoadBoundaries(
            roads, out var roadSrfs, out var parcelSrfs);

        // Visualize
        curves.ForEach(x => x.Debug(zMovt: 0.1));
        int parcelCount = parcelSrfs.Count;
        roadSrfs
            .Select((x, i) => x.Translate(Create.Vector(0, 0, (i + parcelCount) * 0.5)))
            .ToList()
            .ForEach(x => x.Debug(BuiltInMaterials.Concrete));

        parcelSrfs
            .Select((x, i) => x.Translate(Create.Vector(0, 0, i * 0.5)))
            .ToList()
            .ForEach(x => x.Debug(BuiltInMaterials.Topography));
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Equal(2, roadSrfs.Count);
        Assert.Equal(2, parcelSrfs.Count);
    }

    [Fact]
    public void CanBuildRoadsAndParcels_3CurvesSpecialCase()
    {
        // Arrange
        var curve1 = Create.Polyline(
            new List<Point>()
            {
                Create.Point(0, 0),
                Create.Point(25, 0),
                Create.Point(25, 10),
                Create.Point(0, 10),
            });

        var curve2 = Create.Line(Create.Point(2, -2), Create.Point(2, 12));
        var curve3 = Create.Line(Create.Point(15, 0), Create.Point(15, 18));

        var roads = GetRoads([curve1, curve2, curve3]);

        // Act
        CoreModelEditStepLibrary.CalculateParcelAndRoadBoundaries(
            roads, out var roadSrfs, out var parcelSrfs);

        // Visualize
        roadSrfs.ForEach(x => x.Debug(BuiltInMaterials.Concrete));
        parcelSrfs
            .Select((x, i) => x.Translate(Create.Vector(0, 0, i * 0.5)))
            .ToList()
            .ForEach(x => x.Debug(BuiltInMaterials.Topography));
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Single(roadSrfs);
        Assert.Equal(3, parcelSrfs.Count);
    }

    [Fact]
    public void CanBuildRoadsAndParcels_SelfEnclosedCurvesSpecialCase()
    {
        // Arrange
        var curve1 = Create.Polygon(
            new List<Point>()
            {
                Create.Point(0, 0),
                Create.Point(25, 0),
                Create.Point(25, 25),
                Create.Point(0, 25),
            });

        var curve2 = Create.Polygon(
            new List<Point>()
            {
                        Create.Point(5, 5),
                        Create.Point(15, 5),
                        Create.Point(15, 15),
                        Create.Point(5, 15),
            });

        var curve3 = Create.Line(Create.Point(12, -10), Create.Point(12, 30));

        var roads = GetRoads([curve1, curve2, curve3]);

        // Act
        CoreModelEditStepLibrary.CalculateParcelAndRoadBoundaries(
            roads, out var roadSrfs, out var parcelSrfs);

        // Visualize
        roadSrfs
            .Select((x, i) => x.Translate(Create.Vector(0, 0, i * 0.5)))
            .ToList()
            .ForEach(x => x.Debug(BuiltInMaterials.Concrete));
        parcelSrfs
            .Select((x, i) => x.Translate(Create.Vector(0, 0, i * 0.5)))
            .ToList()
            .ForEach(x => x.Debug(BuiltInMaterials.Topography));
        DEBUGMODEL.OpenAsGlb();

        // Assert
        Assert.Single(roadSrfs);
        Assert.Equal(3, parcelSrfs.Count);
    }
}
