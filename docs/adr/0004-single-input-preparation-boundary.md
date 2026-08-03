# ADR 4: Use one input-preparation boundary for every Urban Model mutation

## Status
Accepted

## Context
Rhino commands, HTTP UI messages, MCP tools, document events, and document loading currently prepare pipeline input in different places. Some use `IRhinoDataGatherStep`; others resolve Rhino objects, validate geometry, construct `ICreationData`, or choose Rhino deletion IDs in handlers and pipeline configuration methods. This splits source validation across trigger adapters and permits model mutation data to bypass the intended preparation boundary.

## Decision
Every mutating `RhinoUrbanModelPipeline` invocation has exactly one semantic **Input Preparation Step**. A trigger may pass only raw source handles and user parameters—such as Rhino object IDs, coordinate arrays, names, dimensions, counts, or Use identifiers—to that step. The step resolves source objects and managed elements, creates or extracts geometry, performs source-specific validation and normalization, and returns the invocation's single `List<ICreationData>`.

Concrete `ICreationData` types are authoritative routing contracts with explicit consumers, not advisory hints. Model-operation payloads and Rhino object-lifecycle payloads remain together in the same list, but Core steps do not infer domain intent from Rhino cleanup types. Core constructors and model steps continue to enforce source-independent domain invariants.

Each invocation has one preparation step, although that step may compose smaller validators and resolvers internally. Preparation is atomic within one logical aggregate and permits partial success across independent aggregates; rejected aggregates emit explicit rejection or preservation data.

The pipeline API enforces the boundary: trigger-facing direct injection methods such as `WithCreationData` and `WithRhinoObjectDeletion` are removed, and a mutating run without an Input Preparation Step is invalid. Direct construction of operation data remains valid in Core unit tests because those tests do not exercise a Rhino pipeline trigger.

## Considered alternatives
- Keep input preparation as a convention while retaining direct-injection escape hatches: rejected because new triggers could continue bypassing validation ownership.
- Return model-operation data and Rhino source-object dispositions through separate result channels: rejected in favour of one heterogeneous `List<ICreationData>` whose concrete types define routing.
- Configure multiple ordered preparation steps per invocation: rejected because it creates partial-validation and ordering dependencies at the orchestration boundary.

## Consequences
- `IInputPreparationStep` is the required preparation abstraction, and every production mutation trigger supplies exactly one implementation.
- Event handlers own event capture, batching, deduplication, and dispatch only; they do not resolve or validate replacement geometry.
- Pipeline configuration methods register model steps, outputters, cascades, and post-actions only; they do not accept raw geometry, identifiers, or prebuilt operation data.
- Rhino-specific `ICreationData` implementations may live in the Rhino interface layer while sharing the Core-defined marker interface and list.
- Delete-all gathers typed Transit, Building, Zone, and Parcel Boundary deletion contracts, composes the ordinary removal steps, clears or recomputes derived state normally, and preserves custom Uses. `ResetUrbanElementsStep` is removed rather than renamed; a future factory reset must be a separate explicit operation.
