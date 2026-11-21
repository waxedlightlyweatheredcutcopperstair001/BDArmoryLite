# BDArmory Lite 2.1.5

A streamlined BDArmory for KSP 1.12.x: guns, missiles, bombs, radar, targeting, countermeasures,
armour plates and the weapon manager, with the North Kerbin Dynamics Renewed (NKDR) weapons
and BDArmory Extended weapons built in, a dark ZTheme-style interface, and none of the tournament
machinery.

Based on BDArmory Continued 1.13.1 (BrettRyland and contributors) and North Kerbin Dynamics
Renewed 0.1.1 (Harpwner and contributors) and BDArmory Extended 1.5.3 (SuicidalInsanity and
DocNappers; some missile models by Toonu).

## Install

1. Remove any existing `GameData/BDArmory`, `GameData/NKDR` and `GameData/BDArmoryExtended`
   folders. BDArmory Lite replaces all three and cannot be installed alongside them.
2. Copy the `BDArmoryLite` folder into `GameData`. NKDR and BDArmory Extended are inside it, in
   `BDArmoryLite/NKDR` and `BDArmoryLite/BDArmoryExtended`.

Required: **ModuleManager**.
Optional: **Physics Range Extender** (lets you fight beyond KSP's ~2.5 km physics range),
**Volumetric Explosion FX** or **Volumetric FX Reconfigured**, **VAB Organizer**, TweakScale.

With Volumetric Explosion FX installed, BDArmory's own explosion visuals are hidden so the two
don't draw on top of each other (sound and damage are unchanged). This follows Volumetric
Explosion FX's settings: turn its explosions or "replace stock visuals" off, or watch a blast from
beyond its effect distance, and BDArmory's visuals come back. There is also a toggle under
Settings > Gameplay.

## New in 2.1.5

- With Volumetric FX Reconfigured, all of BDArmory's nuclear explosion effects (the flash, shock, blast smoke, plume and
  debris) are now hidden out to 60 km too: 2.1.4 hid only part of them, so a giant dark smoke ball and a brown ground
  cloud were still drawn around the volumetric mushroom.

## New in 2.1.4

- With Volumetric FX Reconfigured, BDArmory's own nuclear explosion is hidden as far away as the volumetric one is drawn
  (60 km): between 10 and 60 km both were drawn, a huge white ball over the real cloud.
- The bomb blast-radius dome gets new orange materials (the first fix left it green).

## New in 2.1.3

- The bomb blast-radius dome is orange too (its green was baked into the model's vertex colours).

## New in 2.1.2

- The in-flight HUD is orange, matching the interface: aiming circles and reticles, target boxes, GPS markers, the
  bomb blast-radius ring, the radar and RWR displays (radar contacts: blue above you, orange level, yellow below).

## New in 2.1.1

- Works with **Volumetric FX Reconfigured** (which includes Volumetric Explosion FX): BDArmory's own explosion visuals are
  hidden while it draws the explosions, as with Volumetric Explosion FX.

## New in 2.1.0

- **All 152 BDArmory Extended 1.5.3 parts:** fixed guns and cannons (20 mm to 203 mm), lasers,
  more turrets and CIWS, torpedoes and launchers, missiles, missile pods, magazines and VLS cells,
  small warheads, scalable ammo boxes and drums, strip countermeasure dispensers, a cloaking
  device, a drop tank and a repulsor. They're sorted into the same editor groups as everything
  else (fixed lasers go under Guns, the drop tank under Bombs). The repulsor sits in its stock
  category; toggle it with its action and set the hover height in its part menu.
- **Missile launch warning:** missiles and bombs launch when the fire key has been held for the
  launch hold time (Settings > Gameplay). While you hold it, the weapon beeps with a rising
  pitch and a "Hold to launch" bar fills below the centre of the screen; let go early to cancel.
  It uses the UI volume and can be turned off next to the hold time.
- The launch hold time is now measured in real seconds. It used to depend on your frame rate,
  so at high frame rates missiles launched sooner than the setting said.
- Fixed BDArmory Extended's broken paths for the 57 mm gun sound, the assault MG smoke, and the
  Homing Hydra and SRAAM submunitions.

## New in 2.0.0

Everything from BDArmory 1.13.1 (up from 1.6.11), including:

- Reworked missile guidance and aerodynamics (lofting, command-to-line-of-sight, terminal
  guidance, torque and g limits, seeker timeouts), arming delays and altitude fuzes.
- Custom warheads (`BDCustomWarhead`), sonar and torpedo improvements, missile magazines and
  reloadable rails, and a reworked radar warning receiver with a missile warning system.
- Radar notching, asymmetric radar scan zones, chaff notch factors and line-of-sight checks
  through water.
- Laser attenuation in air and water, conic lasers, turret priorities and aim assist on custom
  turrets, and many weapon, armour and battle damage fixes.

New settings: UI scale can follow KSP's; a visual effects switch (with water splashes and
explosion light flashes) for slower computers; infinite countermeasures; ammo crossfeed; radar
notching; water blocking radar and IR; battle damage penetration threshold; hit point and point
defence logs.

Left out, like the rest of BDArmory's tournament tools: the course builder, competition
settings, and the Kerbal suit selector (it adds a suit option to every crewed part).

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
BDArmory Extended is CC BY-SA 4.0 (see `BDArmoryExtended-License.txt`).
This combined, modified work is distributed under CC BY-SA 4.0 with credit to both.
