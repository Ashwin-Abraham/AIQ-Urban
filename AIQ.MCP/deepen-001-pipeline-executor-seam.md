# Deepening Opportunity #1: Pipeline Executor Seam

Extract a pipeline execution seam from `McpServer` to make handler logic testable
without an active RhinoDoc, and concentrate pipeline lifecycle in one place.

---

## 1. Current Architecture

### Problem

Each mutation handler in `McpServer.cs` repeats the same 7-step pattern:

```
HandleAddBuilding / HandleAddTransit / HandleChangeBuildingUse
  │
  ├─ 1. Deserialize JSON arguments → typed request record
  ├─ 2. Call UrbanModelService → validate + create config/step objects
  ├─ 3. Grab RhinoDoc.ActiveDoc (throws if null)
  ├─ 4. Construct RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
  ├─ 5. Configure pipeline via PipelineConfigurations or direct step adds
  ├─ 6. pipeline.Run()
  ├─ 7. doc.Views.Redraw()
  └─ 8. Serialize result (GUIDs → JSON)
```

**Friction points:**

- **Duplication:** Steps 3–7 are identical in structure across all three handlers
  (~50 lines of boilerplate per handler).
- **No testability:** Every handler path except error-early (missing args) requires
  an active `RhinoDoc`. The HTTP integration tests (`McpServerHttpTests`) can only
  exercise error paths — the success path is untestable at this seam.
- **Leaky concerns:** Pipeline assembly logic (`WithMetricsCalculation`,
  `WithTransitAdditionSteps`) and Rhino-doc lifecycle leak into the transport
  layer. Adding a new mutation tool requires duplicating the full pattern.
- ****Lack of locality:** Changing how the pipeline executes (e.g., error handling,
  metrics config, redraw policy) requires editing all three handlers.

### Current call chain (add_building as representative example)

```
CallToolHandler (lambda in McpServer constructor)
  └─ HandleAddBuilding (static method)
       ├─ UrbanModelService.CreateBuildingSteps()      ← testable
       ├─ RhinoDoc.ActiveDoc                           ← NOT testable
       ├─ RhinoUrbanModelPipeline constructor           ← NOT testable
       ├─ PipelineConfigurations builder               ← NOT testable (depends on doc)
       ├─ pipeline.Run()                               ← NOT testable
       ├─ doc.Views.Redraw()                           ← NOT testable
       └─ JSON serialization of result                 ← partially testable
```

---

## 2. Design Tree

### Branch A: Interface shape

What should the `IPipelineExecutor` interface look like?

| Option | Shape | Pros | Cons |
|--------|-------|------|------|
| **A1 — Delegate** | `void Execute(Action<PipelineConfigurations> configure)` | Simple; caller retains full flexibility; PipelineConfigurations API is the contract | Requires PipelineConfigurations to be the configuration surface; change_building_use needs special handling |
| **A2 — Specific methods** | `void ExecuteBuildings(BuildingPipelineConfiguration)` / `ExecuteTransits(...)` / `ExecuteChangeUse(...)` | Each tool has its own typed method; no delegation needed | Proliferates methods; every new tool needs a new executor method; PipelineConfigurations already provides this abstraction |
| **A3 — Step list** | `void Execute(IReadOnlyList<IUrbanModelStep> steps)` | Generic across all step types | Caller loses PipelineConfigurations convenience (deletion registration, multi-step orchestration); `change_building_use` steps would bypass `WithBuildingUseChangeSteps` |
| **A4 — Raw pipeline delegate** | `void Execute(Action<RhinoUrbanModelPipeline> configure)` | Maximum flexibility | Leaks Rhino type into the seam; defeats the purpose of hiding Rhino dependencies |

**Decision: A1 — Delegate**

PipelineConfigurations is *already* the right abstraction for pipeline assembly.
It wraps the multi-step orchestration (delete old → add new → union → recalculate
parcels → metrics). The delegate approach lets handlers use its fluent API
directly, and the executor owns the lifecycle around it.

### Branch B: How change_building_use aligns with PipelineConfigurations

The `change_building_use` handler currently adds steps directly to the pipeline
(via `pipeline.WithUrbanElementStep(step)`) and uses `PipelineConfigurations`
only for metrics. But `PipelineConfigurations` already has a method:
`WithBuildingUseChangeSteps(List<Guid> geometryIds, Use newUse)`.

Two sub-options:

| Option | Approach | Impact |
|--------|----------|--------|
| **B1 — Change service to return groups** | `UrbanModelService.CreateChangeUseGroups()` returns `List<BuildingUseChangeGroup>` instead of `List<ChangeBuildingUseStep>` | Clean; handler iterates groups and calls `p.WithBuildingUseChangeSteps(...)` per group |
| **B2 — Keep service as-is, wrap steps** | The executor receives `ChangeBuildingUseStep` objects and wraps them in `PipelineConfigurations.WithBuildingUseChangeSteps(...)` internally | Requires reading step internals (GeometryIds, NewUse) — awkward; breaks encapsulation of the step |

**Decision: B1 — Change service to return groups**

A new method `CreateChangeUseGroups` on `UrbanModelService` returns grouped data
that maps directly to `PipelineConfigurations.WithBuildingUseChangeSteps`.
The old `CreateChangeUseSteps` is kept but the handler uses the new method.

New model record:
```csharp
public record BuildingUseChangeGroup(
    List<Guid> GeometryIds,
    Use NewUse);
```

### Branch C: Handler signature change (static → instance)

The current handlers are `private static` methods. To access the executor they
must become instance methods.

| Option | Approach | Impact |
|--------|----------|--------|
| **C1 — Instance methods + constructor injection** | `McpServer(IPipelineExecutor executor)` stores as field; handlers become instance methods | Clean DI; McpServer constructor changes; callers (RhinoPlugin) must provide executor |
| **C2 — Static method + parameter** | Keep handlers static but pass executor as parameter | Clumsy; every handler call passes the executor; lambda captures the field anyway |
| **C3 — Separate handler class** | Extract all handlers into `ToolHandler` class that takes executor+service | Clean separation; but adds a new abstraction layer for v1; risk of over-engineering |

**Decision: C1 — Instance methods + constructor injection**

Simplest change with cleanest seam. The plugin creates `new McpServer(new RhinoPipelineExecutor())`.

**Note on the `list_elements` handler:** It doesn't use the executor (no pipeline
execution). It creates a new `UrbanModelService()` inline. For scope of this
candidate, `list_elements` is left as-is (it has no Rhino dependency and is
already testable through the HTTP integration tests). Extracting an
`IUrbanModelService` seam is a separate deepening opportunity.

### Branch D: Executor constructor injection into McpServer

Current callers of `McpServer`:
- **AIQ.Urban.RhinoPlugin** — creates `new McpServer()`, calls Start(port)
- **Standalone test (Program.cs)** — creates `new McpServer()`, calls Start(port)

Both must pass an executor. Default constructor for backward compat is possible
but would silently create a RhinoPipelineExecutor, defeating testability.

**Decision:** Explicit constructor only. No default constructor. The test project
passes `FakePipelineExecutor`. The plugin passes `RhinoPipelineExecutor`.

### Branch E: Test strategy

What can we now test that we couldn't before?

| Test | Before | After |
|------|--------|-------|
| Handler deserializes args correctly | ❌ (needs RhinoDoc) | ✅ (FakePipelineExecutor) |
| Handler extracts correct GUIDs from config | ❌ | ✅ |
| Handler calls executor with correct configure action | ❌ | ✅ |
| Handler serializes result correctly | ❌ | ✅ |
| Executor creates pipeline and runs it | ❌ | ✅ (via RhinoPipelineExecutor + RhinoTestFixture) |
| Executor throws if no active doc | ❌ | ✅ |

---

## 3. Chosen Approach — Summary

```
┌─────────────────────────────────────────────────────────────────────┐
│                          McpServer                                  │
│                                                                     │
│  CallToolHandler ──→ dispatch by name                                │
│       │                                                              │
│       ├─ "list_elements" ──→ (unchanged, no executor needed)         │
│       │                                                              │
│       ├─ "add_building"  ──→ HandleAddBuilding (instance) ─────┐    │
│       │                              │                         │    │
│       ├─ "add_transit"  ──→ HandleAddTransit (instance) ──────┤    │
│       │                              │                         │    │
│       └─ "change_building_use"       │                         │    │
│              ──→ HandleChangeUse (instance) ──────────────────┤    │
│                                                                │    │
│  ┌─────────────────────────────────────────────────────────────┘    │
│  │  IPipelineExecutor.Execute(configure)                             │
│  │    ├─ RhinoDoc.ActiveDoc (or throw)                              │
│  │    ├─ new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)       │
│  │    ├─ new PipelineConfigurations(pipeline)                        │
│  │    ├─ configure(configs)                                          │
│  │    ├─ configs.WithMetricsCalculation(...)  (added by executor)    │
│  │    ├─ pipeline.Run()                                              │
│  │    └─ doc.Views.Redraw()                                          │
│  └─────────────────────────────────────────────────────────────┐    │
└──────────────────────────────────────────────────────────────────│────┘
                                                                   │
                                                   ┌───────────────┘
                                                   ▼
                                          RhinoPipelineExecutor
                                          (implements IPipelineExecutor)
```

### What moves where

| Concern | Before | After |
|---------|--------|-------|
| RhinoDoc.ActiveDoc access | in each handler | in RhinoPipelineExecutor only |
| Pipeline construction | in each handler | in RhinoPipelineExecutor only |
| PipelineConfigurations creation | in each handler | in RhinoPipelineExecutor only |
| Metrics calculation config | in each handler | in RhinoPipelineExecutor only (automatic, with doc-derived tolerance/name) |
| pipeline.Run() | in each handler | in RhinoPipelineExecutor only |
| doc.Views.Redraw() | in each handler | in RhinoPipelineExecutor only |
| Args deserialization | in each handler | unchanged (stays in handler) |
| Service call to create config | in each handler | unchanged |
| Result building (GUID extraction) | in each handler | unchanged |

---

## 4. Files to Create

### `Services/IPipelineExecutor.cs`

```csharp
using AIQ.Urban.RhinoInterface.Pipeline;

namespace AIQ.MCP.Services;

/// <summary>
/// Adapter that owns the pipeline lifecycle.
/// Behind this seam, RhinoDoc access, pipeline construction, execution,
/// and redraw are concentrated.
/// </summary>
public interface IPipelineExecutor
{
    /// <summary>
    /// Executes pipeline steps through the full cascade.
    /// The configure callback receives a PipelineConfigurations builder for
    /// adding steps. Metrics calculation is added automatically. Returns
    /// after pipeline.Run() and doc.Views.Redraw().
    /// </summary>
    void Execute(Action<PipelineConfigurations> configure);
}
```

### `Services/RhinoPipelineExecutor.cs`

```csharp
using System.Diagnostics;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Pipeline;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Rhino;

namespace AIQ.MCP.Services;

/// <summary>
/// Rhino-backed pipeline executor. Requires an active RhinoDoc.
/// Used in production inside the Rhino plugin; not suitable for unit tests.
/// </summary>
public class RhinoPipelineExecutor : IPipelineExecutor
{
    public void Execute(Action<PipelineConfigurations> configure)
    {
        var doc = RhinoDoc.ActiveDoc
            ?? throw new McpProtocolException(
                "Active Rhino document is required. Ensure the MCP server is running within Rhino.",
                McpErrorCode.InternalError);

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        var configs = new PipelineConfigurations(pipeline);
        configure(configs);
        configs.WithMetricsCalculation(
            saveToFile: true,
            tolerance: doc.ModelAbsoluteTolerance,
            docName: doc.Name);
        pipeline.Run();
        doc.Views.Redraw();
    }
}
```

### `Models/BuildingUseChangeGroup.cs`

```csharp
using AIQ.Urban.Core.Models;

namespace AIQ.MCP.Models;

/// <summary>
/// A group of buildings to change to a single use.
/// Produced by UrbanModelService.CreateChangeUseGroups and consumed by
/// PipelineConfigurations.WithBuildingUseChangeSteps.
/// </summary>
public record BuildingUseChangeGroup(
    List<Guid> GeometryIds,
    Use NewUse);
```

---

## 5. Files to Modify

### `Services/UrbanModelService.cs`

**Add** a new method that returns grouped data instead of step objects:

```csharp
/// <summary>
/// Groups change items by new use name and validates GUIDs.
/// Returns one BuildingUseChangeGroup per distinct use, for direct consumption
/// by PipelineConfigurations.WithBuildingUseChangeSteps.
/// </summary>
public List<BuildingUseChangeGroup> CreateChangeUseGroups(ChangeBuildingUseRequest request)
{
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(request.Items);

    if (request.Items.Count == 0)
        throw new ArgumentException("At least one change item is required.");

    var groups = request.Items
        .GroupBy(i => i.NewUseName, StringComparer.OrdinalIgnoreCase)
        .ToList();

    var result = new List<BuildingUseChangeGroup>(groups.Count);

    foreach (var group in groups)
    {
        var use = GeometryFactory.ResolveUse(group.Key);
        var geometryIds = group.Select(i => i.BuildingGeometryId).ToList();

        foreach (var id in geometryIds)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Building geometry ID cannot be empty.");
        }

        result.Add(new BuildingUseChangeGroup(geometryIds, use));
    }

    return result;
}
```

**Keep** existing `CreateChangeUseSteps` (it is tested and may have other callers).

### `Services/McpServer.cs`

**Changes:**

1. Add constructor parameter and field:
```csharp
private readonly IPipelineExecutor _executor;

public McpServer(IPipelineExecutor executor)
{
    _executor = executor;
    // existing init...
}
```

2. Change handlers from `static` to instance methods:
```csharp
// Before:
private static CallToolResult HandleAddBuilding(RequestContext<CallToolRequestParams> request)

// After:
private CallToolResult HandleAddBuilding(RequestContext<CallToolRequestParams> request)
```

3. Replace inline pipeline lifecycle with executor call in each handler:

**HandleAddBuilding** (before → after):
```csharp
// Before:
var service = new UrbanModelService();
var config = service.CreateBuildingSteps(addRequest);
var doc = RhinoDoc.ActiveDoc;
// ... null check ...
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
new PipelineConfigurations(pipeline)
    .WithBuildingAdditionSteps(config)
    .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name);
pipeline.Run();
doc.Views.Redraw();
var elementGuids = config.InputCurveIds.Select(id => id.ToString()).ToList();

// After:
var service = new UrbanModelService();
var config = service.CreateBuildingSteps(addRequest);
_executor.Execute(p => p.WithBuildingAdditionSteps(config));
var elementGuids = config.InputCurveIds.Select(id => id.ToString()).ToList();
```

**HandleAddTransit** (before → after):
```csharp
// Before:
var service = new UrbanModelService();
var config = service.CreateTransitSteps(addRequest);
var doc = RhinoDoc.ActiveDoc;
// ... null check, pipeline construction, execution, redraw ...
var elementGuids = config.CenterlineIds.Select(id => id.ToString()).ToList();

// After:
var service = new UrbanModelService();
var config = service.CreateTransitSteps(addRequest);
_executor.Execute(p => p.WithTransitAdditionSteps(config));
var elementGuids = config.CenterlineIds.Select(id => id.ToString()).ToList();
```

**HandleChangeBuildingUse** (before → after):
```csharp
// Before:
var service = new UrbanModelService();
var steps = service.CreateChangeUseSteps(changeRequest);
var doc = RhinoDoc.ActiveDoc;
// ... null check ...
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
foreach (var step in steps)
    pipeline.WithUrbanElementStep(step);
new PipelineConfigurations(pipeline)
    .WithMetricsCalculation(...);
pipeline.Run();
doc.Views.Redraw();
var affectedGuids = changeRequest.Items.Select(...).ToList();

// After:
var service = new UrbanModelService();
var useGroups = service.CreateChangeUseGroups(changeRequest);
_executor.Execute(p =>
{
    foreach (var g in useGroups)
        p.WithBuildingUseChangeSteps(g.GeometryIds, g.NewUse);
});
var affectedGuids = changeRequest.Items
    .Select(i => i.BuildingGeometryId.ToString())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();
```

4. Remove `using Rhino;` and `using AIQ.Urban.RhinoInterface;` from McpServer.cs
   (no longer needed since pipeline construction moves to RhinoPipelineExecutor).

### `AIQ.MCP.csproj`

No change needed — all required references (`AIQ.Urban.Core`, `AIQ.Urban.RhinoInterface`,
`ModelContextProtocol`) are already present.

### `Program.cs` (standalone test entry point)

```csharp
// Before:
using var server = new McpServer();

// After:
using var server = new McpServer(new RhinoPipelineExecutor());
```

### RhinoPlugin (caller in AIQ.Urban.RhinoPlugin)

The plugin that instantiates `McpServer` must pass the executor:

```csharp
// Before:
_server = new McpServer();

// After:
_server = new McpServer(new RhinoPipelineExecutor());
```

(This is in a different project — AIQ.Urban.RhinoPlugin — but must be updated
for the build to succeed.)

---

## 6. Test Plan

### New test file: `Services/PipelineExecutorTests.cs` (or extend McpServerTests)

These tests do **not** require `[RhinoTestFixture]` — they use `FakePipelineExecutor`:

```csharp
public class FakePipelineExecutor : IPipelineExecutor
{
    public Action<PipelineConfigurations>? LastConfigure { get; private set; }
    public int ExecuteCallCount { get; private set; }

    public void Execute(Action<PipelineConfigurations> configure)
    {
        ExecuteCallCount++;
        LastConfigure = configure;
    }
}
```

#### Test scenarios

| Test | What it verifies |
|------|-----------------|
| `HandleAddBuilding_ValidRequest_ExecutesPipeline` | Executor called once with non-null configure action |
| `HandleAddBuilding_ValidRequest_ReturnsElementGuids` | Response JSON contains "elementGuids" with correct count |
| `HandleAddBuilding_MissingArguments_ThrowsMcpError` | Error path unchanged (no executor call) |
| `HandleAddTransit_ValidRequest_ExecutesPipeline` | Executor called once with non-null configure action |
| `HandleAddTransit_ValidRequest_ReturnsCenterlineGuids` | Response JSON contains "elementGuids" with correct count |
| `HandleChangeBuildingUse_ValidRequest_ExecutesPipeline` | Executor called once |
| `HandleChangeBuildingUse_ValidRequest_ReturnsAffectedGuids` | Response JSON contains unique "affectedGuids" |
| `HandleChangeBuildingUse_MultipleDistinctUses_OneExecuteCall` | Single executor call despite multiple use groups |
| `HandleChangeBuildingUse_EmptyItems_Throws` | Error before executor call |

### Existing test changes: `UrbanModelServiceTests.cs`

Add tests for new `CreateChangeUseGroups` method:
- Valid single group → returns one group with correct GeometryIds and Use
- Valid multiple same use → returns one merged group
- Valid multiple distinct uses → returns N groups
- Empty GUID → throws
- Unknown use name → throws

### Existing test changes: `McpServerTests.cs`

Update `McpServer` construction in all existing tests to pass an executor:

```csharp
// Before:
using var server = new McpServer();

// After:
using var server = new McpServer(new RhinoPipelineExecutor());
```

These tests already require Rhino (they're in a Rhino test context), so using
the real executor is fine. No behaviour change expected.

### What we still CANNOT unit test (and why that's OK)

| Area | Reason | Mitigation |
|------|--------|------------|
| That PipelineConfigurations.W* actually adds correct steps | Rhino geometry dependency | Tested in AIQ.Urban.RhinoInterface tests |
| That RhinoPipelineExecutor creates the right pipeline | RhinoDoc dependency | Integration test via McpServerHttpTests |
| That metrics file is written to disk | File system + pipeline dependency | Integration/E2E test |

---

## 7. Implementation Order

| Step | File(s) | Change | Testable after |
|------|---------|--------|----------------|
| 1 | `Services/IPipelineExecutor.cs` | Create interface | — |
| 2 | `Services/RhinoPipelineExecutor.cs` | Create Rhino implementation | — |
| 3 | `Models/BuildingUseChangeGroup.cs` | Create model record | — |
| 4 | `Services/UrbanModelService.cs` | Add `CreateChangeUseGroups` method | ✅ |
| 5 | `Services/McpServer.cs` | Add constructor param, change handlers to instance methods, replace pipeline lifecycle with `_executor.Execute(...)` | ✅ (with FakePipelineExecutor) |
| 6 | `tests/.../McpServerTests.cs` | Update McpServer construction | ✅ |
| 7 | `tests/.../UrbanModelServiceTests.cs` | Add tests for `CreateChangeUseGroups` | ✅ |
| 8 | `tests/.../PipelineExecutorTests.cs` | Create handler unit tests with FakePipelineExecutor | ✅ |
| 9 | `Program.cs` | Pass RhinoPipelineExecutor | — |
| 10 | AIQ.Urban.RhinoPlugin | Pass RhinoPipelineExecutor | — |

---

## 8. Deletion Test Verification

**Before:** Deleting McpServer would require reimplementing 3 handlers + transport
in every caller. Complexity is concentrated — McpServer is earning its keep as
the transport layer.

**After:** Deleting McpServer still requires reimplementing transport. But the
pipeline lifecycle no longer leaks into transport — it's local in
`RhinoPipelineExecutor`. Deleting `RhinoPipelineExecutor` would require
reimplementing pipeline lifecycle in each handler that needs it. Deleting
`IPipelineExecutor` would require the same + breaking the test seam.

The seam is **deepening** the system: `IPipelineExecutor` (1 method) hides
~30 lines of Rhino lifecycle per handler × 3 handlers = ~90 lines of
complexity concentrated in one adapter. The **interface is the test surface**.
One adapter (`RhinoPipelineExecutor`) is the production path; the test adapter
(`FakePipelineExecutor`) makes the hypothetical seam real.

---

## 9. Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| `PipelineConfigurations` constructor or methods change in RhinoInterface | The delegate type `Action<PipelineConfigurations>` couples to it; but this is the same coupling the handlers already have |
| Existing McpServer callers (RhinoPlugin, Program.cs) miss the new constructor parameter | Both callers are within the solution; compiler error catches it |
| `ChangeBuildingUseStep` validation in `CreateChangeUseGroups` differs subtly from `CreateChangeUseSteps` | Both methods share the same validation logic; keep old method as reference for the first implementation, then delete it once the new method is tested |
| WithMetricsCalculation called inside executor loses control over parameters | The parameters (tolerance, docName) are derived from RhinoDoc which the executor already owns. If future tools need different metrics config, the executor can be extended with an options parameter |
