# AIQ.Urban.Core.Tests — Testing Conventions

## Test Framework

- **NUnit 3.14** with `[Test]`, `[TestCase]`, `[SetUp]`
- **Rhino.Testing 8.0.28-beta** for headless Rhino doc via `[RhinoTestFixture]`
- **No mocking** — step tests exercise the real dependency chain (UrbanModel, geometry kernel)

## Test Fixture Pattern

Every test class is decorated with `[RhinoTestFixture]` (from `Rhino.Testing.Fixtures`) and has a `[SetUp]` that calls `UrbanModel.Instance.Reset()`:

```csharp
[RhinoTestFixture]
public class AddBuildingStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }
}
```

## Test Data Pattern (Parametrized)

When testing a step against multiple scenarios, use a single parametrized `[Test]` + `[TestCase("CaseName")]` + static record list:

1. Define a private record with the scenario's inputs and expected outputs (always includes `string CaseName`)
2. Build a static `_testData` list via a `BuildTestData()` factory method
3. The test looks up the case by name: `_testData.Single(x => x.CaseName == testName)`

### Record Shape Convention

```csharp
private record XxxTestData(
    string CaseName,
    // ... inputs ...
    // ... expected outputs ...
);
```

### Data Builder Convention (Non-RhinoDoc Geometry)

When test data requires creating geometry types that do **not** need a Rhino doc (Polygon2D, Curve2D, etc.), build everything in `BuildTestData()` using helper methods:

```csharp
private static readonly List<XxxTestData> _testData = BuildTestData();

private static List<XxxTestData> BuildTestData()
{
    // Create geometry once per case — each case gets independent instances
    // so mutations in one case don't affect others
    return [...];
}
```

**Important**: each test case must own its own geometry instances. Sharing parcels/buildings between cases causes test pollution when steps mutate them in-place.

### Primitive Data Record (RhinoDoc Geometry)

When test data involves **RhinoCommon types that require a Rhino doc context** (Point3d, Line, GraphNode, etc.), keep the test data record **primitive-only** and construct the Rhino types **inside the test body**. This avoids initialization-order problems with RhinoCommon in static contexts under `[RhinoTestFixture]`.

```csharp
private record PrimitiveIntersectTestCase(
    string CaseName,
    double[][] NodeCoords,
    double[] LineFrom,
    double[] LineTo,
    bool Expected);

private static PrimitiveIntersectTestCase GetTestData(string caseName)
{
    return caseName switch
    {
        "CrossingEdge" => new PrimitiveIntersectTestCase(
            CaseName: "CrossingEdge",
            NodeCoords: [[5, 5, 0], [5, -5, 0]],
            LineFrom: [3, 0, 0],
            LineTo: [7, 0, 0],
            Expected: true),
        // ... more cases ...
        _ => throw new ArgumentException($"Unknown test case: {caseName}")
    };
}
```

The test body looks up the primitive record by case name, constructs `Point3d`, `Line`, `GraphNode` etc. from the coordinate arrays, exercises the method, and asserts against the expected value.

Key rules:
- Record fields are only `string`, `double[]`, `double[][]`, `bool`, `int` — no RhinoCommon types
- Each case uses named arguments for readability
- The `GetTestData()` method uses a `switch` expression to map case names to records
- Rhino types are constructed in the test body from the primitive coordinates
- `MakeNodeWithEdges()` is still a static helper, but it takes `Point3d[]` built inline

### Examples

**Step with inputs only (AddBuildingStepTests):** record stores input `List<BuildingData>` and expected counts. Geometry is defined as static `Polygon2D` fields shared by all cases (steps create new domain objects, so sharing is safe).

**Step that mutates in-place (ChangeParcelUseStepTests):** each case creates its own `Parcel` instances in `BuildTestData()` so mutations don't leak between cases.

**Extension method with Rhino types (GraphNodeExtensionPrimitiveTests):** record stores only `double[][]` coordinates; `Point3d` and `Line` constructed in test body.

## Test Structure Comments

Every test method follows this comment block order:

```csharp
// Arrange
// Act
// Visualise    (inside #if DEBUG, optional)
// Assert
```

## Visualisation (Debug Only)

When a test manipulates geometry, optionally add a `#if DEBUG` visualisation block between `// Act` and `// Assert`. Uses extension methods on `UrbanModel` defined in `AIQ.Urban.DebugViz`:

```csharp
// Visualise
#if DEBUG
UrbanModel.Instance.VisualizeParcels();
#endif
```

Available visualizers:
- `VisualizeAll()` — all elements + transit surfaces
- `VisualizeBuildings()` — buildings only
- `VisualizeTransits()` — transits only
- `VisualizeParcels()` — parcels only
- `VisualizeZones()` — zones only
- `VisualizeTransitSurfaces()` — transit surfaces only

## Step Testing Pattern

Steps implement `IUrbanModelStep` (or its marker subtypes `IAddUrbanElementsStep`, `IDeleteUrbanElementsStep`, `IEditUrbanElementsStep`). Tests cast to the interface and call `Run(UrbanModel.Instance)`:

```csharp
IUrbanModelStep step = new AddBuildingStep(testData.Input);
var result = step.Run(UrbanModel.Instance);
```

Assertions verify:
- Model state after `Run()` (e.g., `UrbanModel.Instance.Parcels[i].Use`)
- Returned GUID list (e.g., `result` count, content, uniqueness)
- No side effects on unrelated model collections

## Assertion Style

Use NUnit's constraint model (`Assert.That(...)`) with `Is.*` and `Has.*`:

```csharp
Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(expectedCount));
Assert.That(UrbanModel.Instance.Parcels[0].Use, Is.EqualTo(DefaultUses.Residential));
Assert.That(result, Is.Empty);
Assert.That(result, Is.Unique);
Assert.That(result, Has.All.Not.EqualTo(Guid.Empty));
```

## Geometry Construction

Create test geometry directly in static context — no Rhino doc required:

```csharp
private static Polygon2D MakeOutline(double size)
{
    return new Polygon2D(new PolylineCurve([
        new Point3d(0, 0, 0),
        new Point3d(size, 0, 0),
        new Point3d(size, size, 0),
        new Point3d(0, size, 0),
        new Point3d(0, 0, 0),
    ]));
}
```

## Prior Art

Refer to these files as canonical examples:

| File | Pattern demonstrated |
|---|---|
| `ModelActions/AddBuildingStepTests.cs` | Parametrized `[TestCase]`, record data, non-mutating step |
| `ModelActions/ChangeParcelUseStepTests.cs` | Parametrized `[TestCase]`, record data, mutating step with per-case geometry |
| `ModelActions/RemoveTransitStepTests.cs` | Parametrized `[TestCase]`, `BuildTestData()` with `out` parameters for GUIDs |
| `ModelActions/TransitSurfaceAndParcelCalculationTests.cs` | Multi-step setup within a test, `#if DEBUG` visualisation with `UrbanSceneBuilder` |
| `ModelActions/ResetUrbanElementsStepTests.cs` | Non-parametrized tests with explicit Arrange/Act/Assert comments |
| `Extensions/GraphNodeExtensionPrimitiveTests.cs` | Primitive-only record, Rhino types constructed in test body |
