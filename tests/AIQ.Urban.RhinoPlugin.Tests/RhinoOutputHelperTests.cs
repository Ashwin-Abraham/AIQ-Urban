using System;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using NUnit.Framework;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests;

[RhinoTestFixture]
public class RhinoOutputHelperTests
{
    [Test]
    public void OutputGeometry_Curve2D_StoredGeometrySurvivesDocModification()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var id = doc.Objects.AddLine(new Line(new Point3d(0, 0, 0), new Point3d(10, 0, 0)));
        var obj = doc.Objects.Find(id);
        var curve = obj!.Geometry as Curve;
        var sut = new Curve2D(curve!, Guid.NewGuid());
        var attrs = new ObjectAttributes();

        RhinoOutputHelper.OutputGeometry(doc, sut, attrs);
        doc.Objects.AddLine(new Line(new Point3d(99, 0, 0), new Point3d(99, 10, 0)));

        Assert.That(sut.Geometry.IsValid, Is.True);
        Assert.That(sut.Geometry.GetLength(), Is.EqualTo(10.0).Within(1e-6));
    }

    [Test]
    public void OutputGeometry_Curve2D_StoredGeometrySurvivesSourceDeletion()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var id = doc.Objects.AddLine(new Line(new Point3d(0, 0, 0), new Point3d(10, 0, 0)));
        var obj = doc.Objects.Find(id);
        var curve = obj!.Geometry as Curve;
        var sut = new Curve2D(curve!, Guid.NewGuid());
        var attrs = new ObjectAttributes();

        RhinoOutputHelper.OutputGeometry(doc, sut, attrs);
        doc.Objects.Delete(obj, true);

        Assert.That(sut.Geometry.IsValid, Is.True);
        Assert.That(sut.Geometry.GetLength(), Is.EqualTo(10.0).Within(1e-6));
    }

    [Test]
    public void OutputGeometry_Polygon2D_StoredGeometrySurvivesDocModification()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var polyline = new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]);
        var id = doc.Objects.AddCurve(polyline);
        var obj = doc.Objects.Find(id);
        var curve = obj!.Geometry as Curve;
        var sut = new Polycurve2D(curve!);
        var attrs = new ObjectAttributes();

        RhinoOutputHelper.OutputGeometry(doc, sut, attrs, false);
        doc.Objects.AddLine(new Line(new Point3d(99, 0, 0), new Point3d(99, 10, 0)));

        Assert.That(sut.Geometry.IsValid, Is.True);
        Assert.That(sut.Geometry.IsClosed, Is.True);
    }

    [Test]
    public void OutputGeometry_Polygon2D_StoredGeometrySurvivesSourceDeletion()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var polyline = new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]);
        var id = doc.Objects.AddCurve(polyline);
        var obj = doc.Objects.Find(id);
        var curve = obj!.Geometry as Curve;
        var sut = new Polycurve2D(curve!);
        var attrs = new ObjectAttributes();

        RhinoOutputHelper.OutputGeometry(doc, sut, attrs, false);
        doc.Objects.Delete(obj, true);

        Assert.That(sut.Geometry.IsValid, Is.True);
        Assert.That(sut.Geometry.IsClosed, Is.True);
    }
}
