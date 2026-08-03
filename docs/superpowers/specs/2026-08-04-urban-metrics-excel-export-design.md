# Design: Urban Metrics Excel Export

## Status

Approved through the design interview — ready for implementation planning.

## Context

The Vue export tool in `aiqurban-ui` must trigger a native Rhino save dialog and produce a readable Excel workbook from the current `UrbanModel` metrics. The existing export abstractions in `AIQ.Exports` should be extended rather than bypassed with report-specific ClosedXML calls in the UI message handler.

The reviewed sample workbook was useful as a visual starting point, but some of its calculations incorrectly implied that a floor-plate Use and a Parcel Use with the same name describe the same spatial population. They do not. The final report therefore keeps Parcel Use and Built-Plate Use as independent summaries and only reports Coverage and FAR at scopes with valid numerator/denominator relationships.

## Goals

1. Provide a single Excel export action in Vue, with no separate path-selection step.
2. Open a native Save dialog from Rhino, export the current metrics, and return a clear success, cancellation, or failure result to Vue.
3. Produce one `Project Overview` worksheet and one worksheet per Zone.
4. Preserve the accepted metric semantics documented in `src/AIQ.Urban.Core/CONTEXT.md`.
5. Keep report mapping, workbook construction, file writing, and UI orchestration modular and independently testable.
6. Produce an auditable workbook whose derived figures are guarded Excel formulas.

## Non-Goals

- Charts in the first implementation.
- Boundary-clipped Zone metrics. Zone rows use the current representative-point, whole-element assignment semantics.
- FAR by Use.
- Automated Vue tests.
- Automatically opening Excel after export.
- Inventing a Zone colour that is not present in the domain model.

## Selected Architecture

Use a layered export pipeline:

```text
Vue export action
    -> Rhino UI message handler and native Save dialog
    -> Urban metrics report mapper
    -> format-neutral report DTOs
    -> MetricsWorkbookBuilder
    -> ExcelDocumentWriter / ClosedXML
    -> typed result returned to Vue
```

This keeps the UI message handler responsible for orchestration only. Metric meaning and presentation enrichment belong in the mapper; worksheet layout belongs in the builder; ClosedXML-specific persistence belongs in the existing writer.

Two alternatives were rejected:

- Filling a fixed workbook template would make evolving tables and dynamic Zone sheets brittle.
- Writing ClosedXML directly in the Rhino message handler would mix UI threading, domain mapping, workbook layout, and persistence.

## Interaction Flow

1. The Vue user presses a single **Export Excel** action.
2. Vue sends an `ExportExcel` message and enters an exporting/disabled state.
3. Rhino handles the message on its UI thread and opens a native `.xlsx` Save dialog.
4. The suggested filename is `<Project Name> - Urban Metrics.xlsx`, where Project Name is the active Rhino document filename without its extension. An unsaved document uses `Urban Metrics.xlsx`.
5. Cancellation returns a typed cancelled result. It creates no file and Vue does not present it as an error.
6. On confirmation, Rhino snapshots the current metrics and maps them to the report DTOs.
7. The builder creates the workbook model and the writer persists it atomically.
8. Success returns the final path for concise Vue feedback. Failure returns a concise user-facing message while Rhino logs the technical exception details.
9. The application does not automatically open the saved workbook.

If no usable metrics snapshot exists, the export fails before file creation with a clear instruction to calculate metrics first.

## Component Responsibilities

### Vue (`aiqurban-ui`)

`PluginExports.vue` owns only interaction state and feedback:

- send the single export request;
- disable the action and show `Exporting…` while awaiting a result;
- show the saved path on success;
- silently return to idle on cancellation;
- show a concise failure message.

The existing standalone location/path picker is removed from this workflow. The implementation will use the shared Rhino API service/message serialization rather than manually constructing JSON in the component.

Automated Vue tests are explicitly out of scope. The small UI change is verified manually.

### Rhino message orchestration

The message converter creates the Excel export handler for the `ExportExcel` request. The handler:

- runs from the Rhino UI thread;
- owns the native Save dialog;
- validates that metrics are available;
- invokes the exporter;
- converts the outcome to a typed result;
- logs technical failure details.

It does not define tables, calculate report aggregates, or call ClosedXML directly.

### Urban metrics report mapper

`UrbanMetricsExcelExporter` maps the current `ProjectMetrics`, Zones, and presentation data into format-neutral immutable DTOs. It is responsible for:

- preserving project and current Zone assignment semantics;
- adding Zone Boundary Area from Zone geometry;
- supplying full Zone names separately from worksheet-safe names;
- supplying Parcel and floor-plate Use colours;
- resolving Transit colour from the same Type + Width mapping used by Rhino output and Vue charts;
- producing the rows needed for Type + Width transit grouping.

The mapper must not clip geometry, match Parcel Use to floor-plate Use, or invent Zone colours.

### Workbook builder

`MetricsWorkbookBuilder` consumes only the report DTOs and constructs the generic Excel document model. It owns:

- worksheet order and names;
- title, timestamp, table, subtotal, and empty-state placement;
- formulas and numeric formats;
- colours, borders, column widths, and frozen regions where supported by the document model;
- collision-safe worksheet naming.

It must not depend on Rhino types or write files.

### Excel writer

The existing `ExcelDocumentWriter` remains the only ClosedXML persistence boundary. Values are written as typed numeric cells, formulas as formulas, and colours/styles through the generic document model.

## Workbook Contract

### Shared presentation

- Dark blue (`#333C55`) title and table-header bands with white text.
- No charts, review comments, reviewer columns, or placeholder data.
- Project and Zone titles retain their full display names.
- Every sheet shows `Exported: yyyy-MM-dd HH:mm` using local time.
- Geometry values: `#,##0.00`.
- Counts: `#,##0`.
- Shares and Coverage: `0.0%`.
- FAR: `0.00`.

The label `units` is used where a unit qualifier is required. Numeric data remains numeric rather than preformatted text.

### Project Overview

The first worksheet is named `Project Overview` and contains, in order:

1. Project name and export timestamp.
2. **Project Summary**: Parcel Area, Building Footprint, Built Area, Transit Length, Transit Area, element counts, Coverage, and FAR.
3. **Parcel Use Summary**: Use, Colour, Parcel Count, Area, Area Share, Sum of Parcel Perimeters, Building Footprint on Parcels, and Coverage.
4. **Built-Plate Use Summary**: Floor-Plate Use, Colour, Built Area, and Built Area Share.
5. **Transit Summary**: Type, Width, Corridor Count, Running Length, Length Share, Transit Area, Area Share, and Colour. Rows are grouped by Type + Width, followed by each Type subtotal and a grand total. Shares use the grand total as denominator.
6. **Zone Comparison**: Zone, Zone Boundary Area, Assigned Parcel Area, Building Footprint, Coverage, Built Area, FAR, Transit Length, and Transit Area.

Zone Comparison has no Zone colour, perimeter, share columns, or total row. Zones may overlap, so aggregating their comparison rows would be misleading.

### Zone worksheets

There is one worksheet per Zone, even if the Zone currently contains no assigned elements. Each Zone worksheet contains:

1. Full Zone name and export timestamp.
2. **Zone Summary** with the same project-scope measures plus Zone Boundary Area.
3. **Parcel Use Summary** scoped to elements assigned to the Zone.
4. **Built-Plate Use Summary** scoped to elements assigned to the Zone.
5. **Transit Summary** scoped to elements assigned to the Zone.

Coverage and FAR use Assigned Parcel Area as their denominator, never Zone Boundary Area.

### Empty tables

The Project worksheet is always created when a metrics snapshot exists. Expected tables remain visible even when empty and contain a single `No data` row rather than disappearing.

### Worksheet naming

Zone worksheet names begin with `Zone - `. Invalid Excel worksheet characters are replaced with `-`, names are truncated to Excel's 31-character limit, and collisions receive ` (2)`, ` (3)`, and so on. The visible title is not truncated.

## Metric Semantics

### Valid whole-scope ratios

Project and Zone Coverage are Building Footprint divided by assigned Parcel Area. Project and Zone FAR are Built Area divided by assigned Parcel Area. A zero denominator produces zero.

### Parcel Use Summary

Parcel Use rows are valid spatial roll-ups because the source Parcels define both the grouping and denominator. Coverage is Building Footprint on those Parcels divided by their Area. The explicit label `Sum of Parcel Perimeters` is retained because it is not the enclosing perimeter of the project or Zone.

### Built-Plate Use Summary

This table reports only floor-plate Use, colour, Built Area, and Built Area Share. It does not report FAR or floor-plate count.

Parcel Use and floor-plate Use are independent classifications. Matching their names to derive FAR is prohibited because the relevant buildings may not be located on Parcels with the same Use.

### Transit Summary

Transit is grouped first by Type and then Width. Type subtotals and the grand total are included. Transit colour is presentation data resolved from Type + Width and must remain consistent with Rhino and Vue.

### Zone assignment

The export preserves current Zone calculation behavior: an element is assigned using its representative point and contributes its full metrics. The exporter performs no boundary intersection or clipping. Boundary-clipped Zone metrics are tracked as a separate future task.

## Formula Policy

Source metrics are exported as fixed numeric values. Derived workbook cells remain auditable Excel formulas:

- Coverage;
- FAR;
- area and built-area shares;
- transit length and area shares;
- Type subtotals and grand totals.

Division formulas are guarded with `IF`/equivalent zero-denominator logic so the workbook produces `0`, not `#DIV/0!`. Formula references must remain correct for empty tables, dynamic row counts, and Zone sheets.

## Error Handling and Atomic Save

The result contract distinguishes `Succeeded`, `Cancelled`, and `Failed`, with an optional final path or user-facing error message as appropriate.

The workbook is first written to a temporary `.xlsx` file in the selected destination directory. Only a fully written workbook is moved/replaced into the chosen final path. On failure, the temporary file is removed and no partial final workbook is left behind. The native Save dialog owns overwrite confirmation.

Expected export failures are converted to a failed result and logged with technical context. Exceptions are not silently swallowed.

## Verification Strategy

Automated tests cover the non-Vue layers:

1. **Workbook builder unit tests**: worksheet names and ordering, table columns, totals, formulas, formats, empty-state rows, invalid characters, truncation, and duplicate Zone names.
2. **Report mapping tests**: project and Zone values, Zone denominator rules, Parcel/Built-Plate separation, Type + Width transit grouping, and model/presentation colours.
3. **Rhino orchestration tests**: success, cancellation, failure, typed results, and temporary-file cleanup. Dialog and filesystem boundaries should be injected where necessary to keep tests deterministic.
4. **Workbook integration test**: write a representative `.xlsx`, reopen it with ClosedXML, and verify key Project and Zone cells, formulas, sheet names, and absence of formula errors.

There are no pixel-perfect Excel tests and no automated Vue tests. The Vue trigger and feedback states receive manual verification in Rhino's embedded UI.

## Cross-Repository Scope

Implementation spans:

- `ArchSitePlan`: domain metric additions where required, report DTOs/mapping, workbook builder, Excel writing, Rhino UI message handling, and .NET tests.
- `aiqurban-ui`: the single export trigger, result handling, and interaction feedback.

The UI and plugin communicate through the typed request/result contract defined by the Rhino bridge. Contract changes must be applied together so the two repositories cannot drift on message names or result status values.

## Documentation Impact

`src/AIQ.Urban.Core/CONTEXT.md` records the accepted domain language and constraints:

- independent Parcel Use and floor-plate Use classifications;
- `Parcel Use Summary` and `Built-Plate Use Summary` terminology;
- valid Coverage/FAR denominators;
- Zone Boundary Area semantics;
- non-additive overlapping Zone Comparison rows;
- current representative-point Zone assignment;
- Transit Type + Width colour consistency;
- no domain Zone colour.

## Open Questions

None. All design decisions required for implementation planning are resolved.
