# Plan: UI POST Handler for Transit & Building Commands

## Goal

Wire up `HttpUiServer.cs` POST handler so that when the Vue UI at `aiqurban-ui` sends `messageType: "Transit"` or `messageType: "Building"`, the server deserializes the request, marshals to the Rhino UI thread, processes selected Rhino geometry with the supplied parameters via the existing `RhinoUrbanModelPipeline`, and returns a `ModelStateMessage` response.

---

## Decisions Made

| Decision | Choice | Rationale |
|---|---|---|
| Transit type | `TransitType.Road` | UI has no type selector; Road is the common case |
| Building use | `DefaultUses.MixedUse` | Most neutral default; changeable later via `change_building_use` |
| ToolSelection handling | Deferred (stub) | Focus on Transit/Building first |
| Response format | `ModelStateMessage` with metrics | UI auto-updates stats panel; matches old behavior |
| Building "Mass" mode | Supported; extract footprint from Brep base | Both Footprint and Mass radio options work |
| CORS headers | Add `Access-Control-Allow-Origin: *` | Matches old code; helps browser testing |
| Thread marshalling | `RhinoApp.InvokeOnUiThread()` | Required — `HttpListener` runs on thread pool, Rhino doc must be accessed on UI thread |

---

## Files to Create (6 new files in `src/AIQ.Urban.Rhino/HTML_UI/`)

### 1. `MessageToUI/IMessageToUI.cs`

Response message interface. The UI's `RhinoApiService.ts` checks `response.data.Type` to route responses.

```csharp
namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

public interface IMessageToUI
{
    string Type { get; }
}
```

### 2. `MessageToUI/ModelStateMessage.cs`

Response DTO matching the UI's `ModelStateMessage.ts` TypeScript class. Contains landuses (from `DefaultUses.All`) and per-zone chart metrics (from `ProjectMetrics`).

Fields:
- `Type` → `"ModelStateMessage"`
- `Landuses` — mapped from `DefaultUses.All`:
  - `Name` → `u.Name`
  - `Color` → `$"{u.Color.R},{u.Color.G},{u.Color.B}"` (RGB comma-separated, matches old format)
  - `Id` → `u.Name` (use name doubles as ID)
- `StudyAreaDataForUI` — built from `UrbanModel.ProjectMetrics`:
  - One entry per `ZoneMetrics` in the model
  - Chart data derived from `TransitMetrics` (RoadLengthData, RoadAreaData) and `AreaPerUse` (UrbanLandAreaData, UrbanBuiltAreaData)
  - Summary: `TotalArea`, `TotalBuiltArea`, `FAR`, `LandCoverage`
- If `ProjectMetrics` is null (no metrics computed yet), return empty chart data arrays — UI handles this gracefully

### 3. `MessageToUI/EmptyMessage.cs`

Simple no-content response for stubs.

```csharp
namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

public class EmptyMessage : IMessageToUI
{
    public string Type => "EmptyMessage";
}
```

### 4. `MessagesFromUI/IMessageFromUI.cs`

Generic interface for incoming UI messages. Each message type implements `HandleMessage()` which contains the logic and returns a response.

```csharp
namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public enum TypeOfMessage { Transit, Building, ToolSelection, ModelState }

public interface IMessageFromUI<out T> where T : IMessageToUI
{
    TypeOfMessage MessageType { get; }
    T HandleMessage();
}
```

### 5. `MessagesFromUI/TransitMessage.cs`

Handles the Transit POST from `TransitTool.vue`.

**Flow:**
1. Get `RhinoDoc.ActiveDoc`
2. Get selected curve objects: `doc.Objects.GetSelectedObjects(...).Where(obj => obj.ObjectType == ObjectType.Curve)`
3. If none selected, throw with instruction message
4. For each selected curve:
   - Cast `obj.Geometry` to `Curve`
   - Create `new Curve2D(curve, obj.Id)`
   - Create `new TransitData(curve2D, RoadWidth, TransitType.Road)`
5. Build pipeline:
   ```csharp
   new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
       .WithTransitAdditionSteps(transitData, [])
       .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance)
       .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler)  // if available
       .Run();
   ```
6. `doc.Views.Redraw()`
7. Return `new ModelStateMessage(UrbanModel.Instance)`

### 6. `MessagesFromUI/BuildingMessage.cs`

Handles the Building POST from `BuildingTool.vue`. Supports both `Footprint` and `Mass` generation modes.

**Flow (Footprint mode):**
1. Get selected curves
2. For each: `new Polygon2D(curve)` → `new BuildingData(polygon, FloorHeight, FloorCount, DefaultUses.MixedUse)`
3. Pipeline: `WithBuildingAdditionSteps(buildingData, [])` + `WithMetricsCalculation()`

**Flow (Mass mode):**
1. Get selected Breps
2. For each: extract lowest face boundary as footprint via `ExtractFootprintFromBrep(brep)`:
   - Find Brep face with minimum Z in bounding box
   - Get outer boundary curve: `face.OuterLoop.To3dCurve()`
   - Create `Polygon2D` from it
3. Same pipeline as Footprint mode

### 7. `MessageConverter.cs`

Routes incoming JSON by `messageType` field to typed message class. Uses `System.Text.Json` (already referenced in project).

```csharp
namespace AIQ.Urban.RhinoPlugin.HTML_UI;

public static class MessageConverter
{
    public static IMessageFromUI<IMessageToUI> Convert(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var type = doc.RootElement.GetProperty("messageType").GetString()?.ToLower();
        return type switch
        {
            "transit" => JsonSerializer.Deserialize<TransitMessage>(json),
            "building" => JsonSerializer.Deserialize<BuildingMessage>(json),
            "toolselection" => new ToolSelectionMessage(),
            _ => throw new Exception($"Unsupported message type: {type}")
        };
    }
}
```

---

## Files to Modify

### 8. `HttpUiServer.cs` — Replace POST stub with full routing

**Current behavior:** POST body is logged in DEBUG, returns 202.

**New behavior:**

```
POST / → parse body → MessageConverter.Convert()
         → RhinoApp.InvokeOnUiThread(() => message.HandleMessage())
         → serialize response as JSON
         → return 200 with Content-Type: application/json
```

**Specific changes:**

1. **CORS preflight**: Handle `OPTIONS` requests, return 200 with CORS headers
2. **CORS headers**: Add `Access-Control-Allow-Origin: *`, `Access-Control-Allow-Methods: POST, GET, OPTIONS`, `Access-Control-Allow-Headers: Content-Type` to all responses
3. **POST handler** (replace current DEBUG-only block):
   ```csharp
   if (context.Request.HttpMethod == "POST")
   {
       using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
       var body = await reader.ReadToEndAsync();

       var messageObj = MessageConverter.Convert(body);

       IMessageToUI? result = null;
       var tcs = new TaskCompletionSource<bool>();
       RhinoApp.InvokeOnUiThread(() =>
       {
           try { result = messageObj.HandleMessage(); }
           catch (Exception ex) { result = new ErrorMessage(ex.Message); }
           finally { tcs.SetResult(true); }
       });
       await tcs.Task;

       response = AddCorsHeaders(response);
       await WriteJsonResponseAsync(response, result);
       return;
   }
   ```
4. **Helper methods**:
   - `WriteJsonResponseAsync(HttpListenerResponse, IMessageToUI)` — serialize to JSON, set content type, write to output
   - `AddCorsHeaders(HttpListenerResponse)` — add CORS headers

**Thread safety note:** `RhinoApp.InvokeOnUiThread()` is essential here. The `HttpListener` runs request handling on thread pool threads. All Rhino doc access (`RhinoDoc.ActiveDoc`, `UrbanModel.Instance`, pipeline operations) must execute on the Rhino UI thread.

---

## Sequence Diagram

```
Vue UI                    HttpUiServer              Rhino UI Thread           UrbanModel/RhinoDoc
  │                            │                           │                        │
  │──POST / {messageType:"Transit", roadWidth:10}──→       │                        │
  │                            │                           │                        │
  │                     MessageConverter.Convert()          │                        │
  │                     → TransitMessage                    │                        │
  │                            │                           │                        │
  │                     RhinoApp.InvokeOnUiThread(──→       │                        │
  │                            │                    TransitMessage.HandleMessage()   │
  │                            │                           │──GetSelectedObjects()──→│
  │                            │                           │←──Curve[]──────────────│
  │                            │                           │                        │
  │                            │                           │──WithTransitAddition()─→│
  │                            │                           │──WithMetrics()─────────→│
  │                            │                           │──Run()─────────────────→│
  │                            │                           │──Redraw()──────────────→│
  │                            │                           │                        │
  │                            │←──ModelStateMessage───────│                        │
  │←──200 {Type:"ModelStateMessage", Landuses:[...]}──    │                        │
  │                            │                           │                        │
```

## Edge Cases

| Case | Handling |
|---|---|
| No curves selected | Throw descriptive exception; UI logs error and returns null |
| Curve is not planar | `Curve2D`/`Polygon2D` constructors validate and throw `ArgumentException` |
| ProjectMetrics not yet computed | Return empty `StudyAreaDataForUI` list — UI handles gracefully |
| Invalid JSON / missing messageType | `JsonException` caught and returned as error message |
| Building "Mass" mode with no Brep selected | Throw with instruction to select a Brep |
| Multiple objects selected | Process all — user can add multiple buildings/transits at once |
| Thread marshalling failure | `InvokeOnUiThread` exception caught, returned as error message |

## Dependencies

- **No new NuGet packages** — `System.Text.Json` already referenced in `AIQ.Urban.Rhino.csproj`
- Uses existing types: `RhinoUrbanModelPipeline`, `PipelineConfigurations`, `Curve2D`, `Polygon2D`, `TransitData`, `BuildingData`, `DefaultUses`
- References `Urban_RhinoPlugin.Instance.EventHandler` — already accessible from the plugin namespace
