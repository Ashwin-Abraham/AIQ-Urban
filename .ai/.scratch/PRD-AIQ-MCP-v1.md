## Problem Statement

Today, the AIQ.Urban modelling functionality is only accessible via Rhino's interactive UI — users must click in the viewport, select geometry, and navigate option dialogs to add transits, add buildings, or change uses. There is no programmatic or AI-accessible interface to the urban model.

Architects and planners who want to iterate on site plans via conversation, automated scripts, or AI assistance have no way to interact with the model without running Rhino commands manually. Each design iteration requires repetitive clicking — select curve, set options, confirm — which is slow and breaks the flow of rapid exploration.

## Solution

Build an MCP (Model Context Protocol) server that runs in-process inside the AIQ.Urban.RhinoPlugin and exposes the existing urban modelling capabilities as structured AI-callable tools. The server uses HTTP/SSE transport, auto-starts with the plugin, and lets any MCP-compatible AI tool (Claude Desktop, Cursor, etc.) read the model state and invoke mutations.

The AI communicates via four tools:

- **`list_elements`** — discover all transits, buildings, parcels, and zones in the current model
- **`add_transit`** — create one or more transit corridors from coordinate arrays
- **`add_building`** — create one or more buildings from coordinate arrays
- **`change_building_use`** — change the use of one or more buildings

Each mutation triggers the full pipeline cascade (transit surface union → parcel recalculation → metrics), so the model stays consistent after every operation. The AI discovers element GUIDs via `list_elements` and reuses them in mutation calls — no Rhino viewport interaction required.

## User Stories

1. As an AI agent, I want to call `add_transit` with an array of centerline coordinates, widths, and transit types, so that I can create a road/rail/cycle network programmatically.

2. As an AI agent, I want to call `add_building` with an array of footprint coordinates, floor heights, floor counts, and use names, so that I can place buildings on the site plan.

3. As an AI agent, I want to call `change_building_use` with a building envelope GUID and a new use name, so that I can rezone a building's floor plates in one step.

4. As an AI agent, I want to call `list_elements` to discover all transits, buildings, parcels, and zones in the current model with their GUIDs and summaries, so that I can reference them in subsequent mutation calls.

5. As an AI agent, I want to pass arrays of items to each mutation tool (e.g. 5 buildings at once), so that I can batch operations efficiently and trigger only one cascade per batch.

6. As a human architect, I want the MCP server to auto-start when the Rhino plugin loads, so that I don't need to remember a separate startup step.

7. As a human architect, I want the MCP server to auto-stop when the plugin unloads, so that there are no orphan processes or port conflicts.

8. As a human architect, I want the server to use a dynamic port (with the actual port written to a known temp file), so that port conflicts are avoided and the AI tool can discover the endpoint.

9. As an AI agent, I want descriptive error messages when I pass invalid coordinates, unknown use names, or stale GUIDs, so that I can self-correct and retry.

10. As an AI agent, I want the model to remain consistent after every mutation — transit surfaces unioned, parcels recalculated, metrics updated — so that subsequent `list_elements` calls always return accurate state.

11. As an AI agent, I want element summaries serialised in a stable JSON format via a dedicated custom serializer, so that future serialization needs (persistence, export) don't couple to the AI-facing format.

12. As a developer, I want the UrbanModelService to be testable without a RhinoDoc, so that I can write fast unit tests for coordinate geometry construction and step creation.

13. As a developer, I want a new `AIQ.MCP.Tests` project following the existing NUnit + Rhino.Testing patterns, so that the test infrastructure is consistent with the rest of the AIQ.Urban suite.

14. As a developer, I want the service layer to throw descriptive exceptions on bad input, so that the MCP server can translate them into meaningful JSON-RPC error responses.

15. As an AI agent, I want `change_building_use` to operate at building granularity (all floor plates of a building), so that the interface stays simple in v1.

16. As an AI agent, I want `list_elements` to include the area, use, and centroid for parcels and buildings, so that I have enough spatial context to make design decisions.

## Implementation Decisions

### Modules

#### Module 1 — Service Models (`AIQ.MCP/Models/`)
Record types for all tool inputs and outputs:

- `AddTransitRequest` — `List<TransitItem>` where each item has `CenterlineCoords` (double[][]), `Width` (double), `TransitType` (string)
- `AddBuildingRequest` — `List<BuildingItem>` where each item has `FootprintCoords` (double[][]), `TypicalFloorHeight` (double), `FloorCount` (int), `UseName` (string)
- `ChangeBuildingUseRequest` — `List<ChangeUseItem>` where each item has `BuildingGeometryId` (Guid), `NewUseName` (string)
- `ElementSummary` — per-element output record with `Id` (Guid), `Type` (string), `Use` (string?), `Summary` (string), plus type-specific fields (area, centroid, width, floor count, etc.)
- `ListElementsResponse` — `Transits`, `Buildings`, `Parcels`, `Zones` lists of `ElementSummary`

No Rhino dependency. Testable in isolation.

#### Module 2 — Geometry Factory (`AIQ.MCP/Services/GeometryFactory.cs`)
Thin translation layer that converts coordinate arrays into Rhino geometry objects:

- `CreateCenterline(double[][] coords)` → `Curve2D`
- `CreateFootprint(double[][] coords)` → `Polygon2D` (validates closed)
- `ResolveTransitType(string name)` → `TransitType` enum
- `ResolveUse(string name)` → `Use` instance (looks up in `DefaultUses`)

Requires RhinoCommon (for `Curve2D`, `Polygon2D`). Pure computation — no RhinoDoc required.

#### Module 3 — UrbanModelService (`AIQ.MCP/Services/UrbanModelService.cs`)
Deep module that encapsulates the coordination between structured input and the existing pipeline configuration layer:

```
service.CreateTransitSteps(request) → (AddTransitStep, RemoveTransitStep, TransitSurfaceUnioner, RecalculateParcelsStep, CenterlineIds)
service.CreateBuildingSteps(request) → (AddBuildingStep, InputCurveIds)
service.CreateChangeUseSteps(request) → ChangeBuildingUseStep
service.GetElementSummaries() → ListElementsResponse
```

The service constructs Rhino geometry via GeometryFactory, creates step objects, and returns them. It does NOT execute the pipeline. The caller (MCP tool handler) constructs `RhinoUrbanModelPipeline(doc, UrbanModel.Instance)`, wires steps via `PipelineConfigurations`, and calls `Run()`.

This decomposition keeps the service testable without a RhinoDoc while keeping pipeline execution (which needs RhinoDoc for baking) in the MCP tool handler.

#### Module 4 — MCP Server (`AIQ.MCP/McpServer.cs`)
HTTP/SSE server using the `ModelContextProtocol` SDK. Responsibilities:

- Tool registration (4 tools)
- JSON-RPC request deserialisation
- Dispatch to `UrbanModelService`
- Coordinate pipeline execution (construct pipeline, run, redraw)
- Exception → JSON-RPC error translation
- Custom JSON serializer for element summaries in `list_elements`
- SSE event loop for real-time communication

Server lifecycle:
- `Start(int port)` — bind HTTP listener, register tools, begin accepting connections
- `Stop()` — graceful shutdown
- `Port` property — the actual port (0 → system-assigned)

#### Module 5 — Rhino Plugin Integration (`AIQ.Urban.RhinoPlugin` modification)
Changes to the existing `Urban_RhinoPlugin`:

- **OnLoad**: instantiate `McpServer`, call `Start(0)`, write port to `%TEMP%\AIQ.MCP.port`
- **OnUnload**: call `McpServer.Stop()`, delete port file
- The plugin already has `<ProjectReference Include="..\..\AIQ.MCP\AIQ.MCP.csproj" />` — no project reference change needed.

#### Module 6 — Test Project (`tests/AIQ.MCP.Tests/`)
New NUnit test project following the existing AIQ.Urban test conventions. Details in Testing Decisions below.

### API Contracts

All tool inputs are batch-capable arrays:

**`add_transit` input:**
```json
{
  "items": [
    {"centerline": [[0,0], [10,0], [20,5]], "width": 7.0, "transitType": "Road"},
    {"centerline": [[0,10], [10,10]], "width": 3.0, "transitType": "CyclePath"}
  ]
}
```

**`add_building` input:**
```json
{
  "items": [
    {"footprint": [[0,0], [10,0], [10,20], [0,20], [0,0]], "typicalFloorHeight": 3.5, "floorCount": 5, "useName": "Residential"}
  ]
}
```

**`change_building_use` input:**
```json
{
  "items": [
    {"buildingGeometryId": "guid-here", "newUseName": "Commercial"}
  ]
}
```

**`list_elements` element summary shape:**
```json
{
  "id": "guid",
  "type": "Building",
  "use": "Residential",
  "summary": "5-storey building at (5.0, 10.0)",
  "details": {
    "footprintCenter": [5.0, 10.0],
    "floorCount": 5,
    "totalHeight": 17.5
  }
}
```

The custom JSON serializer produces this format independently of any future serialization (persistence, export, etc.).

### Pipeline Cascade

Each mutation tool handler constructs a `RhinoUrbanModelPipeline(doc, UrbanModel.Instance)` and configures it via `PipelineConfigurations`:

- **add_transit**: `.WithTransitAdditionSteps(config).WithMetricsCalculation(...)`
- **add_building**: `.WithBuildingAdditionSteps(config).WithMetricsCalculation(...)`
- **change_building_use**: `.WithBuildingUseChangeSteps(geometryIds, newUse).WithMetricsCalculation(...)`

After `pipeline.Run()`, the handler calls `doc.Views.Redraw()` and returns the result.

### Error Handling

The `UrbanModelService` and `GeometryFactory` throw descriptive exceptions:

- Unknown use name → `ArgumentException("Unknown use name: 'Foo'. Valid names: Residential, Commercial, ...")`
- Non-closed footprint → `ArgumentException("Footprint coordinates must form a closed ring")`
- Stale GUID → `KeyNotFoundException("Building with geometry GUID X not found in the model")`
- Empty model → `InvalidOperationException("Model is empty. Add elements first.")`

The MCP server catches all exceptions and returns a JSON-RPC error response with the exception message. The AI reads the message and self-corrects.

## Testing Decisions

### What makes a good test

Tests verify external behaviour of each module — inputs in, outputs out, errors thrown on bad input. No mocking. Tests exercise the real `UrbanModel` singleton (reset before each test).

### Test infrastructure

New project `tests/AIQ.MCP.Tests/` following the established AIQ.Urban conventions:

- **Framework**: NUnit 3.14, Rhino.Testing 8.0.28-beta
- **Fixture**: `[RhinoTestFixture]` for tests that need Rhino geometry types
- **Setup**: `UrbanModel.Instance.Reset()` in `[SetUp]`
- **Target**: `net8.0-windows`
- **Project reference**: `AIQ.MCP`, `AIQ.Urban.Core`
- **Rhino.Testing.Configs.xml**: copied to output (matching existing pattern)

### Modules tested

**UrbanModelService** — comprehensive test coverage:

| Test area | What it tests |
|-----------|---------------|
| `CreateTransitSteps` valid input | Correct step types returned, centerline coordinates produce valid Curve2D |
| `CreateTransitSteps` invalid coordinates | Exception on non-planar, empty, or malformed coordinate arrays |
| `CreateTransitSteps` unknown transit type | Exception with helpful error listing valid types |
| `CreateBuildingSteps` valid input | Correct step types returned, footprint produces valid Polygon2D |
| `CreateBuildingSteps` non-closed footprint | Exception |
| `CreateBuildingSteps` unknown use name | Exception with helpful error |
| `CreateChangeUseSteps` valid input | Correct ChangeBuildingUseStep with expected GUIDs and Use |
| `CreateChangeUseSteps` empty GUID list | Exception |
| `GetElementSummaries` populated model | All element types present in response, correct GUIDs and summaries |
| `GetElementSummaries` empty model | Empty lists for all four categories |

**GeometryFactory** — focused coordinate→geometry tests:

| Test area | What it tests |
|-----------|---------------|
| Centerline construction | Correct point count, planar, non-closed |
| Footprint construction | Closed ring, planar |
| TransitType resolution | Valid names match enum, invalid throws |
| Use resolution | Valid names match DefaultUses, invalid throws |

**MCP Server** — not tested directly in unit tests (Rhino process dependency). Integration testing deferred.

### Prior art

Refer to the existing `tests/AIQ.Urban.Core.Tests/` test files for the pattern:

- `ModelActions/AddBuildingStepTests.cs` — parametrized `[TestCase]`, record data, geometry construction
- `ModelActions/RemoveTransitStepTests.cs` — `BuildTestData()` factory method
- The test CONTEXT.md at `tests/AIQ.Urban.Core.Tests/CONTEXT.md` documents the full convention

### What is NOT tested

- The MCP server transport layer (HTTP/SSE, tool registration) — deferred to integration testing
- The Rhino plugin integration (start/stop lifecycle) — deferred to RhinoPlugin.Tests
- Existing AIQ.Urban.Core step logic — already covered by AIQ.Urban.Core.Tests

## Out of Scope

- Per-floor-plate use changes (v1 only does building-wide; per-plate can be added as a separate tool later)
- `add_zone` or `change_parcel_use` MCP tools (only the three mutations described)
- `recalculate` or `rebuild` explicit tool (cascade runs automatically per mutation)
- Model reconciliation on file open (empty model after file open is an accepted gap)
- Persistence or import/export of the urban model
- Vue UI or web dashboard for the MCP server
- Rhino viewport streaming or geometry preview to the AI
- Authentication, rate limiting, or multi-user access on the HTTP endpoint (localhost-only)
- Health check or status endpoints beyond the standard MCP protocol
- Grasshopper integration via MCP

## Further Notes

### File structure for new test project
```
tests/AIQ.MCP.Tests/
├── CONTEXT.md
├── AIQ.MCP.Tests.csproj
├── Rhino.Testing.Configs.xml
├── Services/
│   ├── UrbanModelServiceTests.cs
│   └── GeometryFactoryTests.cs
```

### Solution-level changes
- Add `tests/AIQ.MCP.Tests/AIQ.MCP.Tests.csproj` to `ArchSitePlan.sln`
- Add `ModelContextProtocol` NuGet package to `AIQ.MCP.csproj`
- No changes to `AIQ.Urban.Core`, `AIQ.Urban.RhinoInterface`, or any other existing project API surface — the MCP layer composes existing abstractions
