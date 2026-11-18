"""Three-way merge of an upstream BDArmory release into BDArmory Lite's src/.

Usage: merge_upstream.py BASE_SRC LITE_SRC NEW_SRC OUT_SRC
  BASE_SRC: upstream source Lite was made from (BDArmory/ folder with BDArmory.csproj)
  LITE_SRC: current Lite src/
  NEW_SRC:  new upstream source (BDArmory/ folder with BDArmory.csproj)
  OUT_SRC:  output folder (created/overwritten)
Files are normalised to LF (and no BOM) for merging. Prints a report of conflicts.
"""
import os, re, shutil, subprocess, sys, tempfile

BASE, LITE, NEW, OUT = sys.argv[1:5]
# Upstream files belonging to systems BDArmory Lite removes (beyond those Lite already deleted).
SKIP_NEW = {'GameModes/Waypoints/CourseBuilderGUI.cs', 'Settings/CompSettings.cs', 'Modules/KerbalSuitSelector.cs'}

def compile_list(src):
    proj = open(os.path.join(src, 'BDArmory.csproj'), encoding='utf-8-sig').read()
    return {p.replace('\\', '/') for p in re.findall(r'<Compile Include="([^"]+\.cs)"', proj)}

def lite_list(src):
    out = set()
    for d, dirs, fs in os.walk(src):
        dirs[:] = [x for x in dirs if x not in ('obj', 'bin')]
        for f in fs:
            if f.endswith('.cs'):
                out.add(os.path.relpath(os.path.join(d, f), src).replace(os.sep, '/'))
    return out

def norm(path):
    return open(path, 'rb').read().decode('utf-8-sig', errors='replace').replace('\r\n', '\n')

base, lite, new = compile_list(BASE), lite_list(LITE), compile_list(NEW)
shutil.rmtree(OUT, ignore_errors=True)
report = {'merged-clean': [], 'conflicts': [], 'lite-only': [], 'upstream-new': [], 'dropped(lite removed)': [],
          'dropped(upstream removed)': [], 'skipped-new(removed system)': [], 'taken-upstream(unchanged in lite)': []}
tmp = tempfile.mkdtemp()
for p in sorted(base | lite | new):
    out = os.path.join(OUT, p)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    inb, inl, inn = p in base, p in lite, p in new
    if inl and not inb and not inn:
        open(out, 'w').write(norm(os.path.join(LITE, p))); report['lite-only'].append(p); continue
    if inn and not inb and not inl:
        if p in SKIP_NEW: report['skipped-new(removed system)'].append(p); continue
        open(out, 'w').write(norm(os.path.join(NEW, p))); report['upstream-new'].append(p); continue
    if inb and not inl:
        report['dropped(lite removed)'].append(p); continue
    if inb and inl and not inn:
        report['dropped(upstream removed)'].append(p); continue
    # in base, lite and new: 3-way merge
    b, l, n = norm(os.path.join(BASE, p)), norm(os.path.join(LITE, p)), norm(os.path.join(NEW, p))
    if l == b:
        open(out, 'w').write(n); report['taken-upstream(unchanged in lite)'].append(p); continue
    fb, fl, fn = (os.path.join(tmp, x) for x in ('b', 'l', 'n'))
    open(fb, 'w').write(b); open(fl, 'w').write(l); open(fn, 'w').write(n)
    r = subprocess.run(['git', 'merge-file', '-p', '--diff3', '-L', 'lite', '-L', 'base-1.6.11', '-L', 'upstream', fl, fb, fn], capture_output=True, text=True)
    open(out, 'w').write(r.stdout)
    (report['conflicts'] if r.returncode > 0 else report['merged-clean']).append(f'{p} ({r.returncode})' if r.returncode > 0 else p)
    if r.returncode < 0: sys.exit(f'merge-file failed on {p}: {r.stderr}')
for k, v in report.items():
    print(f'== {k}: {len(v)}')
    if k in ('conflicts', 'upstream-new', 'lite-only', 'skipped-new(removed system)', 'dropped(upstream removed)', 'merged-clean'):
        for x in v: print('   ', x)
