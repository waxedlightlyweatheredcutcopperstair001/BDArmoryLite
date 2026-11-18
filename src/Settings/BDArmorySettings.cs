using UnityEngine;

using System.IO;
using System.Collections.Generic;

namespace BDArmory.Settings
{
    public class BDArmorySettings
    {
        public static string oldSettingsConfigURL = Path.GetFullPath(Path.Combine(KSPUtil.ApplicationRootPath, "GameData/BDArmoryLite/settings.cfg")); // Migrate from the old settings file to the new one in PluginData so that we don't invalidate the ModuleManager cache.
        public static string settingsConfigURL = Path.GetFullPath(Path.Combine(KSPUtil.ApplicationRootPath, "GameData/BDArmoryLite/PluginData/settings.cfg"));
        public static bool ready = false;

        #region Settings section toggles
        [BDAPersistentSettingsField] public static bool GAMEPLAY_SETTINGS_TOGGLE = false;
        [BDAPersistentSettingsField] public static bool GRAPHICS_UI_SETTINGS_TOGGLE = false;
        [BDAPersistentSettingsField] public static bool SLIDER_SETTINGS_TOGGLE = false;
        [BDAPersistentSettingsField] public static bool RADAR_SETTINGS_TOGGLE = false;
        [BDAPersistentSettingsField] public static bool DEBUG_SETTINGS_TOGGLE = false;
        #endregion

        #region Window settings
        [BDAPersistentSettingsField] public static bool STRICT_WINDOW_BOUNDARIES = true;
        [BDAPersistentSettingsField] public static float GUI_OPACITY = 1f;                   // Modify the GUI opacity.
        [BDAPersistentSettingsField] public static float UI_SCALE = 1f; // Global UI scaling. (Config value for when not following stock.)
        [BDAPersistentSettingsField] public static bool UI_SCALE_FOLLOWS_STOCK = true; // Global UI scaling follows stock
        public static float UI_SCALE_ACTUAL => UI_SCALE_FOLLOWS_STOCK ? GameSettings.UI_SCALE : UI_SCALE; // Use this one in code.
        public static float PREVIOUS_UI_SCALE = 1f; // For tracking changes
        #endregion

        #region General toggle settings
        //[BDAPersistentSettingsField] public static bool INSTAKILL = true; //Deprecated, only affects lasers; use an Instagib mutator instead
        [BDAPersistentSettingsField] public static bool INFINITE_AMMO = false;              //infinite Bullets/rockets/laserpower
        [BDAPersistentSettingsField] public static bool INFINITE_ORDINANCE = false;         //infinite missiles/bombs (on ordnance w/ Reload Module)
        [BDAPersistentSettingsField] public static bool LIMITED_ORDINANCE = false;         //MML ammo clamped to salvo size, no relaods
        [BDAPersistentSettingsField] public static bool INFINITE_COUNTERMEASURES = false;         //infinite CMs
        [BDAPersistentSettingsField] public static bool PERFORMANCE_OPTIONS = true;
        [BDAPersistentSettingsField] public static bool BULLET_HITS = true;
        [BDAPersistentSettingsField] public static bool WATER_HIT_FX = true;
        [BDAPersistentSettingsField] public static bool WEAPONS_RESPECT_CROSSFEED = true;
        public static bool waterHitEffect => PERFORMANCE_OPTIONS && WATER_HIT_FX;
        [BDAPersistentSettingsField] public static bool EJECT_SHELLS = true;
        [BDAPersistentSettingsField] public static bool LIGHTFX = true;                       // explosions spawn a LightFX
        public static bool LightFX => PERFORMANCE_OPTIONS && LIGHTFX;
        [BDAPersistentSettingsField] public static bool VESSEL_RELATIVE_BULLET_CHECKS = false;
        [BDAPersistentSettingsField] public static bool AIM_ASSIST = true;
        [BDAPersistentSettingsField] public static bool AIM_ASSIST_MODE = true;              // true = reticle follows bullet CPA position, false = reticle follows aiming position.
        [BDAPersistentSettingsField] public static bool DRAW_AIMERS = true;
        [BDAPersistentSettingsField] public static bool DISABLE_GUARDMODE_ON_SPAWN = true;   // Disable guardMode on WMs when they're spawned.

        [BDAPersistentSettingsField] public static bool REMOTE_SHOOTING = false;
        [BDAPersistentSettingsField] public static bool BOMB_CLEARANCE_CHECK = false;
        [BDAPersistentSettingsField] public static bool SHOW_AMMO_GAUGES = true;
        [BDAPersistentSettingsField] public static bool SHELL_COLLISIONS = true;
        [BDAPersistentSettingsField] public static bool BULLET_DECALS = true;
        [BDAPersistentSettingsField] public static bool GAPLESS_PARTICLE_EMITTERS = true;         // Use gapless particle emitters.
        public static bool GaplessParticleEmitters => PERFORMANCE_OPTIONS && GAPLESS_PARTICLE_EMITTERS;
        [BDAPersistentSettingsField] public static bool FLARE_SMOKE = true;                       // Flares leave a trail of smoke.
        public static bool FlareSmoke => PERFORMANCE_OPTIONS && FLARE_SMOKE;
        [BDAPersistentSettingsField] public static bool DEFAULT_FFA_TARGETING = false;            // Free-for-all combat style instead of teams (changes target selection behaviour). This could be removed now.
        [BDAPersistentSettingsField] public static bool RUNWAY_PROJECT = false;                    // Enable/disable Runway Project specific enhancements.
        //[BDAPersistentSettingsField] public static bool DISABLE_KILL_TIMER = true;                //disables the kill timers.
        [BDAPersistentSettingsField] public static bool RESET_HP = false;                         // Automatically reset HP of parts of vessels when they're spawned in flight mode.
        [BDAPersistentSettingsField] public static bool RESET_ARMOUR = false;                     // Automatically reset Armor material of parts of vessels when they're spawned in flight mode.
        [BDAPersistentSettingsField] public static bool RESET_HULL = false;                     // Automatically reset hull material of parts of vessels when they're spawned in flight mode.
        [BDAPersistentSettingsField] public static bool VEFX_HIDE_BDA_EXPLOSIONS = true;      // BDArmory Lite: hide BDArmory's explosion visuals when Volumetric Explosion FX is drawing its own.
        [BDAPersistentSettingsField] public static bool IGNORE_TERRAIN_CHECK = false;
        [BDAPersistentSettingsField] public static bool CHECK_WATER_TERRAIN = false;
        [BDAPersistentSettingsField] public static bool RADAR_NOTCHING = false;
        [BDAPersistentSettingsField] public static bool RADAR_ALLOW_SURFACE_WARFARE = true;
        [BDAPersistentSettingsField] public static float RADAR_NOTCHING_FACTOR = 1f;
        [BDAPersistentSettingsField] public static float RADAR_NOTCHING_SCR_FACTOR = 0.01f;
        //[BDAPersistentSettingsField] public static bool ADVANCED_EDIT = true;                     //Used for debug fields not nomrally shown to regular users //SI - Only usage is a commented out function in BDExplosivePart
        [BDAPersistentSettingsField] public static bool SCROLL_ZOOM_PREVENTION = true;                 // Prevent scroll-zoom when over most BDA windows.
        [BDAPersistentSettingsField] public static bool BULLET_WATER_DRAG = true;                       // do bullets/rockets get slowed down if fired into/under water
        [BDAPersistentSettingsField] public static bool UNDERWATER_VISION = false;                       //If false, Subs and other submerged vessels fully visible to surface/air craft and vice versa without detectors?
        [BDAPersistentSettingsField] public static bool PERSISTENT_FX = false;
        [BDAPersistentSettingsField] public static bool LEGACY_ARMOR = false;
        #endregion

        #region Debug Labels
        [BDAPersistentSettingsField] public static bool DEBUG_LINES = false;                 //AI/Weapon aim visualizers
        [BDAPersistentSettingsField] public static bool DEBUG_OTHER = false;                 //internal debugging
        [BDAPersistentSettingsField] public static bool DEBUG_ARMOR = false;                 //armor
        [BDAPersistentSettingsField] public static bool DEBUG_HP = false;                    //HP
        [BDAPersistentSettingsField] public static bool DEBUG_WEAPONS = false;               //Debug messages for guns/rockets/lasers and their projectiles
        [BDAPersistentSettingsField] public static bool DEBUG_APS = false;                   //Debug messages for point defense weapons
        [BDAPersistentSettingsField] public static bool DEBUG_MISSILES = false;              //Missile launch, tracking and targeting debug labels
        [BDAPersistentSettingsField] public static bool DEBUG_DAMAGE = false;                //Explosions and battle damage logging
        [BDAPersistentSettingsField] public static bool DEBUG_AI = false;                    //AI debugging
        [BDAPersistentSettingsField] public static bool DEBUG_RADAR = false;                 //FLIR/Radar and RCS debugging
        [BDAPersistentSettingsField] public static bool DEBUG_TELEMETRY = false;             //AI/WM UI debug telemetry display
        #endregion

        #region General slider settings
        [BDAPersistentSettingsField] public static int MAX_NUM_BULLET_DECALS = 200;
        [BDAPersistentSettingsField] public static int CAMERA_SWITCH_FREQUENCY = 10;                    // Controls the minimum time between automated camera switches
        [BDAPersistentSettingsField] public static float TRIGGER_HOLD_TIME = 0.2f;
        [BDAPersistentSettingsField] public static float BDARMORY_UI_VOLUME = 0.35f;
        [BDAPersistentSettingsField] public static float BDARMORY_WEAPONS_VOLUME = 0.45f;
        [BDAPersistentSettingsField] public static float MAX_GUARD_VISUAL_RANGE = 200000f;
        [BDAPersistentSettingsField] public static float MAX_ACTIVE_RADAR_RANGE = 200000f;        //NOTE: used ONLY for display range of radar windows! Actual radar range provided by part configs!
        [BDAPersistentSettingsField] public static bool LOGARITHMIC_RADAR_DISPLAY = true;                //NOTE: used ONLY for display range of radar windows! Actual radar range provided by part configs!
        [BDAPersistentSettingsField] public static float MAX_ENGAGEMENT_RANGE = 200000f;          //NOTE: used ONLY for missile dlz parameters!
        [BDAPersistentSettingsField] public static float IVA_LOWPASS_FREQ = 2500f;
        [BDAPersistentSettingsField] public static float BALLISTIC_TRAJECTORY_SIMULATION_MULTIPLIER = 128f;      // Multiplier of fixedDeltaTime for the large scale steps of ballistic trajectory simulations. Large values at extreme ranges may cause small inaccuracies. 128 with 1km/s bullets at their max range seems reasonable in most cases.
        [BDAPersistentSettingsField] public static float FIRE_RATE_OVERRIDE = 10f;
        [BDAPersistentSettingsField] public static float HP_THRESHOLD = 2000;                    //HP above this value will be scaled to a logarithmic value
        [BDAPersistentSettingsField] public static float HP_CLAMP = 0;                           //HP will be clamped to this value
        [BDAPersistentSettingsField] public static float MAX_ARMOR_LIMIT = -1;                   //Armor will be clamped to this limit if non-negative.
        [BDAPersistentSettingsField] public static bool PWING_EDGE_LIFT = true;                  //Toggle lift on PWing edges for balance with stock wings/remove edge abuse
        [BDAPersistentSettingsField] public static bool PWING_THICKNESS_AFFECT_MASS_HP = false;  //pWing thickness contributes to its mass calc instead of a static LiftArea derived value
        [BDAPersistentSettingsField] public static float MAX_PWING_LIFT = 4.54f;                //Clamp pWing lift to this amount
        [BDAPersistentSettingsField] public static bool NUMERIC_INPUT_SELF_UPDATE = true;             // Automatically update the display string in NumericInputField after attempting to parse the value.
        [BDAPersistentSettingsField] public static float NUMERIC_INPUT_DELAY = 0.5f;                  // Time before last input for "read and interpret" logic of NumericInputField.
        [BDAPersistentSettingsField] public static Vector2 PROC_ARMOR_ALT_LIMITS = new Vector2(0.01f, 100f); // Unclamped limits of proc armour panels.
        #endregion

        #region Physics constants
        [BDAPersistentSettingsField] public static float GLOBAL_LIFT_MULTIPLIER = 0.25f;
        [BDAPersistentSettingsField] public static float GLOBAL_DRAG_MULTIPLIER = 6f;
        [BDAPersistentSettingsField] public static float RECOIL_FACTOR = 0.75f;
        [BDAPersistentSettingsField] public static float DMG_MULTIPLIER = 100f;
        [BDAPersistentSettingsField] public static float BALLISTIC_DMG_FACTOR = 1.55f;
        [BDAPersistentSettingsField] public static float HITPOINT_MULTIPLIER = 3.0f;
        [BDAPersistentSettingsField] public static float EXP_DMG_MOD_BALLISTIC_NEW = 0.55f;     // HE bullet explosion damage multiplier
        [BDAPersistentSettingsField] public static float EXP_PEN_RESIST_MULT = 2.50f;           // Armor HE penetration resistance multiplier
        [BDAPersistentSettingsField] public static float EXP_DMG_MOD_MISSILE = 6.75f;           // Missile explosion damage multiplier
        [BDAPersistentSettingsField] public static float EXP_DMG_MOD_ROCKET = 1f;               // Rocket explosion damage multiplier (FIXME needs tuning; Note: rockets used Ballistic mod before, but probably ought to be more like missiles)
        [BDAPersistentSettingsField] public static float EXP_DMG_MOD_BATTLE_DAMAGE = 1f;        // Battle damage explosion damage multiplier (FIXME needs tuning; Note: CASE-0 explosions used Missile mod, while CASE-1, CASE-2 and fuel explosions used Ballistic mod)
        [BDAPersistentSettingsField] public static float EXP_DMG_MOD_HEAT = 1f;                 // HEAT warhead spall damage mult
        [BDAPersistentSettingsField] public static float HEAT_SPALL_MAX_RED = 0.5f;              // Max amount of spall that can be reduced by a spall liner layer (since the shaped charge pens, there's gonna be an upper limit to this)
        [BDAPersistentSettingsField] public static float EXP_IMP_MOD = 0.25f;
        [BDAPersistentSettingsField] public static float BUILDING_DMG_MULTIPLIER = 1f;
        [BDAPersistentSettingsField] public static float HEAT_CONE_HALF_ANGLE = 2.5f;
        [BDAPersistentSettingsField] public static float WEAPON_FX_DURATION = 15;               //how long do weapon secondary effects(EMP/choker/gravitic/etc) last
        [BDAPersistentSettingsField] public static float ZOMBIE_DMG_MULT = 0.1f;
        [BDAPersistentSettingsField] public static float ARMOR_MASS_MOD = 1f;                   //Armor mass multiplier
        [BDAPersistentSettingsField] public static bool KERBAL_ERA = true;
        [BDAPersistentSettingsField] public static float HMDCost = 1500f;
        [BDAPersistentSettingsField] public static float HMDCostPerSeat = 500f;
        [BDAPersistentSettingsField] public static float LASER_ATM_GAMMA = 0.000158f;                // Transmission factor for laser in atmosphere. ~75% transmission over 1800m. Based on Figure 6: https://www.mdpi.com/2073-4433/12/7/918
        [BDAPersistentSettingsField] public static float LASER_WATER_GAMMA = 0.151f;                  // Transmission factor for laser in water. 15% at 10m, ~3% at 20m, ~0% at 30m. Based on transmittance (c, Table 2) coefficient for clear water https://www.mdpi.com/1424-8220/25/10/3057.
        [BDAPersistentSettingsField] public static float CUSTOM_TURRET_AIM_ASSIST = 3;                //give weapons on custom turrets this many degrees of free Turret traverse to assist vs stock robotics suckiness
        #endregion

        #region FX
        [BDAPersistentSettingsField] public static bool FIRE_FX_IN_FLIGHT = false;
        [BDAPersistentSettingsField] public static int MAX_FIRES_PER_VESSEL = 10;                 //controls fx for penetration only for landed or splashed //this is only for physical missile collisons into fueltanks - SI
        [BDAPersistentSettingsField] public static float FIRELIFETIME_IN_SECONDS = 90f;           //controls fx for penetration only for landed or splashed
        #endregion

        #region Radar settings
        [BDAPersistentSettingsField] public static float RWR_WINDOW_SCALE_MIN = 0.50f;
        [BDAPersistentSettingsField] public static float RWR_WINDOW_SCALE = 1f;
        [BDAPersistentSettingsField] public static float RWR_WINDOW_SCALE_MAX = 1.50f;
        [BDAPersistentSettingsField] public static float RADAR_WINDOW_SCALE_MIN = 0.50f;
        [BDAPersistentSettingsField] public static float RADAR_WINDOW_SCALE = 1f;
        [BDAPersistentSettingsField] public static float RADAR_WINDOW_SCALE_MAX = 1.50f;
        [BDAPersistentSettingsField] public static float TARGET_WINDOW_SCALE_MIN = 0.50f;
        [BDAPersistentSettingsField] public static float TARGET_WINDOW_SCALE = 1f;
        [BDAPersistentSettingsField] public static float TARGET_WINDOW_SCALE_MAX = 2f;
        [BDAPersistentSettingsField] public static float TARGET_CAM_RESOLUTION = 1024f;
        [BDAPersistentSettingsField] public static bool BW_TARGET_CAM = true;
        [BDAPersistentSettingsField] public static bool TARGET_WINDOW_INVERT_MOUSE_X = false;
        [BDAPersistentSettingsField] public static bool TARGET_WINDOW_INVERT_MOUSE_Y = false;
        #endregion

        #region Game modes
        [BDAPersistentSettingsField] public static bool PEACE_MODE = false;
        [BDAPersistentSettingsField] public static bool PAINTBALL_MODE = false;
        [BDAPersistentSettingsField] public static bool BATTLEDAMAGE = true;
        [BDAPersistentSettingsField] public static bool HEART_BLEED_ENABLED = false;
        [BDAPersistentSettingsField] public static bool RESOURCE_STEAL_ENABLED = false;
        [BDAPersistentSettingsField] public static bool ZOMBIE_MODE = false;
        [BDAPersistentSettingsField] public static bool DISCO_MODE = false;
        [BDAPersistentSettingsField] public static float AIMING_VISUAL_MALUS = 0; // Malus to visual aiming with a mk1 eyeball.
        #endregion

        #region Battle Damage settings
        [BDAPersistentSettingsField] public static bool BATTLEDAMAGE_TOGGLE = false;    // Main battle damage toggle.
        [BDAPersistentSettingsField] public static float BD_DAMAGE_CHANCE = 5;          // Base chance per-hit to proc damage
        [BDAPersistentSettingsField] public static float BD_DAMAGE_PENETRATION = 0.2f;  // Penetration factor required to proc damage
        [BDAPersistentSettingsField] public static bool BD_SUBSYSTEMS = true;           // Non-critical module damage?
        [BDAPersistentSettingsField] public static bool BD_TANKS = true;                // Fuel tanks, batteries can leak/burn
        [BDAPersistentSettingsField] public static float BD_TANK_LEAK_TIME = 20;        // Leak duration
        [BDAPersistentSettingsField] public static float BD_TANK_LEAK_RATE = 1;         // Leak rate modifier
        [BDAPersistentSettingsField] public static bool BD_AMMOBINS = true;             // Can ammo bins explode?
        [BDAPersistentSettingsField] public static bool BD_VOLATILE_AMMO = false;       // Ammo bins guaranteed to explode when destroyed
        [BDAPersistentSettingsField] public static bool BD_PROPULSION = true;           // Engine thrust reduction, fires
        [BDAPersistentSettingsField] public static float BD_PROP_FLOOR = 20;            // Minimum thrust% damaged engines produce
        [BDAPersistentSettingsField] public static float BD_PROP_FLAMEOUT = 25;         // Remaining HP% engines flameout
        [BDAPersistentSettingsField] public static bool BD_PART_STRENGTH = false;        // Part strength - breakingForce/Torque - decreases as part takes damage
        [BDAPersistentSettingsField] public static float BD_PROP_DAM_RATE = 1;          // Rate multiplier, 0.1-2
        [BDAPersistentSettingsField] public static bool BD_INTAKES = true;              // Can intakes be damaged?
        [BDAPersistentSettingsField] public static bool BD_GIMBALS = true;              // Can gimbals be disabled?
        [BDAPersistentSettingsField] public static bool BD_AEROPARTS = true;            // Lift loss & added drag
        [BDAPersistentSettingsField] public static float BD_LIFT_LOSS_RATE = 1;         // Rate multiplier
        [BDAPersistentSettingsField] public static bool BD_CTRL_SRF = true;             // Disable ctrl srf actuatiors?
        [BDAPersistentSettingsField] public static bool BD_COCKPITS = false;            // Control degredation
        [BDAPersistentSettingsField] public static bool BD_PILOT_KILLS = false;         // Cockpit damage can kill pilots?
        [BDAPersistentSettingsField] public static bool BD_FIRES_ENABLED = true;        // Can fires occur
        [BDAPersistentSettingsField] public static bool BD_FIRE_DOT = true;             // Do fires do DoT
        [BDAPersistentSettingsField] public static float BD_FIRE_DAMAGE = 5;            // Do fires do DoT
        [BDAPersistentSettingsField] public static bool BD_FIRE_HEATDMG = true;         // Do fires add heat to parts/are fires able to cook off fuel/ammo?
        [BDAPersistentSettingsField] public static bool BD_INTENSE_FIRES = false;       // Do fuel tank fires DoT get bigger over time?
        [BDAPersistentSettingsField] public static bool BD_FIRE_FUELEX = true;          // Can fires detonate fuel tanks
        [BDAPersistentSettingsField] public static float BD_FIRE_CHANCE_TRACER = 10;
        [BDAPersistentSettingsField] public static float BD_FIRE_CHANCE_HE = 25;
        [BDAPersistentSettingsField] public static float BD_FIRE_CHANCE_INCENDIARY = 90;
        #endregion

        #region Hall of Shame Settings
        [BDAPersistentSettingsField] public static bool ALLOW_ZOMBIE_BD = false;          // Allow battle damage to proc when using zombie mode?
        [BDAPersistentSettingsField] public static bool ENABLE_HOS = false;
        [BDAPersistentSettingsField] public static List<string> HALL_OF_SHAME_LIST = new List<string>();
        [BDAPersistentSettingsField] public static float HOS_FIRE = 0;
        #endregion

        #region Remote logging
        [BDAPersistentSettingsField] public static int RUNWAY_PROJECT_ROUND = 0;                                          // RWP round index.
        [BDAPersistentSettingsField] public static string REMOTE_ORCHESTRATION_NPC_SWAPPER = "Rammer";
        #endregion

        #region Heartbleed
        [BDAPersistentSettingsField] public static float HEART_BLEED_RATE = 0.01f;
        [BDAPersistentSettingsField] public static float HEART_BLEED_INTERVAL = 10f;
        [BDAPersistentSettingsField] public static float HEART_BLEED_THRESHOLD = 10f;
        #endregion

        #region Resource steal
        [BDAPersistentSettingsField] public static bool RESOURCE_STEAL_RESPECT_FLOWSTATE_IN = true;     // Respect resource flow state in (stealing).
        [BDAPersistentSettingsField] public static bool RESOURCE_STEAL_RESPECT_FLOWSTATE_OUT = false;   // Respect resource flow state out (stolen).
        [BDAPersistentSettingsField] public static float RESOURCE_STEAL_FUEL_RATION = 0.2f;
        [BDAPersistentSettingsField] public static float RESOURCE_STEAL_AMMO_RATION = 0.2f;
        [BDAPersistentSettingsField] public static float RESOURCE_STEAL_CM_RATION = 0f;
        #endregion

        #region GunGame
        //[BDAPersistentSettingsField] public static bool GG_ANNOUNCER = false;
        #endregion

        #region Tournament settings
        [BDAPersistentSettingsField] public static float QUIT_MEMORY_USAGE_THRESHOLD = float.MaxValue; // Automatically quit KSP when memory usage is beyond this. (0 = disabled)
        [BDAPersistentSettingsField] public static string LAST_USED_SAVEGAME = "";                 // Name of the last used savegame (for auto_generate_tournament_on_resume).
        #endregion

        #region Missile & Countermeasure Settings
        [BDAPersistentSettingsField] public static bool MISSILE_CM_SETTING_TOGGLE = false;
        [BDAPersistentSettingsField] public static bool VARIABLE_MISSILE_VISIBILITY = false;        //missile visual detection range dependant on boost/cruise/post-thrust state
        [BDAPersistentSettingsField] public static bool ASPECTED_RCS = false;                   //RCS evaluated in real-time based on aircraft's aspect
        [BDAPersistentSettingsField] public static float ASPECTED_RCS_OVERALL_RCS_WEIGHT = 0.25f;   //When ASPECTED_RCS = true, final aspected RCS will be = (1-ASPECTED_RCS_OVERALL_RCS_WEIGHT) * [Aspected RCS] + ASPECTED_RCS_OVERALL_RCS_WEIGHT * [Overall RCS]
        [BDAPersistentSettingsField] public static bool ASPECTED_IR_SEEKERS = false;                //IR Missiles will be subject to thermal occlusion mechanic
        [BDAPersistentSettingsField] public static bool DUMB_IR_SEEKERS = false;                  // IR missiles will go after hottest thing they can see
        [BDAPersistentSettingsField] public static float FLARE_FACTOR = 1.6f;                       // Change this to make flares more or less effective, values close to or below 1.0 will cause flares to fail to decoy often
        [BDAPersistentSettingsField] public static float CHAFF_FACTOR = 0.65f;                       // Change this to make chaff more or less effective. Higher values will make chaff batter, lower values will make chaff worse.
        [BDAPersistentSettingsField] public static float SMOKE_DEFLECTION_FACTOR = 10f;
        [BDAPersistentSettingsField] public static int APS_THRESHOLD = 60;                           // Threshold caliber that APS will register for intercepting hostile shells/rockets
        #endregion

    }
}
