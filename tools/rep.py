"""Exact-text replacement helper. Usage: python3 -I rep.py FILE  (reads pairs from stdin)
stdin format: blocks separated by lines '<<<<', '====', '>>>>':
<<<<
old text
====
new text
>>>>
Each old text must occur exactly once (or N times if the '<<<<' line is '<<<< N' or '<<<< all')."""
import sys, re
path = sys.argv[1]
raw = open(path, 'rb').read()
bom = raw.startswith(b'\xef\xbb\xbf')
text = raw.decode('utf-8-sig')
crlf = '\r\n' in text
if crlf: text = text.replace('\r\n', '\n')
spec = sys.stdin.read()
blocks = re.findall(r'^<<<<( \S+)?\n(.*?)\n====\n(.*?)\n?>>>>$', spec, re.S | re.M)
if not blocks: sys.exit('no blocks parsed')
for cnt, old, new in blocks:
    cnt = cnt.strip()
    n = text.count(old)
    if cnt == 'all':
        if n == 0: sys.exit(f'{path}: NOT FOUND:\n{old[:300]}')
    else:
        want = int(cnt) if cnt else 1
        if n != want: sys.exit(f'{path}: expected {want} got {n}:\n{old[:300]}')
    text = text.replace(old, new)
if crlf: text = text.replace('\n', '\r\n')
open(path, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + text.encode('utf-8'))
print(f'{path}: {len(blocks)} edit(s) ok')
