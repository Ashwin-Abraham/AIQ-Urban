# Plan: Selected Urban Element Metrics over WebSocket

## Goal

Emit a complete, typed metrics snapshot whenever the Rhino selection changes, using the existing UI websocket at `ws://localhost:6400/`.

The backend change covers Buildings, Parcels, and Transits. Zones remain in `ModelStateMessage`, and the `aiqurban-ui` consumer will be adapted separately.

## Decisions

| Decision | Choice |
|---|---|
| Message contract | New `SelectionMetricsMessage`; do not emit the legacy `SelectedObjectMetricsMessage` |
| Snapshot contents | Always include non-null Building, Parcel, and Transit aggregate records |
| Empty selection | Send all three records with `Count = 0` and empty/zero metrics |
| Event semantics | Send a complete current-selection snapshot, never a delta |
| Mixed selection | Include all three selected categories in the same message |
| Duplicate geometry | Count each source urban element once |
| Floor plates | Resolve to and count their owning Building |
| Unsupported geometry | Ignore Zones, transit surfaces, parcel boundaries, and unmanaged Rhino objects |
| Initial connection | Send the latest complete snapshot to the newly connected UI client |
| Numeric formatting | Send raw numeric values; formatting and labels belong to the UI |
| Metric lookup | Filter the existing `UrbanModel.ProjectMetrics`; do not recalculate on selection |
| Metric provenance | Every `IMetrics` has a singular `Guid SourceElementId` |
| Transit storage | `ProjectMetrics.TransitMetrics` stores one record per Transit |
| Transit grouping | Retain per-Transit metrics in Core; only `ModelStateMessage` groups them for its existing charts |
| Synthetic zone | Preserve the UI-required `"All"` `ZoneMetrics` with `SourceElementId = Guid.Empty` |

## Message Shape

The exact C# records should follow this shape:

```text
SelectionMetricsMessage
  Type: "SelectionMetricsMessage"
  Buildings: BuildingSelectionMetrics
  Parcels: ParcelSelectionMetrics
  Transits: TransitSelectionMetrics

BuildingSelectionMetrics
  Count
  BuiltUpArea
  FootprintArea
  Volume

ParcelSelectionMetrics
  Count
  LandArea
  Perimeter
  BuiltUpArea
  BuiltFootprint
  FAR
  CoveragePercentage

TransitSelectionMetrics
  Count
  RunningLength
  Area
```

For multiple selected Parcels:

```text
FAR = total BuiltUpArea / total LandArea
CoveragePercentage = total BuiltFootprint / total LandArea * 100
```

Parcel building-dependent metrics include every Building contained by the selected Parcel, whether or not those Buildings are selected. The Building aggregate itself includes only explicitly selected Buildings.

## Slice 1: Make Existing Metrics Source-Addressable

### Core contract

1. Add `Guid SourceElementId { get; }` to `IMetrics`.
2. Populate it in:
   - `BuildingMetrics` from `Building.Id`
   - `ParcelMetrics` from `Parcel.Id`
   - `TransitMetrics` from `Transit.Id`
   - `ZoneMetrics` from `Zone.Id`
3. Keep the synthetic `"All"` zone used by `ModelStateMessage`, passing `Guid.Empty`.

### Preserve per-Transit metrics

1. Change `ProjectMetricsCalculator` to retain the per-Transit `TransitMetrics` list instead of grouping it by type and width.
2. Change `ZoneMetrics` to retain its per-Transit records as well.
3. Remove the group-specific `Count`, private aggregate constructor, and `TransitMetrics.Sum()` from the Core metric type.
4. Keep `ProjectMetrics.TotalTransitLength` and `TotalTransitArea` as sums over the per-Transit records.

### Move presentation aggregation

Update `ModelStateMessage` so both the synthetic `"All"` entry and real Zone entries group their per-Transit records by `(Type, Width)` when producing road-length and road-area chart points. Preserve the existing UI JSON shape and `"All"` zone behavior.

### Tests

Add focused NUnit tests proving:

- each concrete metric receives the source element ID;
- the project calculator retains two separate Transit metrics when two Transits share type and width;
- project totals remain unchanged;
- `ModelStateMessage` still produces one grouped chart point for those two Transits;
- the synthetic `"All"` zone uses `Guid.Empty`.

## Slice 2: Build the Typed Selection Snapshot

### Transport records

Create the records under:

`src/AIQ.Urban.Rhino/HTML_UI/MessageToUI/`

Keep these records transport-only. Core must not reference websocket messages or Rhino selection state.

### Selection resolution

`SelectionMetricsMessage` resolves the current selected Rhino object IDs to deduplicated source element IDs internally:

1. A Building matches when any selected ID occurs in `Building.GetGeometryIds()`.
2. This naturally rolls selected floor-plate geometry up to its owning Building.
3. A Parcel matches through `Parcel.GetGeometryIds()`.
4. A Transit matches through `Transit.GetGeometryIds()`, including its centerline or owned outline geometry.
5. Do not resolve Zones, transit surfaces, parcel boundaries, or unmanaged geometry.

### Metric filtering and aggregation

Build `SelectionMetricsMessage` from:

- the three selected source-ID sets; and
- the already-calculated `UrbanModel.ProjectMetrics`.

Filter metrics using `SourceElementId`, then aggregate:

- additive Building and Parcel values by summation;
- Parcel FAR and coverage from combined totals;
- Transit totals by summation;

If `ProjectMetrics` is absent, emit a zero-count snapshot rather than recalculating metrics inside the selection path.

### Tests

Add NUnit tests for:

- empty selection;
- a mixed Building/Parcel/Transit selection;
- selection of multiple geometry IDs belonging to one Building;
- floor-plate selection rolling up to its Building;
- unsupported IDs being ignored;
- combined Parcel FAR and coverage;
- typed JSON serialization, including `Type = "SelectionMetricsMessage"`.

Keep RhinoCommon test-case data primitive-only and construct Rhino geometry inside the test body.

## Slice 3: Wire Rhino Selection Events to the UI WebSocket

### Event boundary

Extend `RhinoUrbanEventHandler` to subscribe to:

- `RhinoDoc.SelectObjects`
- `RhinoDoc.DeselectObjects`
- `RhinoDoc.DeselectAllObjects`

Expose one selection-changed notification to the Rhino plugin. The interface layer should report that selection changed; it should not know about websocket DTOs.

Unsubscribe all three handlers in `Dispose()`.

### Publish path

In `Urban_RhinoPlugin`:

1. Subscribe to the event handler’s selection-changed notification.
2. Read the complete current selection from the active document.
3. Resolve selected source elements.
4. build `SelectionMetricsMessage` from the stored project metrics;
5. publish it through `WebSocketUiServer`.

### Websocket behavior

Extend `WebSocketUiServer` to:

1. serialize and cache the latest selection message even when no UI client is connected;
2. broadcast selection messages only to the `/` UI websocket service, never `/mcp-bridge`;
3. expose the cached message to `UrbanUiWebSocketBehavior`;
4. send the cached snapshot to a client in `OnOpen`.

After `StartUI_AIQUrban` loads the document model and calculates project metrics, explicitly publish the current selection before or immediately after starting the websocket server. This covers objects selected before the UI opens.

### Lifecycle checks

- Selection events before UI startup update cached state without requiring a listening server.
- Closing the UI stops the server but does not damage event subscriptions.
- Plugin shutdown disposes the Rhino event handler and selection subscriptions.
- Selection handling stays on the Rhino UI thread; websocket serialization contains no Rhino document access.

## Slice 4: Verification and Documentation

1. Run focused Core metric tests.
2. Run focused Rhino plugin message and selection tests.
3. Build the affected projects in `Debug_NoViz` where supported.
4. Run the broader `AIQ.Urban.Core.Tests` and `AIQ.Urban.RhinoInterface.Tests` suites.
5. Manually smoke-test in Rhino:
   - open the UI with nothing selected;
   - open the UI with objects already selected;
   - select mixed Buildings, Parcels, and Transits;
   - select both a Building envelope and floor plate;
   - partially deselect;
   - deselect all;
   - confirm the MCP websocket path receives no UI selection messages.
6. Update the Rhino context’s outgoing-message contract with the final record names and fields.

## Acceptance Criteria

- Every selection change emits one complete `SelectionMetricsMessage`.
- All three aggregate properties are always present.
- Counts and metrics are deduplicated by source urban element.
- The message uses existing stored metrics and does not trigger metric recalculation.
- Per-Transit metrics remain individually addressable in `ProjectMetrics`.
- Transit grouping occurs only in `ModelStateMessage` for its existing chart contract.
- Existing `ModelStateMessage` JSON behavior, including the `"All"` zone, remains intact.
- UI broadcasts never leak onto `/mcp-bridge`.
- All focused and affected test suites pass.
