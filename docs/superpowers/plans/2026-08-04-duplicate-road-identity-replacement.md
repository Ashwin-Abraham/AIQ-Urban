# Duplicate Road Identity Replacement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reusing a managed Rhino Transit centerline replaces its existing Transit by ObjectId instead of baking a duplicate road.

**Architecture:** Keep interactive edit preparation in `SelectTransitCenterlinesStep`, the operation boundary shared by the Rhino command and HTML UI. The gatherer will emit accepted ObjectIds as `TransitDeletionData`, allowing the existing remove-before-add pipeline to replace model and Rhino state without changing Core, MCP, document-load, or update semantics.

**Tech Stack:** .NET 8, C# 12, RhinoCommon, Rhino.Testing 8.0.28-beta, NUnit 3.14, Microsoft.NET.Test.Sdk, NUnit3TestAdapter.

## Global Constraints

- Duplicate identity means the same managed Rhino centerline ObjectId only; do not compare curve geometry.
- A replacement adopts the newly submitted width and `TransitType`.
- Coincident curves with different ObjectIds remain independent.
- MCP additions, document loading, and `TransitUpdateData` behavior remain unchanged.
- Existing coincident Transits with distinct ObjectIds are not cleaned automatically.
- Only accepted centerlines may appear in `TransitDeletionData` or `RhinoObjectDeletionData`.
- Rejected managed centerlines retain their existing Transit and Rhino object.
- Rhino test-data records contain primitive values only; construct Rhino types and GUIDs inside test bodies.
- Tests use `[TestCase("<case name>")]`, private records, named record arguments, switch-expression lookup, Arrange–Act–Preview–Assert, and NUnit constraint assertions.
- Do not change the pipeline step order or add identity-upsert behavior to `AddTransitStep` or `UrbanModel`.
- A verification run that discovers zero tests is a failure, not a passing signal.

## Reference Documents

- Design: `docs/superpowers/specs/2026-08-04-duplicate-road-identity-replacement-design.md`
- Gather-step ownership: `docs/adr/0004-stage-model-edits-through-gather-steps.md`
- Existing behavior contract: `src/AIQ.Urban.Rhino/CONTEXT.md`
- NUnit conventions: `.agents/skills/nunit-tests/SKILL.md`
- Test-project conventions: `tests/AIQ.Urban.Core.Tests/CONTEXT.md`

## File Structure

- Modify `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectTransitCenterlinesStep.cs`: deduplicate selected ObjectIds and stage one accepted-ID `TransitDeletionData` record.
- Modify `tests/AIQ.Urban.RhinoPlugin.Tests/PipelineConfigurations/TransitPipelineConfigurationTests.cs`: exercise the production pipeline seam with primitive-record NUnit cases.
- Do not modify command, HTML UI, MCP, Core model, pipeline configuration, or domain-context files.

## Preflight: Require a Working NUnit/Rhino Test Runner

- [ ] **Step 1: Confirm the worktree and branch are clean before implementation**

Run:

```powershell
git branch --show-current
git status --short
```

Expected: branch `codex/duplicate-road-design`; no uncommitted files except this committed plan after the plan commit.

- [ ] **Step 2: Confirm the target fixture is discovered**

Run:

```powershell
dotnet test 'tests\AIQ.Urban.RhinoPlugin.Tests\AIQ.Urban.RhinoInterface.Tests.csproj' --list-tests -v minimal
```

Expected: output lists `TransitPipelineConfigurationTests` tests, including `WithTransitAdditionSteps_GatheredExistingCenterline_ReplacesRhinoObjectWithoutDuplicate` before it is renamed.

If the output reports zero discovered tests, stop before editing implementation files and report the test-runner blocker. Use `docs/adr/0003-load-rhino-plugin-as-dll-in-tests.md` to verify the plugin test assembly is copied as `AIQ.Urban.RhinoPlugin.dll`; do not treat a zero-test run as baseline evidence.

---

### Task 1: Restore Managed Centerline Replacement

**Files:**
- Modify: `tests/AIQ.Urban.RhinoPlugin.Tests/PipelineConfigurations/TransitPipelineConfigurationTests.cs:72-110`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectTransitCenterlinesStep.cs:34-76`

**Interfaces:**
- Consumes: `SelectTransitCenterlinesStep.Run(RhinoDoc) -> List<IModelEditData>` and the existing `WithTransitAdditionSteps()` remove-before-add recipe.
- Produces: one `TransitDeletionData` containing every accepted centerline ObjectId exactly once; no record when every selected centerline is rejected.

- [ ] **Step 1: Replace the masked regression test with real-pipeline record cases**

Replace `WithTransitAdditionSteps_GatheredExistingCenterline_ReplacesRhinoObjectWithoutDuplicate` with this test. Do not register `TransitDeletionGatherStep` in the replacement operation.

```csharp
[Test]
[TestCase("SameAttributes")]
[TestCase("ChangedAttributes")]
public void WithTransitAdditionSteps_ManagedCenterline_ReplacesTransitWithoutDuplicate(string testName)
{
    // Arrange
    var testData = GetManagedReplacementTestData(testName);
    using var doc = RhinoDoc.CreateHeadless(null);
    var points = testData.CenterlineCoordinates
        .Select(point => new Point3d(point[0], point[1], point[2]))
        .ToArray();
    var sourceId = doc.Objects.AddCurve(new PolylineCurve(points));

    new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
        .WithDataGatherStep(new SelectTransitCenterlinesStep(
            [doc.Objects.FindId(sourceId)!],
            testData.ExistingWidth,
            (TransitType)testData.ExistingTransitType))
        .WithTransitAdditionSteps()
        .WithFullModelOutput()
        .Run();

    var existingTransit = UrbanModel.Instance.Transits.Single();
    var oldGeometryIds = existingTransit.GetGeometryIds().ToList();
    var managedCenterline = doc.Objects.FindId(existingTransit.Centerline.Id)!;

    // Act
    new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
        .WithDataGatherStep(new SelectTransitCenterlinesStep(
            [managedCenterline],
            testData.ReplacementWidth,
            (TransitType)testData.ReplacementTransitType))
        .WithTransitAdditionSteps()
        .WithFullModelOutput()
        .Run();

    // Preview
#if DEBUG
    UrbanModel.Instance.VisualizeTransits();
#endif

    // Assert
    Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
    var replacement = UrbanModel.Instance.Transits.Single();
    Assert.That(replacement.Width, Is.EqualTo(testData.ReplacementWidth));
    Assert.That(replacement.TransitType, Is.EqualTo((TransitType)testData.ReplacementTransitType));
    Assert.That(replacement.Centerline.Id, Is.Not.EqualTo(existingTransit.Centerline.Id));
    Assert.That(oldGeometryIds.All(id => doc.Objects.FindId(id) is null), Is.True);
    Assert.That(doc.Objects.FindId(replacement.Centerline.Id), Is.Not.Null);
    RhinoDocAssertions.AssertLayerObjectCount(doc, testData.ExpectedCenterlineLayer, 1);
}
```

Add this primitive-only record and switch-expression lookup near the other private test-data declarations:

```csharp
private record ManagedReplacementTestData(
    string CaseName,
    double[][] CenterlineCoordinates,
    double ExistingWidth,
    int ExistingTransitType,
    double ReplacementWidth,
    int ReplacementTransitType,
    string ExpectedCenterlineLayer);

private static ManagedReplacementTestData GetManagedReplacementTestData(string caseName)
{
    return caseName switch
    {
        "SameAttributes" => new ManagedReplacementTestData(
            CaseName: "SameAttributes",
            CenterlineCoordinates: [[0, 0, 0], [50, 0, 0]],
            ExistingWidth: 6,
            ExistingTransitType: (int)TransitType.Road,
            ReplacementWidth: 6,
            ReplacementTransitType: (int)TransitType.Road,
            ExpectedCenterlineLayer: "AIQ Urban::Transit::Road::Centerline::6"),

        "ChangedAttributes" => new ManagedReplacementTestData(
            CaseName: "ChangedAttributes",
            CenterlineCoordinates: [[0, 0, 0], [50, 0, 0]],
            ExistingWidth: 6,
            ExistingTransitType: (int)TransitType.Road,
            ReplacementWidth: 4,
            ReplacementTransitType: (int)TransitType.CyclePath,
            ExpectedCenterlineLayer: "AIQ Urban::Transit::CyclePath::Centerline::4"),

        _ => throw new ArgumentException($"Unknown test case: {caseName}")
    };
}
```

- [ ] **Step 2: Run the replacement cases and verify RED**

Run:

```powershell
dotnet test 'tests\AIQ.Urban.RhinoPlugin.Tests\AIQ.Urban.RhinoInterface.Tests.csproj' --filter "FullyQualifiedName~TransitPipelineConfigurationTests.WithTransitAdditionSteps_ManagedCenterline_ReplacesTransitWithoutDuplicate" -v minimal
```

Expected: both cases are discovered and fail because `UrbanModel.Instance.Transits` contains 2 items after the second pipeline run. A compile error or zero discovered tests is not the expected red signal.

- [ ] **Step 3: Stage accepted ObjectIds in `SelectTransitCenterlinesStep`**

Add an accepted-ID collection beside `result` and `rejectedCount`:

```csharp
var result = new List<IModelEditData>();
var acceptedCenterlineIds = new List<Guid>();
var rejectedCount = 0;
```

After staging each accepted `TransitData` and `RhinoObjectDeletionData`, collect its ObjectId:

```csharp
var curve2D = new Curve2D(normalizedCurve, obj.Id);
result.Add(new TransitData(curve2D, width, transitType));
result.Add(new RhinoObjectDeletionData(obj.Id));
acceptedCenterlineIds.Add(obj.Id);
```

After the loop and before logging, stage one deletion record only when at least one centerline was accepted:

```csharp
if (acceptedCenterlineIds.Count > 0)
    result.Add(new TransitDeletionData(acceptedCenterlineIds.Distinct().ToList()));
```

Do not change `RemoveTransitStep`, `AddTransitStep`, or `WithTransitAdditionSteps()`.

- [ ] **Step 4: Run the replacement cases and verify GREEN**

Run the same focused command from Step 2.

Expected: 2 tests pass; each case leaves one Transit, removes its old geometry IDs, and bakes one centerline on the literal expected layer.

- [ ] **Step 5: Commit the replacement behavior**

```powershell
git add -- 'tests/AIQ.Urban.RhinoPlugin.Tests/PipelineConfigurations/TransitPipelineConfigurationTests.cs' 'src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectTransitCenterlinesStep.cs'
git commit -m "fix: replace transit for reused centerline identity"
```

---

### Task 2: Deduplicate Repeated Selection Identities

**Files:**
- Modify: `tests/AIQ.Urban.RhinoPlugin.Tests/PipelineConfigurations/TransitPipelineConfigurationTests.cs:111-141`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectTransitCenterlinesStep.cs:27-30`

**Interfaces:**
- Consumes: the Task 1 gatherer contract that emits accepted `TransitDeletionData`.
- Produces: at most one `TransitData`, one `RhinoObjectDeletionData`, and one deletion identity per selected ObjectId in a single operation.

- [ ] **Step 1: Replace the inline mixed-ground test with primitive-record gatherer cases**

Replace `SelectTransitCenterlinesStep_MixedGroundPlaneInputs_ReturnsDataAndWarningsPerCurve` with:

```csharp
[Test]
[TestCase("DistinctValidObjects")]
[TestCase("MixedValidAndInvalidObjects")]
[TestCase("RepeatedObject")]
public void SelectTransitCenterlinesStep_Selection_ReturnsExpectedEditData(string testName)
{
    // Arrange
    var testData = GetTransitGatherTestData(testName);
    using var doc = RhinoDoc.CreateHeadless(null);
    var objectIds = testData.CurveCoordinates
        .Select(coordinates => doc.Objects.AddCurve(new PolylineCurve(
            coordinates.Select(point => new Point3d(point[0], point[1], point[2])))))
        .ToList();
    var selected = testData.SelectedCurveIndices
        .Select(index => doc.Objects.FindId(objectIds[index])!)
        .ToList();
    var expectedAcceptedIds = testData.ExpectedAcceptedCurveIndices
        .Select(index => objectIds[index])
        .ToList();
    var loggerProvider = new CapturingLoggerProvider();
    using var loggerFactory = new LoggerFactory([loggerProvider]);
    var step = new SelectTransitCenterlinesStep(selected, 6, TransitType.Road, loggerFactory);

    // Act
    var result = step.Run(doc);

    // Preview
    TestContext.WriteLine($"Accepted identities: {string.Join(", ", expectedAcceptedIds)}");

    // Assert
    var transitData = result.OfType<TransitData>().ToList();
    var rhinoDeletionIds = result.OfType<RhinoObjectDeletionData>().Select(data => data.Id).ToList();
    var transitDeletionData = result.OfType<TransitDeletionData>().Single();
    Assert.That(transitData, Has.Count.EqualTo(expectedAcceptedIds.Count));
    Assert.That(rhinoDeletionIds, Is.EquivalentTo(expectedAcceptedIds));
    Assert.That(rhinoDeletionIds, Is.Unique);
    Assert.That(transitDeletionData.CenterlineIds, Is.EquivalentTo(expectedAcceptedIds));
    Assert.That(transitDeletionData.CenterlineIds, Is.Unique);
    Assert.That(
        transitData.SelectMany(data => data.Centerline.Geometry.Points()).Select(point => point.Z),
        Is.All.EqualTo(0));

    var warnings = loggerProvider.Entries.Where(entry => entry.Level == LogLevel.Warning).ToList();
    Assert.That(warnings, Has.Count.EqualTo(testData.ExpectedWarningCount));
    if (testData.ExpectedWarningText.Length > 0)
        Assert.That(warnings.Single().Message, Does.Contain(testData.ExpectedWarningText));
}
```

Add this primitive-only record and lookup:

```csharp
private record TransitGatherTestData(
    string CaseName,
    double[][][] CurveCoordinates,
    int[] SelectedCurveIndices,
    int[] ExpectedAcceptedCurveIndices,
    int ExpectedWarningCount,
    string ExpectedWarningText);

private static TransitGatherTestData GetTransitGatherTestData(string caseName)
{
    return caseName switch
    {
        "DistinctValidObjects" => new TransitGatherTestData(
            CaseName: "DistinctValidObjects",
            CurveCoordinates: [
                [[0, 0, 0], [10, 0, 0]],
                [[0, 5, 0.000009], [10, 5, -0.000009]],
            ],
            SelectedCurveIndices: [0, 1],
            ExpectedAcceptedCurveIndices: [0, 1],
            ExpectedWarningCount: 0,
            ExpectedWarningText: ""),

        "MixedValidAndInvalidObjects" => new TransitGatherTestData(
            CaseName: "MixedValidAndInvalidObjects",
            CurveCoordinates: [
                [[0, 0, 0], [10, 0, 0]],
                [[0, 10, 1], [10, 10, 1]],
            ],
            SelectedCurveIndices: [0, 1],
            ExpectedAcceptedCurveIndices: [0],
            ExpectedWarningCount: 1,
            ExpectedWarningText: "Z range"),

        "RepeatedObject" => new TransitGatherTestData(
            CaseName: "RepeatedObject",
            CurveCoordinates: [
                [[0, 0, 0], [10, 0, 0]],
            ],
            SelectedCurveIndices: [0, 0],
            ExpectedAcceptedCurveIndices: [0],
            ExpectedWarningCount: 0,
            ExpectedWarningText: ""),

        _ => throw new ArgumentException($"Unknown test case: {caseName}")
    };
}
```

- [ ] **Step 2: Run the gatherer cases and verify the repeated-object case is RED**

Run:

```powershell
dotnet test 'tests\AIQ.Urban.RhinoPlugin.Tests\AIQ.Urban.RhinoInterface.Tests.csproj' --filter "FullyQualifiedName~TransitPipelineConfigurationTests.SelectTransitCenterlinesStep_Selection_ReturnsExpectedEditData" -v minimal
```

Expected: `DistinctValidObjects` and `MixedValidAndInvalidObjects` pass; `RepeatedObject` fails because it produces 2 `TransitData` records and duplicate Rhino deletion IDs.

- [ ] **Step 3: Deduplicate selected Rhino objects before validation**

Change the initial selection query to:

```csharp
var selected = objectsToParse
    .Where(obj => obj.ObjectType == ObjectType.Curve)
    .DistinctBy(obj => obj.Id)
    .ToList();
```

Keep the distinct accepted-ID conversion from Task 1. It documents the operation-level uniqueness contract even though selected objects are now unique.

- [ ] **Step 4: Run the gatherer and replacement cases and verify GREEN**

Run:

```powershell
dotnet test 'tests\AIQ.Urban.RhinoPlugin.Tests\AIQ.Urban.RhinoInterface.Tests.csproj' --filter "FullyQualifiedName~TransitPipelineConfigurationTests.SelectTransitCenterlinesStep_Selection_ReturnsExpectedEditData|FullyQualifiedName~TransitPipelineConfigurationTests.WithTransitAdditionSteps_ManagedCenterline_ReplacesTransitWithoutDuplicate" -v minimal
```

Expected: 5 tests pass: 3 gatherer cases and 2 managed replacement cases.

- [ ] **Step 5: Commit selection identity deduplication**

```powershell
git add -- 'tests/AIQ.Urban.RhinoPlugin.Tests/PipelineConfigurations/TransitPipelineConfigurationTests.cs' 'src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectTransitCenterlinesStep.cs'
git commit -m "fix: deduplicate selected transit identities"
```

---

### Task 3: Lock Down Rejected Managed Centerlines

**Files:**
- Modify: `tests/AIQ.Urban.RhinoPlugin.Tests/PipelineConfigurations/TransitPipelineConfigurationTests.cs`

**Interfaces:**
- Consumes: `SelectTransitCenterlinesStep.Run(RhinoDoc)` validation behavior and the Task 1 rule that only accepted IDs enter `TransitDeletionData`.
- Produces: regression coverage proving rejected managed objects preserve their existing `Transit` instance and Rhino object.

- [ ] **Step 1: Add primitive-record managed-rejection cases**

Add this test:

```csharp
[Test]
[TestCase("OutsideGroundTolerance")]
[TestCase("SelfIntersecting")]
public void WithTransitAdditionSteps_RejectedManagedCenterline_PreservesExistingTransit(string testName)
{
    // Arrange
    var testData = GetRejectedManagedTransitTestData(testName);
    using var doc = RhinoDoc.CreateHeadless(null);
    var rejectedCurve = new PolylineCurve(testData.RejectedCoordinates
        .Select(point => new Point3d(point[0], point[1], point[2])));
    var managedId = doc.Objects.AddCurve(rejectedCurve);
    var existingTransit = new TransitData(
        new Curve2D(
            new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)),
            managedId),
        6,
        TransitType.Road).ToTransit();
    UrbanModel.Instance.AddTransit([existingTransit]);
    var loggerProvider = new CapturingLoggerProvider();
    using var loggerFactory = new LoggerFactory([loggerProvider]);

    // Act
    new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, loggerFactory)
        .WithDataGatherStep(new SelectTransitCenterlinesStep(
            [doc.Objects.FindId(managedId)!],
            4,
            TransitType.CyclePath,
            loggerFactory))
        .WithTransitAdditionSteps()
        .WithFullModelOutput()
        .Run();

    // Preview
    TestContext.WriteLine($"Preserved Transit ID: {existingTransit.Centerline.Id}");

    // Assert
    Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
    Assert.That(UrbanModel.Instance.Transits.Single(), Is.SameAs(existingTransit));
    Assert.That(existingTransit.Width, Is.EqualTo(6));
    Assert.That(existingTransit.TransitType, Is.EqualTo(TransitType.Road));
    Assert.That(existingTransit.Centerline.Geometry.GetBoundingBox(true).Min.Z, Is.EqualTo(0));
    Assert.That(doc.Objects.FindId(managedId), Is.Not.Null);
    var warning = loggerProvider.Entries.Single(entry => entry.Level == LogLevel.Warning);
    Assert.That(warning.Message, Does.Contain(testData.ExpectedWarningText));
}
```

Add the primitive-only record and lookup:

```csharp
private record RejectedManagedTransitTestData(
    string CaseName,
    double[][] RejectedCoordinates,
    string ExpectedWarningText);

private static RejectedManagedTransitTestData GetRejectedManagedTransitTestData(string caseName)
{
    return caseName switch
    {
        "OutsideGroundTolerance" => new RejectedManagedTransitTestData(
            CaseName: "OutsideGroundTolerance",
            RejectedCoordinates: [[0, 0, 1], [10, 0, 1]],
            ExpectedWarningText: "Z range"),

        "SelfIntersecting" => new RejectedManagedTransitTestData(
            CaseName: "SelfIntersecting",
            RejectedCoordinates: [[0, 0, 0], [10, 10, 0], [0, 10, 0], [10, 0, 0]],
            ExpectedWarningText: "self-intersects"),

        _ => throw new ArgumentException($"Unknown test case: {caseName}")
    };
}
```

- [ ] **Step 2: Run the managed-rejection cases**

Run:

```powershell
dotnet test 'tests\AIQ.Urban.RhinoPlugin.Tests\AIQ.Urban.RhinoInterface.Tests.csproj' --filter "FullyQualifiedName~TransitPipelineConfigurationTests.WithTransitAdditionSteps_RejectedManagedCenterline_PreservesExistingTransit" -v minimal
```

Expected: 2 tests pass. These are safety-characterization cases for behavior that already exists; they require no new production change.

- [ ] **Step 3: Commit rejected-input safety coverage**

```powershell
git add -- 'tests/AIQ.Urban.RhinoPlugin.Tests/PipelineConfigurations/TransitPipelineConfigurationTests.cs'
git commit -m "test: preserve rejected managed transits"
```

---

### Task 4: Full Verification and Knowledge Graph Refresh

**Files:**
- Verify: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectTransitCenterlinesStep.cs`
- Verify: `tests/AIQ.Urban.RhinoPlugin.Tests/PipelineConfigurations/TransitPipelineConfigurationTests.cs`
- Update if available: `graphify-out/`

**Interfaces:**
- Consumes: all behavior and tests from Tasks 1–3.
- Produces: a clean, verified branch with current graph metadata when the repository graph is initialized.

- [ ] **Step 1: Run the complete Transit pipeline fixture**

```powershell
dotnet test 'tests\AIQ.Urban.RhinoPlugin.Tests\AIQ.Urban.RhinoInterface.Tests.csproj' --filter "FullyQualifiedName~TransitPipelineConfigurationTests" -v minimal
```

Expected: every discovered `TransitPipelineConfigurationTests` case passes, including the 7 new record-based cases. Confirm the output reports a non-zero executed-test count.

- [ ] **Step 2: Run the complete Rhino interface test project**

```powershell
dotnet test 'tests\AIQ.Urban.RhinoPlugin.Tests\AIQ.Urban.RhinoInterface.Tests.csproj' -v minimal
```

Expected: all discovered tests pass. Existing package warnings may be reported separately, but no test failure or zero-test result is acceptable.

- [ ] **Step 3: Run formatting and diff checks**

```powershell
git diff --check
rg -n '\[DEBUG-' src tests
git status --short
```

Expected: `git diff --check` succeeds; no temporary `[DEBUG-...]` instrumentation is found; only intentional graph updates, if any, remain uncommitted.

- [ ] **Step 4: Refresh graphify metadata**

```powershell
graphify update .
```

Expected: the repository graph updates successfully. If graphify reports that `graphify-out/graph.json` is not initialized, record that exact pre-existing limitation and do not initialize a new graph as part of this focused bug fix.

- [ ] **Step 5: Commit graph metadata only if `graphify update .` changed tracked files**

First inspect:

```powershell
git status --short
git diff -- graphify-out
```

If tracked graph files changed, commit only them:

```powershell
git add -- graphify-out
git commit -m "chore: refresh code knowledge graph"
```

If no tracked graph files changed, do not create an empty commit.

- [ ] **Step 6: Confirm final branch state and commit history**

```powershell
git status --short
git log --oneline --decorate -5
```

Expected: clean worktree; the design commit plus the focused implementation/test commits are visible on `codex/duplicate-road-design`.
