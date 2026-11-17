using System.Text;
using UnityEngine;

using BDArmory.Extensions;
using BDArmory.FX;
using BDArmory.Settings;
using BDArmory.Weapons.Missiles;

namespace BDArmory.Weapons
{
    /// <summary>
    /// Nuclear warhead. Detonated by its MissileLauncher (when fired), by the Detonate action,
    /// or on contact once armed with the Arm action. See <see cref="WarheadFuze"/>.
    /// Note: Volumetric Explosion FX reads the public "yield" field by name; keep it.
    /// </summary>
    public class BDModuleNuke : PartModule
    {
        public static string defaultflashModelPath = "BDArmoryLite/Models/explosion/nuke/nukeFlash";
        [KSPField] public string flashModelPath = defaultflashModelPath;

        public static string defaultShockModelPath = "BDArmoryLite/Models/explosion/nuke/nukeShock";
        [KSPField] public string shockModelPath = defaultShockModelPath;

        public static string defaultBlastModelPath = "BDArmoryLite/Models/explosion/nuke/nukeBlast";
        [KSPField] public string blastModelPath = defaultBlastModelPath;

        public static string defaultPlumeModelPath = "BDArmoryLite/Models/explosion/nuke/nukePlume";
        [KSPField] public string plumeModelPath = defaultPlumeModelPath;

        public static string defaultDebrisModelPath = "BDArmoryLite/Models/explosion/nuke/nukeScatter";
        [KSPField] public string debrisModelPath = defaultDebrisModelPath;

        public static string defaultBlastSoundPath = "BDArmoryLite/Models/explosion/nuke/nukeBoom";
        [KSPField] public string blastSoundPath = defaultBlastSoundPath;

        [KSPField(isPersistant = true)] public float thermalRadius = 750;
        [KSPField(isPersistant = true)] public float yield = 0.05f;
        [KSPField(isPersistant = true)] public float fluence = 0.05f;
        [KSPField(isPersistant = true)] public bool isEMP = false;
        [KSPField(isPersistant = true)] public string reportingName = "Nuclear Warhead";

        /// <summary>Contact fuze set by the Arm action; survives save/load.</summary>
        [KSPField(isPersistant = true)] public bool contactArmed = false;

        public string Sourcevessel;
        bool hasDetonated = false;

        MissileLauncher missile;
        public MissileLauncher Launcher
        {
            get
            {
                if (missile) return missile;
                missile = part.FindModuleImplementing<MissileLauncher>();
                return missile;
            }
        }

        [KSPAction("Arm Warhead")]
        public void ArmAG(KSPActionParam param) => WarheadFuze.ArmContact(part);

        [KSPAction("Detonate Warhead")]
        public void DetonateAG(KSPActionParam param) => WarheadFuze.Detonate(part);

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Detonate Warhead", active = true)]
        public void DetonateEvent() => WarheadFuze.Detonate(part);

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Disarm Contact Fuze", active = true)]
        public void DisarmEvent() => WarheadFuze.DisarmContact(part);

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            WarheadFuze.SetupControls(this);
            if (HighLogic.LoadedSceneIsFlight)
            {
                Sourcevessel = part.vessel.GetName();
                part.OnJustAboutToBeDestroyed += OnAboutToBeDestroyed;
                GameEvents.onGameSceneSwitchRequested.Add(HandleSceneChange);
                if (contactArmed) WarheadFuze.ArmContact(part, quiet: true);
            }
        }

        void OnDestroy()
        {
            GameEvents.onGameSceneSwitchRequested.Remove(HandleSceneChange);
            if (part != null) part.OnJustAboutToBeDestroyed -= OnAboutToBeDestroyed;
        }

        void HandleSceneChange(GameEvents.FromToAction<GameScenes, GameScenes> fromTo)
        {
            if (fromTo.from == GameScenes.FLIGHT) hasDetonated = true; // Never detonate because the scene is unloading.
        }

        /// <summary>An armed (contact-fuzed) warhead goes off if the part is destroyed. Launched missiles handle this themselves.</summary>
        void OnAboutToBeDestroyed()
        {
            if (contactArmed && (Launcher == null || !Launcher.HasFired)) Detonate();
        }

        /// <summary>Detonate now. Callers decide whether detonation is allowed; this only guards against repeats and scene changes.</summary>
        public void Detonate()
        {
            if (hasDetonated || FlightGlobals.currentMainBody == null || part == null) return;
            hasDetonated = true;
            contactArmed = false;
            if (BDArmorySettings.DEBUG_OTHER) Debug.Log($"[BDArmory.BDModuleNuke]: Detonating {reportingName} on {Sourcevessel}");
            NukeFX.CreateExplosion(part.transform.position, Launcher != null ? ExplosionSourceType.Missile : ExplosionSourceType.BattleDamage, Sourcevessel, reportingName, 0, thermalRadius, yield, fluence, isEMP, blastSoundPath, flashModelPath, shockModelPath, blastModelPath, plumeModelPath, debrisModelPath, "", "", nukePart: part);
            if (part.vessel != null) part.Destroy(); // Not already being destroyed.
        }

        public override string GetInfo()
        {
            var output = new StringBuilder();
            output.AppendLine("Nuclear Warhead");
            output.AppendLine($"Yield: {yield} kt");
            output.AppendLine($"Thermal radius: {thermalRadius} m");
            output.AppendLine($"Generates EMP: {isEMP}");
            return output.ToString();
        }
    }
}
