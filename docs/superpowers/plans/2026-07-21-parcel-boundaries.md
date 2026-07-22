# Parcel Boundaries Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `ParcelBoundary` urban element that stores user-drawn curves and integrate it into the parcel calculation step so that boundaries split/enclose parcels within transit-defined holes.

**Architecture:** `ParcelBoundary` is a new `IUrbanElement` holding a `Curve2D` (open for splitting, closed for enclosure). `RecalculateParcelsStep` gains a blank `SplitHoleWithSeparators` method that the implementer will fill in later. Tests are written for the fully-implemented splitting logic so the implementer can validate against them.

**Tech Stack:** C# 12, .NET 8, RhinoCommon (Rhino.Geometry), NUnit 3.14, Rhino.Testing

## Global Constraints

- Target framework: `net8.0`
- Nullable reference types: enabled
- LangVersion: 12.0
- Use file-scoped namespaces (`namespace X.Y.Z;`)
- Use K&R brace style (opening brace on same line)
- Private fields: `_camelCase`
- No redundant `using` statements (implicit usings enabled)
- NUnit with `[RhinoTestFixture]`, `[SetUp]` calls `UrbanModel.Instance.Reset()`
- Test data: parametrized `[TestCase("CaseName")]` + static record list + `BuildTestData()` factory
- Do NOT run the parcel splitting tests — only run `dotnet build`

---

## File Map

| Action | File | Purpose |
|--------|------|---------|
| Create | `src/AIQ.Urban.Core/Models/UrbanElements/ParcelBoundary.cs` | New IUrbanElement |
| Create | `src/AIQ.Urban.Core/ModelActions/CreationData/ParcelBoundaryData.cs` | Creation data record |
| Create | `src/AIQ.Urban.Core/ModelActions/AddParcelBoundaryStep.cs` | Add step |
| Modify | `src/AIQ.Urban.Core/UrbanModel.cs` | Add ParcelBoundaries list + Reset + GetUrbanElements |
| Modify | `src/AIQ.Urban.Core/ModelActions/RecalculateParcelsStep.cs` | Add SplitHoleWithSeparators stub |
| Create | `tests/AIQ.Urban.Core.Tests/Models/ParcelBoundaryTests.cs` | Unit tests for ParcelBoundary |
| Create | `tests/AIQ.Urban.Core.Tests/ModelActions/AddParcelBoundaryStepTests.cs` | Step tests |
| Create | `tests/AIQ.Urban.Core.Tests/ModelActions/ParcelBoundarySplittingTests.cs` | Splitting integration tests |

---

### Task 1: Create `ParcelBoundary` class

**Files:**
- Create: `src/AIQ.Urban.Core/Models/UrbanElements/ParcelBoundary.cs`

**Interfaces:**
- Implements: `IUrbanElement`
- Consumes: `Curve2D` (from `Models/GeometryElements`)
- Produces: `Guid Id`, `List<Guid> GetGeometryIds()`, `Point3d GetRepresentativeCenter()`

- [ ] **Step 1: Create the ParcelBoundary class**

```csharp
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanElements;

/// <summary>A user-drawn curve that splits or encloses parcels within transit-defined holes.</summary>
public class ParcelBoundary : IUrbanElement
{
    /// <summary>The boundary curve (open for splitting, closed for enclosure).</summary>
    public Curve2D Curve { get; }

    public Guid Id => Curve.Id;

    public ParcelBoundary(Curve2D curve)
    {
        Curve = curve;
    }

    public Point3d GetRepresentativeCenter()
    {
        return Curve.Geometry.PointAtNormalizedLength(0.5);
    }

    public List<Guid> GetGeometryIds()
    {
        return [Curve.Id];
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build src/AIQ.Urban.Core/AIQ.Urban.Core.csproj`
Expected: BUILD SUCCEEDED

---

### Task 2: Create `ParcelBoundaryData` creation data record

**Files:**
- Create: `src/AIQ.Urban.Core/ModelActions/CreationData/ParcelBoundaryData.cs`

**Interfaces:**
- Implements: `ICreationData`
- Consumes: `Curve2D`
- Produces: `ParcelBoundary ToParcelBoundary()`

- [ ] **Step 1: Create the ParcelBoundaryData record**

```csharp
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record ParcelBoundaryData(Curve2D Curve) : ICreationData
{
    public ParcelBoundary ToParcelBoundary()
    {
        return new ParcelBoundary(Curve);
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build src/AIQ.Urban.Core/AIQ.Urban.Core.csproj`
Expected: BUILD SUCCEEDED

---

### Task 3: Add `ParcelBoundaries` to `UrbanModel`

**Files:**
- Modify: `src/AIQ.Urban.Core/UrbanModel.cs`

**Interfaces:**
- Consumes: `ParcelBoundary` (from Task 1)
- Produces: `List<ParcelBoundary> ParcelBoundaries`, updated `Reset()`, updated `GetUrbanElements()`

- [ ] **Step 1: Add ParcelBoundaries property**

After the `Zones` property (around line 23), add:

```csharp
/// <summary>All parcel boundary curves in the model.</summary>
public List<ParcelBoundary> ParcelBoundaries { get; } = [];
```

- [ ] **Step 2: Add to Reset()**

Inside the `Reset()` method, after `Zones.Clear();` (around line 44), add:

```csharp
ParcelBoundaries.Clear();
```

- [ ] **Step 3: Add to GetUrbanElements()**

Inside `GetUrbanElements()`, after `list.AddRange(Zones);` (around line 60), add:

```csharp
list.AddRange(ParcelBoundaries);
```

- [ ] **Step 4: Add AddParcelBoundary method**

After the `AddZone` method (around line 72), add:

```csharp
/// <summary>Add a parcel boundary to the model.</summary>
public void AddParcelBoundary(ParcelBoundary boundary) { ParcelBoundaries.Add(boundary); }
```

- [ ] **Step 5: Build to verify compilation**

Run: `dotnet build src/AIQ.Urban.Core/AIQ.Urban.Core.csproj`
Expected: BUILD SUCCEEDED

---

### Task 4: Create `AddParcelBoundaryStep`

**Files:**
- Create: `src/AIQ.Urban.Core/ModelActions/AddParcelBoundaryStep.cs`

**Interfaces:**
- Implements: `IUrbanModelStep`
- Consumes: `ParcelBoundaryData` (from Task 2), `UrbanModel` (from Task 3)
- Produces: `StepResult` with added geometry GUIDs

- [ ] **Step 1: Create the AddParcelBoundaryStep class**

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class AddParcelBoundaryStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AddParcelBoundaryStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var boundaries = creationData.OfType<ParcelBoundaryData>()
            .Select(x => x.ToParcelBoundary())
            .ToList();

        foreach (var boundary in boundaries)
        {
            model.AddParcelBoundary(boundary);
        }

        _logger.LogInformation("Added {Count} parcel boundaries", boundaries.Count);

        return new(boundaries.SelectMany(x => x.GetGeometryIds()).ToList(), []);
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build src/AIQ.Urban.Core/AIQ.Urban.Core.csproj`
Expected: BUILD SUCCEEDED

---

### Task 5: Add `SplitHoleWithSeparators` stub to `RecalculateParcelsStep`

**Files:**
- Modify: `src/AIQ.Urban.Core/ModelActions/RecalculateParcelsStep.cs`

**Interfaces:**
- Consumes: `UrbanModel.ParcelBoundaries` (from Task 3)
- Produces: `List<Parcel>` from each hole

- [ ] **Step 1: Modify the Run method to use SplitHoleWithSeparators**

Replace lines 45-52 (the `foreach` loop over holes) with:

```csharp
        foreach (var (profile, _) in model.TransitSurfaces)
        {
            foreach (var hole in profile.Holes)
            {
                var splitParcels = SplitHoleWithSeparators(hole, model);

                foreach (var (splitPolygon, originalCentroid) in splitParcels)
                {
                    var use = ResolveUse(splitPolygon, oldCentroids, originalCentroid, model);
                    model.Parcels.Add(new Parcel(splitPolygon, use));
                }
            }
        }
```

- [ ] **Step 2: Update ResolveUse to accept an optional original centroid hint**

Replace the existing `ResolveUse` method (lines 73-88) with:

```csharp
    private static Use ResolveUse(
        Polygon2D hole,
        List<(Point3d Centroid, Use Use)> oldCentroids,
        Point3d? originalHoleCentroid,
        UrbanModel model)
    {
        if (oldCentroids.Count == 0)
            return model.GetUse("Public");

        // Try the original hole centroid first (preserves Use when splitting)
        if (originalHoleCentroid.HasValue)
        {
            var matchByOrigin = oldCentroids
                .OrderBy(x => x.Centroid.DistanceTo(originalHoleCentroid.Value))
                .First();
            if (matchByOrigin.Centroid.DistanceTo(originalHoleCentroid.Value) < 1e-6)
                return matchByOrigin.Use;
        }

        // Fall back to nearest-centroid on the split polygon itself
        var holeCentroid = ComputeCentroid(hole.Geometry);
        if (holeCentroid is null)
            return model.GetUse("Public");

        return oldCentroids
            .OrderBy(x => x.Centroid.DistanceTo(holeCentroid.Value))
            .First().Use;
    }
```

- [ ] **Step 3: Add the blank SplitHoleWithSeparators method**

Add this method at the end of the class (before the closing brace):

```csharp
    /// <summary>
    /// Splits a single transit-defined hole into multiple parcels using parcel boundary curves.
    /// Returns each resulting sub-polygon paired with the original hole's centroid (for Use resolution).
    ///
    /// The default implementation returns the hole unchanged — the implementer should replace
    /// this with actual curve-based splitting logic.
    /// </summary>
    private static List<(Polygon2D Polygon, Point3d? OriginalCentroid)> SplitHoleWithSeparators(
        Polygon2D hole,
        UrbanModel model)
    {
        var centroid = ComputeCentroid(hole.Geometry);
        return [(hole, centroid)];
    }
```

- [ ] **Step 4: Update the ResolveUse call in Run to pass the centroid**

In the `Run` method, the `ResolveUse` call should pass the original hole centroid. The `splitParcels` tuple already carries `OriginalCentroid`. Verify the call matches the updated signature from Step 2.

- [ ] **Step 5: Build to verify compilation**

Run: `dotnet build src/AIQ.Urban.Core/AIQ.Urban.Core.csproj`
Expected: BUILD SUCCEEDED

---

### Task 6: Write `ParcelBoundary` unit tests

**Files:**
- Create: `tests/AIQ.Urban.Core.Tests/Models/ParcelBoundaryTests.cs`

**Interfaces:**
- Consumes: `ParcelBoundary` (from Task 1), `Curve2D`

- [ ] **Step 1: Create the test file**

```csharp
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System;

namespace AIQ.Urban.Core.Tests.Models;

[RhinoTestFixture]
public class ParcelBoundaryTests
{
    [Test]
    public void Constructor_SetsCurveProperty()
    {
        // Arrange
        var curve = new Curve2D(new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)), Guid.NewGuid());

        // Act
        var boundary = new ParcelBoundary(curve);

        // Assert
        Assert.That(boundary.Curve, Is.SameAs(curve));
    }

    [Test]
    public void Id_ReturnsCurveId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var curve = new Curve2D(new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)), id);

        // Act
        var boundary = new ParcelBoundary(curve);

        // Assert
        Assert.That(boundary.Id, Is.EqualTo(id));
    }

    [Test]
    public void GetRepresentativeCenter_ReturnsMidpoint()
    {
        // Arrange
        var curve = new Curve2D(new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)), Guid.NewGuid());
        var boundary = new ParcelBoundary(curve);

        // Act
        var center = boundary.GetRepresentativeCenter();

        // Assert
        Assert.That(center.X, Is.EqualTo(5.0).Within(1e-6));
        Assert.That(center.Y, Is.EqualTo(0.0).Within(1e-6));
        Assert.That(center.Z, Is.EqualTo(0.0).Within(1e-6));
    }

    [Test]
    public void GetGeometryIds_ReturnsSingleCurveId()
    {
        // Arrange
        var curve = new Curve2D(new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)), Guid.NewGuid());
        var boundary = new ParcelBoundary(curve);

        // Act
        var ids = boundary.GetGeometryIds();

        // Assert
        Assert.That(ids, Has.Count.EqualTo(1));
        Assert.That(ids[0], Is.EqualTo(curve.Id));
    }

    [Test]
    public void GetRepresentativeCenter_ClosedCurve_ReturnsMidpoint()
    {
        // Arrange — closed rectangle, midpoint at normalized length 0.5
        var curve = new Curve2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]), Guid.NewGuid());
        var boundary = new ParcelBoundary(curve);

        // Act
        var center = boundary.GetRepresentativeCenter();

        // Assert — midpoint of perimeter at 0.5 is on the bottom edge at x=5
        Assert.That(center.X, Is.EqualTo(5.0).Within(1e-6));
        Assert.That(center.Y, Is.EqualTo(0.0).Within(1e-6));
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj`
Expected: BUILD SUCCEEDED

---

### Task 7: Write `AddParcelBoundaryStep` tests

**Files:**
- Create: `tests/AIQ.Urban.Core.Tests/ModelActions/AddParcelBoundaryStepTests.cs`

**Interfaces:**
- Consumes: `AddParcelBoundaryStep` (from Task 4), `ParcelBoundaryData` (from Task 2), `UrbanModel` (from Task 3)

- [ ] **Step 1: Create the test file**

```csharp
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
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
        var result = step.Run(UrbanModel.Instance, testData.Input.Cast<ICreationData>().ToList());

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
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj`
Expected: BUILD SUCCEEDED

---

### Task 8: Write parcel boundary splitting integration tests

**Files:**
- Create: `tests/AIQ.Urban.Core.Tests/ModelActions/ParcelBoundarySplittingTests.cs`

**Interfaces:**
- Consumes: `AddTransitStep`, `AddParcelBoundaryStep`, `TransitSurfaceUnioner`, `RecalculateParcelsStep`, `UrbanModel`

- [ ] **Step 1: Create the test file**

These tests assume the full splitting logic is implemented. The blank stub will cause some to fail — that's intentional. The implementer uses these tests to validate their `SplitHoleWithSeparators` implementation.

```csharp
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class ParcelBoundarySplittingTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("ClosedBoundarySplitsHoleIntoTwo")]
    [TestCase("OpenBoundarySplitsHoleIntoTwo")]
    [TestCase("BoundaryOutsideHoleNoSplit")]
    [TestCase("NoBoundariesNoSplit")]
    [TestCase("TwoBoundariesSplitIntoThree")]
    public void Run_SplitsParcelsCorrectly(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange — add transits to form enclosure
        IUrbanModelStep addTransitStep = new AddTransitStep();
        addTransitStep.Run(UrbanModel.Instance, testData.TransitInputs.Cast<ICreationData>().ToList());

        // Arrange — add parcel boundaries
        if (testData.BoundaryInputs.Count > 0)
        {
            IUrbanModelStep addBoundaryStep = new AddParcelBoundaryStep();
            addBoundaryStep.Run(UrbanModel.Instance, testData.BoundaryInputs.Cast<ICreationData>().ToList());
        }

        // Act — union transit surfaces, then recalculate parcels
        var unionerStep = new TransitSurfaceUnioner();
        unionerStep.Run(UrbanModel.Instance, []);
        var parcelsStep = new RecalculateParcelsStep();
        var result = parcelsStep.Run(UrbanModel.Instance, []);

        // Assert
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(testData.ExpectedParcelCount));
        Assert.That(result.AddedIds, Has.Count.EqualTo(testData.ExpectedAddedCount));

        for (int i = 0; i < testData.ExpectedParcelCount; i++)
        {
            Assert.That(UrbanModel.Instance.Parcels[i].Use.Name,
                Is.EqualTo(testData.ExpectedUses[i].Name));
        }
    }

    private record SplittingTestData(
        string CaseName,
        List<TransitData> TransitInputs,
        List<ParcelBoundaryData> BoundaryInputs,
        int ExpectedParcelCount,
        int ExpectedAddedCount,
        List<Use> ExpectedUses);

    private static Curve2D MakeCenterline(Point3d from, Point3d to)
        => new(new LineCurve(from, to), Guid.NewGuid());

    private static Curve2D MakeOpenBoundary(Point3d from, Point3d to)
        => new(new LineCurve(from, to), Guid.NewGuid());

    private static Curve2D MakeClosedBoundary(Point3d[] corners)
        => new(new PolylineCurve(corners.Append(corners[0]).ToArray()), Guid.NewGuid());

    // Rectangle enclosure: roads at y=0, y=30, x=0, x=30 (width 6)
    private static readonly List<TransitData> _rectEnclosure =
    [
        new TransitData(MakeCenterline(new(0, 0, 0), new(30, 0, 0)), 6, TransitType.Road),
        new TransitData(MakeCenterline(new(30, 0, 0), new(30, 30, 0)), 6, TransitType.Road),
        new TransitData(MakeCenterline(new(30, 30, 0), new(0, 30, 0)), 6, TransitType.Road),
        new TransitData(MakeCenterline(new(0, 30, 0), new(0, 0, 0)), 6, TransitType.Road),
    ];

    private static readonly List<SplittingTestData> _testData = BuildTestData();

    private static List<SplittingTestData> BuildTestData()
    {
        return
        [
            // No boundaries — same as existing SingleHole test, 1 parcel
            new(CaseName: "NoBoundariesNoSplit",
                TransitInputs: _rectEnclosure,
                BoundaryInputs: [],
                ExpectedParcelCount: 1,
                ExpectedAddedCount: 1,
                ExpectedUses: [DefaultUses.Create("Public")]),

            // Closed boundary inside the hole — splits into 2 parcels
            // Boundary: rectangle from (10,10) to (20,20) inside the 30x30 enclosure
            new(CaseName: "ClosedBoundarySplitsHoleIntoTwo",
                TransitInputs: _rectEnclosure,
                BoundaryInputs:
                [
                    new ParcelBoundaryData(MakeClosedBoundary([
                        new Point3d(10, 10, 0),
                        new Point3d(20, 10, 0),
                        new Point3d(20, 20, 0),
                        new Point3d(10, 20, 0),
                    ])),
                ],
                ExpectedParcelCount: 2,
                ExpectedAddedCount: 2,
                ExpectedUses:
                [
                    DefaultUses.Create("Public"),
                    DefaultUses.Create("Public"),
                ]),

            // Open boundary across the hole — splits into 2 parcels
            // Boundary: vertical line at x=15 from y=3 to y=27 (spanning the hole)
            new(CaseName: "OpenBoundarySplitsHoleIntoTwo",
                TransitInputs: _rectEnclosure,
                BoundaryInputs:
                [
                    new ParcelBoundaryData(MakeOpenBoundary(
                        new Point3d(15, 3, 0), new Point3d(15, 27, 0))),
                ],
                ExpectedParcelCount: 2,
                ExpectedAddedCount: 2,
                ExpectedUses:
                [
                    DefaultUses.Create("Public"),
                    DefaultUses.Create("Public"),
                ]),

            // Boundary entirely outside the hole — no splitting, still 1 parcel
            new(CaseName: "BoundaryOutsideHoleNoSplit",
                TransitInputs: _rectEnclosure,
                BoundaryInputs:
                [
                    new ParcelBoundaryData(MakeClosedBoundary([
                        new Point3d(50, 50, 0),
                        new Point3d(60, 50, 0),
                        new Point3d(60, 60, 0),
                        new Point3d(50, 60, 0),
                    ])),
                ],
                ExpectedParcelCount: 1,
                ExpectedAddedCount: 1,
                ExpectedUses: [DefaultUses.Create("Public")]),

            // Two open boundaries crossing — splits into 3 or 4 parcels
            // Horizontal at y=15, vertical at x=15
            new(CaseName: "TwoBoundariesSplitIntoThree",
                TransitInputs: _rectEnclosure,
                BoundaryInputs:
                [
                    new ParcelBoundaryData(MakeOpenBoundary(
                        new Point3d(3, 15, 0), new Point3d(27, 15, 0))),
                    new ParcelBoundaryData(MakeOpenBoundary(
                        new Point3d(15, 3, 0), new Point3d(15, 27, 0))),
                ],
                ExpectedParcelCount: 4,
                ExpectedAddedCount: 4,
                ExpectedUses:
                [
                    DefaultUses.Create("Public"),
                    DefaultUses.Create("Public"),
                    DefaultUses.Create("Public"),
                    DefaultUses.Create("Public"),
                ]),
        ];
    }
}
```

- [ ] **Step 2: Build to verify compilation (DO NOT RUN tests)**

Run: `dotnet build tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj`
Expected: BUILD SUCCEEDED

---

### Task 9: Final build verification

- [ ] **Step 1: Build entire solution**

Run: `dotnet build ArchSitePlan.sln`
Expected: BUILD SUCCEEDED

- [ ] **Step 2: Run existing parcel calculation tests to verify no regression**

Run: `dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~TransitSurfaceAndParcelCalculationTests"`
Expected: All 4 existing test cases PASS (NoTransits, NoHoles, SingleHole, TwoSeparateHoles)

- [ ] **Step 3: Run AddParcelBoundaryStep tests**

Run: `dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~AddParcelBoundaryStepTests"`
Expected: All 3 test cases PASS

- [ ] **Step 4: Run ParcelBoundary unit tests**

Run: `dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~ParcelBoundaryTests"`
Expected: All 5 test cases PASS
