using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.DebugViz;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Models.UrbanGraph;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using AIQ.Primitives.Extensions;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.Tests.Models.UrbanGraph;

[RhinoTestFixture]
public class GraphCreationStepTests
{
    #region testdata
    [SetUp]
    public void SetUp() => UrbanModel.Instance.Reset();

    private record TransitGraphTestData(
        string CaseName,
        (double X, double Y)[][] InputCoords,
        NodeDescriptor[] ExpectedNodes,
        EdgeDescriptor[] ExpectedEdges);

    private record ParcelGraphTestData(
        string CaseName,
        (double X, double Y)[][] TransitCoords,
        int ExpectedNodeCount,
        int ExpectedEdgeCount,
        int ExpectedParcelNodeCount,
        int ExpectedParcelEdgeCount);

    private record BuildingGraphTestData(
        string CaseName,
        (double X, double Y)[][] TransitCoords,
        List<BuildingData> Buildings,
        int ExpectedNodeCount,
        int ExpectedEdgeCount,
        int ExpectedParcelNodeCount,
        int ExpectedBuildingNodeCount,
        int ExpectedParcelBuildingEdgeCount);

    private record NodeDescriptor(double X, double Y);

    private record EdgeDescriptor(
        double StartX, double StartY,
        double EndX, double EndY);

    private static NodeDescriptor N(double x, double y) => new(x, y);
    private static EdgeDescriptor E(double sx, double sy, double ex, double ey) =>
        new(sx, sy, ex, ey);

    private static List<TransitGraphTestData> BuildTestData()
    {
        return
        [
            new("EmptyTransitList",
                InputCoords: [],
                ExpectedNodes: [],
                ExpectedEdges: []),

            new("SingleTransit",
                InputCoords: [[(0, 0), (10, 0)]],
                ExpectedNodes: [N(0, 0), N(10, 0)],
                ExpectedEdges: [E(0, 0, 10, 0)]),

            new("NoIntersections",
                InputCoords: [
                    [(0, 0), (10, 0)],
                    [(0, 5), (10, 5)],
                ],
                ExpectedNodes: [N(0, 0), N(10, 0), N(0, 5), N(10, 5)],
                ExpectedEdges: [
                    E(0, 0, 10, 0),
                    E(0, 5, 10, 5),
                ]),

            new("TwoCrossingLines",
                InputCoords: [
                    [(0, 0), (10, 0)],
                    [(5, -5), (5, 5)],
                ],
                ExpectedNodes: [N(5, 0), N(0, 0), N(10, 0), N(5, -5), N(5, 5)],
                ExpectedEdges: [
                    E(0, 0, 5, 0),
                    E(5, 0, 10, 0),
                    E(5, -5, 5, 0),
                    E(5, 0, 5, 5),
                ]),

            new("ThreeLinesCommonJunction",
                InputCoords: [
                    [(-5, 0), (5, 0)],
                    [(0, -5), (0, 5)],
                    [(-5, -5), (5, 5)],
                ],
                ExpectedNodes: [N(0, 0), N(-5, 0), N(5, 0), N(0, -5), N(0, 5), N(-5, -5), N(5, 5)],
                ExpectedEdges: [
                    E(-5, 0, 0, 0),
                    E(0, 0, 5, 0),
                    E(0, -5, 0, 0),
                    E(0, 0, 0, 5),
                    E(-5, -5, 0, 0),
                    E(0, 0, 5, 5),
                ]),

            new("SquareWithCross",
                InputCoords: [
                    [(-5, -5), (5, -5)],
                    [(5,-5), (5, 5)],
                    [(5,5), (-5, 5)],
                    [(-5,5),(-5, -5)],
                    [(-5, 0), (5, 0)],
                    [(0, -5), (0, 5)],
                ],
                ExpectedNodes: [
                    N(-5, 0), 
                    N(5, 0),
                    N(0, -5),
                    N(0, 5),
                    N(0, 0),
                    N(-5, -5), 
                    N(5, -5), 
                    N(5, 5), 
                    N(-5, 5),
                ],
                ExpectedEdges: [
                    E(-5, -5, 0, -5),
                    E(0, -5, 5, -5),
                    E(5, -5, 5, 0),
                    E(5, 0, 5, 5),
                    E(5, 5, 0, 5),
                    E(0, 5, -5, 5),
                    E(-5, 5, -5, 0),
                    E(-5, 0, -5, -5),

                    E(-5, 0, 0, 0),
                    E(0, 0, 5, 0),
                    E(0, -5, 0, 0),
                    E(0, 0, 0, 5),
                ]),
        ];
    }

    private static readonly Lazy<List<TransitGraphTestData>> _testData = new(BuildTestData);
    private static readonly Lazy<List<ParcelGraphTestData>> _parcelTestData = new(BuildParcelTestData);
    private static readonly Lazy<List<BuildingGraphTestData>> _buildingTestData = new(BuildBuildingTestData);
    #endregion

    [TestCase("EmptyTransitList")]
    [TestCase("SingleTransit")]
    [TestCase("NoIntersections")]
    [TestCase("TwoCrossingLines")]
    [TestCase("ThreeLinesCommonJunction")]
    [TestCase("SquareWithCross")]
    public void Run_with_transits_produces_correct_graph(string testName)
    {
        // Arrange
        var data = _testData.Value.Single(d => d.CaseName == testName);
        AddTransits(data.InputCoords);

        // Act
        var graph = RunGraphCreationStep();

        // Visualise
#if DEBUG
        graph.PreviewViaHub();
#endif

        // Assert
        Assert.That(graph.Nodes.Count, Is.EqualTo(data.ExpectedNodes.Length),
            $"{data.CaseName}: node count mismatch. Expected {data.ExpectedNodes.Length}, got {graph.Nodes.Count}");
        Assert.That(graph.Edges.Count, Is.EqualTo(data.ExpectedEdges.Length),
            $"{data.CaseName}: edge count mismatch. Expected {data.ExpectedEdges.Length}, got {graph.Edges.Count}");

        foreach(var node in data.ExpectedNodes)
        {
            bool pass = graph.Nodes.Exists(n => n.Point.X.AlmostEquals(node.X) && n.Point.Y.AlmostEquals(node.Y));
            Assert.True(pass, $"Node with X:{node.X} and Y:{node.Y} not found");
        }

        foreach (var edge in data.ExpectedEdges)
        {
            bool startPass = graph.Edges.Exists(e => e.Curve.Value.PointAtStart.X.AlmostEquals(edge.StartX) && e.Curve.Value.PointAtStart.Y.AlmostEquals(edge.StartY));
            Assert.True(startPass, $"Edge with starts X:{edge.StartX} and Y:{edge.StartY} not found");

            bool endPass = graph.Edges.Exists(e => e.Curve.Value.PointAtEnd.X.AlmostEquals(edge.EndX) && e.Curve.Value.PointAtEnd.Y.AlmostEquals(edge.EndY));
            Assert.True(endPass, $"Edge with X:{edge.EndX} and Y:{edge.EndY} not found");
        }
    }

    [TestCase("SingleParcel")]
    public void Run_with_parcels_produces_correct_graph(string testName)
    {
        // Arrange
        var data = _parcelTestData.Value.Single(d => d.CaseName == testName);
        AddTransits(data.TransitCoords);
        RunParcelCreationSteps();

        // Act
        var graph = RunGraphCreationStep();

        // Visualise
#if DEBUG
        graph.PreviewViaHub();
#endif

        // Assert
        Assert.That(graph.Nodes, Has.Count.EqualTo(data.ExpectedNodeCount));
        Assert.That(graph.Edges, Has.Count.EqualTo(data.ExpectedEdgeCount));
        Assert.That(graph.Nodes.Count(x => x.SourceObject is Parcel), Is.EqualTo(data.ExpectedParcelNodeCount));
        Assert.That(graph.Edges.Count(x => x.Start.SourceObject is Parcel || x.End.SourceObject is Parcel), Is.EqualTo(data.ExpectedParcelEdgeCount));
    }

    [TestCase("ContainedAndUncontainedBuildings")]
    public void Run_with_buildings_produces_correct_graph(string testName)
    {
        // Arrange
        var data = _buildingTestData.Value.Single(d => d.CaseName == testName);
        AddTransits(data.TransitCoords);
        RunParcelCreationSteps();
        AddBuildings(data.Buildings);

        // Act
        var graph = RunGraphCreationStep();

        // Visualise
#if DEBUG
        graph.PreviewViaHub();
#endif

        // Assert
        Assert.That(graph.Nodes, Has.Count.EqualTo(data.ExpectedNodeCount));
        Assert.That(graph.Edges, Has.Count.EqualTo(data.ExpectedEdgeCount));
        Assert.That(graph.Nodes.Count(x => x.SourceObject is Parcel), Is.EqualTo(data.ExpectedParcelNodeCount));
        Assert.That(graph.Nodes.Count(x => x.SourceObject is Building), Is.EqualTo(data.ExpectedBuildingNodeCount));
        Assert.That(graph.Edges.Count(x => x.Start.SourceObject is Parcel && x.End.SourceObject is Building), Is.EqualTo(data.ExpectedParcelBuildingEdgeCount));
    }

    private static List<ParcelGraphTestData> BuildParcelTestData()
    {
        return
        [
            new(
                CaseName: "SingleParcel",
                TransitCoords: MakeSquareTransitCoords(),
                ExpectedNodeCount: 5,
                ExpectedEdgeCount: 12,
                ExpectedParcelNodeCount: 1,
                ExpectedParcelEdgeCount: 8),
        ];
    }

    private static List<BuildingGraphTestData> BuildBuildingTestData()
    {
        return
        [
            new(
                CaseName: "ContainedAndUncontainedBuildings",
                TransitCoords: MakeSquareTransitCoords(),
                Buildings:
                [
                    new BuildingData(MakeFootprint(5, 15, 25, 25), 3.5, 2, DefaultUses.Residential),
                    new BuildingData(MakeFootprint(60, 60, 70, 70), 3.5, 2, DefaultUses.Commercial),
                ],
                ExpectedNodeCount: 7,
                ExpectedEdgeCount: 13,
                ExpectedParcelNodeCount: 1,
                ExpectedBuildingNodeCount: 2,
                ExpectedParcelBuildingEdgeCount: 1),
        ];
    }

    private static void AddTransits((double X, double Y)[][] transitCoords)
    {
        var transitData = transitCoords
            .Select(coords => new TransitData(Curve2D.Create(coords.ToList()), 0.5, TransitType.Road))
            .ToList();
        IUrbanModelStep addStep = new AddTransitStep();

        addStep.Run(UrbanModel.Instance, transitData.Cast<ICreationData>().ToList());
    }

    private static void AddBuildings(List<BuildingData> buildingData)
    {
        IUrbanModelStep addStep = new AddBuildingStep();
        addStep.Run(UrbanModel.Instance, buildingData.Cast<ICreationData>().ToList());
    }

    private static void RunParcelCreationSteps()
    {
        var unionerStep = new TransitSurfaceUnioner();
        unionerStep.Run(UrbanModel.Instance);

        var parcelsStep = new RecalculateParcelsStep();
        parcelsStep.Run(UrbanModel.Instance);
    }

    private static Graph RunGraphCreationStep()
    {
        var graphStep = new GraphCreationStep();
        graphStep.Run(UrbanModel.Instance);
        return UrbanModel.Instance.UrbanGraph;
    }

    private static (double X, double Y)[][] MakeSquareTransitCoords()
    {
        return [
            [(0, 0), (30, 0)],
            [(30, 0), (30, 30)],
            [(30, 30), (0, 30)],
            [(0, 30), (0, 0)],
        ];
    }

    private static Polygon2D MakeFootprint(double minX, double minY, double maxX, double maxY)
    {
        return new Polygon2D(new PolylineCurve([
            new Point3d(minX, minY, 0),
            new Point3d(maxX, minY, 0),
            new Point3d(maxX, maxY, 0),
            new Point3d(minX, maxY, 0),
            new Point3d(minX, minY, 0),
        ]));
    }
}
