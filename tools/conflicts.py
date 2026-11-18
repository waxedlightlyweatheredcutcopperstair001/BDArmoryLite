"""List merge conflict hunks: file, line, lines on each side. Usage: conflicts.py ROOT [file...]  (-v to print hunks)"""
import os, sys
root = sys.argv[1]; verbose = '-v' in sys.argv
files = [a for a in sys.argv[2:] if a != '-v']
if not files:
    files = [os.path.relpath(os.path.join(d, f), root) for d, _, fs in os.walk(root) for f in fs if f.endswith('.cs')]
for rel in sorted(files):
    L = open(os.path.join(root, rel)).read().split('\n')
    hunks, i = [], 0
    while i < len(L):
        if L[i].startswith('<<<<<<< '):
            s = i; m = b = None
            while not L[i].startswith('>>>>>>> '):
                if L[i].startswith('||||||| '): m = i
                if L[i] == '=======': b = i
                i += 1
            hunks.append((s + 1, m - s - 1, b - m - 1, i - b - 1, s, m, b, i))
        i += 1
    if not hunks: continue
    print(f'{rel}: {len(hunks)} hunks ' + ' '.join(f'L{h[0]}[{h[1]}/{h[2]}/{h[3]}]' for h in hunks))
    if verbose:
        for h in hunks:
            print('\n'.join(L[h[4]:h[7] + 1])); print('-' * 60)
