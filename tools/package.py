"""Assemble GameData/BDArmoryLite (with NKDR in BDArmoryLite/NKDR) from BDArmory 1.13.1, the NKDR download and the built DLL.

Asset paths are rewritten from BDArmory/... and NKDR/... to BDArmoryLite/... and BDArmoryLite/NKDR/....
Every weapon part is also given an editor subcategory (see tools/categorize.py) for VAB Organizer.

Usage: python3 -I tools/package.py [path/to/North_Kerbin_Dynamic_Renewed-0.1.1/gameData] [path/to/BDArmory-1.13.1/BDArmory/Distribution/GameData]

Everything under GameData/BDArmoryLite is regenerated from scratch on each run, except for
files listed in OVERLAY (hand-written files in tools/overlay/ that are copied over the top).
"""
import os, re, shutil, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import categorize

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(ROOT), 'North_Kerbin_Dynamic_Renewed-0.1.1', 'gameData')
BDA_SRC = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(ROOT), 'BDArmory-1.13.1', 'BDArmory', 'Distribution', 'GameData')
BDA = os.path.join(BDA_SRC, 'BDArmory')
NKDR = os.path.join(SRC, 'NKDR')
OUT_ROOT = os.path.join(ROOT, 'GameData')
OUT = os.path.join(OUT_ROOT, 'BDArmoryLite')
EXTRAS = os.path.join(ROOT, 'Extras')
DLL = os.path.join(ROOT, 'build', 'BDArmory.dll')
OVERLAY = os.path.join(ROOT, 'tools', 'overlay')

# --- What to take from BDArmory ---
BDA_DIRS = ['AssetBundles', 'BulletDefs', 'FX', 'Localization', 'MMPatches', 'Models', 'Parts', 'Resources', 'Sounds', 'Textures', 'Agencies']
BDA_FILES = ['License.txt', 'Credits.txt']
EXCLUDE = [  # paths relative to the BDArmory folder (prefix match), plus junk everywhere
    'Parts/aiPilot',                 # AI pilots (removed)
    'Models/Mutators', 'Models/WayPoint', 'Textures/Mutators',  # Mutators / waypoint courses (removed)
    'Textures/icon_ai.png', 'Textures/icon_vm.png', 'Textures/icon_Armor.png', 'Textures/Formation',  # Toolbar icons and wing commander icons for removed systems
    'MMPatches/VABOrganiser.cfg',    # BDArmory's own VAB Organizer groups; BDArmory Lite writes its own (see categorize.py)
    'MMPatches/BDA_KerbalSuitSelector.cfg',  # Suit chooser on every crewed part (module not included)
    'BulletDefs/BD_Mutators.cfg', 'BulletDefs/Inventory.txt',
    'MMPatches/StructPWing.cfg',     # B9 procedural structural panel clone, not a weapons system
    'Parts/AmmoBox/UniversalAmmoBoxBDA.cfg',  # "Universal Ammo Box (Legacy)": duplicate of BDAcUniversalAmmoBox (same model, same ammo)
]
JUNK = re.compile(r'(^|/)(\.DS_Store|Thumbs\.db|\.note)$')


def excluded(rel):
    rel = rel.replace(os.sep, '/')
    return JUNK.search(rel) is not None or any(rel == e or rel.startswith(e + '/') for e in EXCLUDE)


def copy_tree(src, dst, base=''):
    for dirpath, dirnames, filenames in os.walk(src):
        rel_dir = os.path.relpath(dirpath, src)
        rel_dir = '' if rel_dir == '.' else rel_dir
        dirnames[:] = [d for d in dirnames if not excluded(os.path.join(base, rel_dir, d))]
        for f in filenames:
            rel = os.path.join(rel_dir, f)
            if excluded(os.path.join(base, rel)):
                continue
            os.makedirs(os.path.join(dst, rel_dir), exist_ok=True)
            shutil.copy2(os.path.join(dirpath, f), os.path.join(dst, rel))


# --- Config text helpers ---
def read(path):
    raw = open(path, 'rb').read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    text = raw.decode('utf-8-sig', errors='replace').replace('\r\n', '\n')
    return text, bom


def write(path, text, bom=False):
    open(path, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + text.encode('utf-8'))


def remove_module_nodes(text, module_names):
    """Remove MODULE { name = X ... } nodes (any nesting depth) whose name is in module_names."""
    lines = text.split('\n')
    out, i, removed = [], 0, 0
    while i < len(lines):
        if re.match(r'^\s*MODULE\s*(\{.*)?$', lines[i]):
            # find block extent
            depth, j, opened = 0, i, False
            while j < len(lines):
                code = lines[j].split('//')[0]
                depth += code.count('{') - code.count('}')
                opened = opened or '{' in code
                if opened and depth <= 0:
                    break
                j += 1
            block = '\n'.join(lines[i:j + 1])
            m = re.search(r'^\s*name\s*=\s*([^\s/]+)', block, re.M)
            if m and m.group(1) in module_names:
                removed += 1
                i = j + 1
                continue
        out.append(lines[i])
        i += 1
    return '\n'.join(out), removed


def remove_patch_blocks(text, needle):
    """Remove top-level ModuleManager patch blocks (@PART[...] { ... }) whose body contains needle."""
    lines = text.split('\n')
    out, i, removed = [], 0, 0
    while i < len(lines):
        if re.match(r'^\s*[@+%$!-]?PART\[', lines[i]):
            depth, j, opened = 0, i, False
            while j < len(lines):
                code = lines[j].split('//')[0]
                depth += code.count('{') - code.count('}')
                opened = opened or '{' in code
                if opened and depth <= 0:
                    break
                j += 1
            block = '\n'.join(lines[i:j + 1])
            if needle in block:
                removed += 1
                i = j + 1
                continue
        out.append(lines[i])
        i += 1
    return '\n'.join(out), removed


def rewrite_paths(text):
    """Point asset paths at the merged mod folder."""
    text = re.sub(r'(?<![\w/])BDArmory/', 'BDArmoryLite/', text)
    text = re.sub(r'(?<![\w/])NKDR/', 'BDArmoryLite/NKDR/', text)
    text = text.replace('BDArmoryLite/parts/', 'BDArmoryLite/Parts/')  # NKDR used the wrong case for BDArmory's Parts folder.
    text = text.replace('BDArmoryLite/NKDR/Explosion_Huge', 'BDArmoryLite/NKDR/effects/Explosion_Huge')  # Broken path in the K-12 Cloudmaker.
    text = text.replace('ammoName = 50Californium bullet', 'ammoName = 50CalAmmo')  # NKDR's .50cal(ifornium) turret named an ammo resource that doesn't exist.
    return text


VABO_CATEGORY_PRIORITY = 72   # VAB Organizer's stock Utility subcategories use 70, so the armory groups follow them.
ARMORY_STOCK_CATEGORY = 'Utility'  # Weapon parts in KSP's "none" (or, for armour, Structural) category move here.


def categorize_part(text):
    """Give a weapon part its editor subcategory. Returns (new_text, subcategory key or None)."""
    if not re.search(r'^\s*PART\s*$|^\s*PART\s*\{', text, re.M):
        return text, None
    key = categorize.classify(text)
    if key is None:
        return text, None
    label = dict(categorize.SUBCATEGORIES)[key]
    lines = text.split('\n')
    # Drop any existing bdacategory; it's replaced below.
    lines = [l for l in lines if not re.match(r'^\s*bdacategory\s*=', l)]
    cat_idx = next((i for i, l in enumerate(lines) if re.match(r'^\s*category\s*=', l)), None)
    if cat_idx is not None:
        current = lines[cat_idx].split('=', 1)[1].split('//')[0].strip()
        if current in ('none', 'Structural'):
            indent = re.match(r'^(\s*)', lines[cat_idx]).group(1)
            lines[cat_idx] = f'{indent}category = {ARMORY_STOCK_CATEGORY} // BDArmory Lite (was {current})'
    name_idx = next(i for i, l in enumerate(lines) if re.match(r'^\s*name\s*=', l))
    indent = re.match(r'^(\s*)', lines[name_idx]).group(1) or '\t'
    insert = [f'{indent}bdacategory = {label}',
              f'{indent}VABORGANIZER',
              f'{indent}{{',
              f'{indent}\torganizerSubcategory = bda{key}',
              f'{indent}}}']
    if cat_idx is None:
        insert.insert(0, f'{indent}category = {ARMORY_STOCK_CATEGORY}')
    lines[name_idx + 1:name_idx + 1] = insert
    return '\n'.join(lines), key


def write_vabo_subcategories(path):
    out = ['// Editor subcategories for VAB Organizer (https://github.com/KSPModStewards/VABOrganizer).',
           '// Generated by tools/package.py. Harmless when VAB Organizer is not installed.']
    for priority, (key, label) in enumerate(categorize.SUBCATEGORIES, 1):
        out += ['ORGANIZERSUBCATEGORY', '{', f'\tname = bda{key}', f'\tLabel = {label}',
                f'\tPriority = {priority}', f'\tCategoryPriority = {VABO_CATEGORY_PRIORITY}', '}']
    open(path, 'w', encoding='utf-8').write('\n'.join(out) + '\n')


def main():
    if not os.path.isdir(BDA) or not os.path.isdir(NKDR):
        sys.exit(f'Source folders not found under {SRC}')
    if not os.path.isfile(DLL):
        sys.exit(f'{DLL} not found: run ./build.sh first')
    shutil.rmtree(OUT_ROOT, ignore_errors=True)
    shutil.rmtree(EXTRAS, ignore_errors=True)
    os.makedirs(OUT)

    for d in BDA_DIRS:
        copy_tree(os.path.join(BDA, d), os.path.join(OUT, d), d)
    for f in BDA_FILES:
        shutil.copy2(os.path.join(BDA, f), os.path.join(OUT, 'BDArmory-' + f))
    for d in ['parts', 'effects', 'sounds', 'flags']:
        copy_tree(os.path.join(NKDR, d), os.path.join(OUT, 'NKDR', d), 'NKDR/' + d)
    shutil.copy2(os.path.join(NKDR, 'NKD_Bullets.cfg'), os.path.join(OUT, 'NKDR', 'NKD_Bullets.cfg'))
    shutil.copy2(os.path.join(NKDR, 'Licences.txt'), os.path.join(OUT, 'NKDR-Licence.txt'))
    os.makedirs(os.path.join(OUT, 'Plugins'))
    shutil.copy2(DLL, os.path.join(OUT, 'Plugins', 'BDArmory.dll'))

    # Sample craft (KSP doesn't load craft from GameData; these are for copying into a save's Ships folder).
    craft_src = os.path.join(NKDR, 'Craft', 'SPH')
    for f in os.listdir(craft_src):
        if f.endswith('.craft'):  # SICBM (VAB) is left out: it needs the removed AI pilot part.
            os.makedirs(os.path.join(EXTRAS, 'Ships', 'SPH'), exist_ok=True)
            shutil.copy2(os.path.join(craft_src, f), os.path.join(EXTRAS, 'Ships', 'SPH', f))

    stats = {'paths': 0, 'modules': 0, 'patches': 0}
    categorize.load_ammo_resources(read(os.path.join(OUT, 'Resources', 'BDAmmo_Universal.cfg'))[0])
    categorized = {}
    for dirpath, _, filenames in os.walk(OUT):
        for f in filenames:
            if not f.endswith('.cfg'):
                continue
            p = os.path.join(dirpath, f)
            text, bom = read(p)
            new = rewrite_paths(text)
            if new != text:
                stats['paths'] += 1
            # Modules that no longer exist (AI pilots, wing commander) or never existed in this BDArmory (old NKDR leftovers).
            new, n = remove_module_nodes(new, {'BDModulePilotAI', 'BDModuleSurfaceAI', 'BDModuleVTOLAI', 'BDModuleOrbitalAI',
                                               'ModuleWingCommander', 'ModuleSelfSealingTank', 'BDALookConstraintUp', 'BDACategoryModule'})
            stats['modules'] += n
            if f == 'en-us.cfg' and 'Localization' in dirpath:
                new = new.replace('#LOC_BDArmory_WMWindow_title = BDA Weapon Manager', '#LOC_BDArmory_WMWindow_title = Weapon Manager')
            if 'Localization' not in dirpath:
                new, key = categorize_part(new)
                if key:
                    categorized[key] = categorized.get(key, 0) + 1
            if f == 'BDA_battledamage.cfg':
                new, n = remove_patch_blocks(new, 'ModuleSelfSealingTank')
                stats['patches'] += n
            if new != text:
                write(p, new, bom)

    write_vabo_subcategories(os.path.join(OUT, 'VABOrganizerSubcategories.cfg'))

    if os.path.isdir(OVERLAY):
        for dirpath, _, filenames in os.walk(OVERLAY):
            for f in filenames:
                rel = os.path.relpath(os.path.join(dirpath, f), OVERLAY)
                dst = os.path.join(OUT_ROOT, rel)
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                shutil.copy2(os.path.join(dirpath, f), dst)

    files = sum(len(fs) for _, _, fs in os.walk(OUT))
    size = sum(os.path.getsize(os.path.join(d, f)) for d, _, fs in os.walk(OUT) for f in fs)
    print(f'GameData/BDArmoryLite: {files} files, {size / 1e6:.1f} MB; rewrote paths in {stats["paths"]} configs, '
          f'removed {stats["modules"]} dead MODULE nodes and {stats["patches"]} self-sealing-tank patches.')
    print('Editor subcategories: ' + ', '.join(f'{k} {categorized.get(k, 0)}' for k, _ in categorize.SUBCATEGORIES))


if __name__ == '__main__':
    main()
