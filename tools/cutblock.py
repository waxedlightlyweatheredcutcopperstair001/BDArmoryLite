"""Remove C# blocks by brace matching. Usage: cutblock.py FILE 'start text' ['start text' ...]
For each start text (must match exactly one line, substring match), deletes from that line
(plus any directly preceding '//' comment lines) through the line where the brace depth
opened at/after it returns to zero. A statement without braces (ending in ';') is removed alone.
If start text is prefixed with 'N:' the Nth (1-based) match is used; 'all:' removes every match."""
import sys
path = sys.argv[1]
raw = open(path, 'rb').read()
bom = raw.startswith(b'\xef\xbb\xbf'); text = raw.decode('utf-8-sig'); crlf = '\r\n' in text
L = text.replace('\r\n', '\n').split('\n')

def strip_code(l):
    out, i, s = [], 0, None
    while i < len(l):
        c = l[i]
        if s:
            if c == '\\': i += 2; continue
            if c == s: s = None
            i += 1; continue
        if c in '"\'': s = c; i += 1; continue
        if l.startswith('//', i): break
        out.append(c); i += 1
    return ''.join(out)

def cut(start_idx):
    depth, opened, j = 0, False, start_idx
    while j < len(L):
        code = strip_code(L[j])
        for ch in code:
            if ch == '{': depth += 1; opened = True
            elif ch == '}': depth -= 1
        if opened and depth <= 0: break
        if not opened and code.rstrip().endswith(';') and depth == 0: break
        j += 1
    s = start_idx
    while s > 0 and L[s-1].strip().startswith('//'): s -= 1
    return s, j

for spec in sys.argv[2:]:
    nth = None
    if spec.startswith('all:'): nth, spec = 'all', spec[4:]
    elif ':' in spec[:3] and spec.split(':',1)[0].isdigit(): n, spec = spec.split(':',1); nth = int(n)
    idxs = [i for i, l in enumerate(L) if spec in l]
    if not idxs: sys.exit(f'{path}: not found: {spec}')
    if nth is None and len(idxs) != 1: sys.exit(f'{path}: {len(idxs)} matches for: {spec}')
    targets = idxs if nth == 'all' else [idxs[(nth or 1) - 1]]
    for t in sorted(targets, reverse=True):
        t = next(i for i, l in enumerate(L) if i >= 0 and l is L[t]) if False else t
        s, e = cut(t)
        del L[s:e+1]
    print(f'{path}: removed {len(targets)} block(s) for: {spec[:70]}')
text = '\n'.join(L)
if crlf: text = text.replace('\n', '\r\n')
open(path, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + text.encode('utf-8'))
