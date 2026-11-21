using System;
using System.IO;
using UnityEngine;

using BDArmory.Settings;

namespace BDArmory.FX
{
    /// <summary>
    /// Volumetric Explosion FX (VEFX) draws its own explosions for BDArmory's blasts (it watches ExplosionFx, NukeFX and FXEmitter).
    /// When it is installed and showing explosions, BDArmory hides its own explosion visuals (meshes, particles and the flash
    /// light) so the two don't draw on top of each other. Sound, damage and the explosion objects themselves are unchanged,
    /// since VEFX relies on them. VEFX's own settings are respected: if its explosions or its "replace stock visuals" option
    /// are off, or the blast is beyond its fxDistance from the camera, BDArmory's visuals are shown as normal.
    /// </summary>
    public static class VolumetricExplosionFXBridge
    {
        static bool? installed;
        static string pluginSettingsPath;
        static DateTime pluginSettingsWriteTime = DateTime.MinValue;
        static float lastCheck = -1;
        static bool vefxEnabled = true, vefxExplosions = true, vefxReplaceStock = true;
        static float vefxFxDistance = 10000f;

        public static bool Installed
        {
            get
            {
                if (installed == null)
                {
                    installed = false;
                    // Volumetric Explosion FX, or Volumetric FX Reconfigured (which includes it; its settings are in PluginData/Explosions).
                    foreach (var assembly in AssemblyLoader.loadedAssemblies)
                    {
                        if (assembly.name == "VolumetricFXReconfigured")
                        {
                            installed = true;
                            pluginSettingsPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "VolumetricFXReconfigured", "PluginData", "Explosions", "settings.cfg");
                            break;
                        }
                        if (assembly.name == "VolumetricExplosionFX")
                        {
                            installed = true;
                            pluginSettingsPath = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "VolumetricExplosionFX", "PluginData", "settings.cfg");
                        }
                    }
                    if (installed == true)
                    {
                        // Shipped defaults (loaded by KSP's GameDatabase), then the player's saved settings (checked again when the file changes).
                        foreach (var node in GameDatabase.Instance.GetConfigNodes("VOLUMETRIC_EXPLOSION_FX")) ReadSettings(node);
                        RefreshPlayerSettings();
                        Debug.Log($"[BDArmory.VolumetricExplosionFXBridge]: Volumetric Explosion FX detected; BDArmory explosion visuals hidden: {VisualsReplaced}");
                    }
                }
                return installed == true;
            }
        }

        /// <summary>VEFX is installed and set to replace explosion visuals (and the player hasn't asked to keep BDArmory's).</summary>
        static bool VisualsReplaced => Installed && BDArmorySettings.VEFX_HIDE_BDA_EXPLOSIONS && vefxEnabled && vefxExplosions && vefxReplaceStock;

        /// <summary>Should BDArmory hide its own visuals for an explosion at this position?</summary>
        /// <param name="nuclear">A nuclear explosion: Volumetric Explosion FX / FX Reconfigured draw those out to 60 km (at least
        /// fxDistance), so BDArmory's own nuke visuals stay hidden that far (both were drawn between 10 and 60 km).</param>
        public static bool HideVisualsAt(Vector3 position, bool nuclear = false)
        {
            if (!Installed) return false;
            if (Time.unscaledTime - lastCheck > 2f) { lastCheck = Time.unscaledTime; RefreshPlayerSettings(); } // VEFX settings can change in game.
            if (!VisualsReplaced) return false;
            var camera = FlightCamera.fetch != null ? FlightCamera.fetch.mainCamera : null;
            float reach = nuclear ? Mathf.Max(vefxFxDistance, 60000f) : vefxFxDistance;
            if (camera != null && vefxFxDistance > 0 && (camera.transform.position - position).sqrMagnitude > reach * reach)
                return false; // VEFX doesn't draw blasts this far from the camera.
            return true;
        }

        /// <summary>Hide the renderers and lights under a (pooled) explosion object, or restore the ones that were hidden.</summary>
        public static void SetVisible(GameObject explosion, bool visible)
        {
            if (explosion == null) return;
            var hidden = explosion.GetComponent<HiddenExplosionVisuals>();
            if (visible)
            {
                if (hidden == null) return;
                foreach (var renderer in hidden.renderers) if (renderer != null) renderer.enabled = true;
                foreach (var light in hidden.lights) if (light != null) light.enabled = true;
                hidden.renderers.Clear();
                hidden.lights.Clear();
                return;
            }
            if (hidden == null) hidden = explosion.AddComponent<HiddenExplosionVisuals>();
            foreach (var renderer in explosion.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && renderer.enabled) { renderer.enabled = false; hidden.renderers.Add(renderer); }
            foreach (var light in explosion.GetComponentsInChildren<Light>(true))
                if (light != null && light.enabled) { light.enabled = false; hidden.lights.Add(light); }
        }

        /// <summary>Remembers which parts of a pooled explosion were hidden, so only those are restored when it is reused.</summary>
        class HiddenExplosionVisuals : MonoBehaviour
        {
            public readonly System.Collections.Generic.List<Renderer> renderers = new System.Collections.Generic.List<Renderer>();
            public readonly System.Collections.Generic.List<Light> lights = new System.Collections.Generic.List<Light>();
        }

        static void RefreshPlayerSettings()
        {
            try
            {
                if (string.IsNullOrEmpty(pluginSettingsPath) || !File.Exists(pluginSettingsPath)) return;
                var writeTime = File.GetLastWriteTimeUtc(pluginSettingsPath);
                if (writeTime == pluginSettingsWriteTime) return;
                pluginSettingsWriteTime = writeTime;
                var root = ConfigNode.Load(pluginSettingsPath);
                if (root == null) return;
                var node = root.GetNode("VOLUMETRIC_EXPLOSION_FX") ?? root;
                ReadSettings(node);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BDArmory.VolumetricExplosionFXBridge]: Couldn't read Volumetric Explosion FX settings: {e.Message}");
            }
        }

        static void ReadSettings(ConfigNode node)
        {
            node.TryGetValue("enabled", ref vefxEnabled);
            node.TryGetValue("explosions", ref vefxExplosions);
            node.TryGetValue("replaceStockVisuals", ref vefxReplaceStock);
            node.TryGetValue("fxDistance", ref vefxFxDistance);
        }
    }
}
