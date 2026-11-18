"""Resolve diff3 conflict hunks. Usage: resolve.py FILE MODE [hunk-numbers...]
MODE: upstream | lite | base | both (lite then upstream). Without hunk numbers, applies to all hunks (1-based)."""
import sys
path, mode = sys.argv[1], sys.argv[2]
which = {int(x) for x in sys.argv[3:]}
L = open(path).read().split('\n'); out = []; i = n = 0
while i < len(L):
    if L[i].startswith('<<<<<<< '):
        s = i
        while not L[i].startswith('||||||| '): i += 1
        m = i
        while L[i] != '=======': i += 1
        b = i
        while not L[i].startswith('>>>>>>> '): i += 1
        n += 1
        lite, base, up = L[s+1:m], L[m+1:b], L[b+1:i]
        if which and n not in which:
            out.extend(L[s:i+1])
        else:
            out.extend({'upstream': up, 'lite': lite, 'base': base, 'both': lite + up}[mode])
        i += 1; continue
    out.append(L[i]); i += 1
open(path, 'w').write('\n'.join(out))
print(f'{path}: {n} hunks, resolved {"all" if not which else sorted(which)} -> {mode}')
