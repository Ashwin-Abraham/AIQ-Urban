# Duplicate Road Identity Replacement Design

## Problem

Submitting a managed Rhino Transit centerline through `AddTransit_AIQUrban` or the HTML UI can leave the existing Transit in `UrbanModel` and add a second Transit at the same location. Both Transits are then selected for Rhino output because they temporarily share the submitted centerline ObjectId, after which rebaking assigns them distinct ObjectIds and makes the duplicate persistent.

The regression was introduced when interactive Transit creation moved to eager data gathering. The command and HTML UI stopped passing selected centerline IDs directly to `WithTransitAdditionSteps()`, but `SelectTransitCenterlinesStep` was not made responsible for staging the equivalent `TransitDeletionData`. The existing pipeline regression test masks this mismatch by registering a separate `TransitDeletionGatherStep` that production does not register.

## Decision

Interactive Transit creation uses identity-based replacement. Reusing the same managed Rhino centerline ObjectId replaces the existing Transit before the replacement is added. The newly submitted width and `TransitType` win.

`SelectTransitCenterlinesStep` is the single operation boundary responsible for validating selected centerlines and staging all edit data and Rhino directives that follow from accepted inputs. This follows ADR 0004, which keeps validation and edit staging together in ordered `IRhinoDataGatherStep` instances.

## Scope

This design covers `AddTransit_AIQUrban` and the HTML UI `TransitMessage`, which share `SelectTransitCenterlinesStep`.

It does not introduce geometric duplicate detection. Coincident curves with different ObjectIds remain independent. MCP coordinate submissions continue to receive new GUIDs and remain additive. Document loading continues to trust each managed centerline found in the Rhino document. Direct Rhino centerline edits continue to use `TransitUpdateData` and `UpdateTransitStep`.

Existing coincident Transits that already have distinct ObjectIds are not migrated or removed automatically. Cleaning those historical duplicates would require geometry-based matching and is outside this change.

## Gatherer Contract

`SelectTransitCenterlinesStep` will deduplicate selected Rhino objects by ObjectId before validation.

For every accepted centerline, the gatherer will stage:

- one `TransitData` carrying the accepted ObjectId, normalized centerline, submitted width, and submitted `TransitType`;
- one `RhinoObjectDeletionData` for the accepted ObjectId; and
- the accepted ObjectId in an operation-level collection.

After processing the selection, the gatherer will stage one `TransitDeletionData` containing the distinct accepted ObjectIds. If no centerline is accepted, it will stage no `TransitDeletionData`.

Rejected centerlines stage none of these records. Their existing model state and Rhino geometry remain untouched.

## Pipeline Data Flow

The existing `WithTransitAdditionSteps()` order remains unchanged:

1. `SelectTransitCenterlinesStep` validates and stages the complete operation.
2. `RemoveTransitStep` consumes `TransitDeletionData` and removes every existing Transit matching an accepted ObjectId.
3. `AddTransitStep` creates the replacement Transit from `TransitData`.
4. `TransitSurfaceUnioner` rebuilds derived Transit surfaces.
5. `RecalculateParcelsStep` rebuilds derived parcels.
6. `RhinoUrbanModelPipeline` deletes staged and step-reported obsolete Rhino geometry.
7. Registered outputters bake exactly one replacement Transit for each accepted ObjectId.

An unmanaged accepted curve has no model identity match, so removal is a no-op and normal addition proceeds. If corrupted in-memory state contains multiple Transits sharing the exact accepted ObjectId, `RemoveTransitStep` removes all of them before one replacement is added.

## Validation and Failure Semantics

Existing per-object validation remains authoritative:

- centerlines outside the ground-plane tolerance are rejected;
- self-intersecting centerlines are rejected;
- mixed selections process accepted centerlines and preserve rejected centerlines; and
- an entirely rejected selection remains a warning-only no-op.

No new exception policy, geometric tolerance, or recovery path is introduced. In particular, model deletion data is derived only after a centerline passes validation, so an invalid managed centerline cannot delete its last valid Transit.

## NUnit Regression Design

Regression coverage will use the real headless Rhino pipeline with NUnit and `[RhinoTestFixture]`. Tests follow the repository's record-based convention:

- each behavior shape uses a parametrized `[Test]` with descriptive `[TestCase("<case name>")]` values;
- richer case data is held in private records;
- Rhino-related records contain primitive values only, such as `string`, `double`, `double[]`, `double[][]`, `bool`, and `int`;
- `Point3d`, `LineCurve`, `Curve2D`, Rhino objects, and GUIDs are constructed inside the test body;
- record construction uses named arguments and case-name lookup uses a switch expression; and
- test bodies follow Arrange–Act–Preview–Assert with NUnit constraint assertions.

The tests cover three behavior groups:

1. **Managed identity replacement**
   - same ObjectId with unchanged attributes leaves exactly one Transit;
   - same ObjectId with changed width and type leaves exactly one Transit using the new attributes;
   - obsolete Rhino geometry is deleted and exactly one managed centerline is baked.

2. **Gatherer edit-data contract**
   - distinct valid objects produce one addition per ObjectId and one operation-level `TransitDeletionData`;
   - mixed valid and invalid objects include only accepted ObjectIds in model and Rhino deletion data; and
   - repeated references to one ObjectId produce one addition and one deletion identity.

3. **Rejected managed centerlines**
   - an off-ground managed centerline does not remove or mutate its existing Transit; and
   - a self-intersecting managed centerline does not remove or mutate its existing Transit.

The existing replacement regression test must stop injecting `TransitDeletionGatherStep`; production behavior must pass through `SelectTransitCenterlinesStep` alone.

## Verification

Implementation follows red–green TDD: first expose the production mismatch by removing the test-only deletion gatherer and observing the expected duplicate-count failure, then make the minimal gatherer change.

Verification runs the focused Transit pipeline fixture followed by the complete `AIQ.Urban.RhinoInterface.Tests` project. A run that discovers zero tests is not accepted as evidence. The test runner must report discovered and executed tests before the change can be considered complete.

## Documentation Impact

`src/AIQ.Urban.Rhino/CONTEXT.md` already states that reusing a centerline ObjectId invokes `RemoveTransitStep` and replaces the old Transit. This change restores conformance with that documented contract, so no domain-policy update is required.
