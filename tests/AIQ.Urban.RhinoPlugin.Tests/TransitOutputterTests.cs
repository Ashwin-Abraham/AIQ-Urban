using System;
using System.Drawing;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using AIQ.Urban.RhinoInterface.Utils;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests;

[RhinoTestFixture]
public class TransitOutputterTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public void Run_Transit_OutputsCenterlineWithoutOutline()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var transit = MakeTransit();

        new TransitOutputter().Run(UrbanModel.Instance, doc, [transit]);

        var centerlineLayerIndex = doc.Layers.FindByFullPath(
            LayerConstants.TransitCenterline(transit.TransitType, transit.Width),
            -1);
        var outlineLayerIndex = doc.Layers.FindByFullPath(
            LayerConstants.TransitOutline(transit.TransitType, transit.Width),
            -1);

        Assert.That(doc.Objects.Count, Is.EqualTo(1));
        Assert.That(doc.Objects.FindId(transit.Centerline.Id), Is.Not.Null);
        Assert.That(centerlineLayerIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(outlineLayerIndex, Is.EqualTo(-1));
    }

    [Test]
    public void Run_TransitSurface_OutputsToLockedLayer()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var transit = MakeTransit();
        UrbanModel.Instance.TransitSurfaces.Add((transit.Outline, transit.TransitType));

        new TransitSurfaceOutputter().Run(UrbanModel.Instance, doc, []);

        var layerIndex = doc.Layers.FindByFullPath(
            LayerConstants.TransitSurface(transit.TransitType),
            -1);

        Assert.That(doc.Objects.Count, Is.EqualTo(1));
        Assert.That(layerIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(doc.Layers[layerIndex].IsLocked, Is.True);
    }

    [Test]
    public void Run_ExistingLockedTransitSurfaceLayer_OutputsAndRelocksLayer()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var transit = MakeTransit();
        UrbanModel.Instance.TransitSurfaces.Add((transit.Outline, transit.TransitType));
        var layerPath = LayerConstants.TransitSurface(transit.TransitType);
        var attributes = RhinoOutputHelper.MakeLayerDrivenAttributes(doc, layerPath, Color.Black);
        RhinoOutputHelper.SetLayerLocked(doc, attributes.LayerIndex, true);

        new TransitSurfaceOutputter().Run(UrbanModel.Instance, doc, []);

        Assert.That(doc.Objects.Count, Is.EqualTo(1));
        Assert.That(doc.Layers[attributes.LayerIndex].IsLocked, Is.True);
    }

    private static Transit MakeTransit()
    {
        var centerline = new Curve2D(
            new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)),
            Guid.NewGuid());

        return new Transit(centerline, 6, TransitType.Road);
    }
}
