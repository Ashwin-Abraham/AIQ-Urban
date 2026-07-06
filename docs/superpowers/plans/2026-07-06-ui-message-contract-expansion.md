# UI Message Contract Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add C# message handlers for zone creation and building/parcel use changes while keeping the Vue app unchanged.

**Architecture:** Keep `HttpUiServer` as the single POST entry point, expand `MessageConverter` to route the existing Vue `messageType` values to new typed handlers, and implement each action as a focused `IMessageFromUI<T>` class that reads the current Rhino selection, runs the existing Rhino pipeline helpers, and returns `ModelStateMessage` on success.

**Tech Stack:** C# 12, .NET 8, RhinoCommon, `System.Text.Json`, NUnit, `Rhino.Testing.Fixtures`

## Global Constraints

- Nullable reference types enabled.
- Do not modify the Vue app.
- Keep the existing Vue `messageType` values as the primary contract.
- Incoming UI handlers must read geometry from the current Rhino selection.
- Successful UI handlers return `ModelStateMessage`.

---

### Task 1: Expand the incoming message contract

**Files:**
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/IMessageFromUI.cs`
- Create: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/AddZoneMessage.cs`
- Create: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/ChangeParcelUseMessage.cs`
- Create: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/ChangeBuildingUseMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessageConverter.cs`

**Interfaces:**
- Consumes: `IMessageFromUI<T>`, `ModelStateMessage`, `EmptyMessage`, `DefaultUses.Resolve(string)`, existing `TypeOfMessage` enum.
- Produces: three new concrete message classes, the extra `TypeOfMessage` values they need, and converter branches for `Zone`, `Landuse`, and `ChangeBuildingUse` payloads.

- [ ] **Step 1: Write the failing converter tests**

Create a new NUnit test file `tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/MessageConverterTests.cs` with cases that parse these payloads and assert the concrete message type:

```csharp
[Test]
public void Convert_ZoneMessage_ReturnsAddZoneMessage()
{
    var message = MessageConverter.Convert("{\"messageType\":\"Zone\",\"name\":\"Downtown\"}");
    Assert.That(message, Is.TypeOf<AddZoneMessage>());
}

[Test]
public void Convert_LanduseMessage_ReturnsChangeParcelUseMessage()
{
    var message = MessageConverter.Convert("{\"messageType\":\"Landuse\",\"use\":\"Residential\"}");
    Assert.That(message, Is.TypeOf<ChangeParcelUseMessage>());
}

[Test]
public void Convert_ChangeBuildingUseMessage_ReturnsChangeBuildingUseMessage()
{
    var message = MessageConverter.Convert("{\"messageType\":\"ChangeBuildingUse\",\"useName\":\"Commercial\"}");
    Assert.That(message, Is.TypeOf<ChangeBuildingUseMessage>());
}
```

- [ ] **Step 2: Run the converter tests and confirm they fail**

Run: `dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj --filter FullyQualifiedName~MessageConverterTests -v minimal`

Expected: fail because the three message classes and `MessageConverter` branches do not exist yet.

- [ ] **Step 3: Implement the new message classes**

Add three focused handlers with this shape:

```csharp
namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public class AddZoneMessage : IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType => TypeOfMessage.Zone;
    public string Name { get; init; } = string.Empty;
    public ModelStateMessage HandleMessage() => throw new NotImplementedException();
}

public class ChangeParcelUseMessage : IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType => TypeOfMessage.Landuse;
    public string Use { get; init; } = string.Empty;
    public ModelStateMessage HandleMessage() => throw new NotImplementedException();
}

public class ChangeBuildingUseMessage : IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType => TypeOfMessage.ChangeBuildingUse;
    public string UseName { get; init; } = string.Empty;
    public ModelStateMessage HandleMessage() => throw new NotImplementedException();
}
```

Make `TypeOfMessage` include any new enum value needed for `ChangeBuildingUse` while preserving `Zone` and `Landuse` for the existing Vue contract.

- [ ] **Step 4: Wire `MessageConverter` to the new classes**

Extend `MessageConverter.Convert()` so it routes:

```csharp
return messageType switch
{
    "transit" => Deserialize<TransitMessage>(json),
    "building" => Deserialize<BuildingMessage>(json),
    "zone" => Deserialize<AddZoneMessage>(json),
    "landuse" => Deserialize<ChangeParcelUseMessage>(json),
    "changebuildinguse" => Deserialize<ChangeBuildingUseMessage>(json),
    "toolselection" => new ToolSelectionMessage(),
    _ => throw new Exception($"Unsupported messageType: {messageType}")
};
```

Use `JsonSerializerOptions` with case-insensitive property names so Vue can keep sending lower-case JSON keys.

- [ ] **Step 5: Run the converter tests and confirm they pass**

Run: `dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj --filter FullyQualifiedName~MessageConverterTests -v minimal`

Expected: pass.

- [ ] **Step 6: Commit the contract scaffolding**

Commit message: `feat: add incoming ui message contract`

---

### Task 2: Implement the message handlers

**Files:**
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/AddZoneMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/ChangeParcelUseMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/ChangeBuildingUseMessage.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs`

**Interfaces:**
- Consumes: `RhinoDoc.ActiveDoc`, `doc.Objects.GetSelectedObjects(false, false)`, `ZoneData`, `Polygon2D`, `DefaultUses.Resolve`, `RhinoUrbanModelPipeline`, `WithZoneAdditionSteps`, `WithZoneDeletionSteps`, `WithBuildingUseChangeSteps`, `ChangeParcelUseStep`.
- Produces: three handlers that mutate `UrbanModel` through the pipeline and return `ModelStateMessage`.

- [ ] **Step 1: Add failing handler tests**

Create `tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/AddZoneMessageTests.cs` with a headless Rhino doc and one selected closed curve:

```csharp
[Test]
public void HandleMessage_AddsZoneAndReturnsModelStateMessage()
{
    using var doc = RhinoDoc.CreateHeadless(null);
    var curveId = doc.Objects.AddCurve(new PolylineCurve([
        new Point3d(0, 0, 0),
        new Point3d(10, 0, 0),
        new Point3d(10, 10, 0),
        new Point3d(0, 10, 0),
        new Point3d(0, 0, 0),
    ]));
    doc.Objects.Select(curveId);

    var message = new AddZoneMessage { Name = "Downtown" };
    var result = message.HandleMessage();

    Assert.That(result, Is.TypeOf<ModelStateMessage>());
    Assert.That(UrbanModel.Instance.Zones, Has.Count.EqualTo(1));
}
```

Create `ChangeParcelUseMessageTests.cs` and `ChangeBuildingUseMessageTests.cs` that seed the model through the existing Rhino pipeline helpers, select the matching baked geometry, invoke the message, and assert the expected use changes plus `ModelStateMessage`.

- [ ] **Step 2: Run the new handler tests and confirm they fail**

Run: `dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj --filter FullyQualifiedName~MessageTests -v minimal`

Expected: fail because the handlers are still stubbed.

- [ ] **Step 3: Implement `AddZoneMessage.HandleMessage()`**

Implementation shape:

```csharp
public ModelStateMessage HandleMessage()
{
    var doc = RhinoDoc.ActiveDoc ?? throw new Exception("No active Rhino document.");
    var selected = doc.Objects.GetSelectedObjects(false, false)
        .Where(obj => obj.ObjectType == ObjectType.Curve)
        .ToList();

    if (selected.Count == 0)
        throw new Exception("Select one closed zone boundary curve first.");
    if (selected.Count > 1)
        throw new Exception("Select exactly one zone boundary curve first.");

    var gatherStep = new SelectZoneBoundaryStep(selected, Name);
    var creationData = gatherStep.Run(doc);
    var idsToDelete = creationData.OfType<ZoneData>().Select(z => z.Boundary.Id).ToList();

    var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
        .WithDataGatherStep(gatherStep)
        .WithZoneAdditionSteps([], idsToDelete)
        .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

    if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
        pipeline.WithEventHandler(handler);

    pipeline.Run();
    doc.Views.Redraw();
    return new ModelStateMessage(UrbanModel.Instance);
}
```

Use the existing zone gather step and pipeline configuration already present in `AIQ.Urban.RhinoInterface`.

- [ ] **Step 4: Implement `ChangeParcelUseMessage.HandleMessage()`**

Implementation shape:

```csharp
public ModelStateMessage HandleMessage()
{
    var doc = RhinoDoc.ActiveDoc ?? throw new Exception("No active Rhino document.");
    var selected = doc.Objects.GetSelectedObjects(false, false)
        .Where(obj => obj.ObjectType == ObjectType.Curve)
        .ToList();

    if (selected.Count == 0)
        throw new Exception("Select one or more parcel outline curves first.");

    var use = DefaultUses.Resolve(Use);
    var parcelGeometryIds = selected.Select(obj => obj.Id).ToList();
    var changeStep = new ChangeParcelUseStep(parcelGeometryIds, use);

    var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
        .WithUrbanElementStep(changeStep)
        .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

    if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
        pipeline.WithEventHandler(handler);

    pipeline.Run();
    doc.Views.Redraw();
    return new ModelStateMessage(UrbanModel.Instance);
}
```

- [ ] **Step 5: Implement `ChangeBuildingUseMessage.HandleMessage()`**

Implementation shape:

```csharp
public ModelStateMessage HandleMessage()
{
    var doc = RhinoDoc.ActiveDoc ?? throw new Exception("No active Rhino document.");
    var selected = doc.Objects.GetSelectedObjects(false, false)
        .Where(obj => obj.ObjectType == ObjectType.Brep || obj.ObjectType == ObjectType.Curve)
        .ToList();

    if (selected.Count == 0)
        throw new Exception("Select one or more building envelopes or floor plate curves first.");

    var use = DefaultUses.Resolve(UseName);
    var geometryIds = selected.Select(obj => obj.Id).ToList();

    var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
        .WithBuildingUseChangeSteps(geometryIds, use)
        .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

    if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
        pipeline.WithEventHandler(handler);

    pipeline.Run();
    doc.Views.Redraw();
    return new ModelStateMessage(UrbanModel.Instance);
}
```

This keeps the selection model simple: envelopes and floor-plate curves can both be selected in one request, and the existing step already handles building-wide priority.

- [ ] **Step 6: Run the handler tests and confirm they pass**

Run: `dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj --filter FullyQualifiedName~MessageTests -v minimal`

Expected: pass.

- [ ] **Step 7: Commit the handler implementation**

Commit message: `feat: implement ui zone and use handlers`

---

### Task 3: Verify the full UI POST flow

**Files:**
- Modify if needed: `src/AIQ.Urban.Rhino/HTML_UI/HttpUiServer.cs`
- Modify if needed: `src/AIQ.Urban.Rhino/HTML_UI/MessageConverter.cs`
- Modify if needed: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/*.cs`
- Test: `tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/*.cs`

**Interfaces:**
- Consumes: the completed message classes and converter branches from Tasks 1 and 2.
- Produces: end-to-end POST handling for the three new Vue-driven actions.

- [ ] **Step 1: Add a focused end-to-end server test**

Create a test that posts a `ChangeBuildingUse` JSON body through `HttpUiServer` and asserts a JSON `ModelStateMessage` response. Keep the test local to the Rhino plugin test project and use a headless document.

```csharp
[Test]
public async Task Post_ChangeBuildingUse_ReturnsModelStateMessage()
{
    var response = await PostJsonAsync(
        "http://localhost:6300/",
        "{\"messageType\":\"ChangeBuildingUse\",\"useName\":\"Commercial\"}");

    Assert.That(response.Type, Is.EqualTo("ModelStateMessage"));
}
```

- [ ] **Step 2: Run the server test**

Run: `dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj --filter FullyQualifiedName~HttpUiServer -v minimal`

Expected: pass.

- [ ] **Step 3: Run the targeted test suite**

Run:

```bash
dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj -v minimal
dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj -v minimal
```

Expected: both pass.

- [ ] **Step 4: Commit the verification pass**

Commit message: `test: verify ui message contract expansion`
