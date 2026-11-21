"""Static checks on the packaged mod. Usage: python3 -I tools/validate.py

1. Every asset path (BDArmoryLite/...) in configs and in the DLL's string literals points at a file that exists.
2. Every part MODULE name is defined in BDArmory.dll or KSP's Assembly-CSharp (or a known optional mod).
3. Every bulletType / rocketType / ammo resource referenced by a weapon is defined.
4. Every part's mesh / MODEL file exists.
Exit code 1 on any problem."""
import os, re, sys, subprocess

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GD = os.path.join(ROOT, 'GameData')
MOD = os.path.join(GD, 'BDArmoryLite')
SCRATCH_META = os.environ.get('CLRMETA', os.path.join(os.path.dirname(os.path.abspath(__file__)), 'clrmeta.py'))  # CLR metadata reader for module checks
MANAGED = os.environ.get('KSPManaged', os.path.join(os.path.dirname(ROOT), 'Managed-20261117T003226Z-1-001', 'Managed'))
ASSET_EXT = ['.mu', '.png', '.dds', '.tga', '.jpg', '.mbm', '.truecolor', '.wav', '.ogg', '.mp3', '.bundle', '']
OPTIONAL_MODULES = {'TweakScale', 'FSanimateGeneric', 'ModuleB9PartSwitch', 'FSfuelSwitch', 'ModuleWaterfallFX'}  # handled by other mods if installed

# Referenced by BDArmory 1.6.11's code but never shipped in its release either (unused or optional features).
UPSTREAM_MISSING = {'BDArmoryLite/Models/seismicCharge/seismicExplosion', 'BDArmoryLite/Sounds/seismicCharge',
                    'BDArmoryLite/Sounds/decoySound', 'BDArmoryLite/Textures/grayCircle'}

problems = []
files_lower = {}
for d, _, fs in os.walk(GD):
    for f in fs:
        rel = os.path.relpath(os.path.join(d, f), GD).replace(os.sep, '/')
        files_lower[rel.lower()] = rel


def asset_exists(url):
    url = url.strip().strip('"')
    base = url.lower()
    for ext in ASSET_EXT:
        if (base + ext) in files_lower:
            return True
    return False


def read(p):
    return open(p, 'rb').read().decode('utf-8-sig', errors='replace').replace('\r\n', '\n')


cfgs = [os.path.join(d, f) for d, _, fs in os.walk(MOD) for f in fs if f.endswith('.cfg')]
texts = {p: read(p) for p in cfgs}

# 1. Asset paths in configs
path_re = re.compile(r'BDArmoryLite/[^\s,;"\'}]+')
for p, t in texts.items():
    for line in t.split('\n'):
        code = line.split('//')[0]
        for m in path_re.finditer(code):
            url = m.group(0)
            if not asset_exists(url):
                problems.append(f'missing asset {url}  ({os.path.relpath(p, MOD)})')

# 1b. Asset paths in the DLL (UTF-16 string literals)
dll = os.path.join(MOD, 'Plugins', 'BDArmory.dll')
raw = open(dll, 'rb').read()
for m in re.finditer(rb'B\x00D\x00A\x00r\x00m\x00o\x00r\x00y\x00L\x00i\x00t\x00e\x00/\x00((?:[\x20-\x7e]\x00)+)', raw):
    url = 'BDArmoryLite/' + m.group(1).decode('utf-16-le')
    url = url.split(' ')[0]  # Paths quoted inside log messages ("... not found.")
    if url.endswith('/') or url.endswith('.cfg') or '{' in url:
        continue  # directories, settings files and format strings
    if url in UPSTREAM_MISSING:
        continue
    if not asset_exists(url):
        problems.append(f'missing asset referenced by DLL: {url}')

# 2. Part modules
known_types = set()
if SCRATCH_META:
    for asm in [dll, os.path.join(MANAGED, 'Assembly-CSharp.dll')]:
        out = subprocess.run([sys.executable, '-I', SCRATCH_META, asm, 'types'], capture_output=True, text=True).stdout
        for line in out.split('\n'):
            if line.startswith('TYPE '):
                known_types.add(line.split()[1].split('.')[-1])
    for p, t in texts.items():
        if '/Localization/' in p:
            continue
        for m in re.finditer(r'^\s*MODULE\s*\n?\s*\{?\s*\n\s*name\s*=\s*([A-Za-z0-9_]+)', t, re.M):
            name = m.group(1)
            if name not in known_types and name not in OPTIONAL_MODULES:
                problems.append(f'unknown MODULE {name}  ({os.path.relpath(p, MOD)})')

# 3. Bullets, rockets, ammo resources
def names_of(node, alltext):
    # The first "name =" after each top-level NODE header.
    return set(m.group(1).strip() for m in re.finditer(r'^%s\s*\n\s*\{\s*\n(?:\s*//[^\n]*\n)*\s*name\s*=\s*([^\n/]+)' % node, alltext, re.M))
alltext = '\n'.join(texts.values())
bullets = names_of('BULLET', alltext)
rockets = names_of('ROCKET', alltext)
resources = names_of('RESOURCE_DEFINITION', alltext)
STOCK_RES = {'LiquidFuel', 'Oxidizer', 'MonoPropellant', 'ElectricCharge', 'SolidFuel', 'XenonGas', 'IntakeAir', 'Ore', 'Ablator'}
for p, t in texts.items():
    for key, pool in (('bulletType', bullets), ('rocketType', rockets)):
        for m in re.finditer(r'^\s*%s\s*=\s*([^\n/]+)' % key, t, re.M):
            for v in m.group(1).split(';'):
                v = v.strip()
                if v and v not in bullets and v not in rockets:  # Rocket launchers name their ROCKET in bulletType.
                    problems.append(f'undefined {key} "{v}"  ({os.path.relpath(p, MOD)})')
    for m in re.finditer(r'^\s*ammoName\s*=\s*([^\n/]+)', t, re.M):
        for v in m.group(1).split(';'):
            v = v.strip()
            if v and v not in resources and v not in STOCK_RES:
                problems.append(f'undefined ammo resource "{v}"  ({os.path.relpath(p, MOD)})')

# 4. Part meshes / models
for p, t in texts.items():
    if not re.search(r'^\s*PART\s*$', t, re.M) and not re.search(r'^\s*PART\s*\{', t, re.M):
        continue
    # KSP ignores "mesh =" and loads the .mu file in the part's folder when there's no MODEL node.
    folder = os.path.dirname(p)
    if re.search(r'^\s*mesh\s*=', t, re.M) and not re.search(r'^\s*MODEL\s*$|^\s*MODEL\s*\{', t, re.M):
        if not any(f.lower().endswith('.mu') for f in os.listdir(folder)):
            problems.append(f'no model in part folder  ({os.path.relpath(p, MOD)})')

for pr in sorted(set(problems)):
    print(pr)
print(f'{len(set(problems))} problem(s); checked {len(cfgs)} configs, {len(bullets)} bullets, {len(rockets)} rockets, {len(resources)} resources' + (f', {len(known_types)} known module types' if known_types else ''))
sys.exit(1 if problems else 0)
