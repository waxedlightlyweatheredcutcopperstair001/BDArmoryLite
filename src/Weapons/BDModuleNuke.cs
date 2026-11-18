using System.Text;
using UnityEngine;

using BDArmory.Extensions;
using BDArmory.FX;
using BDArmory.Settings;
using BDArmory.Weapons.Missiles;

namespace BDArmory.Weapons
{
<<<<<<< lite
    /// <summary>
    /// Nuclear warhead. Detonated by its MissileLauncher (when fired), by the Detonate action,
    /// or on contact once armed with the Arm action. See <see cref="WarheadFuze"/>.
    /// Note: Volumetric Explosion FX reads the public "yield" field by name; keep it.
    /// </summary>
    public class BDModuleNuke : PartModule
||||||| base-1.6.11
    class BDModuleNuke : PartModule
=======
    class BDModuleNuke : BDAPartModule
>>>>>>> upstream
    {
<<<<<<< lite
        public static string defaultflashModelPath = "BDArmoryLite/Models/explosion/nuke/nukeFlash";
        [KSPField] public string flashModelPath = defaultflashModelPath;
||||||| base-1.6.11
        [KSPField(isPersistant = true, guiActive = true, guiName = "WARNING: Reactor Safeties:", guiActiveEditor = false), UI_Label(affectSymCounterparts = UI_Scene.All, scene = UI_Scene.All)]//Weapon Name
        public string status = "OFFLINE";
=======
        //[KSPField(isPersistant = true, guiActive = true, guiName = "WARNING: Reactor Safeties:", guiActiveEditor = false), UI_Label(affectSymCounterparts = UI_Scene.All, scene = UI_Scene.All)]//Weapon Name
        //public string status = "OFFLINE";
>>>>>>> upstream

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

        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "#LOC_BDArmory_Status")]//Status
        public string guiStatusString = "DISARMED";

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
<<<<<<< lite
||||||| base-1.6.11
        ModuleEngines thisEngine;
        public ModuleEngines engineCoreEngine
        {
            get
            {
                if (hasCheckedEngineCore || thisEngine) return thisEngine;
                thisEngine = part.FindModuleImplementing<ModuleEngines>();
                hasCheckedEngineCore = true;
                return thisEngine;
            }
        }
        bool hasCheckedEngineCore = false; // Only check once, it's not going to change.
        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (engineCore)
                {
                    if (part.vessel.rootPart != part)
                    {
                        if (engineCoreEngine != null)
                        {
                            engineCoreEngine.allowShutdown = false;
                        }
                        part.force_activate();
                    }
                }
                else
                {
                    //Fields["status"].guiActive = false;
                    // Fields["fuelleft"].guiActive = false;
                    //Fields["status"].guiActiveEditor = false;
                    // Fields["fuelleft"].guiActiveEditor = false;
                }
                Sourcevessel = part.vessel.GetName();
=======
        ModuleEngines thisEngine;
        public ModuleEngines engineCoreEngine
        {
            get
            {
                if (hasCheckedEngineCore || thisEngine) return thisEngine;
                thisEngine = part.FindModuleImplementing<ModuleEngines>();
                hasCheckedEngineCore = true;
                return thisEngine;
            }
        }
        bool hasCheckedEngineCore = false; // Only check once, it's not going to change.

        bool Armed = false;

        [KSPAction("Arm")]
        public void ArmAG(KSPActionParam param)
        {
            Armed = true;
            guiStatusString = "ARMED"; // Future me, this needs localization at some point
            Events[nameof(ArmAG)].guiName = StringUtils.Localize("Disarm Warhead");//"Enable Engage Options"
        }

        [KSPAction("Detonate")]
        public void DetonateAG(KSPActionParam param)
        {
            if (Armed) Detonate();
        }

        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (engineCore)
                {
                    if (part.vessel.rootPart != part)
                    {
                        if (engineCoreEngine != null)
                        {
                            engineCoreEngine.allowShutdown = false;
                        }
                        part.force_activate();
                    }
                    // Fields[nameof(status)].guiActive = false;
                    // Fields[nameof(status)].guiActiveEditor = false;
                }
                else
                {
                    //Fields[nameof(status)].guiActive = false;
                    // Fields[nameof(fuelleft)].guiActive = false;
                    //Fields[nameof(status)].guiActiveEditor = false;
                    // Fields[nameof(fuelleft)].guiActiveEditor = false;
                }
                Sourcevessel = part.vessel.GetName();
>>>>>>> upstream

        [KSPAction("Arm Warhead")]
        public void ArmAG(KSPActionParam param) => WarheadFuze.ArmContact(part);

        [KSPAction("Detonate Warhead")]
        public void DetonateAG(KSPActionParam param) => WarheadFuze.Detonate(part);

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Detonate Warhead", active = true)]
        public void DetonateEvent() => WarheadFuze.Detonate(part);

<<<<<<< lite
        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Disarm Contact Fuze", active = true)]
        public void DisarmEvent() => WarheadFuze.DisarmContact(part);
||||||| base-1.6.11
                            }
                        }
                    }
                }
            }
        }
=======
                            }
                        }
                    }
                }
                if (guiStatusString != "ARMED" && Launcher != null && Launcher.MissileState > MissileBase.MissileStates.Drop) guiStatusString = "ARMED";
            }
        }
>>>>>>> upstream

        public override void OnStart(StartState state)
        {
<<<<<<< lite
            base.OnStart(state);
            WarheadFuze.SetupControls(this);
            if (HighLogic.LoadedSceneIsFlight)
||||||| base-1.6.11
            if (v != vessel || hasDetonated || goingCritical || !engineCore) return;
            VesselModuleRegistry.OnVesselModified(v);
            if (VesselModuleRegistry.GetModuleCount<MissileFire>(v) == 0)
=======
            if (v != vessel || hasDetonated || goingCritical || !engineCore) return;
            VesselModuleRegistry.OnVesselModified(v);
            if (v.ActiveController().WM == null)
>>>>>>> upstream
            {
                Sourcevessel = part.vessel.GetName();
                part.OnJustAboutToBeDestroyed += OnAboutToBeDestroyed;
                GameEvents.onGameSceneSwitchRequested.Add(HandleSceneChange);
                if (contactArmed) WarheadFuze.ArmContact(part, quiet: true);
            }
        }

        void OnDestroy()
        {
<<<<<<< lite
            GameEvents.onGameSceneSwitchRequested.Remove(HandleSceneChange);
            if (part != null) part.OnJustAboutToBeDestroyed -= OnAboutToBeDestroyed;
        }
||||||| base-1.6.11
            if (BDArmorySettings.DEBUG_OTHER) Debug.Log("[BDArmory.RWPS3R2NukeModule]: Nuclear engine on " + Sourcevessel + " going critical in " + delay.ToString("0.0") + "s.");
            goingCritical = true;
            yield return new WaitForSecondsFixed(0.5f);
            if (part.vessel.rootPart == part) //double check to ensure vessel is legitimately out of fuel, and not from poorly timed drop tanks/ions running out of Ec but not Xe, etc
            {
                bool engineOut = true;
                {
                    foreach (var e in VesselModuleRegistry.GetModuleEngines(vessel))
                    {
                        if (e != null && !e.flameout && e.vessel == part.vessel && e.thrustPercentage > 0)
                        {
                            engineOut = false; //we have a functioning engine, hold off on detonation
                            break;
                        }
                    }
                }
                if (!engineOut) //oops, we do still have working engines; abort!
                {
                    goingCritical = false;
                    if (BDArmorySettings.DEBUG_OTHER) Debug.Log("[BDArmory.BDModuleNuke]: engines on " + Sourcevessel + " still have fuel, aborting detonation");
                    StopCoroutine(DelayedDetonation(delay));
                }
                if (BDACompetitionMode.Instance.competitionIsActive)
                {
                    string msg = $"{vessel.GetName()} is out of fuel!";
                    BDACompetitionMode.Instance.competitionStatus.Add(msg);
                }
            }
            yield return new WaitForSecondsFixed(delay - 0.5f);
=======
            if (BDArmorySettings.DEBUG_OTHER) Debug.Log("[BDArmory.RWPS3R2NukeModule]: Nuclear engine on " + Sourcevessel + " going critical in " + delay.ToString("0.0") + "s.");
            goingCritical = true;
            yield return new WaitForSecondsFixed(0.5f);
            if (part.vessel.rootPart == part) //double check to ensure vessel is legitimately out of fuel, and not from poorly timed drop tanks/ions running out of Ec but not Xe, etc
            {
                bool engineOut = true;
                {
                    foreach (var e in VesselModuleRegistry.GetModuleEngines(vessel))
                    {
                        if (e != null && !e.flameout && e.vessel == part.vessel && e.thrustPercentage > 0)
                        {
                            engineOut = false; //we have a functioning engine, hold off on detonation
                            break;
                        }
                    }
                }
                if (!engineOut) //oops, we do still have working engines; abort!
                {
                    goingCritical = false;
                    if (BDArmorySettings.DEBUG_OTHER) Debug.Log("[BDArmory.BDModuleNuke]: engines on " + Sourcevessel + " still have fuel, aborting detonation");
                    yield break;
                }
                if (BDACompetitionMode.Instance.competitionIsActive)
                {
                    string msg = $"{vessel.GetName()} is out of fuel!";
                    BDACompetitionMode.Instance.competitionStatus.Add(msg);
                }
            }
            yield return new WaitForSecondsFixed(delay - 0.5f);
>>>>>>> upstream

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
<<<<<<< lite
            var output = new StringBuilder();
            output.AppendLine("Nuclear Warhead");
            output.AppendLine($"Yield: {yield} kt");
            output.AppendLine($"Thermal radius: {thermalRadius} m");
            output.AppendLine($"Generates EMP: {isEMP}");
||||||| base-1.6.11
            StringBuilder output = new StringBuilder();
            if (engineCore)
            {
                output.Append(Environment.NewLine);
                output.AppendLine($"Reactor Core");
                output.AppendLine($"An unstable reactor core that will detonate if the engine is disabled");
                output.AppendLine($"Yield: {yield}");
                output.AppendLine($"Generates EMP: {isEMP}");
            }
            if (Launcher != null)
            {
                output.AppendLine($"Nuclear Warhead");
                output.AppendLine($"Yield: {yield}");
                output.AppendLine($"Generates EMP: {isEMP}");
            }
=======
            StringBuilder output = new StringBuilder();
            if (engineCore)
            {
                output.Append(Environment.NewLine);
                output.AppendLine($"Reactor Core");
                output.AppendLine($"An unstable reactor core that will detonate if the engine is disabled");
                output.AppendLine($"Yield: {yield}");
                output.AppendLine($"Generates EMP: {isEMP}");
            }
            else
            {
                output.AppendLine($"Nuclear Warhead");
                output.AppendLine($"Yield: {yield}");
                output.AppendLine($"Generates EMP: {isEMP}");
            }
>>>>>>> upstream
            return output.ToString();
        }
    }
}
