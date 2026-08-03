using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models.GeometryElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class AddParcelBoundaryStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("SingleBoundary")]
    [TestCase("MultipleBoundaries")]
    [TestCase("EmptyList")]
    public void Run_AddsParcelBoundariesToModel(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        IUrbanModelStep step = new AddParcelBoundaryStep();

        // Act
        var result = step.Run(UrbanModel.Instance, testData.Input.Cast<IModelEditData>().ToList());

        // Assert
        Assert.That(UrbanModel.Instance.ParcelBoundaries, Has.Count.EqualTo(testData.ExpectedCount));
        Assert.That(result.AddedIds, Has.Count.EqualTo(testData.ExpectedGeometryIdCount));
        Assert.That(result.DeletedIds, Is.Empty);

        if (testData.ExpectedCount > 0)
        {
            var allGeometryIds = UrbanModel.Instance.ParcelBoundaries
                .SelectMany(x => x.GetGeometryIds())
                .OrderBy(x => x)
                .ToList();
            var returnedIds = result.AddedIds.OrderBy(x => x).ToList();
            Assert.That(returnedIds, Is.EqualTo(allGeometryIds));
        }
    }

    private record AddParcelBoundaryTestData(
        string CaseName,
        List<ParcelBoundaryData> Input,
        int ExpectedCount,
        int ExpectedGeometryIdCount);

    private static Curve2D MakeOpenCurve(Point3d from, Point3d to)
        => new(new LineCurve(from, to), Guid.NewGuid());

    private static Curve2D MakeClosedCurve(Point3d[] corners)
        => new(new PolylineCurve(corners.Append(corners[0]).ToArray()), Guid.NewGuid());

    private static readonly List<AddParcelBoundaryTestData> _testData = BuildTestData();

    private static List<AddParcelBoundaryTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "SingleBoundary",
                Input:
                [
                    new ParcelBoundaryData(MakeOpenCurve(new(0, 0, 0), new(10, 0, 0))),
                ],
                ExpectedCount: 1,
                ExpectedGeometryIdCount: 1),

            new(CaseName: "MultipleBoundaries",
                Input:
                [
                    new ParcelBoundaryData(MakeOpenCurve(new(0, 0, 0), new(10, 0, 0))),
                    new ParcelBoundaryData(MakeClosedCurve([
                        new Point3d(2, 2, 0),
                        new Point3d(8, 2, 0),
                        new Point3d(8, 8, 0),
                        new Point3d(2, 8, 0),
                    ])),
                ],
                ExpectedCount: 2,
                ExpectedGeometryIdCount: 2),

            new(CaseName: "EmptyList",
                Input: [],
                ExpectedCount: 0,
                ExpectedGeometryIdCount: 0),
        ];
    }
}
