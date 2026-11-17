# BDArmory Lite 1.2.0

A streamlined BDArmory for KSP 1.12.x: guns, missiles, bombs, radar, targeting, countermeasures,
armour plates and the weapon manager, with the North Kerbin Dynamics Renewed (NKDR) weapons
built in, a dark ZTheme-style interface, and none of the tournament machinery.

Based on BDArmory Continued 1.6.11 (BrettRyland and contributors) and North Kerbin Dynamics
Renewed 0.1.1 (Harpwner and contributors).

## Install

1. Remove any existing `GameData/BDArmory` and `GameData/NKDR` folders. BDArmory Lite replaces
   both and cannot be installed alongside them.
2. Copy the `BDArmoryLite` folder into `GameData`. NKDR is inside it, in `BDArmoryLite/NKDR`.

Required: **ModuleManager**.
Optional: **Physics Range Extender** (lets you fight beyond KSP's ~2.5 km physics range),
**Volumetric Explosion FX**, **VAB Organizer**, TweakScale.

With Volumetric Explosion FX installed, BDArmory's own explosion visuals are hidden so the two
don't draw on top of each other (sound and damage are unchanged). This follows Volumetric
Explosion FX's settings: turn its explosions or "replace stock visuals" off, or watch a blast from
beyond its effect distance, and BDArmory's visuals come back. There is also a toggle under
Settings > Gameplay.

## What's different from BDArmory

**Removed:** competitions and tournaments, the vessel switcher/spawner/mover, evolution,
remote orchestration, scoring, mutators, waypoint courses, asteroid modes, space-friction hacks,
the AI pilots (pilot, surface, VTOL, orbital) and their parts, Wing Commander, Kerbal Safety,
self-sealing tanks, the armour/hull customisation menus, the extra toolbar buttons, and the
"Universal Ammo Box (Legacy)" part (a duplicate of the Universal Ammo Box; craft that used it
need the newer box instead).

**Kept:** all weapons (guns, lasers, rockets, missiles, bombs, nukes, EMP), turrets and rails,
radar/IRST/RWR, targeting pods, ECM and cloaking, flares/chaff/smoke/decoys, armour plates and
reactive armour, ammo boxes and CASE, hit points and battle damage, guard mode, team icons.

**Editor.** Weapon parts are in the stock **Utility** category, plus an **Armory** button under
Filter by Function that lists only BDArmory Lite parts. With **VAB Organizer** installed they are
grouped under headers: Control, Guns, Gun Turrets, Laser Turrets, Rocket Pods, Rocket Turrets,
Missiles, Missile Turrets (including rails, bays and launch tubes), Bombs (including the small
High Explosive Warhead), Ammo, Radars (including targeting pods and the RWR), Countermeasures and
Armor. BDArmory's own sub-tab bar has been removed.

**One toolbar button.** In flight it opens the weapon manager; in the editor it opens settings.
Team Icons and Radar Cross-Section Analysis now open from the settings window.

**Hit points.** No armour or hull options on parts. Every part gets the hit points it would have
with a steel hull (x1.75) but keeps its normal mass, cost, heat and crash tolerance. Armour
plates are mild steel; their only option is thickness. Hit points appear in a part's menu only
in flight, once it has been damaged.

## Arming and detonation

- **Fire** (weapon manager, the Fire Missile action, or the part menu): the weapon releases as
  normal and arms itself after a short safety window: 1 second after release *and* once it is
  more than 10 m from the craft that launched it. Once armed it explodes on any impact, including
  the ground, water and buildings, even if no target was selected.
  Missiles with proximity fuzes still use them near their target.
- **Arm Warhead** (action group): arms a weapon that is still attached to its craft. It then
  detonates when it hits anything that is *not* part of its own craft (or enters water), and if
  the part is destroyed. Bumping against its own craft is ignored. This is what you want for a
  custom ICBM whose warhead never separates. Use the part's "Disarm Contact Fuze" button to make
  it safe again. The armed state is saved with the craft.
- **Detonate Warhead** (action group or part menu): explodes immediately, no matter what.

Like all KSP action groups, Arm and Detonate only reach parts on the craft you are flying. To
detonate a bomb you've already dropped, switch to it first.

Staying clear of your own blast is up to you. A nuke dropped from low altitude *will* take you
with it.

## Fixed NKDR issues

- Tsar Bob-a (and the other large nukes) never armed. Fixed by the new arming rules.
- K-12 Cloudmaker pointed at an explosion effect that didn't exist.
- The .50cal(ifornium) turret used an ammunition type that didn't exist; it now uses .50 cal ammo.
- Several turrets referenced BDArmory sounds with the wrong folder case.
- Leftover modules from an older BDArmory (BDALookConstraintUp, BDACategoryModule) removed.

## Licence

BDArmory is CC-BY-SA 2.0 (see `BDArmory-License.txt` and `BDArmory-Credits.txt`).
North Kerbin Dynamics Renewed is CC BY-SA 4.0 (see `NKDR-Licence.txt`).
This combined, modified work is distributed under CC BY-SA 4.0 with credit to both.
