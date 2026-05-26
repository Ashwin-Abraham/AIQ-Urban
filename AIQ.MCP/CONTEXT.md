# AIQ.MCP — Context

## Purpose
Model Context Protocol (MCP) server hosting AI-accessible urban modelling tools. Runs inside the Rhino process, started by `Urban_RhinoPlugin` on load. Exposes `AIQ.Urban.Core` capabilities to AI agents via MCP's standardised tool-calling protocol.

## Design Constraints

- **Nullable reference types enabled** — `?` marks values that can legitimately be null; unadorned types are assumed non-null and need not be checked.
- References `AIQ.Urban.Core` (domain model, steps) and `AIQ.Urban.RhinoInterface` (pipeline orchestration)
- **Not a Rhino plugin** — `AIQ.MCP.csproj` uses `Microsoft.NET.Sdk` with `FrameworkReference Microsoft.AspNetCore.App`. The MCP server is instantiated by the Rhino plugin, not loaded as its own plugin.
- **Internal tools** — `UrbanModelTools` is `internal` and accessed by MCP protocol via the `McpServer` DI registration.
- No direct Rhino UI dependency — tools receive raw coordinates, not Rhino ObjectIds.

## Domain Glossary

### MCP Server
An ASP.NET `WebApplication` hosting the Model Context Protocol endpoint at `/mcp`. Created and started in `Urban_RhinoPlugin.OnLoad()`; stopped in `OnShutdown()`. Uses the `ModelContextProtocol.AspNetCore` NuGet package. Port is auto-selected (pass `0` to `Start()` — actual port available via `Port` property).

### MCP Tool
A method on `UrbanModelTools` decorated with `[McpServerTool]`, registered via `.WithTools()`. Exposed to AI agents via the MCP `tools/list` and `tools/call` protocol methods. Currently:
- `add_transit` — Adds a transit corridor from centerline curve coordinates, width, and TransitType
- `add_building` — Adds a building from footprint curve coordinates, floor height, floor count, and Use

### Tool Argument Pattern
MCP tools receive **raw coordinate data** (lists of XY pairs) rather than Rhino ObjectIds. This means AI agents describe geometry numerically, not by picking objects in the Rhino viewport. Tools convert coordinates to domain types (`Curve2D`, `Polygon2D`) via `AIQ.Urban.Core` factories, then create pipeline actions executed by `RhinoPipelineExecutor`.

### ElementSummary
AI-optimised JSON format for listing urban elements. Produced by `ElementSummarySerializer`. Contains flattened summaries per element type (transits, buildings, parcels, zones) with ID, type, use, summary string, and optional typed details. Not used for persistence — only for MCP `list_elements` tool output.

### RhinoPipelineExecutor
Thin wrapper around `RhinoUrbanModelPipeline`. Takes a `RhinoDoc` and `UrbanModel`, accepts an `Action<RhinoUrbanModelPipeline>`, and calls `Run()`. Exists so MCP tools don't depend on `RhinoUrbanModelPipeline` construction directly. Requires an active RhinoDoc and is not suitable for unit tests.
