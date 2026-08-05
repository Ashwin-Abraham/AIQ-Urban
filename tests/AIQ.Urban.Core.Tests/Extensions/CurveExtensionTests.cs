using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Extensions;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Extensions;

[RhinoTestFixture]
public class CurveExtensionTests
{

    [Test]
    [TestCase("Line")]
    [TestCase("Wild Spline")]
    [TestCase("Square")]

    public void CanOffsetCurve(string testName)
    {
        // Arrange
        var testData = _offsetTestData.Single(x => x.CaseName == testName);

        //// Act
        var offsetCurves = testData.Curve.OffsetBothWays(testData.OffsetSize, Plane.WorldXY, 1e-5);

#if DEBUG
        // Preview
        PreviewHub.ResetDoc();
        testData.Curve.AddToPreviewDoc(Color.Red);
        offsetCurves.PreviewViaHub();
#endif

        //// Assert
        var results = offsetCurves.OrderBy(x => x.GetLength()).ToList();
        var expectedResults = testData.Expected.OrderBy(x => x.GetLength()).ToList();

        for(int i = 0; i < results.Count && expectedResults.Any(); i++)
        {
            var equal = results[i].ToNurbsCurve().Points.EpsilonEquals(expectedResults[i].ToNurbsCurve().Points, 1e-3);
            Assert.True(equal);
        }

        if(testData.ExpectedLength != null)
            Assert.AreEqual((double)testData.ExpectedLength, offsetCurves.Sum(x => x.GetLength()), 1e-3);
        
    }

    private record OffsetTestData(
        string CaseName,
        double OffsetSize,
        Curve Curve,
        List<Curve> Expected,
        double? ExpectedLength = null);

    private readonly List<OffsetTestData> _offsetTestData = [
        new OffsetTestData(
            CaseName: "Line",
            OffsetSize: 4,
            Curve: new LineCurve(Point3d.Origin, new Point3d(5,0,0)),
            Expected: [
                new PolylineCurve([
                    new Point3d(0,2,0),
                    new Point3d(5,2,0),
                    new Point3d(5,-2,0),
                    new Point3d(0,-2,0),
                    new Point3d(0,2,0),
                    ])
                ]
            ),

        new OffsetTestData(
                CaseName: "Square",
                OffsetSize: 2,
                Curve: new PolylineCurve(new List<Point3d>()
                {
                    new Point3d(1,1,0),
                    new Point3d(5,1,0),
                    new Point3d(5,5,0),
                    new Point3d(1,5,0),
                    new Point3d(1,1,0),
                }),
                Expected: [
                   new PolylineCurve(new List<Point3d>()
                {
                    new Point3d(0,0,0),
                    new Point3d(6,0,0),
                    new Point3d(6,6,0),
                    new Point3d(0,6,0),
                    new Point3d(0,0,0),
                }),
                new PolylineCurve(new List<Point3d>()
                {
                    new Point3d(2,2,0),
                    new Point3d(4,2,0),
                    new Point3d(4,4,0),
                    new Point3d(2,4,0),
                    new Point3d(2,2,0),
                })
                ]),
        new OffsetTestData(
                CaseName: "Wild Spline",
                OffsetSize: 2,
                Curve: Curve.CreateInterpolatedCurve(new List<Point3d>()
                {
                    new Point3d(0,2,0),
                    new Point3d(3,0,0),
                    new Point3d(6,10,0),
                    new Point3d(9,0,0),
                    new Point3d(12,2,0),
                }, 3),
                Expected: [],
                ExpectedLength: 62.3541)];
}
