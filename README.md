# BDArmory Lite: source and build

Player-facing documentation is in `tools/overlay/BDArmoryLite/README.md` (it ships as
`GameData/BDArmoryLite/README.md`).

## Layout

| Path | What |
|---|---|
| `src/` | C# source (from BDArmory Continued 1.13.1, tag `v1.13.1.0`, trimmed and modified; 1.x was based on 1.6.11) |
| `build.sh` | Builds `build/BDArmory.dll` |
| `tools/package.py` | Assembles `GameData/BDArmoryLite` (NKDR in `BDArmoryLite/NKDR`) from BDArmory 1.13.1's `Distribution/GameData`, the NKDR download and the DLL |
| `tools/validate.py` | Static checks on the package (asset paths, part modules, bullets, ammo, models) |
| `tools/overlay/` | Hand-written files copied over the package (README) |
| `tools/rep.py`, `tools/cutblock.py`, `tools/errs.sh`, `tools/merge_upstream.py`, `tools/resolve.py`, `tools/conflicts.py`, `tools/hunkdiff.py` | Editing/build helpers used during the port |
| `tools/categorize.py` | Assigns each weapon part its editor subcategory (VAB Organizer) |
| `GameData/BDArmoryLite/` | The finished mod (generated: don't edit by hand) |
| `Extras/Ships/SPH/` | NKDR sample craft (copy into a save's `Ships/SPH`) |

## Build

Requires the .NET SDK (8+; built with 10.0) and KSP's `KSP_Data/Managed` folder. No NuGet packages
are used: the project compiles against KSP's own mscorlib and Unity assemblies.

```bash
./build.sh                                   # uses ../Managed-20261117T003226Z-1-001/Managed
KSPManaged=/path/to/KSP_Data/Managed ./build.sh
python3 -I tools/package.py                  # needs ../North_Kerbin_Dynamic_Renewed-0.1.1/gameData and ../BDArmory-1.13.1
python3 -I tools/validate.py
```

The assembly is still named `BDArmory` and keeps BDArmory's namespaces, module names and config
fields, so NKDR-style part configs, other mods' `:NEEDS[BDArmory]` patches and Volumetric Explosion
FX (which reflects on `BDArmory.FX.ExplosionFx`, `NukeFX`, `BulletHitFX`, `FXEmitter`,
`Utils.ObjectPool`, `BDExplosivePart.tntMass`, `BDModuleNuke.yield`, ...) keep working. Asset paths
are rewritten from `BDArmory/...` and `NKDR/...` to `BDArmoryLite/...` and `BDArmoryLite/NKDR/...` (so add-ons
that reference BDArmory's files by path, e.g. TURD's default texture sets, won't find them).

Editor categories: `tools/package.py` writes `bdacategory`, `category = Utility` (for parts that were in
`none`/Structural) and a `VABORGANIZER { organizerSubcategory = bda... }` block on every weapon part, and
generates `VABOrganizerSubcategories.cfg`. `UI/BDAEditorCategory.cs` only adds the Armory filter button.

`FX/VolumetricExplosionFXBridge.cs` hides BDArmory's explosion renderers and lights when Volumetric
Explosion FX is installed and replacing visuals (read from its settings), keeping sound, damage and
the pooled explosion objects it watches.

## Version history

The git repository keeps every version (tags `v1.1.0`, `v1.2.0`, ...). GameData is tracked too, so each
version is complete without the original BDArmory/NKDR download.

## Main changes from upstream

- Removed: `Competition/` (except `BDTeam`), `VesselSpawning/`, `Evolution/`, game modes (asteroids,
  mutators, waypoints, space friction), all AI pilots and `IBDAIControl`, Wing Commander, Kerbal
  Safety, self-sealing tanks, the armour editor window, vessel switcher, score and remote
  orchestration windows, RWP settings, developer test code, and 193 dead settings.
- Scoring calls removed from the damage pipeline (bullets, rockets, explosions, nukes, fires).
- `Weapons/WarheadFuze.cs` (new): Arm (contact fuze) / Detonate (immediate) actions shared by
  `MissileLauncher`, `BDModuleNuke` and `BDExplosivePart`.
- `MissileBase`: arming after `ArmingDelay` (1 s) and `ArmingClearance` (10 m) instead of 1.25x blast
  radius; armed missiles detonate on any collision. `MissileLauncher`: armed missiles detonate on
  water contact; `Detonate(force)`.
- `BDModuleNuke`: rewritten without the RWP reactor-core logic.
- `HitpointTracker`: fixed aluminium hull for mass, steel health modifier for HP, armour type
  None (parts) / Mild Steel (plates), no customisation fields, HP shown only once damaged.
- UI: `UI/BDAStyle.cs` builds a dark ZTheme-style skin at runtime (no image assets); every window
  uses it. New compact settings window. One toolbar button.
