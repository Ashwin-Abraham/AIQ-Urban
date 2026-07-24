using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Models.GeometryElements;

[RhinoTestFixture]
internal class Profile2DTests
{
    [Test]
    [TestCase("Two Arcs")]
    public void CreateArcedProfile(string testName)
    {
        // Arrange
        var testCurves = BuildTestData().Single(x => x.CaseName == testName).Input;
        double tolerence = 1e-5;

        // Act
        var offsets = testCurves
           .SelectMany(x => x.ToNurbsCurve().OffsetBothWays(1, Plane.WorldXY, 1e-5))
           .ToList();

        var brep = Brep.CreatePlanarBreps(offsets, tolerence)[0];
        var result = Profile2D.Create(brep);

        // Preview
        PreviewHub.ResetDoc();
        result.PreviewViaHub(color: Color.Black);

        // Assert
        var area = result.Area();
    }

    private record Profile2DTestData(string CaseName, List<Curve> Input);


    private static List<Profile2DTestData> BuildTestData()
    {
        return
        [
            new Profile2DTestData(
                CaseName: "Two Arcs",
                Input: [
                    new ArcCurve(new Arc(Point3d.Origin, new Point3d(1,2,0), new Point3d(0,4,0))),
                    new ArcCurve(new Arc(new Point3d(1,0,0), new Point3d(0,2,0), new Point3d(1,4,0)))
                ])
        ];
    }

    private static Profile2D GetProfiles(double width, double tolerence = 1e-5, params ArcCurve[] arcs)
    {
        var offsets = arcs
            .SelectMany(x => x.ToNurbsCurve().OffsetBothWays(width, Plane.WorldXY, 1e-5))
            .ToList();

        var brep = Brep.CreatePlanarBreps(offsets, tolerence)[0];

        return Profile2D.Create(brep);
    }
}
