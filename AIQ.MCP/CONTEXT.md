# AIQ.MCP — Context

## Purpose
Standalone EXE hosting the Model Context Protocol (MCP) server for AI-accessible urban modelling tools. Launched by `Urban_RhinoPlugin` via `Process.Start` and communicates with Rhino over a WebSocket bridge (`/mcp-bridge`). Exposes `AIQ.Urban.Core` capabilities to AI agents without direct Rhino process coupling.

## Design Constraints

- **Nullable reference types enabled** — `?` marks values that can legitimately be null; unadorned types are assumed non-null and need not be checked.
- References `AIQ.Urban.Core` (domain models only — no pipeline or Rhino types)
- **Does not** reference `AIQ.Urban.RhinoInterface` — the EXE is a pure proxy; all Rhino operations happen on the other side of the bridge.
- **Standalone EXE** — `AIQ.MCP.csproj` outputs `AIQ.MCP.exe` to the Rhino plugin's build directory. Started with `--bridge ws://localhost:6400/mcp-bridge` argument.
- **No direct Rhino dependency** — tools receive raw coordinate data, not Rhino ObjectIds.
- **Tool schemas discovered via handshake** — on connect, the EXE sends `ListTools` bridge message; Rhino responds with tool schemas. Tools are registered dynamically on the MCP server.
- **Port file** written to `%TEMP%\AIQ.MCP.port` containing the MCP HTTP URL (e.g. `http://localhost:12345/mcp`). Cleaned up on shutdown.

## Architecture

```
AI Agent → MCP HTTP/SSE → AIQ.MCP.exe → WebSocket → Rhino Plugin (/mcp-bridge) → UrbanModelTools
```

- `Program.cs` — entry point, parses `--bridge` arg, orchestrates lifecycle
- `BridgeClient.cs` — WebSocket client to Rhino's `/mcp-bridge` using typed envelope protocol (`BridgeEnvelope` with `requestId`)
- `McpServerHost.cs` — ASP.NET `WebApplication` hosting the MCP endpoint at `/mcp`. Has no Rhino dependency. Uses `ModelContextProtocol.AspNetCore` NuGet. Tool handlers are registered via `OnListTools`/`OnCallTool`, which proxy to the Rhino bridge.
- `BridgeMessage.cs` — `BridgeEnvelope` (type, requestId, tool, arguments, result, error) and `ToolDefinition` (name, description, inputSchema)

## Lifecycle

1. Rhino plugin OnLoad → starts `AIQ.MCP.exe --bridge ws://localhost:6400/mcp-bridge`
2. EXE connects to bridge WebSocket, sends `ListTools` → receives tool schemas
3. EXE starts ASP.NET MCP server on random port, writes URL to port file
4. MCP server proxies `tools/list` and `tools/call` over the bridge
5. On Rhino plugin shutdown → kills the EXE process (port file auto-cleaned)

## Domain Glossary

### MCP Server
An ASP.NET `WebApplication` hosting the Model Context Protocol endpoint at `/mcp`. Port is auto-selected (pass `0` to `Start()` — actual port available via `Port` property). Uses `McpServerHost` (not `McpServer`) to avoid ambiguity with `ModelContextProtocol.Server.McpServer`.

### Bridge Protocol
Typed envelope messages over WebSocket between the EXE and Rhino plugin. Message types:
- `ListTools` / `ToolsList` — handshake for tool discovery
- `ToolCall` / `ToolResult` — tool invocation and response
- `Error` — error responses

### McpServerHost
Class in `Services/McpServerHost.cs`. Thin wrapper around ASP.NET + MCP SDK. Registers `WithListToolsHandler` and `WithCallToolHandler` that delegate to bridge responses. No reference to `UrbanModelTools` or `RhinoDoc` — all tool logic lives in the Rhino plugin.

### BridgeClient
WebSocket client in `Services/BridgeClient.cs`. Connects to `ws://localhost:6400/mcp-bridge` with retry (15s timeout). Sends typed envelopes, deserializes responses. `ListToolsAsync()` returns `List<ToolDefinition>`, `CallToolAsync()` returns `JsonElement`.

### ElementSummary
AI-optimised JSON format for listing urban elements. Produced by `ElementSummarySerializer` in the Rhino plugin. Not used in the EXE — serialized/deserialized transparently through the bridge.

### Tool Argument Pattern
MCP tools receive **raw coordinate data** (lists of XY pairs) rather than Rhino ObjectIds. AI agents describe geometry numerically, not by picking objects in the viewport. Tools convert coordinates to domain types via `AIQ.Urban.Core` factories (`Curve2D.Create`, `Polygon2D.Create`).
