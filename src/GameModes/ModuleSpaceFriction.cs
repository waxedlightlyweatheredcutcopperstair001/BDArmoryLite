using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using BDArmory.Utils;

namespace BDArmory.GameModes
{
    /// <summary>
    /// BDArmory Lite: only the repulsor part of BDArmory's ModuleSpaceFriction (used by BDArmory Extended's
    /// repulsor, "bdRepulsorGear", with RepulsorOverride = true). The space-friction and counter-gravity game
    /// modes are not included, and the module is never added to craft automatically.
    ///
    /// The class keeps BDArmory's name and fields so part configs and saved craft load unchanged.
    /// Unlike upstream (which reads the hover height from an AI pilot and fails without one), the hover
    /// height is the part's own "Adjust Altitude" slider.
    /// </summary>
    public class ModuleSpaceFriction : BDAPartModule
    {
        const float RepulsorStrength = 5f; // BDArmory's default SF_REPULSOR_STRENGTH.

        public bool repulsorActivated = false;

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "#LOC_BDArmory_Settings_Repulsor", active = true)]
        public void ToggleRepulsor()
        {
            repulsorActivated = !repulsorActivated;
            isLanded = part.vessel.LandedOrSplashed;
            targetAlt = 0.1f;
        }
        bool isLanded = true;

        [KSPField(isPersistant = true)]
        public bool AntiGravOverride = false; // Kept for config compatibility (counter-gravity isn't included).
        [KSPField(isPersistant = true)]
        public bool RepulsorOverride = false;

        [KSPField(isPersistant = true)]
        public float maxRepulsorMass = 10; //levitate up to 10t per repulsor

        [KSPField(isPersistant = true)]
        public float resourcePerSec = -1;

        [KSPField(isPersistant = true)]
        public string resourceName = "ElectricCharge";
        private int resourceID;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_BDArmory_VesselMover_Help_AdjustAltitude"),
            UI_FloatRange(minValue = 1f, maxValue = 100, stepIncrement = 1f, scene = UI_Scene.All)]
        public float repulsorAlt = 10;

        float targetAlt = 0.1f;

        [KSPAction("#autoLOC_6001380")]
        public void AGToggleRepulsor(KSPActionParam param)
        {
            ToggleRepulsor();
        }

        List<ModuleSpaceFriction> spaceFrictionModules;

        static bool GameIsPaused => PauseMenu.isOpen || Time.timeScale == 0;

        ModuleEngines foundEngine
        {
            get
            {
                if (_foundEngine == null || _foundEngine.vessel != vessel)
                    _foundEngine = VesselModuleRegistry.GetModuleEngines(vessel).FirstOrDefault();
                return _foundEngine;
            }
        }
        ModuleEngines _foundEngine;

        void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (!RepulsorOverride)
            {
                // Added by an old craft or patch for BDArmory's space-hacks game mode, which BDArmory Lite doesn't have.
                enabled = false;
                Events[nameof(ToggleRepulsor)].guiActive = false;
                Fields[nameof(repulsorAlt)].guiActive = false;
                return;
            }
            resourceID = PartResourceLibrary.Instance.GetDefinition(resourceName).id;
        }

        public void FixedUpdate()
        {
            if (!RepulsorOverride || !repulsorActivated || !HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready || vessel == null || vessel.packed || GameIsPaused) return;
            if (vessel.situation == Vessel.Situations.ORBITING || vessel.situation == Vessel.Situations.DOCKED || vessel.situation == Vessel.Situations.ESCAPING || vessel.situation == Vessel.Situations.PRELAUNCH) return;
            if (foundEngine == null) return; // As in BDArmory: repulsors only work on craft with an engine.

            if (isLanded)
            {
                targetAlt = Mathf.Lerp(targetAlt, repulsorAlt, 0.02f / 4);
                if (targetAlt >= repulsorAlt) isLanded = false;
            }
            else
                targetAlt = repulsorAlt;
            float pointAltitude = BodyUtils.GetRadarAltitudeAtPos(part.transform.position);
            if (pointAltitude <= 0 || pointAltitude > 2f * targetAlt) return;
            if (!DrainResource()) return;

            spaceFrictionModules = VesselModuleRegistry.GetModules<ModuleSpaceFriction>(vessel); // Cached by the registry; picks up repulsors lost to damage.
            int repulsorCount = Mathf.Max(1, spaceFrictionModules.Count(m => m != null && m.RepulsorOverride));
            Vector3d grav = FlightGlobals.getGeeForceAtPosition(vessel.CoM);
            var vesselMass = vessel.GetTotalMass();
            var factor = Mathf.Clamp(Mathf.Exp(RepulsorStrength * (targetAlt - pointAltitude) / targetAlt - (float)vessel.verticalSpeed / targetAlt), 0f, 5f * RepulsorStrength); // Decaying exponential balanced at the target altitude with velocity damping.
            float repulsorForce = Mathf.Min((float)vesselMass * factor / repulsorCount, maxRepulsorMass); // Spread the force between the repulsors.
            if (float.IsNaN(factor) || float.IsInfinity(factor)) return; // Only if targetAlt is 0, which shouldn't happen.
            part.Rigidbody.AddForce(-grav * repulsorForce, ForceMode.Force);
        }

        bool DrainResource()
        {
            if (resourcePerSec <= 0) return true;
            double drainAmount = resourcePerSec * TimeWarp.fixedDeltaTime;
            double chargeAvailable = part.RequestResource(resourceID, drainAmount, ResourceFlowMode.ALL_VESSEL);
            return chargeAvailable >= drainAmount * 0.95f;
        }
    }
}
