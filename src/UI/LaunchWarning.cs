using UnityEngine;

using BDArmory.Control;
using BDArmory.Settings;

namespace BDArmory.UI
{
    /// <summary>
    /// BDArmory Lite: while the fire key is held to launch a missile or bomb (it has to be held for the
    /// "Trigger hold time" setting), beep with a rising pitch and show a "Hold to launch" bar below the
    /// centre of the screen. Releasing the key early cancels the launch and the warning.
    ///
    /// The beep is generated at startup (no sound file) and uses the BDArmory UI volume.
    /// Turned off with Settings > Gameplay > "Missile launch warning".
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class LaunchWarning : MonoBehaviour
    {
        const float BeepInterval = 0.1f;   // Seconds between beeps (the first one plays as soon as the key goes down).
        const float BeepLength = 0.045f;   // Seconds.
        const float BeepFrequency = 1400f; // Hz, rising to 1.5x by the end of the hold.

        static AudioClip beepClip;
        AudioSource audioSource;
        float lastBeep = float.NegativeInfinity;
        bool wasPending;

        float progress = -1;
        string weaponName = "";
        Texture2D white;
        GUIStyle labelStyle, shadowStyle;
        float stylesScale = -1;

        void Awake()
        {
            if (beepClip == null) beepClip = MakeBeep();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0; // 2D: a cockpit tone, not a sound in the world.
            audioSource.playOnAwake = false;
            audioSource.bypassListenerEffects = true;
            white = Texture2D.whiteTexture;
        }

        static AudioClip MakeBeep()
        {
            const int sampleRate = 44100;
            int samples = Mathf.RoundToInt(BeepLength * sampleRate);
            var data = new float[samples];
            int fade = sampleRate / 400; // 2.5 ms fade in and out so the beep doesn't click.
            for (int i = 0; i < samples; ++i)
            {
                float envelope = Mathf.Min(1f, Mathf.Min(i, samples - 1 - i) / (float)fade);
                data[i] = 0.5f * envelope * Mathf.Sin(2f * Mathf.PI * BeepFrequency * i / sampleRate);
            }
            var clip = AudioClip.Create("BDArmoryLite_LaunchBeep", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Update()
        {
            progress = -1;
            if (!BDArmorySettings.LAUNCH_WARNING || BDArmorySetup.Instance == null) { wasPending = false; return; }
            MissileFire wm = BDArmorySetup.Instance.ActiveWeaponManager;
            if (wm == null || wm.LaunchHoldProgress < 0) { wasPending = false; return; }

            progress = wm.LaunchHoldProgress;
            if (!wasPending) { lastBeep = float.NegativeInfinity; weaponName = wm.selectedWeapon != null ? wm.selectedWeapon.GetShortName() : ""; }
            wasPending = true;

            if (Time.unscaledTime - lastBeep >= BeepInterval && BDArmorySettings.BDARMORY_UI_VOLUME > 0)
            {
                lastBeep = Time.unscaledTime;
                audioSource.pitch = 1f + 0.5f * progress;
                audioSource.PlayOneShot(beepClip, BDArmorySettings.BDARMORY_UI_VOLUME);
            }
        }

        void OnGUI()
        {
            if (progress < 0 || !BDArmorySetup.GAME_UI_ENABLED || Event.current.type != EventType.Repaint) return;
            float scale = BDArmorySettings.UI_SCALE_ACTUAL;
            float width = 180 * scale, barHeight = 6 * scale, labelHeight = 20 * scale;
            float x = (Screen.width - width) / 2f, y = Screen.height * 0.5f + 90 * scale;

            if (labelStyle == null || stylesScale != scale)
            {
                stylesScale = scale;
                labelStyle = new GUIStyle(BDAStyle.Header) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(13 * scale) };
                shadowStyle = new GUIStyle(labelStyle); shadowStyle.normal.textColor = new Color(0, 0, 0, 0.8f);
            }
            string text = string.IsNullOrEmpty(weaponName) ? "Hold to launch" : $"Hold to launch: {weaponName}";
            GUI.Label(new Rect(x + 1, y + 1, width, labelHeight), text, shadowStyle);
            GUI.Label(new Rect(x, y, width, labelHeight), text, labelStyle);

            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(x - 1, y + labelHeight - 1, width + 2, barHeight + 2), white);
            GUI.color = BDAStyle.Title;
            GUI.DrawTexture(new Rect(x, y + labelHeight, width * Mathf.Clamp01(progress), barHeight), white);
            GUI.color = old;
        }
    }
}
