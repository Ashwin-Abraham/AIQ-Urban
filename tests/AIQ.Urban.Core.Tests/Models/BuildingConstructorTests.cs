using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System;
using System.Collections.Generic;

namespace AIQ.Urban.Core.Tests.Models;

[RhinoTestFixture]
public class BuildingConstructorTests
{
    private static Brep MakeEnvelope()
    {
        return new Box(Plane.WorldXY, new Interval(0, 10), new Interval(0, 10), new Interval(0, 30)).ToBrep();
    }

    private static FloorPlate MakePlate(double z, double size = 10)
    {
        var curve = new PolylineCurve([
            new Point3d(0, 0, z),
            new Point3d(size, 0, z),
            new Point3d(size, size, z),
            new Point3d(0, size, z),
            new Point3d(0, 0, z),
        ]);
        return new FloorPlate(new Polygon2D(curve), DefaultUses.Create("Residential"));
    }

    [Test]
    public void Constructor_NullEnvelope_ThrowsArgumentNullException()
    {
        var plates = new List<FloorPlate> { MakePlate(0) };

        Assert.Throws<ArgumentNullException>(() => new Building(null!, plates));
    }

    [Test]
    public void Constructor_NullPlates_ThrowsArgumentNullException()
    {
        var envelope = MakeEnvelope();

        Assert.Throws<ArgumentNullException>(() => new Building(envelope, null!));
    }

    [Test]
    public void Constructor_EmptyPlates_ThrowsArgumentException()
    {
        var envelope = MakeEnvelope();

        Assert.Throws<ArgumentException>(() => new Building(envelope, new List<FloorPlate>()));
    }

    [Test]
    public void Constructor_SinglePlate_TypicalFloorHeightIs3()
    {
        var envelope = MakeEnvelope();
        var plates = new List<FloorPlate> { MakePlate(0) };

        var building = new Building(envelope, plates);

        Assert.That(building.FloorCount, Is.EqualTo(1));
        Assert.That(building.TypicalFloorHeight, Is.EqualTo(3.0));
    }

    [Test]
    public void Constructor_UniformSpacing_ReturnsSpacing()
    {
        var envelope = MakeEnvelope();
        var plates = new List<FloorPlate>
        {
            MakePlate(0),
            MakePlate(4),
            MakePlate(8),
            MakePlate(12),
        };

        var building = new Building(envelope, plates);

        Assert.That(building.FloorCount, Is.EqualTo(4));
        Assert.That(building.TypicalFloorHeight, Is.EqualTo(4.0));
    }

    [Test]
    public void Constructor_MixedSpacing_ReturnsMode()
    {
        var envelope = MakeEnvelope();
        // Three gaps of 3.0 and one gap of 5.0 — mode is 3.0
        var plates = new List<FloorPlate>
        {
            MakePlate(0),
            MakePlate(3),
            MakePlate(6),
            MakePlate(9),
            MakePlate(14),
        };

        var building = new Building(envelope, plates);

        Assert.That(building.FloorCount, Is.EqualTo(5));
        Assert.That(building.TypicalFloorHeight, Is.EqualTo(3.0));
    }

    [Test]
    public void Constructor_PreservesPlatesAsIs()
    {
        var envelope = MakeEnvelope();
        var plates = new List<FloorPlate>
        {
            MakePlate(0),
            MakePlate(3),
        };

        var building = new Building(envelope, plates);

        Assert.That(building.FloorPlates, Is.SameAs(plates));
    }

    [Test]
    public void Constructor_FootprintIsLowestPlate()
    {
        var envelope = MakeEnvelope();
        var plates = new List<FloorPlate>
        {
            MakePlate(3),
            MakePlate(0),
            MakePlate(6),
        };

        var building = new Building(envelope, plates);

        Assert.That(building.Footprint, Is.EqualTo(plates[0].Outline));
    }

    [Test]
    public void Constructor_DuplicatesEnvelope()
    {
        var envelope = MakeEnvelope();
        var plates = new List<FloorPlate> { MakePlate(0) };

        var building = new Building(envelope, plates);

        Assert.That(building.Envelope.Geometry, Is.Not.SameAs(envelope));
    }
}
