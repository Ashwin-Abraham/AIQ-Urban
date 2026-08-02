# Parcel Boundary UI Integration Implementation Plan

**Goal:** Connect the Vue `Divider` Tool Use to the current `AIQ.Urban.RhinoPlugin` so selected Rhino curves become managed `ParcelBoundary` elements, recalculate parcels, survive document reload, and respond correctly to Rhino replacement and deletion events.

**Architecture:** Keep `"Divider"` as the wire-level `messageType` for compatibility with the existing Vue app and Usage Telemetry. Translate that command at the plugin boundary into the current domain term, `ParcelBoundary`. Follow the established flow used by Transit and Zone commands: typed UI message -> selected Rhino objects -> data-gather step -> `RhinoUrbanModelPipeline` configuration -> Core model steps -> Rhino outputters -> `ModelStateMessage`. Do not reference the legacy `ArchSitePlan.RhinoInterface.HTML_UI` implementation.

**Tech stack:** Vue 3, TypeScript/JavaScript, C# 12, .NET 8, RhinoCommon, `System.Text.Json`, NUnit, `Rhino.Testing.Fixtures`

> **Implementation workflow:** Use the repository `tdd` skill while implementing this plan. Before writing tests, agree the public seams to cover, then deliver the work as vertical red -> green slices: one failing behavioural test, the minimum implementation needed to pass it, then the next slice. After all required behaviour is green, use the `nunit-tests` skill for a focused test-quality pass. Condense repetitive cases with readable `[TestCase]`/`[TestCaseSource]` patterns, retain Arrange-Act-Preview-Assert structure, use domain-focused test names and constraint assertions, and keep RhinoCommon static test data primitive-only. This final pass must preserve behaviour and readability rather than introduce new production work.

## Current State and Known Gaps

- `DivideLandTool.vue` already emits `{ messageType: "Divider" }`.
- `ModellingTools.vue` and `App.vue` already forward the command to `POST http://localhost:6300`.
- `AIQ.Urban.RhinoPlugin.HTML_UI.MessageConverter` does not recognise `Divider`.
- Core already contains `ParcelBoundary`, `ParcelBoundaryData`, `AddParcelBoundaryStep`, and `UrbanModel.ParcelBoundaries`.
- `RecalculateParcelsStep` already reads `UrbanModel.ParcelBoundaries`, but the current closed-boundary regression test is failing: expected 2 parcels and received 3.
- RhinoInterface has no parcel-boundary selection gatherer, pipeline configuration, outputter, managed layer, document-load support, or edit/delete event handling.
- The Vue response decoder ignores the plugin's `Error` messages, leaving most failures visible only in developer/Rhino logs.
- The plugin copies the prebuilt Vue `dist` directory into `HTML_UI/wwwroot`; building the plugin does not build the Vue app.

## Decisions and Constraints

- Use `ParcelBoundary` in Core, RhinoInterface, filenames, tests, logs, and documentation.
- Retain `Divider` only at compatibility boundaries: Vue component/tool label, `messageType`, and Usage Telemetry Tool Type.
- Use Rhino preselection, matching current Transit, Building, Zone, and Change Use handlers. The UI button must not imply that clicking it launches a Rhino picker.
- Accept one or more selected Rhino curves.
- Accept open curves for splitting and closed curves for enclosure.
- Require planar geometry on World XY within the project tolerance. Invalid objects are rejected with an actionable error; no partial mutation occurs when the entire selection is invalid.
- All model changes go through `RhinoUrbanModelPipeline`; UI message handlers must not mutate `UrbanModel` directly.
- All `HandleMessage()` execution remains marshalled to the Rhino UI thread by `HttpUiServer`.
- Managed parcel-boundary curves live on `AIQ Urban::ParcelBoundary` and are owned by `UrbanModel` after successful import.
- Add, replace, delete, and document-load paths must all produce the same model invariants.
- Do not add a dependency on any `ArchSitePlan.*` UI or model project. Legacy code may be read as behavioural reference only.
- Preserve unrelated user changes in both repositories.

## End-to-End Flow

```text
Vue Divide Land tool
  -> POST { messageType: "Divider" }
  -> MessageConverter
  -> AddParcelBoundaryMessage
  -> selected Rhino curves
  -> SelectParcelBoundariesStep
  -> ParcelBoundaryData + RhinoObjectDeletionData
  -> WithParcelBoundaryAdditionSteps
  -> AddParcelBoundaryStep
  -> RecalculateParcelsStep
  -> ParcelBoundaryOutputter + ParcelOutputter
  -> metrics + ModelStateMessage
  -> Vue model-state update
```

## File Map

| Action | File | Purpose |
|---|---|---|
| Modify | `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/IMessageFromUI.cs` | Add the `Divider` message enum value |
| Modify | `src/AIQ.Urban.Rhino/HTML_UI/MessageConverter.cs` | Route `"divider"` to the new handler |
| Create | `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/AddParcelBoundaryMessage.cs` | Handle the Vue command through the Rhino pipeline |
| Create | `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectParcelBoundariesStep.cs` | Validate selected curves and produce creation/deletion data |
| Modify | `src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs` | Add boundary add/update/delete pipeline configurations and document-load integration |
| Create | `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/ParcelBoundaryOutputter.cs` | Bake managed boundary curves |
| Modify | `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs` | Include boundary output in full output |
| Modify | `src/AIQ.Urban.RhinoInterface/Utils/LayerConstants.cs` | Add `AIQ Urban::ParcelBoundary` |
| Create | `src/AIQ.Urban.Core/ModelActions/CreationData/ParcelBoundaryDeletionData.cs` | Carry boundary IDs to remove |
| Create | `src/AIQ.Urban.Core/ModelActions/CreationData/ParcelBoundaryUpdateData.cs` | Carry an old boundary ID and replacement curve |
| Create | `src/AIQ.Urban.Core/ModelActions/RemoveParcelBoundaryStep.cs` | Remove managed boundaries |
| Create | `src/AIQ.Urban.Core/ModelActions/UpdateParcelBoundaryStep.cs` | Replace managed boundary geometry |
| Modify | `src/AIQ.Urban.Core/ModelActions/RecalculateParcelsStep.cs` | Fix the closed-boundary case and remove/activate the dead helper |
| Modify | `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/DocLoadGatherStep.cs` | Restore boundaries from their managed layer |
| Modify | `src/AIQ.Urban.RhinoInterface/EventHandler/RhinoUrbanEventHandler.cs` | Handle boundary replacement and deletion |
| Modify | `tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/MessageConverterTests.cs` | Lock down the wire contract |
| Modify | `tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/MessageHandlersTests.cs` | Test selected-curve command handling |
| Create | `tests/AIQ.Urban.RhinoPlugin.Tests/Pipeline/ParcelBoundaryIntegrationTests.cs` | Test output, reload, update, and deletion lifecycle |
| Modify | `tests/AIQ.Urban.Core.Tests/ModelActions/ParcelBoundarySplittingTests.cs` | Make all supported split/enclosure cases green |
| Modify | `C:/Users/AshwinAbraham/source/repos/aiqurban-ui/src/components/MODELLING_TOOLS/DivideLandTool.vue` | Clarify preselection UI text |
| Modify | `C:/Users/AshwinAbraham/source/repos/aiqurban-ui/src/services/RhinoApiService.ts` | Decode plugin error responses |
| Modify/Create | Vue notification component/store, selected during implementation | Display command errors to the user |

---

## Task 1: Lock Down the Incoming Command Contract

**Files:**

- Modify `tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/MessageConverterTests.cs`
- Modify `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/IMessageFromUI.cs`
- Modify `src/AIQ.Urban.Rhino/HTML_UI/MessageConverter.cs`
- Create `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/AddParcelBoundaryMessage.cs`

- [ ] Add a failing `Convert_DividerMessage_ReturnsAddParcelBoundaryMessage` test using the exact Vue payload `{ "messageType": "Divider" }`.
- [ ] Run the converter test and confirm the current `Unsupported messageType: divider` failure.
- [ ] Add `TypeOfMessage.Divider`.
- [ ] Add the case-insensitive converter route:

```csharp
"divider" => Deserialize<AddParcelBoundaryMessage>(json),
```

- [ ] Create `AddParcelBoundaryMessage : IMessageFromUI<ModelStateMessage>` with a stubbed `HandleMessage()` until Task 4.
- [ ] Run `MessageConverterTests` and confirm the new contract test passes.

**Acceptance criteria:** The exact existing Vue payload resolves to `AddParcelBoundaryMessage`; existing message conversions remain unchanged.

---

## Task 2: Stabilise Parcel-Boundary Geometry Behaviour

**Files:**

- Modify `src/AIQ.Urban.Core/Extensions/PolyCurve2DExtensions.cs` as required
- Modify `src/AIQ.Urban.Core/ModelActions/RecalculateParcelsStep.cs`
- Modify `tests/AIQ.Urban.Core.Tests/ModelActions/ParcelBoundarySplittingTests.cs` only where assertions or coverage are genuinely incomplete

- [ ] Reproduce `ClosedBoundarySplitsHoleIntoTwo`: expected 2 parcels, currently returns 3.
- [ ] Minimise the case to the enclosing transit hole and one closed inner boundary.
- [ ] Determine whether the extra result is the thin offset band, a duplicate region, or incorrect closed-curve expansion.
- [ ] Fix the geometry operation without special-casing the test coordinates.
- [ ] Confirm these existing behaviours:
  - no boundaries -> one parcel;
  - one open boundary spanning the hole -> two parcels;
  - one closed boundary inside the hole -> inner and outer parcels;
  - boundary outside the hole -> no split;
  - two crossing open boundaries -> four parcels.
- [ ] Add tolerance-focused cases for a boundary touching or nearly touching the parcel edge if the minimised defect exposes ambiguity.
- [ ] Either route `Run()` through `SplitHoleWithSeparators()` and give that helper a single clear responsibility, or remove the unused helper. Do not leave two apparent splitting implementations.

**Acceptance criteria:** All `ParcelBoundarySplittingTests`, `AddParcelBoundaryStepTests`, and `ParcelBoundaryTests` pass deterministically.

---

## Task 3: Add Rhino Selection and Managed Output

**Files:**

- Create `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectParcelBoundariesStep.cs`
- Create `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/ParcelBoundaryOutputter.cs`
- Modify `src/AIQ.Urban.RhinoInterface/Utils/LayerConstants.cs`
- Modify `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs`

- [ ] Write gather-step tests for empty selection, non-curve selection, non-planar curves, off-plane curves, one valid open curve, one valid closed curve, and mixed valid/invalid selections.
- [ ] Implement `SelectParcelBoundariesStep` following `SelectTransitCenterlinesStep`:
  - filter Rhino curve objects;
  - duplicate and validate geometry before mutation;
  - normalise acceptable geometry to the Core ground-plane invariant;
  - preserve the source Rhino object ID in `Curve2D` until managed output rebakes it;
  - emit matching `RhinoObjectDeletionData` for accepted source curves;
  - log accepted and rejected counts.
- [ ] Add `LayerConstants.ParcelBoundary` with value `AIQ Urban::ParcelBoundary`.
- [ ] Implement `ParcelBoundaryOutputter` following `ZoneOutputter`, using a distinct layer colour and curve output.
- [ ] Include `ParcelBoundaryOutputter` in `WithFullModelOutput()`.
- [ ] Test that output rebakes only the boundary IDs reported as added, updates their stored geometry IDs, and places them on the managed layer.

**Acceptance criteria:** Valid selected curves can be represented as creation data and baked as managed boundary curves without changing `UrbanModel` inside the gatherer or outputter.

---

## Task 4: Implement the Add Command Vertical Slice

**Files:**

- Modify `src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs`
- Implement `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/AddParcelBoundaryMessage.cs`
- Modify `tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/MessageHandlersTests.cs`

- [ ] Add a failing headless handler test that selects an open curve spanning an existing transit hole.
- [ ] Add `WithParcelBoundaryAdditionSteps()`:
  - accept gathered or supplied `ParcelBoundaryData`;
  - delete accepted unmanaged source curves;
  - run `AddParcelBoundaryStep`;
  - run `RecalculateParcelsStep`.
- [ ] Implement `AddParcelBoundaryMessage.HandleMessage()` following `TransitMessage`:
  - require an active Rhino document;
  - read selected objects;
  - fail with `Select one or more parcel boundary curves first.` when empty;
  - construct the gather step and pipeline;
  - attach the plugin event handler when available;
  - request full model output and metrics;
  - run, redraw, and return `ModelStateMessage`.
- [ ] Assert the handler adds `ParcelBoundary` elements, replaces source curves with managed output, recalculates parcels, refreshes metrics, and returns a model-state response.
- [ ] Add a closed-boundary handler case after Task 2 is green.

**Acceptance criteria:** Posting the existing `Divider` payload with valid preselected curves produces managed parcel boundaries and updated parcels in one pipeline transaction.

---

## Task 5: Complete Replace and Delete Lifecycles

**Files:**

- Create `src/AIQ.Urban.Core/ModelActions/CreationData/ParcelBoundaryDeletionData.cs`
- Create `src/AIQ.Urban.Core/ModelActions/CreationData/ParcelBoundaryUpdateData.cs`
- Create `src/AIQ.Urban.Core/ModelActions/RemoveParcelBoundaryStep.cs`
- Create `src/AIQ.Urban.Core/ModelActions/UpdateParcelBoundaryStep.cs`
- Modify `src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs`
- Modify `src/AIQ.Urban.RhinoInterface/EventHandler/RhinoUrbanEventHandler.cs`

- [ ] Add Core step tests proving removal reports deleted geometry IDs and update replaces the curve while retaining the managed element lifecycle correctly.
- [ ] Add `WithParcelBoundaryDeletionSteps()` and `WithParcelBoundaryUpdateSteps()`; both must run `RecalculateParcelsStep` after the boundary mutation.
- [ ] Add `ParcelBoundary` handling to the event handler's deletion dispatch.
- [ ] Add `ParcelBoundary` handling to replacement dispatch, retrieving and validating the replacement Rhino curve before pipeline execution.
- [ ] Ensure invalid replacement geometry is rejected or restored consistently with the Transit replacement policy; never leave Core and Rhino representing different boundary geometry.
- [ ] Verify `RebuildLookup()` maps the newly baked boundary ID after every pipeline run.
- [ ] Add headless event-handler tests for open and closed boundary replacement and deletion.

**Acceptance criteria:** Editing or deleting a managed boundary in Rhino updates `UrbanModel`, recalculates parcels and metrics, rebakes affected geometry, and emits one model-change notification.

---

## Task 6: Restore Boundaries During Document Load

**Files:**

- Modify `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/DocLoadGatherStep.cs`
- Modify `src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs`
- Create/modify document-load integration tests

- [ ] Add `GatherParcelBoundaries()` scanning curves on the exact managed boundary layer.
- [ ] Apply the same geometric validation/normalisation policy as interactive selection.
- [ ] Preserve invalid managed curves in Rhino for correction and log their object IDs, matching invalid Transit load behaviour.
- [ ] Add `AddParcelBoundaryStep` to `WithDocModelLoad()` before `RecalculateParcelsStep`.
- [ ] Ensure managed boundary objects are included in the load pipeline's delete-and-rebake set only when accepted.
- [ ] Test a save/load-equivalent round trip with open and closed boundaries, parcel Uses, and stable boundary counts.

**Acceptance criteria:** Starting the UI against an existing document restores boundaries before parcel calculation and produces the same parcel topology as the original session.

---

## Task 7: Align UI Input Text and Error Feedback

**Files in `C:/Users/AshwinAbraham/source/repos/aiqurban-ui`:**

- Modify `src/components/MODELLING_TOOLS/DivideLandTool.vue`
- Modify `src/services/RhinoApiService.ts`
- Modify or create the smallest notification state/component consistent with the current app

- [ ] Change the action label from `Select Divider Curve` to `Add Selected Boundaries`.
- [ ] Change the help text to state: select one or more open or closed planar curves in Rhino, then click the action button.
- [ ] Keep the emitted payload exactly `{ messageType: 'Divider' }` so Usage Telemetry remains compatible.
- [ ] Add `Error` to `RhinoMessageType` and decode `{ type: "Error", message: string }` from both converter failures and handler exceptions.
- [ ] Present the latest command error visibly in the modelling UI and clear it after the next successful command.
- [ ] Do not classify validation errors or tool selection as additional Tool Uses.
- [ ] Run formatting, lint, and the Vue production build.

**Acceptance criteria:** The UI accurately describes preselection, preserves telemetry naming, and displays actionable Rhino validation/handler errors without requiring developer tools.

---

## Task 8: Verify Packaging and the End-to-End Workflow

- [ ] Build the Vue app first so `aiqurban-ui/dist` contains the updated component and error decoder.
- [ ] Build `AIQ.Urban.RhinoPlugin`; verify its output `HTML_UI/wwwroot` receives the current `dist` assets.
- [ ] Run the targeted Core and Rhino plugin test suites.
- [ ] Run the broader relevant test projects.
- [ ] Run `graphify update .` after all C# changes.
- [ ] Perform a Rhino smoke test:
  1. create or load a transit enclosure;
  2. draw and select an open curve;
  3. click **Add Selected Boundaries**;
  4. confirm parcels split and the curve moves to `AIQ Urban::ParcelBoundary`;
  5. replace the boundary and confirm parcels recalculate;
  6. delete it and confirm parcels merge/recalculate;
  7. save, reopen, start the UI, and confirm the boundary and parcel topology reload;
  8. invoke the command with no selection and confirm the UI shows the validation error.
- [ ] Confirm no code in the new projects references `ArchSitePlan.RhinoInterface.HTML_UI` or other legacy UI types.

## Verification Commands

From `C:/Users/AshwinAbraham/source/repos/ArchSitePlan`:

```powershell
dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~ParcelBoundary" -v minimal
dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj --filter "FullyQualifiedName~MessageConverterTests|FullyQualifiedName~MessageHandlersTests|FullyQualifiedName~ParcelBoundary" -v minimal
dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj -v minimal
dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj -v minimal
dotnet build src/AIQ.Urban.Rhino/AIQ.Urban.RhinoPlugin.csproj
graphify update .
```

From `C:/Users/AshwinAbraham/source/repos/aiqurban-ui`:

```powershell
npm run format
npm run lint
npm run build
```

Build the Vue app before the Rhino plugin because the plugin copies `aiqurban-ui/dist` and does not invoke Vite itself.

## Definition of Done

- The existing Vue `Divider` command is accepted by the current Rhino plugin.
- Selected open and closed planar curves become managed `ParcelBoundary` elements.
- Adding, replacing, deleting, and loading boundaries all recalculate parcels correctly.
- Boundaries are baked to and restored from `AIQ Urban::ParcelBoundary`.
- All parcel-boundary geometry tests pass, including the currently failing closed-boundary case.
- Successful commands return and apply `ModelStateMessage`; failures are visible in the Vue UI.
- Usage Telemetry still records the Tool Type as `Divider`.
- The production Vue assets embedded/copied into the plugin match the updated Vue source.
- Relevant Core, Rhino plugin, and UI verification commands pass.
- The knowledge graph is updated.
