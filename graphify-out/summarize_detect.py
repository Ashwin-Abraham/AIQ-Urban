import json
from pathlib import Path
r = json.loads(Path('graphify-out/.graphify_detect.json').read_text(encoding='utf-8'))
print('total_files:', r.get('total_files'))
print('total_words:', r.get('total_words'))
for cat, files in r.get('files', {}).items():
    print(f'  {cat}: {len(files)} files')
if r.get('skipped_sensitive'):
    print(f'  skipped_sensitive: {len(r["skipped_sensitive"])} files')
print('scan_root:', r.get('scan_root'))
