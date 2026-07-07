import json
from graphify.build import build_from_json
from graphify.cluster import score_all
from graphify.analyze import god_nodes, surprising_connections, suggest_questions
from graphify.report import generate
from pathlib import Path

extraction = json.loads(Path('graphify-out/.graphify_extract.json').read_text(encoding='utf-8'))
detection  = json.loads(Path('graphify-out/.graphify_detect.json').read_text(encoding='utf-8'))
analysis   = json.loads(Path('graphify-out/.graphify_analysis.json').read_text(encoding='utf-8'))

root = str(Path('src').resolve())
G = build_from_json(extraction, root=root, directed=False)
communities = {int(k): v for k, v in analysis['communities'].items()}
cohesion = {int(k): v for k, v in analysis['cohesion'].items()}
tokens = {'input': extraction.get('input_tokens', 0), 'output': extraction.get('output_tokens', 0)}

# Build node-label lookup
node_labels = {n['id']: n['label'] for n in extraction['nodes']}

# Label communities based on member nodes
com_labels = {}
for cid, members in sorted(communities.items(), key=lambda x: -len(x[1])):
    labels = [node_labels.get(m, m) for m in members]
    if cid == 0:
        com_labels[cid] = "Domain Model & Urban Elements"
    elif cid == 1:
        com_labels[cid] = "Rhino Plugin & Pipeline"
    elif cid == 2:
        com_labels[cid] = "Command Surface"
    elif cid == 3:
        com_labels[cid] = "UI Message Contract"
    elif cid == 4:
        com_labels[cid] = "Metrics & Analytics"
    elif cid == 5:
        com_labels[cid] = "UI Infrastructure"
    elif cid == 6:
        com_labels[cid] = "Masterplan Design Process"
    elif cid == 7:
        com_labels[cid] = "Rhino UI HTML Entry Points"
    elif cid == 8:
        com_labels[cid] = "Plugin Packaging"
    elif cid == 9:
        com_labels[cid] = "Legacy Embedded UIs"
    else:
        com_labels[cid] = "Resource Assets"

questions = suggest_questions(G, communities, com_labels)
report = generate(G, communities, cohesion, com_labels, analysis['gods'], analysis['surprises'], detection, tokens, root, suggested_questions=questions)
Path('graphify-out/GRAPH_REPORT.md').write_text(report, encoding='utf-8')
Path('graphify-out/.graphify_labels.json').write_text(json.dumps({str(k): v for k, v in com_labels.items()}, ensure_ascii=False), encoding='utf-8')
print('Report updated with community labels')
