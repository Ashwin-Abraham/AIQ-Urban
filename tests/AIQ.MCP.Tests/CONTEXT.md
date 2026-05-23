# AIQ.MCP.Tests — Testing Conventions

## Test Framework

- **NUnit 3.14** with `[Test]`, `[TestCase]`, `[SetUp]`
- **Rhino.Testing 8.0.28-beta** for headless Rhino doc via `[RhinoTestFixture]`
- **No mocking** — tests exercise the real dependency chain (UrbanModel, GeometryFactory, UrbanModelService)

## Test Fixture Pattern

Every test class is decorated with `[RhinoTestFixture]` (from `Rhino.Testing.Fixtures`) and has a `[SetUp]` that calls `UrbanModel.Instance.Reset()`:

```csharp
[RhinoTestFixture]
public class UrbanModelServiceTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }
}
```

## Test Data Pattern (Parametrized)

When testing a service method against multiple scenarios, use a single parametrized `[Test]` + `[TestCase("CaseName")]` + static record list:

1. Define a private record with the scenario's inputs and expected outputs (always includes `string CaseName`)
2. Build a static `_testData` list via a `BuildTestData()` factory method
3. The test looks up the case by name: `_testData.Single(x => x.CaseName == testName)`

### Record Shape Convention

```csharp
private record ServiceTestData(
    string CaseName,
    // ... inputs ...
    // ... expected outputs ...
);
```

### Data Builder Convention

When test data requires creating Rhino geometry (Polygon2D, Curve2D, etc.), build everything in `BuildTestData()` using helper methods:

```csharp
private static readonly List<ServiceTestData> _testData = BuildTestData();

private static List<ServiceTestData> BuildTestData()
{
    // Create geometry once per case
    return [...];
}
```

## Test Structure Comments

Every test method follows this comment block order:

```csharp
// Arrange
// Act
// Assert
```

## Service Testing Pattern

The UrbanModelService is data-only — it constructs steps, it does not execute the pipeline. Tests verify:

- Correct step types returned (e.g., `AddTransitStep`, `ChangeBuildingUseStep`)
- Correct parameters on constructed steps (GUIDs, Use instances, TransitData records)
- Exceptions on invalid input (unknown use names, non-closed footprints, stale GUIDs)
- Element summaries contain expected content and correct GUIDs

```csharp
IUrbanModelStep step = service.CreateBuildingSteps(request);
Assert.That(step, Is.InstanceOf<AddBuildingStep>());
```

## Geometry Factory Testing Pattern

GeometryFactory tests verify coordinate→geometry conversion:

- Centerline construction: correct point count, planar, non-closed
- Footprint construction: closed ring, planar
- TransitType resolution: valid names map to enum values, invalid names throw
- Use resolution: valid names map to DefaultUses instances, invalid names throw

## Assertion Style

Use NUnit's constraint model (`Assert.That(...)`) with `Is.*` and `Has.*`:

```csharp
Assert.That(result, Has.Count.EqualTo(expectedCount));
Assert.That(step, Is.InstanceOf<AddBuildingStep>());
Assert.That(ex.Message, Does.Contain("Unknown use name"));
```

## Geometry Construction

Create test geometry directly in static context — no Rhino doc required:

```csharp
private static double[][] MakeCoords(params (double x, double y)[] pts)
    => pts.Select(p => new[] { p.x, p.y }).ToArray();
```

## What Is NOT Tested Here

- MCP server transport (HTTP/SSE, tool registration) — deferred to integration testing
- Rhino plugin lifecycle (start/stop) — tested in AIQ.Urban.RhinoPlugin.Tests
- Existing AIQ.Urban.Core step logic — already covered by AIQ.Urban.Core.Tests

## Prior Art

Refer to these files as canonical examples of the same patterns:

| File | Pattern demonstrated |
|---|---|
| `tests/AIQ.Urban.Core.Tests/ModelActions/AddBuildingStepTests.cs` | Parametrized `[TestCase]`, record data |
| `tests/AIQ.Urban.Core.Tests/ModelActions/RemoveTransitStepTests.cs` | `BuildTestData()` factory method |
| `tests/AIQ.Urban.Core.Tests/CONTEXT.md` | Full test convention documentation |
