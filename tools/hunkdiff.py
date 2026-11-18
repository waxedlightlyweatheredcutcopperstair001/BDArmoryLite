"""For each conflict hunk, show what Lite changed vs base and what upstream changed vs base (compact).
Usage: hunkdiff.py ROOT FILE [maxlines]"""
import os, sys, difflib
root, rel = sys.argv[1], sys.argv[2]; mx = int(sys.argv[3]) if len(sys.argv) > 3 else 12
L = open(os.path.join(root, rel)).read().split('\n')
i = n = 0
def show(tag, a, b):
    d = [x for x in difflib.unified_diff(a, b, n=0, lineterm='') if x[:1] in '+-' and x[:3] not in ('+++', '---')]
    for x in d[:mx]: print(f'   {tag} {x.strip()[:170]}')
    if len(d) > mx: print(f'   {tag} ... (+{len(d) - mx} more)')
while i < len(L):
    if L[i].startswith('<<<<<<< '):
        s = i
        while not L[i].startswith('||||||| '): i += 1
        m = i
        while L[i] != '=======': i += 1
        b = i
        while not L[i].startswith('>>>>>>> '): i += 1
        lite, base, up = L[s+1:m], L[m+1:b], L[b+1:i]
        n += 1
        print(f'## {rel} hunk {n} @L{s+1} [lite {len(lite)}/base {len(base)}/up {len(up)}]')
        show('LITE', base, lite); show('UP  ', base, up)
    i += 1
