"""Classify BDArmory Lite parts into the editor subcategories (used by package.py for VAB Organizer).

classify(cfg_text) -> subcategory name or None. The order of the checks matters (a gun turret with a radar is a gun turret).
"""
import re

# (key, label) in display order. Keys become VAB Organizer subcategory names prefixed with "bda".
SUBCATEGORIES = [
    ('Control', 'Control'),
    ('Guns', 'Guns'),
    ('GunTurrets', 'Gun Turrets'),
    ('LaserTurrets', 'Laser Turrets'),
    ('RocketPods', 'Rocket Pods'),
    ('RocketTurrets', 'Rocket Turrets'),
    ('Missiles', 'Missiles'),
    ('MissileTurrets', 'Missile Turrets'),
    ('Bombs', 'Bombs'),
    ('Ammo', 'Ammo'),
    ('Radars', 'Radars'),
    ('Countermeasures', 'Countermeasures'),
    ('Armor', 'Armor'),
]

CM_RESOURCES = {'CMFlare', 'CMChaff', 'CMSmoke', 'CMDecoy', 'CMBubble'}
AMMO_RESOURCES = set()  # Filled in by load_ammo_resources() from BDArmory's RESOURCE_DEFINITIONs.
MOUNT_MODULES = {'BDAdjustableRail', 'BDRotaryRail', 'BDDeployableRail', 'ModuleMissileRearm'}
MOUNT_PARTS = {'harpMGMCap', 'harpMGMTube', 'harpRocketTube'}  # NKDR launch tubes (plain structural parts used to build launchers).


def load_ammo_resources(text):
    for m in re.finditer(r'RESOURCE_DEFINITION\s*\n\s*\{\s*\n\s*name\s*=\s*([^\s/]+)', text):
        name = m.group(1)
        if name not in CM_RESOURCES:
            AMMO_RESOURCES.add(name)


def modules(text):
    return set(re.findall(r'^\s*MODULE\s*\n?\s*\{?\s*\n?\s*name\s*=\s*([A-Za-z0-9_]+)', text, re.M))


def field(text, key):
    m = re.search(r'^\s*%s\s*=\s*([^\n/]+)' % key, text, re.M)
    return m.group(1).strip() if m else None


def resources(text):
    return set(re.findall(r'^\s*RESOURCE\s*\n\s*\{\s*\n\s*name\s*=\s*([A-Za-z0-9_]+)', text, re.M))


def classify(text):
    mods = modules(text)
    weapon_type = (field(text, 'weaponType') or '').lower()
    turret = 'ModuleTurret' in mods
    if 'MissileFire' in mods:
        return 'Control'
    if 'ModuleWeapon' in mods:
        if weapon_type == 'laser':
            return 'LaserTurrets'  # BDArmory Lite has no fixed lasers; any laser counts as a laser turret.
        if weapon_type == 'rocket':
            return 'RocketTurrets' if turret else 'RocketPods'
        return 'GunTurrets' if turret else 'Guns'
    if 'MissileTurret' in mods or mods & MOUNT_MODULES or field(text, 'name') in MOUNT_PARTS:
        return 'MissileTurrets'  # Turrets, rails, bays and launch tubes.
    if 'MissileLauncher' in mods or 'BDModularGuidance' in mods or 'MultiMissileLauncher' in mods:
        missile_type = (field(text, 'missileType') or '').lower()
        return 'Bombs' if missile_type == 'bomb' else 'Missiles'  # Torpedoes go with missiles.
    if 'BDExplosivePart' in mods or 'BDModuleNuke' in mods or 'ModuleEMP' in mods:
        return 'Bombs'  # Standalone warheads (e.g. the small High Explosive Warhead).
    if mods & {'ModuleRadar', 'ModuleSpaceRadar', 'ModuleIRST', 'RadarWarningReceiver', 'ModuleTargetingCamera'}:
        return 'Radars'
    if mods & {'ModuleECMJammer', 'CMDropper', 'ModuleCloakingDevice'}:
        return 'Countermeasures'
    name = field(text, 'name') or ''
    if mods & {'BDAdjustableArmor', 'ModuleReactiveArmor'} or (field(text, 'bdacategory') or '').lower() == 'armor' \
            or re.match(r'BD[0-9.]+x[0-9.]+(panel|slope)Armor$', name):
        return 'Armor'
    if name == 'harpPod':
        return 'Guns'  # NKDR's modular gun pod body.
    res = resources(text)
    if res & CM_RESOURCES:
        return 'Countermeasures'  # Flare/chaff refill boxes.
    if 'ModuleAmmoSwitch' in mods or res & AMMO_RESOURCES:
        return 'Ammo'
    return None
