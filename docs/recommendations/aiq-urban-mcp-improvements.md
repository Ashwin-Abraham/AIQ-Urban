# AIQ Urban MCP improvements

## Purpose

Give an AI agent a reliable way to read and edit an AIQ Urban model in an open Rhino document. AIQ Urban remains responsible for geometry checks, model edits, derived parcels, layers, metrics, and reports. The agent chooses the inputs and checks the results.

This is a design recommendation based on a source review on 2026-10-11. It is not an implementation plan or a record of a live Rhino test.

## First implementation slice

The `codex/aiq-urban-optional-inputs` branch adds optional `objectIds` fields to the Transit, Building, Zone, Divider, and Landuse UI messages. A missing field uses the current Rhino selection. An explicit list resolves only those IDs and rejects empty, missing, or wrong-type inputs before the pipeline runs. The ExportExcel message accepts an optional `filePath`; a missing path opens the existing save dialog. This slice does not yet add non-interactive Rhino command-line options or a Python entry point.

## Current path

- The `AIQUrban` Rhino command loads the model from the document, starts the UI HTTP server on port 6300 and the WebSocket server on port 6400, starts `AIQ.MCP.exe`, and opens the UI. See `src/AIQ.Urban.Rhino/Commands/StartUI_AIQUrban.cs`.
- `AIQ.MCP.exe` exposes MCP at `http://localhost:6410/mcp` and connects to the Rhino WebSocket bridge. See `AIQ.MCP/Program.cs` and `src/AIQ.Urban.Rhino/Urban_RhinoPlugin.cs`.
- The MCP tools can add transits, buildings, and zones from XY coordinate lists; delete elements; change building use; list elements and uses; and compute metrics. See `src/AIQ.Urban.Rhino/MCP_Bridge/UrbanModelTools.cs`.
- The UI and Rhino commands can take selected Rhino curves or building masses. That path retains native Rhino geometry, including curves that the coordinate-list tools cannot express. See `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/BuildingMessage.cs` and the `Select*Step` gather steps.
- Document load already finds native objects by managed layer path. See `src/AIQ.Urban.RhinoInterface/Utils/RhinoDocScanner.cs` and `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/DocLoadGatherStep.cs`.

## Recommended internal interface

Add one in-process AIQ Urban edit module that the Rhino commands, MCP bridge, and UI can call. The module must accept explicit inputs, validate them, run the existing pipeline, and return a structured result. Keep the modelling rules in this one place. The best external route depends on how the agent can reach the running Rhino process: a Rhino Python runner can use callable commands, while an agent outside Rhino needs a transport such as the existing MCP bridge.

Support two geometry input modes:

1. **Coordinate input:** The agent sends simple XY lines and polygons. AIQ Urban creates the geometry. This fits new conceptual layouts.
2. **Rhino object input:** The agent sends Rhino object IDs or a filter. AIQ Urban reads the native curves, Breps, or extrusions and passes them to the existing gather steps. This fits NURBS curves, building masses, and objects prepared in Rhino.

Do not make the agent change Rhino selection as a required step. Selection can remain a human convenience, but an agent request must name its source objects. A script can create source geometry in Rhino and pass its IDs to a command or MCP tool.

## Required improvements

### 1. Check the target document

Add `get_session` with document path, document runtime serial, units, absolute tolerance, model load state, bridge state, and supported operations. Each edit request must identify the expected document. Reject an edit if the active document changed. Avoid treating `AIQUrban` as a read-only session start: it calls `WithDocModelLoad`, which can rebuild managed geometry.

### 2. Run edits on the Rhino UI thread

Dispatch MCP edit calls through Rhino's UI thread before reading or changing document objects. The UI HTTP path already uses `RhinoApp.InvokeOnUiThread()` in `HttpUiServer.cs`; the WebSocket bridge invokes `UrbanModelTools` directly in `McpBridgeBehavior.cs`. A Rhino command already runs in Rhino's command context. Both paths must call the same edit module.

### 3. Use one edit path and report real outcomes

Put pipeline setup, metrics calculation, event handler use, redraw, and model state publication behind the same edit module. At present `RhinoPipelineExecutorForMcp` does not add the metrics or event handler steps used by the UI path. `RhinoUrbanModelPipeline.Run()` logs and continues after several failures, while MCP methods can still return `Succeeded`. Return a structured result with accepted, rejected, created, updated, and deleted IDs; metrics status; warnings; and errors. Mark an MCP tool error as an error. Make partial success explicit.

### 4. Expose a useful model snapshot

Add `get_model_snapshot` with stable element IDs, Rhino geometry IDs, type, use, key parameters, relationships, and current metrics. Add a revision value so the agent can detect changes between read and edit. The current `list_elements` gives only short summaries and no geometry details. Provide a bounded geometry summary or a separate geometry read operation where needed.

### 5. Add explicit object and layer filters

Add a read operation such as `find_rhino_objects` with these fields:

- `objectIds`: exact IDs, when known.
- `layerPath`: exact full layer path by default.
- `includeChildLayers`: explicit option for a managed layer tree.
- `objectTypes`: an allowlist such as Curve, Brep, and Extrusion.
- `scope`: source objects, AIQ Urban managed objects, or all document objects.
- `includeHidden` and `includeLocked`: explicit options, with safe defaults.

Return ID, full layer path, object type, name, bounds, and relevant user strings. For an edit, let the agent send exact IDs or the filter plus an expected ID set or revision. Resolve the filter in Rhino, show which objects matched, and reject an unexpected match before modifying the model. Use an exact layer match unless the caller asks for child layers; a broad prefix can include derived outlines, surfaces, floor plates, or similarly named layers. Existing layer scanning provides a starting point, but it uses prefix matching and is internal to document load.

Add operations that take the resolved IDs for transit centerlines, zone and parcel boundaries, building footprints, and building masses. Reuse the current gather steps so AIQ Urban applies the same geometry rules as its UI. State which input objects were consumed, preserved, or rejected.

### 6. Add save and report operations

Add `save_document` and `export_metrics` with an explicit output path and structured result. The current UI Excel export opens a save dialog; an agent needs a path-based route. Check that metrics are current before export. Return the saved path, model revision, and any failure.

### 7. Make Rhino commands callable by scripts

Add a non-interactive mode to each relevant Rhino command, or add a single `AIQUrbanApply` command. The mode must accept explicit object IDs, document identity, edit parameters, and optional layer and object-type filters. It must not call `GetObject` or `RhinoGet.GetString` when those inputs are supplied. Keep the existing prompts for human use.

The command must write a machine-readable result with accepted and rejected IDs, created IDs, warnings, errors, metrics status, and model revision. Rhino's command `Result.Success` alone cannot tell a script which objects were skipped or whether a pipeline step logged an error. Short calls can use command options. For batches or complex filters, a request JSON file plus result JSON file avoids a long, fragile command string. The command is a thin adapter to the edit module; do not copy pipeline setup into a second path.

This route is good when the agent can already run Python inside the open Rhino process. It does not itself give an external agent a way to start the script. That entry point must be provided by the user's Rhino automation setup, an existing bridge, or an explicit user action.

Rhino commands return a `Rhino.Commands.Result` status, not an arbitrary model result. A Python caller can check the command status, but it cannot receive created IDs, rejected objects, or metrics through that status. For the command route, either write a result JSON file keyed by request ID or expose the shared edit module as a callable .NET method for Rhino Python. Keep command-line text as a human-readable summary, not the machine result.

#### Small ID-based command set

These are proposed names, not existing command names. Every write command takes the expected document identity and exact Rhino object IDs. Each returns the same structured result through the chosen result channel.

| Command | Inputs | Role |
| --- | --- | --- |
| `AIQUrbanAddTransit` | Centerline curve IDs, width, transit type | Add transit corridors. |
| `AIQUrbanAddBuilding` | Footprint curve or mass Brep/extrusion IDs, input mode, height or floor settings, use | Add buildings from native Rhino geometry. |
| `AIQUrbanAddZone` | Boundary curve ID, zone name | Add a zone. |
| `AIQUrbanAddParcelBoundary` | Boundary curve IDs | Add parcel boundaries and recalculate derived parcels. |
| `AIQUrbanSetUse` | Building, floor plate, or parcel geometry IDs; use name | Change the relevant use. The UI already supports all three kinds. |
| `AIQUrbanDelete` | Managed geometry IDs | Remove the matching urban elements through the model pipeline. |
| `AIQUrbanRead` | Optional element IDs or read scope | Return model state, valid uses, and metrics as structured data. |

Provide a separate `AIQUrbanLoad` only if the agent must load the model without opening the UI. Provide `AIQUrbanExportMetrics` only when a script needs the existing Excel report. Rhino's normal save command can save the document; a custom save operation is useful only if it must verify document identity and return a path.

### 8. Improve session lifecycle

Report whether Rhino, the bridge, and MCP are ready. Keep the bridge available for the intended session even when the UI window closes, or stop MCP and report a clear disconnected state. Avoid a live MCP endpoint that appears ready after its Rhino bridge closes. Consider a dedicated `AIQUrbanMcpStart` command only if agents need to start without opening the UI; it should use the same model load rules and edit module.

## Suggested agent workflow

1. Open or identify the target Rhino document; read `get_session`.
2. Read uses, the model snapshot, and any source objects with explicit layer and object type filters.
3. Prepare a small edit. Use coordinate input for simple new geometry, or object IDs for native Rhino geometry.
4. Send the edit with the expected document and revision.
5. Read the structured result, the new model snapshot, and metrics. Inspect or capture Rhino views when geometry quality matters.
6. Save the document and export the report to explicit paths. Keep the returned IDs and rejected objects in the run record.

## Interface alternatives

| Route | Strength | Limit |
| --- | --- | --- |
| Existing MCP bridge, improved as above | Agent can discover typed tools and use AIQ Urban's model pipeline. It also supports Rhino-independent coordinate requests. | Needs the reliability and filter work above. |
| Rhino Python script calls AIQ Urban commands with ID mode | Strong local route. Python can find layers, create NURBS geometry, and pass IDs to a command on Rhino's command context. | Requires a non-interactive mode and a structured result. An external agent still needs a way to start the Python script in Rhino. |
| Rhino Python script calls a new in-process AIQ Urban module | Direct native geometry access with no network hop. Good for local batch scripts. | Requires an exposed .NET interface and keeps the agent tied to Rhino Python. Use the same edit module as MCP. |
| Direct HTTP POST to the UI server | Already present and dispatched to Rhino's UI thread. | Current messages get geometry from the live selection and use UI-shaped requests. It needs new object-ID/filter messages to be reliable for an agent. |
| Direct WebSocket bridge call | Short transport path to the same tool methods. | It is an internal protocol. A caller would need to own bridge discovery, result handling, and lifecycle. |

## Decision for a first version

If the agent can run Python in the open Rhino process, a non-interactive Rhino command with object IDs is a good first version. It can avoid the MCP network path for local work. Add structured results and filter rules before agents rely on it. If the agent works outside Rhino and has no script runner, use the existing MCP transport to reach the same edit module. In either case, add a model snapshot and path-based save/report operations. Do not maintain a second set of urban modelling rules in Python.

## Verification needed before an agent edits a real project

- Run MCP calls against a live Rhino document and confirm the edit runs on the UI thread.
- Run a Python-driven Rhino command with explicit IDs and confirm that it returns the full result without any prompt.
- Check one simple coordinate edit, one native curved object, one building mass, and one rejected object.
- Check exact layer matching and optional child-layer matching on a layer tree with derived AIQ Urban geometry.
- Check document mismatch, stale revision, disconnection, and partial pipeline failure.
- Check that model metrics, Rhino layers, returned IDs, saved document, and exported report agree after each edit.
