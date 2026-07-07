import json
from pathlib import Path

nodes = []
edges = []
hyperedges = []
node_ids = set()

def add_node(nid, ntype, label, props=None):
    if nid not in node_ids:
        node_ids.add(nid)
        node = {"id": nid, "type": ntype, "label": label, "properties": props or {}}
        nodes.append(node)

def add_edge(src, tgt, etype, props=None):
    edges.append({"source": src, "target": tgt, "type": etype, "properties": props or {}})

SRC = str(Path("src").resolve())

# === AIQ.Urban.CONTEXT ===
add_node("aiq-urban-core", "module", "AIQ.Urban.Core",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md", "purpose": "Pure city-modelling domain logic"})

add_node("urban-model", "concept", "UrbanModel",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md", "description": "Singleton holding all IUrbanElements"})
add_node("iurban-element", "interface", "IUrbanElement",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("building", "concept", "Building",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("transit", "concept", "Transit",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("parcel", "concept", "Parcel",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("zone", "concept", "Zone",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("floor-plate", "concept", "FloorPlate",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("use", "concept", "Use",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md", "description": "Semantic label with Name and Color"})
add_node("transit-type", "enum", "TransitType",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md", "values": "Road, Railway, CyclePath, Pedestrian, Waterway"})
add_node("profile2d", "concept", "Profile2D",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md", "description": "Planar curve wrapper enforcing XY plane"})
add_node("polygon2d", "concept", "Polygon2D",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("transit-surface", "concept", "TransitSurface",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md", "description": "Unioned planar Brep of merged transit right-of-way"})
add_node("transit-graph", "concept", "TransitGraph",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("parcel2d", "concept", "Parcel2D",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("project-metrics", "concept", "ProjectMetrics",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md", "description": "Aggregate metrics for entire urban model"})
add_node("project-metrics-calc", "concept", "ProjectMetricsCalculator",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("zone-metrics", "concept", "ZoneMetrics",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("transit-metrics", "concept", "TransitMetrics",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("default-uses", "concept", "DefaultUses",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md", "uses": "Residential, Commercial, Retail, Industrial, MixedUse, Public, Park, Parking, Utility"})
add_node("topo-mesh", "concept", "TopoMesh",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})
add_node("vegetation", "concept", "Vegetation",
    {"source_location": f"{SRC}/AIQ.Urban.Core/CONTEXT.md"})

# Core edges
add_edge("urban-model", "iurban-element", "contains")
add_edge("building", "iurban-element", "implements")
add_edge("transit", "iurban-element", "implements")
add_edge("parcel", "iurban-element", "implements")
add_edge("zone", "iurban-element", "implements")
add_edge("floor-plate", "building", "part-of")
add_edge("floor-plate", "use", "has-use")
add_edge("parcel2d", "parcel", "is-type")
add_edge("transit-surface", "transit", "derived-from")
add_edge("transit-surface", "profile2d", "wraps")
add_edge("project-metrics", "building", "aggregates")
add_edge("project-metrics", "transit", "aggregates")
add_edge("project-metrics", "parcel", "aggregates")
add_edge("project-metrics", "zone", "aggregates")
add_edge("project-metrics-calc", "project-metrics", "computes")
add_edge("zone-metrics", "project-metrics", "part-of")
add_edge("transit-metrics", "project-metrics", "part-of")

# === AIQ.Urban.Rhino CONTEXT ===
add_node("aiq-urban-rhino", "module", "AIQ.Urban.RhinoPlugin",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md", "purpose": "Rhino 8 plugin bridging interaction model with Core"})
add_node("rhino-urban-pipeline", "concept", "RhinoUrbanModelPipeline",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md", "description": "Fluent step-based pipeline for model sync"})
add_node("rhino-event-handler", "concept", "RhinoUrbanEventHandler",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md", "description": "Handles Rhino doc events for mutation/deletion"})
add_node("event-queue", "concept", "EventQueue",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("reverse-lookup", "concept", "ReverseLookupDict",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md", "description": "Dictionary<Guid, IUrbanElement> keyed by Rhino ObjectId"})
add_node("transit-surface-unioner", "concept", "TransitSurfaceUnioner",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("recalculate-parcels", "concept", "RecalculateParcelsStep",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("add-transit-command", "command", "AddTransit_AIQUrban",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("add-building-command", "command", "AddBuilding_AIQUrban",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("add-zone-command", "command", "AddZone_AIQUrban",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("change-use-command", "command", "ChangeBuildingUse_AIQUrban",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("change-parcel-use-command", "command", "ChangeParcelUse_AIQUrban",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("reset-urban-command", "command", "ResetUrban_AIQUrban",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("start-ui-command", "command", "StartUI_AIQUrban",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("layer-structure", "concept", "LayerStructure",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md", "description": "AIQ Urban :: Transit/Building/Zone/Parcel/Vegetation/Topography hierarchy"})
add_node("html-ui", "concept", "HTML_UI",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md", "description": "Vue-based HTML UI for urban modelling"})
add_node("http-message-contract", "concept", "HTTP_POST_MessageContract",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("message-converter", "concept", "MessageConverter",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("imessage-from-ui", "interface", "IMessageFromUI",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("imessage-to-ui", "interface", "IMessageToUI",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})
add_node("project-metrics-serializer", "concept", "ProjectMetricsJsonSerializer",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/CONTEXT.md"})

add_edge("aiq-urban-rhino", "aiq-urban-core", "references")
add_edge("rhino-urban-pipeline", "urban-model", "operates-on")
add_edge("rhino-event-handler", "reverse-lookup", "maintains")
add_edge("rhino-event-handler", "event-queue", "uses")
add_edge("transit-surface-unioner", "transit-surface", "produces")
add_edge("recalculate-parcels", "parcel2d", "produces")
add_edge("add-transit-command", "rhino-urban-pipeline", "configures")
add_edge("add-building-command", "rhino-urban-pipeline", "configures")
add_edge("add-zone-command", "rhino-urban-pipeline", "configures")
add_edge("change-use-command", "rhino-urban-pipeline", "configures")
add_edge("html-ui", "start-ui-command", "started-by")
add_edge("http-message-contract", "imessage-from-ui", "defines")
add_edge("http-message-contract", "imessage-to-ui", "defines")
add_edge("message-converter", "imessage-from-ui", "routes")
add_edge("project-metrics-serializer", "project-metrics", "serializes")

# === AmbitionAndPlan ===
add_node("ambition-plan", "document", "AmbitionAndPlan",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/AmbitionAndPlan.md", "purpose": "Guides AI-assisted urban masterplanning process"})
add_node("design-steps", "concept", "DesignSteps",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/AmbitionAndPlan.md", "steps": "Program Mix, Site Details, Design Principles, Concept Planning, Spatial Strategy, Geometry Generation, Verification"})
add_node("frame-propose-ask", "pattern", "Frame-Propose-Ask",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/AmbitionAndPlan.md"})
add_node("concept-planning", "concept", "ConceptPlanning",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/AmbitionAndPlan.md"})
add_node("spatial-strategy", "concept", "SpatialStrategy",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/AmbitionAndPlan.md"})
add_edge("ambition-plan", "design-steps", "defines")
add_edge("design-steps", "frame-propose-ask", "uses-pattern")
add_edge("design-steps", "concept-planning", "includes")
add_edge("design-steps", "spatial-strategy", "includes")

# === HTML files (minimal) ===
add_node("rhino-ui-index", "file", "Rhino UI index.html",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/HTML_UI/wwwroot/index.html", "description": "Vue app entry point for AIQ Urban UI"})
add_node("rhino-ui-404", "file", "Rhino UI 404.html",
    {"source_location": f"{SRC}/AIQ.Urban.Rhino/HTML_UI/wwwroot/404.html", "description": "Custom 404 page for UI"})
add_node("old-embedded-index", "file", "Old Embedded index.html",
    {"source_location": f"{SRC}/ArchSitePlan.RhinoAddin/EmbeddedResources/index.html", "description": "Legacy embedded Vue app (ArchSitePlan)"})
add_node("old-rhino-interface-index", "file", "Old RhinoInterface index.html",
    {"source_location": f"{SRC}/ArchSitePlan.RhinoInterface/Resources/index.html", "description": "Legacy RhinoInterface Vue app"})

# === Yak Package & Manifest ===
add_node("yak-package", "file", "Yak Package Steps",
    {"source_location": f"{SRC}/ArchSitePlan.RhinoAddin/YakPackageCreationSteps.txt", "description": "Instructions to build and push Rhino plugin"})
add_node("plugin-manifest", "file", "Plugin Manifest",
    {"source_location": f"{SRC}/ArchSitePlan.RhinoAddin/manifest.yml", "name": "AIQ_Urban", "version": "2.1.0", "author": "Ashwin Abraham Mukkaranath"})
add_edge("plugin-manifest", "yak-package", "references")

# === Image files (basic nodes) ===
images = [
    ("lego-png", f"{SRC}/AIQ.Exports/AIQ Export Resources/LEGO.png", "AIQ Exports LEGO icon"),
    ("gh-apply-layerstate", f"{SRC}/AIQ.GH.Exports/Resources/ApplyLayerStateToDetail.png", "GH Apply LayerState icon"),
    ("gh-copy-layout", f"{SRC}/AIQ.GH.Exports/Resources/Copy Layout.png", "GH Copy Layout icon"),
    ("gh-find-detail", f"{SRC}/AIQ.GH.Exports/Resources/Find Detail.png", "GH Find Detail icon"),
    ("gh-print-pdf", f"{SRC}/AIQ.GH.Exports/Resources/Print PDF.png", "GH Print PDF icon"),
    ("gh-save-layerstate", f"{SRC}/AIQ.GH.Exports/Resources/Save LayerState.png", "GH Save LayerState icon"),
    ("gh-save-layerstate2", f"{SRC}/AIQ.GH.Exports/Resources/Save LayerState2.png", "GH Save LayerState icon 2"),
    ("gh-set-detail-target", f"{SRC}/AIQ.GH.Exports/Resources/Set Detail Target.png", "GH Set Detail Target icon"),
    ("gh-write-layerstate", f"{SRC}/AIQ.GH.Exports/Resources/WriteLayerState.png", "GH WriteLayerState icon"),
    ("core-logo", f"{SRC}/ArchSitePlan.Core/Resources/LOGO.png", "ArchSitePlan Core logo"),
    ("addin-embedded-logo", f"{SRC}/ArchSitePlan.RhinoAddin/EmbeddedResources/LOGO.png", "RhinoAddin embedded logo"),
    ("addin-icon", f"{SRC}/ArchSitePlan.RhinoAddin/Icon.png", "RhinoAddin plugin icon"),
    ("interface-cursor", f"{SRC}/ArchSitePlan.RhinoInterface/Resources/Cursor.png", "RhinoInterface cursor icon"),
    ("interface-logo", f"{SRC}/ArchSitePlan.RhinoInterface/Resources/LOGO.png", "RhinoInterface logo"),
    ("interface-logo-black", f"{SRC}/ArchSitePlan.RhinoInterface/Resources/LOGO_Black.png", "RhinoInterface logo black"),
    ("interface-line", f"{SRC}/ArchSitePlan.RhinoInterface/Resources/Line.png", "RhinoInterface line icon"),
    ("interface-pline", f"{SRC}/ArchSitePlan.RhinoInterface/Resources/Pline.png", "RhinoInterface polyline icon"),
    ("interface-question", f"{SRC}/ArchSitePlan.RhinoInterface/Resources/Question.png", "RhinoInterface question icon"),
]
for img_id, img_path, img_label in images:
    add_node(img_id, "image", img_label, {"source_location": img_path})

# === Cross-module edges ===
add_edge("rhino-urban-pipeline", "add-transit-command", "used-by")
add_edge("rhino-urban-pipeline", "add-building-command", "used-by")
add_edge("rhino-urban-pipeline", "add-zone-command", "used-by")

# === Write output ===
result = {"nodes": nodes, "edges": edges, "hyperedges": hyperedges, "input_tokens": 0, "output_tokens": 0}
Path("graphify-out/.graphify_semantic.json").write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding="utf-8")
print(f"Semantic extraction: {len(nodes)} nodes, {len(edges)} edges")
