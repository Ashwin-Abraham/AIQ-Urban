# Graph Report - C:\Users\AshwinAbraham\source\repos\ArchSitePlan\src  (2026-07-07)

## Corpus Check
- 443 files · ~117,650 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 72 nodes · 37 edges · 41 communities (7 shown, 34 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Domain Model & Urban Elements
- Rhino Plugin & Pipeline
- Command Surface
- UI Message Contract
- Metrics & Analytics
- UI Infrastructure
- Masterplan Design Process
- Rhino UI HTML Entry Points
- Plugin Packaging
- Legacy Embedded UIs
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets
- Resource Assets

## God Nodes (most connected - your core abstractions)

## Surprising Connections (you probably didn't know these)
- `RhinoUrbanModelPipeline` ----> `UrbanModel`  [EXTRACTED]
   →   _Bridges community 1 → community 2_
- `Building` ----> `IUrbanElement`  [EXTRACTED]
   →   _Bridges community 1 → community 0_
- `Transit` ----> `IUrbanElement`  [EXTRACTED]
   →   _Bridges community 1 → community 5_
- `ProjectMetrics` ----> `Transit`  [EXTRACTED]
   →   _Bridges community 5 → community 0_

## Communities (41 total, 34 thin omitted)

### Community 0 - "Domain Model & Urban Elements"
Cohesion: 0.25
Nodes (8): Building, FloorPlate, ProjectMetrics, ProjectMetricsCalculator, ProjectMetricsJsonSerializer, TransitMetrics, Use, ZoneMetrics

### Community 1 - "Rhino Plugin & Pipeline"
Cohesion: 0.33
Nodes (6): IUrbanElement, Parcel, Parcel2D, RecalculateParcelsStep, UrbanModel, Zone

### Community 2 - "Command Surface"
Cohesion: 0.40
Nodes (5): AddBuilding_AIQUrban, AddTransit_AIQUrban, AddZone_AIQUrban, ChangeBuildingUse_AIQUrban, RhinoUrbanModelPipeline

### Community 3 - "UI Message Contract"
Cohesion: 0.40
Nodes (5): AmbitionAndPlan, ConceptPlanning, DesignSteps, Frame-Propose-Ask, SpatialStrategy

### Community 4 - "Metrics & Analytics"
Cohesion: 0.50
Nodes (4): HTTP_POST_MessageContract, IMessageFromUI, IMessageToUI, MessageConverter

### Community 5 - "UI Infrastructure"
Cohesion: 0.50
Nodes (4): Profile2D, Transit, TransitSurface, TransitSurfaceUnioner

### Community 6 - "Masterplan Design Process"
Cohesion: 0.67
Nodes (3): EventQueue, ReverseLookupDict, RhinoUrbanEventHandler

## Knowledge Gaps
- **34 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Not enough signal to generate questions. This usually means the corpus has no AMBIGUOUS edges, no bridge nodes, no INFERRED relationships, and all communities are tightly cohesive. Add more files or run with --mode deep to extract richer edges._