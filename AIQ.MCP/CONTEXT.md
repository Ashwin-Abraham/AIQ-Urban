# AIQ.MCP — Domain Context

## Purpose
Thin interoperability layer that exposes the existing AIQ.Urban.Core urban modelling and analysis functionality as structured AI-callable tools using the Model Context Protocol (MCP). Hosted in-process inside the AIQ.Urban.RhinoPlugin.

## Design Constraints

### Dependency Rule
- **AIQ.MCP** references `AIQ.Urban.Core` and `AIQ.Urban.RhinoInterface`.
- AIQ.MCP depends on **no** other solution projects.
- AIQ.Urban.RhinoPlugin references AIQ.MCP to start/stop the server.
- NuGet package: `ModelContextProtocol` (official MCP C# SDK).

### Process Model
- MCP server runs **in-process inside Rhino** as part of the AIQ.Urban.RhinoPlugin.
- Transport: **HTTP/SSE** — the plugin starts an embedded HTTP listener on `OnLoad`, stops it on `OnUnload`.
- The AI tool connects to `http://localhost:<port>/sse`.
- Port discovery: server binds to port 0, writes actual port to `%TEMP%\AIQ.MCP.port`.
- Server auto-starts on plugin load; auto-stops on plugin unload.

### No Direct RhinoDoc Access
- The MCP server does **not** access `RhinoDoc` directly.
- It calls into a data-only **UrbanModelService** that constructs pipeline steps from structured input.
- The MCP tool handler (caller) owns `RhinoDoc` access and pipeline execution.

### Error Handling
- All service methods throw descriptive exceptions on failure (invalid input, stale GUIDs, empty model).
- The MCP layer catches exceptions and returns JSON-RPC error responses.
- The AI reads the error message and self-corrects.

### Model State After File Open
- `UrbanModel` singleton is empty on file open (no reconciliation pass yet).
- `list_elements` returns empty until the user (or AI) repopulates via commands.
- Accepted gap for v1 — documented in AIQ.Urban.Rhino CONTEXT.md.

### Pipeline Cascade
- Each MCP mutation tool triggers the full pipeline cascade (steps → unions → parcels → metrics).
- All mutation tools accept arrays of items to support batching (maps to existing `List<T>` step records).
- No separate `recalculate` tool — cascade runs once per tool call.

## Tools (v1)

### `add_transit`
Adds one or more transit corridors. Accepts an array.
- **Input:** `[{centerline: [[x,y], ...], width, transitType}], ...`
- **Output:** `{ elementGuids: [guid, ...] }`

### `add_building`
Adds one or more buildings. Accepts an array.
- **Input:** `[{footprint: [[x,y], ...], typicalFloorHeight, floorCount, useName}], ...`
- **Output:** `{ elementGuids: [guid, ...] }`

### `change_building_use`
Changes the use of one or more buildings (all floor plates). Accepts an array.
- **Input:** `[{buildingGeometryId: guid, newUseName}], ...`
- **Output:** `{ affectedGuids: [guid, ...] }`

### `list_elements`
Lists all urban elements in the model.
- **Input:** (none)
- **Output:** `{ transits: [...], buildings: [...], parcels: [...], zones: [...] }`
- Each element: `{ id, type, use, summary }` — serialized via a custom JSON serializer.

## Key Abstractions

### UrbanModelService
Data-only coordinator that:
- Accepts structured request records (coordinates, names, enums)
- Constructs Rhino geometry from coordinate arrays
- Creates and returns `IUrbanModelStep` objects
- Does **not** own pipeline execution or RhinoDoc access

### UrbanModelService Records
```
AddTransitRequest(CenterlineCoords, Width, TransitType)
AddBuildingRequest(FootprintCoords, TypicalFloorHeight, FloorCount, UseName)
ChangeBuildingUseRequest(BuildingGeometryId, NewUseName)
ListElementsRequest() → List<ElementSummary>
```

### Element Referencing
- Elements are identified by their `Guid` (from `IGeometryElement<T>.Id`).
- The AI discovers GUIDs via the `list_elements` tool.
- `ChangeBuildingUseStep` operates on envelope GUIDs (building-wide).

## Project Structure
```
AIQ.MCP/
├── CONTEXT.md
├── AIQ.MCP.csproj          — net8.0, references AIQ.Urban.Core + AIQ.Urban.RhinoInterface
├── Services/
│   └── UrbanModelService.cs — coordinate→step translation
├── Models/
│   ├── AddTransitRequest.cs
│   ├── AddBuildingRequest.cs
│   ├── ChangeBuildingUseRequest.cs
│   └── ElementSummary.cs
├── McpServer.cs            — HTTP/SSE listener, tool registration, request dispatch
└── Program.cs              — entry point (used for standalone testing only)
```
