using UnityEngine;

using BDArmory.Weapons.Missiles;

namespace BDArmory.Weapons
{
    /// <summary>
    /// Part-level warhead control shared by MissileLauncher, BDModuleNuke and the warheads (BDExplosivePart, BDCustomWarhead).
    ///
    /// - Fired munitions arm themselves after a short safety window (see MissileBase.CheckDetonationState).
    /// - The Arm action puts a contact fuze on a part that is still attached: it detonates when the part
    ///   touches anything that is not part of its own craft (or enters water), and if the part is destroyed.
    /// - The Detonate action detonates immediately, regardless of arming or launch state.
    ///
    /// A part can carry several of these modules (e.g. a missile with a nuke and a conventional charge).
    /// Only the "primary" one shows the Arm/Detonate controls so the action group editor has no duplicates,
    /// but every module routes to the same part-level functions, so old craft bindings still work.
    /// </summary>
    public static class WarheadFuze
    {
        /// <summary>Does this missile launcher carry its own warhead (as opposed to being a multi-missile pod)?</summary>
        public static bool IsWarheadMissile(MissileLauncher ml)
        {
            if (ml == null) return false;
            var mml = ml.multiLauncher != null ? ml.multiLauncher : ml.part.FindModuleImplementing<MultiMissileLauncher>();
            return mml == null || mml.isClusterMissile;
        }

        /// <summary>The module that shows the warhead controls: the missile, else the nuke, else the (first) warhead.</summary>
        public static PartModule PrimaryModule(Part p)
        {
            if (p == null) return null;
            var ml = p.FindModuleImplementing<MissileLauncher>();
            if (ml != null) return IsWarheadMissile(ml) ? ml : null; // Multi-missile pods have no warhead of their own.
            var nuke = p.FindModuleImplementing<BDModuleNuke>();
            if (nuke != null) return nuke;
            return p.FindModuleImplementing<BDWarheadBase>();
        }

        /// <summary>Show the Arm/Detonate action and Detonate button only on the primary module.</summary>
        public static void SetupControls(PartModule module)
        {
            bool primary = PrimaryModule(module.part) == module;
            var arm = module.Actions["ArmAG"]; if (arm != null) arm.active = primary;
            var detonate = module.Actions["DetonateAG"]; if (detonate != null) detonate.active = primary;
            if (module.Events.Contains("DetonateEvent")) module.Events["DetonateEvent"].guiActive = primary;
            UpdateDisarmButton(module.part);
        }

        static bool IsLaunched(Part p)
        {
            var ml = p.FindModuleImplementing<MissileLauncher>();
            return ml != null && ml.HasFired;
        }

        static void SetContactArmedFlag(Part p, bool armed)
        {
            var ml = p.FindModuleImplementing<MissileLauncher>(); if (ml != null) ml.contactArmed = armed;
            var nuke = p.FindModuleImplementing<BDModuleNuke>(); if (nuke != null) nuke.contactArmed = armed;
            foreach (var tnt in p.FindModulesImplementing<BDWarheadBase>()) tnt.contactArmed = armed;
        }

        static bool IsContactArmed(Part p)
        {
            var ml = p.FindModuleImplementing<MissileLauncher>(); if (ml != null && ml.contactArmed) return true;
            var nuke = p.FindModuleImplementing<BDModuleNuke>(); if (nuke != null && nuke.contactArmed) return true;
            foreach (var tnt in p.FindModulesImplementing<BDWarheadBase>()) if (tnt.contactArmed) return true;
            return false;
        }

        static void UpdateDisarmButton(Part p)
        {
            var primary = PrimaryModule(p);
            if (primary == null || !primary.Events.Contains("DisarmEvent")) return;
            primary.Events["DisarmEvent"].guiActive = IsContactArmed(p); // (Not the ContactFuze component: Destroy() is deferred to the end of the frame.)
        }

        /// <summary>Arm the contact fuze on an attached part. Fired munitions are already handled by their own arming.</summary>
        public static void ArmContact(Part p, bool quiet = false)
        {
            if (p == null || !HighLogic.LoadedSceneIsFlight || IsLaunched(p) || PrimaryModule(p) == null) return;
            SetContactArmedFlag(p, true);
            foreach (var tnt in p.FindModulesImplementing<BDWarheadBase>()) tnt.Armed = true;
            if (p.GetComponent<ContactFuze>() == null) p.gameObject.AddComponent<ContactFuze>();
            UpdateDisarmButton(p);
            if (!quiet && p.vessel != null && p.vessel.isActiveVessel)
                ScreenMessages.PostScreenMessage($"{p.partInfo.title}: armed, detonates on contact", 3f, ScreenMessageStyle.UPPER_CENTER);
        }

        /// <summary>Remove the contact fuze (an unfired missile's charge goes back to safe).</summary>
        public static void DisarmContact(Part p)
        {
            if (p == null) return;
            SetContactArmedFlag(p, false);
            var fuze = p.GetComponent<ContactFuze>();
            if (fuze != null) Object.Destroy(fuze);
            var ml = p.FindModuleImplementing<MissileLauncher>();
            if (ml != null && !ml.HasFired)
                foreach (var tnt in p.FindModulesImplementing<BDWarheadBase>()) tnt.Armed = false;
            UpdateDisarmButton(p);
        }

        /// <summary>Called when a missile is fired: its own safety-window arming takes over from any contact fuze.</summary>
        public static void OnLaunched(Part p)
        {
            if (p == null) return;
            SetContactArmedFlag(p, false);
            foreach (var tnt in p.FindModulesImplementing<BDWarheadBase>()) tnt.Armed = false; // Safe until the launch safety window has passed (MissileBase re-arms it).
            var fuze = p.GetComponent<ContactFuze>();
            if (fuze != null) Object.Destroy(fuze);
            UpdateDisarmButton(p);
        }

        /// <summary>Detonate every warhead on the part immediately, regardless of arming or launch state.</summary>
        public static void Detonate(Part p)
        {
            if (p == null || !HighLogic.LoadedSceneIsFlight) return;
            var fuze = p.GetComponent<ContactFuze>();
            if (fuze != null) Object.Destroy(fuze);

            var ml = p.FindModuleImplementing<MissileLauncher>();
            if (IsWarheadMissile(ml))
            {
                ml.Detonate(force: true);
                return;
            }
            var nuke = p.FindModuleImplementing<BDModuleNuke>();
            var warheads = p.FindModulesImplementing<BDWarheadBase>();
            foreach (var tnt in warheads) tnt.Armed = true; // So destroying the part (below) also sets off the charge, as when fired.
            if (nuke != null) nuke.Detonate();
            foreach (var tnt in warheads) if (tnt != null) tnt.ForceDetonate(); // No-op if the nuke's destruction of the part already set it off.
        }
    }

    /// <summary>
    /// Contact fuze for an attached, manually armed warhead. Ignores the craft it is part of.
    /// To avoid false triggers when a saved craft is loaded, it waits until the vessel is off rails and has
    /// settled for a moment, and ignores resting contacts (below 1 m/s).
    /// </summary>
    public class ContactFuze : MonoBehaviour
    {
        const float SettleTime = 0.5f;
        const float MinImpactSpeed = 1f;
        const float SeparatedGrace = 3f; // Parts that were on this craft in the last few seconds (fairings, spent stages) don't count.
        const float MembershipRefresh = 0.25f;

        Part part;
        bool wasInWater;
        bool triggered;
        float unpackedSince = -1;
        float lastMembershipRefresh = -1;
        readonly System.Collections.Generic.Dictionary<uint, float> recentCraftParts = new System.Collections.Generic.Dictionary<uint, float>(); // persistentId -> last time seen on this craft

        void Awake()
        {
            part = GetComponent<Part>();
        }

        bool IgnoresWater() // Worked out when needed: the weapon class isn't parsed yet when the fuze is restored on load.
        {
            var ml = part.FindModuleImplementing<MissileLauncher>();
            return ml != null && (ml.torpedo || ml.GetWeaponClass() == WeaponClasses.SLW); // Torpedoes live in the water.
        }

        void RefreshCraftMembership()
        {
            lastMembershipRefresh = Time.time;
            foreach (var p in part.vessel.parts) if (p != null) recentCraftParts[p.persistentId] = Time.time;
        }

        bool WasRecentlyOnThisCraft(Part other) =>
            recentCraftParts.TryGetValue(other.persistentId, out float lastSeen) && Time.time - lastSeen < SeparatedGrace;

        bool Settled => part != null && part.vessel != null && !part.vessel.packed && unpackedSince >= 0 && Time.time - unpackedSince > SettleTime;

        void OnCollisionEnter(Collision col)
        {
            if (triggered || !Settled || col.collider == null) return;
            if (col.relativeVelocity.sqrMagnitude < MinImpactSpeed * MinImpactSpeed) return; // Resting contact, not an impact.
            var other = col.collider.GetComponentInParent<Part>();
            if (other != null && (other.vessel == part.vessel || WasRecentlyOnThisCraft(other))) return; // Never on contact with its own craft (or bits of it that just separated).
            Trigger();
        }

        void FixedUpdate()
        {
            if (triggered || part == null || part.vessel == null) return;
            if (part.vessel.packed) { unpackedSince = -1; return; }
            if (unpackedSince < 0) unpackedSince = Time.time;
            if (Time.time - lastMembershipRefresh > MembershipRefresh) RefreshCraftMembership();
            bool inWater = part.WaterContact;
            if (Settled && inWater && !wasInWater && !IgnoresWater()) Trigger(); // Entering water counts as a hit; already being in it doesn't.
            wasInWater = inWater;
        }

        void Trigger()
        {
            if (part.State == PartStates.DEAD) return; // KSP is already destroying it; OnJustAboutToBeDestroyed handles the detonation.
            triggered = true;
            WarheadFuze.Detonate(part);
        }
    }
}
