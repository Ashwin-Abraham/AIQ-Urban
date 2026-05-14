using System.Collections.Generic;
using System.Linq;
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
    [TestCase("Square")]

    public void CanOffsetCurve(string testName)
    {
        // Arrange
        var testData = _offsetTestData.Single(x => x.CaseName == testName);

        //// Act
        var offsetCurves = testData.Curve.OffsetBothWays(testData.OffsetSize, Plane.WorldXY, 1e-5);
        
        // Preview
        var curvePoints = offsetCurves
            .Select(x => x.ToNurbsCurve().Points)
            .Select(ptList => ptList.Select(pt => (pt.X, pt.Y)))
            .ToList();

        //// Assert
        var results = offsetCurves.OrderBy(x => x.GetLength()).ToList();
        var expectedResults = testData.Expected.OrderBy(x => x.GetLength()).ToList();

        for(int i = 0; i < results.Count; i++)
        {
            var equal = results[i].ToNurbsCurve().Points.EpsilonEquals(expectedResults[i].ToNurbsCurve().Points, 1e-3);
            Assert.True(equal);
        }
        
    }

    private record OffsetTestData(
        string CaseName,
        double OffsetSize,
        Curve Curve,
        List<Curve> Expected);

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
                ])
        ];
}
