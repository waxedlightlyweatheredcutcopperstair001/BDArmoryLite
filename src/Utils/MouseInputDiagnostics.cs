using UnityEngine;

using BDArmory.Settings;

namespace BDArmory.Utils
{
    /// <summary>
    /// Diagnostics for "the fire button does nothing" reports (enabled by "Log: guns, rockets and lasers").
    ///
    /// Logs every left-mouse press and release as seen by two independent paths:
    ///  - Unity's Input class (what BDArmory's fire key uses), sampled in Update and again in LateUpdate;
    ///  - IMGUI events (OnGUI), which don't go through the Input class.
    /// A click that shows up in OnGUI but never in Input means another mod is resetting input
    /// (e.g. Input.ResetInputAxes()); a click that shows up in neither means the mouse isn't reaching the game.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class MouseInputDiagnostics : MonoBehaviour
    {
        bool updateDown, lateDown;
        int pressesThisSecond;
        float secondStart;

        static bool Enabled => BDArmorySettings.DEBUG_WEAPONS;

        void Update()
        {
            if (!Enabled) return;
            bool down = Input.GetMouseButton(0);
            if (Input.GetMouseButtonDown(0)) Log("Input.GetMouseButtonDown(0) in Update");
            if (down != updateDown) { updateDown = down; Log($"Input.GetMouseButton(0) in Update is now {(down ? "DOWN" : "up")}"); }
        }

        void LateUpdate()
        {
            if (!Enabled) return;
            bool down = Input.GetMouseButton(0);
            if (down != lateDown) { lateDown = down; Log($"Input.GetMouseButton(0) in LateUpdate is now {(down ? "DOWN" : "up")}"); }
        }

        void OnGUI()
        {
            if (!Enabled) return;
            var e = Event.current;
            if (e == null || e.button != 0) return;
            if (e.rawType == EventType.MouseDown || e.rawType == EventType.MouseUp)
                Log($"OnGUI {e.rawType} (button 0) [Input.GetMouseButton(0)={Input.GetMouseButton(0)}]");
        }

        void Log(string what)
        {
            if (Time.unscaledTime - secondStart > 1f) { secondStart = Time.unscaledTime; pressesThisSecond = 0; }
            if (++pressesThisSecond > 20) return; // Don't flood the log.
            var locks = InputLockManager.lockStack.Count == 0 ? "none" : string.Join(", ", InputLockManager.lockStack.Keys);
            Debug.Log($"[BDArmory.MouseInputDiagnostics]: {what}; frame={Time.frameCount}, cursorLock={Cursor.lockState}, cursorVisible={Cursor.visible}, " +
                $"guiHotControl={GUIUtility.hotControl}, mouse=({Input.mousePosition.x:F0},{Input.mousePosition.y:F0}), appFocused={Application.isFocused}, controlLocks=[{locks}]");
        }
    }
}
