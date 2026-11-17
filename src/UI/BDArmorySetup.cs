using System.Runtime.CompilerServices;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KSP.Localization;
using KSP.UI.Screens;

using BDArmory.Armor;
using BDArmory.Bullets;
using BDArmory.Competition;
using BDArmory.Control;
using BDArmory.CounterMeasure;
using BDArmory.Extensions;
using BDArmory.FX;
using BDArmory.GameModes;
using BDArmory.ModIntegration;
using BDArmory.Modules;
using BDArmory.Radar;
using BDArmory.Settings;
using BDArmory.Targeting;
using BDArmory.Utils;
using BDArmory.Weapons;

namespace BDArmory.UI
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class BDArmorySetup : MonoBehaviour
    {
        public static bool SMART_GUARDS = true;
        public static bool showTargets = true;

        //=======Window position settings
        [BDAWindowSettingsField] public static Rect WindowRectToolbar;
        [BDAWindowSettingsField] public static Rect WindowRectGps;
        [BDAWindowSettingsField] public static Rect WindowRectSettings;
        [BDAWindowSettingsField] public static Rect WindowRectRadar;
        [BDAWindowSettingsField] public static Rect WindowRectRwr;
        [BDAWindowSettingsField] public static Rect WindowRectTargetingCam;

        //reflection field lists
        static FieldInfo[] iFs;

        static FieldInfo[] inputFields
        {
            get
            {
                if (iFs == null)
                {
                    iFs = typeof(BDInputSettingsFields).GetFields();
                }
                return iFs;
            }
        }

        //dependency checks
        bool ModuleManagerLoaded = false;

        //EVENTS
        public delegate void VolumeChange();

        public static event VolumeChange OnVolumeChange;

        public delegate void SavedSettings();

        public static event SavedSettings OnSavedSettings;

        public delegate void PeaceEnabled();

        public static event PeaceEnabled OnPeaceEnabled;

        //particle optimization
        public static int numberOfParticleEmitters = 0;
        public static BDArmorySetup Instance;
        public static bool GAME_UI_ENABLED = true;
        public static string Version { get; private set; } = "Unknown";

        //toolbar button
        public static bool toolbarButtonAdded = false;

        //settings gui
        public static bool windowSettingsEnabled;
        public string fireKeyGui;

        //editor alignment
        public static bool showWeaponAlignment;
        public static bool showCASESimulation;

        //check for Apple Silicon
        public static bool AppleSilicon = false;

        // Gui Skin
        public static GUISkin BDGuiSkin => BDAStyle.Skin; // ZTheme-style dark skin, see BDAStyle.
        public static GUIStyle ButtonStyle;
        public static GUIStyle SelectedButtonStyle;
        public static GUIStyle CloseButtonStyle;

        //toolbar gui
        public static bool hasAddedButton = false;
        public static bool windowBDAToolBarEnabled;
        float toolWindowWidth = 400;
        float toolWindowHeight = 100;
        float columnWidth = 400;
        bool showWeaponList;
        bool showGuardMenu;
        bool showModules;
        bool showPriorities;
        bool showTargetOptions;
        bool showEngageList;
        int numberOfModules;
        bool showWindowGPS;
        bool infoLinkEnabled;
        bool NumFieldsEnabled;
        int numberOfButtons = 6; // 6 without evolution, will adjust automatically.
        private Vector2 scrollInfoVector;
        public Dictionary<string, NumericInputField> textNumFields;

        //gps window
        public bool showingWindowGPS
        {
            get { return showWindowGPS; }
        }

        bool saveWindowPosition = false;
        float gpsEntryCount;
        float gpsEntryHeight = 24;
        float gpsBorder = 5;
        bool editingGPSName;
        int editingGPSNameIndex;
        bool hasEnteredGPSName;
        string newGPSName = string.Empty;

        public MissileFire ActiveWeaponManager;
        public bool missileWarning;
        public float missileWarningTime = 0;

        public static List<CMFlare> Flares = new List<CMFlare>();
        public static List<CMDecoy> Decoys = new List<CMDecoy>();

        List<string> dependencyWarnings = new List<string>();
        double dependencyLastCheckTime = 0;

        //gui styles
        GUIStyle settingsTitleStyle;
        GUIStyle centerLabel;
        GUIStyle centerLabelRed;
        GUIStyle centerLabelOrange;
        GUIStyle centerLabelBlue;
        GUIStyle leftLabel;
        GUIStyle leftLabelBold;
        GUIStyle infoLinkStyle;
        GUIStyle leftLabelRed;
        GUIStyle rightLabelRed;
        GUIStyle leftLabelGray;
        GUIStyle rippleSliderStyle;
        GUIStyle rippleThumbStyle;
        GUIStyle kspTitleLabel;
        GUIStyle middleLeftLabel;
        GUIStyle middleLeftLabelOrange;
        GUIStyle targetModeStyle;
        GUIStyle targetModeStyleSelected;
        GUIStyle waterMarkStyle;
        GUIStyle redErrorStyle;
        GUIStyle redErrorShadowStyle;
        GUIStyle textFieldStyle;
        bool stylesConfigured = false;

        public SortedList<string, BDTeam> Teams = new SortedList<string, BDTeam>
        {
            { "Neutral", new BDTeam("Neutral", neutral: true) }
        };

        #region Textures

        public static string textureDir = "BDArmory/Textures/";

        bool drawCursor;
        Texture2D cursorTexture = GameDatabase.Instance.GetTexture(textureDir + "aimer", false);

        private Texture2D dti;

        public Texture2D directionTriangleIcon
        {
            get { return dti ? dti : dti = GameDatabase.Instance.GetTexture(textureDir + "directionIcon", false); }
        }

        private Texture2D cgs;

        public Texture2D crossedGreenSquare
        {
            get { return cgs ? cgs : cgs = GameDatabase.Instance.GetTexture(textureDir + "crossedGreenSquare", false); }
        }

        private Texture2D dlgs;

        public Texture2D dottedLargeGreenCircle
        {
            get
            {
                return dlgs
                    ? dlgs
                    : dlgs = GameDatabase.Instance.GetTexture(textureDir + "dottedLargeGreenCircle", false);
            }
        }

        private Texture2D ogs;

        public Texture2D openGreenSquare
        {
            get { return ogs ? ogs : ogs = GameDatabase.Instance.GetTexture(textureDir + "openGreenSquare", false); }
        }

        private Texture2D gdott;

        public Texture2D greenDotTexture
        {
            get { return gdott ? gdott : gdott = GameDatabase.Instance.GetTexture(textureDir + "greenDot", false); }
        }

        private Texture2D rdott;

        public Texture2D redDotTexture
        {
            get { return rdott ? rdott : rdott = GameDatabase.Instance.GetTexture(textureDir + "redDot", false); }
        }

        private Texture2D rspike;

        public Texture2D irSpikeTexture
        {
            get { return rspike ? rspike : rspike = GameDatabase.Instance.GetTexture(textureDir + "IRspike", false); }
        }
        private Texture2D gdt;

        public Texture2D greenDiamondTexture
        {
            get { return gdt ? gdt : gdt = GameDatabase.Instance.GetTexture(textureDir + "greenDiamond", false); }
        }

        private Texture2D lgct;

        public Texture2D largeGreenCircleTexture
        {
            get { return lgct ? lgct : lgct = GameDatabase.Instance.GetTexture(textureDir + "greenCircle3", false); }
        }

        private Texture2D gct;

        public Texture2D greenCircleTexture
        {
            get { return gct ? gct : gct = GameDatabase.Instance.GetTexture(textureDir + "greenCircle2", false); }
        }

        private Texture2D gpct;

        public Texture2D greenPointCircleTexture
        {
            get
            {
                if (gpct == null)
                {
                    gpct = GameDatabase.Instance.GetTexture(textureDir + "greenPointCircle", false);
                }
                return gpct;
            }
        }

        private Texture2D gspct;

        public Texture2D greenSpikedPointCircleTexture
        {
            get
            {
                return gspct ? gspct : gspct = GameDatabase.Instance.GetTexture(textureDir + "greenSpikedCircle", false);
            }
        }

        private Texture2D wSqr;

        public Texture2D whiteSquareTexture
        {
            get { return wSqr ? wSqr : wSqr = GameDatabase.Instance.GetTexture(textureDir + "whiteSquare", false); }
        }

        private Texture2D oWSqr;

        public Texture2D openWhiteSquareTexture
        {
            get
            {
                return oWSqr ? oWSqr : oWSqr = GameDatabase.Instance.GetTexture(textureDir + "openWhiteSquare", false);
                ;
            }
        }

        private Texture2D tDir;

        public Texture2D targetDirectionTexture
        {
            get
            {
                return tDir
                    ? tDir
                    : tDir = GameDatabase.Instance.GetTexture(textureDir + "targetDirectionIndicator", false);
            }
        }

        private Texture2D hInd;

        public Texture2D horizonIndicatorTexture
        {
            get
            {
                return hInd ? hInd : hInd = GameDatabase.Instance.GetTexture(textureDir + "horizonIndicator", false);
            }
        }

        private Texture2D si;

        public Texture2D settingsIconTexture
        {
            get { return si ? si : si = GameDatabase.Instance.GetTexture(textureDir + "settingsIcon", false); }
        }


        private Texture2D FAimg;

        public Texture2D FiringAngleImage
        {
            get { return FAimg ? FAimg : FAimg = GameDatabase.Instance.GetTexture(textureDir + "FiringAnglePic", false); }
        }

        #endregion Textures

        public static bool GameIsPaused
        {
            get { return HighLogic.LoadedSceneIsFlight && (PauseMenu.isOpen || Time.timeScale == 0); }
        }

        void Awake()
        {
            if (Instance != null) Destroy(Instance);
            Instance = this;
            if (!(HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor))
            {
                windowSettingsEnabled = false; // Close the settings on other scenes (it's been saved when the other scene was destroyed).
            }

            // Create settings file if not present or migrate the old one to the PluginsData folder for compatibility with ModuleManager.
            var fileNode = ConfigNode.Load(BDArmorySettings.settingsConfigURL);
            if (fileNode == null)
            {
                fileNode = ConfigNode.Load(BDArmorySettings.oldSettingsConfigURL); // Try the old location.
                if (fileNode == null)
                {
                    fileNode = new ConfigNode();
                    fileNode.AddNode("BDASettings");
                }
                if (!Directory.GetParent(BDArmorySettings.settingsConfigURL).Exists)
                { Directory.GetParent(BDArmorySettings.settingsConfigURL).Create(); }
                var success = fileNode.Save(BDArmorySettings.settingsConfigURL);
                if (success && File.Exists(BDArmorySettings.oldSettingsConfigURL)) // Remove the old settings if it exists and the new settings were saved.
                { File.Delete(BDArmorySettings.oldSettingsConfigURL); }
            }

            // window position settings
            WindowRectToolbar = new Rect(Screen.width - toolWindowWidth - 40, 150, toolWindowWidth, toolWindowHeight); // Default, if not in file.
            WindowRectGps = new Rect(0, 0, WindowRectToolbar.width - 10, 0);
            SetupSettingsSize();
            BDAWindowSettingsField.Load();
            CheckIfWindowsSettingsAreWithinScreen();
            toolWindowHeight = WindowRectToolbar.height;

            // Configure UI visibility and window rects
            GAME_UI_ENABLED = true;

            WindowRectGps.width = WindowRectToolbar.width - 10;

            // Get the BDA version. We can do this here since it's this assembly we're interested in, other assemblies have to wait until Start.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().ToList())
                if (assembly.FullName.Split(new char[1] { ',' })[0] == "BDArmory")
                {
                    var info = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute));
                    Version = "Lite " + (info != null ? info.InformationalVersion : assembly.GetName().Version.ToString());
                }

            // Load settings
            LoadConfig();

            // Check for Apple Processor
            AppleSilicon = CultureInfo.InvariantCulture.CompareInfo.IndexOf(SystemInfo.processorType, "Apple", CompareOptions.IgnoreCase) >= 0;
        }

        void Start()
        {
            //wmgr toolbar
            if (HighLogic.LoadedSceneIsFlight)
            {
                saveWindowPosition = true;     //otherwise later we should NOT save the current window positions!
            }

            fireKeyGui = BDInputSettingsFields.WEAP_FIRE_KEY.inputString;

            //setup gui styles
            CloseButtonStyle = new GUIStyle(BDGuiSkin.button) { alignment = TextAnchor.MiddleCenter }; // Configure this one separately since it's static.
            CloseButtonStyle.hover.textColor = Color.red;

            ButtonStyle = new GUIStyle(BDArmorySetup.BDGuiSkin.button);
            SelectedButtonStyle = new GUIStyle(BDArmorySetup.BDGuiSkin.button);
            var tmp = SelectedButtonStyle.normal;
            SelectedButtonStyle.normal = SelectedButtonStyle.active;
            SelectedButtonStyle.active = tmp;
            SelectedButtonStyle.hover = SelectedButtonStyle.normal;
            //

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetName().Name == "ModuleManager") { ModuleManagerLoaded = true; break; }

            if (HighLogic.LoadedSceneIsFlight)
            {
                SaveVolumeSettings();

                GameEvents.onHideUI.Add(HideGameUI);
                GameEvents.onShowUI.Add(ShowGameUI);
                GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
                GameEvents.OnGameSettingsApplied.Add(SaveVolumeSettings);
                GameEvents.onVesselChange.Add(VesselChange);
            }
            GameEvents.onGameSceneSwitchRequested.Add(OnGameSceneSwitchRequested);

            BulletInfo.Load();
            RocketInfo.Load();
            ArmorInfo.Load();
            HullInfo.Load();
            ProjectileUtils.SetUpPartsHashSets();
            ProjectileUtils.SetUpWeaponReporting();

            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            { StartCoroutine(ToolbarButtonRoutine()); }
        }

        void ConfigureStyles()
        {
            centerLabel = new GUIStyle();
            centerLabel.alignment = TextAnchor.UpperCenter;
            centerLabel.normal.textColor = Color.white;

            settingsTitleStyle = new GUIStyle(centerLabel);
            settingsTitleStyle.alignment = TextAnchor.MiddleCenter;
            settingsTitleStyle.fontSize = 16;
            settingsTitleStyle.fontStyle = FontStyle.Bold;

            centerLabelRed = new GUIStyle();
            centerLabelRed.alignment = TextAnchor.UpperCenter;
            centerLabelRed.normal.textColor = Color.red;

            centerLabelOrange = new GUIStyle();
            centerLabelOrange.alignment = TextAnchor.UpperCenter;
            centerLabelOrange.normal.textColor = XKCDColors.BloodOrange;

            centerLabelBlue = new GUIStyle();
            centerLabelBlue.alignment = TextAnchor.UpperCenter;
            centerLabelBlue.normal.textColor = XKCDColors.AquaBlue;

            leftLabel = new GUIStyle();
            leftLabel.alignment = TextAnchor.UpperLeft;
            leftLabel.normal.textColor = Color.white;

            leftLabelBold = new GUIStyle();
            leftLabelBold.alignment = TextAnchor.UpperLeft;
            leftLabelBold.normal.textColor = Color.white;
            leftLabelBold.fontStyle = FontStyle.Bold;

            infoLinkStyle = new GUIStyle(BDArmorySetup.BDGuiSkin.label);
            infoLinkStyle.alignment = TextAnchor.UpperLeft;
            infoLinkStyle.normal.textColor = Color.white;

            middleLeftLabel = new GUIStyle(leftLabel);
            middleLeftLabel.alignment = TextAnchor.MiddleLeft;

            middleLeftLabelOrange = new GUIStyle(middleLeftLabel);
            middleLeftLabelOrange.normal.textColor = XKCDColors.BloodOrange;

            targetModeStyle = new GUIStyle();
            targetModeStyle.alignment = TextAnchor.MiddleRight;
            targetModeStyle.fontSize = 9;
            targetModeStyle.normal.textColor = Color.white;

            targetModeStyleSelected = new GUIStyle(targetModeStyle);
            targetModeStyleSelected.normal.textColor = XKCDColors.BloodOrange;

            waterMarkStyle = new GUIStyle(middleLeftLabel);
            waterMarkStyle.normal.textColor = XKCDColors.LightBlueGrey;

            leftLabelRed = new GUIStyle();
            leftLabelRed.alignment = TextAnchor.UpperLeft;
            leftLabelRed.normal.textColor = Color.red;

            rightLabelRed = new GUIStyle();
            rightLabelRed.alignment = TextAnchor.UpperRight;
            rightLabelRed.normal.textColor = Color.red;

            leftLabelGray = new GUIStyle();
            leftLabelGray.alignment = TextAnchor.UpperLeft;
            leftLabelGray.normal.textColor = Color.gray;

            rippleSliderStyle = new GUIStyle(BDGuiSkin.horizontalSlider);
            rippleThumbStyle = new GUIStyle(BDGuiSkin.horizontalSliderThumb);
            rippleSliderStyle.fixedHeight = rippleThumbStyle.fixedHeight = 0;

            kspTitleLabel = new GUIStyle();
            kspTitleLabel.normal.textColor = BDGuiSkin.window.normal.textColor;
            kspTitleLabel.font = BDGuiSkin.window.font;
            kspTitleLabel.fontSize = BDGuiSkin.window.fontSize;
            kspTitleLabel.fontStyle = BDGuiSkin.window.fontStyle;
            kspTitleLabel.alignment = TextAnchor.UpperCenter;

            redErrorStyle = new GUIStyle(BDGuiSkin.label);
            redErrorStyle.normal.textColor = Color.red;
            redErrorStyle.fontStyle = FontStyle.Bold;
            redErrorStyle.fontSize = 24;
            redErrorStyle.alignment = TextAnchor.UpperCenter;

            redErrorShadowStyle = new GUIStyle(redErrorStyle);
            redErrorShadowStyle.normal.textColor = new Color(0, 0, 0, 0.75f);

            textFieldStyle = new GUIStyle(GUI.skin.textField);
            textFieldStyle.alignment = TextAnchor.MiddleRight;

            stylesConfigured = true;
        }

        /// <summary>
        /// Modify the background opacity of a window.
        /// 
        /// GUI.Window stores the color values it was called with, so call this with enable=true before GUI.Window to enable
        /// transparency for that window and again with enable=false afterwards to avoid affect later GUI.Window calls.
        ///
        /// Note: This can only lower the opacity of the window background, so windows with a background texture that
        /// already includes some transparency can only be made more transparent, not less.
        /// </summary>
        /// <param name="enable">Enable or reset the modified background opacity.</param>
        public static void SetGUIOpacity(bool enable = true)
        {
            if (!enable && BDArmorySettings.GUI_OPACITY == 1f) return; // Nothing to do.
            var guiColor = GUI.backgroundColor;
            if (guiColor.a != (enable ? BDArmorySettings.GUI_OPACITY : 1f))
            {
                guiColor.a = (enable ? BDArmorySettings.GUI_OPACITY : 1f);
                GUI.backgroundColor = guiColor;
            }
        }

        IEnumerator ToolbarButtonRoutine()
        {
            if (toolbarButtonAdded) yield break;
            yield return new WaitUntil(() => ApplicationLauncher.Ready);
            if (toolbarButtonAdded) yield break;
            Texture buttonTexture = GameDatabase.Instance.GetTexture(BDArmorySetup.textureDir + "icon", false);
            ApplicationLauncher.Instance.AddModApplication(
                ToggleToolbarButton,
                ToggleToolbarButton,
                () => { },
                () => { },
                () => { },
                () => { },
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                buttonTexture
            );
            toolbarButtonAdded = true;
        }
        /// <summary>
        /// Toggle the BDAToolbar or BDA settings window depending on the scene.
        /// </summary>
        void ToggleToolbarButton()
        {
            if (HighLogic.LoadedSceneIsFlight) { windowBDAToolBarEnabled = !windowBDAToolBarEnabled; }
            else { windowSettingsEnabled = !windowSettingsEnabled; }
        }

        private void CheckIfWindowsSettingsAreWithinScreen()
        {
            GUIUtils.UseMouseEventInRect(WindowRectSettings);
            GUIUtils.RepositionWindow(ref WindowRectToolbar);
            GUIUtils.RepositionWindow(ref WindowRectSettings);
            GUIUtils.RepositionWindow(ref WindowRectRwr);
            GUIUtils.RepositionWindow(ref WindowRectTargetingCam);
        }

        void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (missileWarning && Time.time - missileWarningTime > 1.5f) missileWarning = false;
                if (BDInputUtils.GetKeyDown(BDInputSettingsFields.GUI_WM_TOGGLE)) windowBDAToolBarEnabled = !windowBDAToolBarEnabled;
#if DEBUG
                if (BDInputUtils.GetKeyDown(BDInputSettingsFields.DEBUG_CLEAR_DEV_CONSOLE)) Debug.ClearDeveloperConsole();
#endif
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    showWeaponAlignment = !showWeaponAlignment;
                }
                if (Input.GetKeyDown(KeyCode.F3))
                {
                    showCASESimulation = !showCASESimulation;
                }
            }

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                if (Input.GetKeyDown(KeyCode.B))
                {
                    ToggleWindowSettings();
                }
            }
        }

        void ToggleWindowSettings()
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING || HighLogic.LoadedScene == GameScenes.LOADINGBUFFER)
            {
                return;
            }

            windowSettingsEnabled = !windowSettingsEnabled;
            if (windowSettingsEnabled)
            {
                // LoadConfig(); // Don't reload settings, since they're already loaded and mess with other settings windows.
            }
            else
            {
                SaveConfig();
            }
        }

        public void UpdateCursorState()
        {
            if (ActiveWeaponManager == null)
            {
                drawCursor = false;
                //Screen.showCursor = true;
                Cursor.visible = true;
                return;
            }

            if (!GAME_UI_ENABLED || CameraMouseLook.MouseLocked)
            {
                drawCursor = false;
                Cursor.visible = false;
                return;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                drawCursor = false;
                if (!MapView.MapIsEnabled && !GUIUtils.CheckMouseIsOnGui() && !PauseMenu.isOpen)
                {
                    if (ActiveWeaponManager.selectedWeapon != null && ActiveWeaponManager.weaponIndex > 0 &&
                        !ActiveWeaponManager.guardMode)
                    {
                        if (ActiveWeaponManager.selectedWeapon.GetWeaponClass() == WeaponClasses.Gun ||
                            ActiveWeaponManager.selectedWeapon.GetWeaponClass() == WeaponClasses.Rocket ||
                            ActiveWeaponManager.selectedWeapon.GetWeaponClass() == WeaponClasses.DefenseLaser)
                        {
                            ModuleWeapon mw = ActiveWeaponManager.selectedWeapon.GetWeaponModule();
                            if (mw != null && mw.weaponState == ModuleWeapon.WeaponStates.Enabled && mw.maxPitch > 1 && !mw.slaved && !mw.GPSTarget && !mw.aiControlled)
                            {
                                //Screen.showCursor = false;
                                Cursor.visible = false;
                                drawCursor = true;
                                return;
                            }
                        }
                    }

                    if (MouseAimFlight.IsMouseAimActive)
                    {
                        Cursor.visible = false;
                        drawCursor = false;
                        return;
                    }
                }
            }

            //Screen.showCursor = true;
            Cursor.visible = true;
        }

        void VesselChange(Vessel v)
        {
            if (v != null && v.isActiveVessel)
            {
                GetWeaponManager();
                Instance.UpdateCursorState();
            }
        }

        void GetWeaponManager()
        {
            ActiveWeaponManager = VesselModuleRegistry.GetMissileFire(FlightGlobals.ActiveVessel, true);
            if (ActiveWeaponManager != null)
            { ConfigTextFields(); }
        }
        public void ConfigTextFields()
        {
            textNumFields = new Dictionary<string, NumericInputField> {
                { "rippleRPM", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.rippleRPM, 0, 1600) },
                { "targetScanInterval", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetScanInterval, 0.5f, 60f) },
                { "fireBurstLength", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.fireBurstLength, 0, 10) },
                { "AutoFireCosAngleAdjustment", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.AutoFireCosAngleAdjustment, 0, 4) },
                { "guardAngle", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.guardAngle, 10, 360) },
                { "guardRange", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.guardRange, 100, BDArmorySettings.MAX_GUARD_VISUAL_RANGE) },
                { "gunRange", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.gunRange, 0, ActiveWeaponManager.maxGunRange) },
                { "multiTargetNum", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.multiTargetNum, 1, 10) },
                { "multiMissileTgtNum", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.multiMissileTgtNum, 1, 10) },
                { "maxMissilesOnTarget", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.maxMissilesOnTarget, 1, MissileFire.maxAllowableMissilesOnTarget) },

                { "targetBias", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetBias, -10, 10) },
                { "targetWeightRange", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightRange, -10, 10) },
                { "targetWeightAirPreference", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightAirPreference, -10, 10) },
                { "targetWeightATA", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightATA, -10, 10) },
                { "targetWeightAoD", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightAoD, -10, 10) },
                { "targetWeightAccel", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightAccel,-10, 10) },
                { "targetWeightClosureTime", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightClosureTime, -10, 10) },
                { "targetWeightWeaponNumber", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightWeaponNumber, -10, 10) },
                { "targetWeightMass", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightMass,-10, 10) },
                { "targetWeightDamage", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightDamage,-10, 10) },
                { "targetWeightFriendliesEngaging", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightFriendliesEngaging, -10, 10) },
                { "targetWeightThreat", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightThreat, -10, 10) },
                { "targetWeightProtectTeammate", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightProtectTeammate, -10, 10) },
                { "targetWeightProtectVIP", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightProtectVIP, -10, 10) },
                { "targetWeightAttackVIP", gameObject.AddComponent<NumericInputField>().Initialise(0, ActiveWeaponManager.targetWeightAttackVIP, -10, 10) },
            };
        }

        static bool firstLoad = true;
        public static void LoadConfig()
        {
            try
            {
                Debug.Log("[BDArmory.BDArmorySetup]=== Loading settings.cfg ===");

                if (firstLoad)
                {
                    BDAPersistentSettingsField.Upgrade();
                    firstLoad = false;
                }
                BDAPersistentSettingsField.Load();
                BDInputSettingsFields.LoadSettings();
                SanitiseSettings();
                BDArmorySettings.ready = true;
            }
            catch (NullReferenceException e)
            {
                Debug.LogError("[BDArmory.BDArmorySetup]=== Failed to load settings config ===: " + e.Message + "\n" + e.StackTrace);
            }
        }

        public static void SaveConfig()
        {
            try
            {
                Debug.Log("[BDArmory.BDArmorySetup] == Saving settings.cfg ==	");

                BDAPersistentSettingsField.Save(BDArmorySettings.settingsConfigURL);

                BDInputSettingsFields.SaveSettings();

                if (OnSavedSettings != null)
                {
                    OnSavedSettings();
                }
            }
            catch (NullReferenceException e)
            {
                Debug.LogError("[BDArmory.BDArmorySetup]: === Failed to save settings.cfg ====: " + e.Message + "\n" + e.StackTrace);
            }
        }

        static void SanitiseSettings()
        {
            BDArmorySettings.PROC_ARMOR_ALT_LIMITS.y = Mathf.Min(BDArmorySettings.PROC_ARMOR_ALT_LIMITS.y, 1e5f); // Anything over this pretty much breaks KSP.
            BDArmorySettings.PROC_ARMOR_ALT_LIMITS.x = Mathf.Clamp(BDArmorySettings.PROC_ARMOR_ALT_LIMITS.x, BDArmorySettings.PROC_ARMOR_ALT_LIMITS.y * 1e-8f, BDArmorySettings.PROC_ARMOR_ALT_LIMITS.y); // More than 8 orders of magnitude breaks the mesh collider engine.
            BDArmorySettings.PREVIOUS_UI_SCALE = BDArmorySettings.UI_SCALE;
        }
        #region GUI

        void OnGUI()
        {
            if (!GAME_UI_ENABLED) return;
            GUI.skin = BDGuiSkin;
            if (!stylesConfigured) ConfigureStyles();
            if (windowSettingsEnabled)
            {
                var guiMatrix = GUI.matrix; // Store and restore the GUI.matrix so we can apply a different scaling for the WM window.
                if (scalingUI && Mouse.Left.GetButtonUp()) scalingUI = false; // Don't rescale the settings window until the mouse is released otherwise it messes with the slider.
                if (!scalingUI) { oldUIScale = BDArmorySettings.UI_SCALE; BDArmorySettings.PREVIOUS_UI_SCALE = BDArmorySettings.UI_SCALE; }
                if (oldUIScale != 1) GUIUtility.ScaleAroundPivot(oldUIScale * Vector2.one, WindowRectSettings.position);
                SetGUIOpacity();
                WindowRectSettings = GUI.Window(129419, WindowRectSettings, WindowSettings, GUIContent.none, BDGuiSkin.window);
                SetGUIOpacity(false);
                GUI.matrix = guiMatrix;
            }

            if (drawCursor)
            {
                //mouse cursor
                int origDepth = GUI.depth;
                GUI.depth = -100;
                float cursorSize = 40;
                Vector3 cursorPos = Input.mousePosition;
                Rect cursorRect = new Rect(cursorPos.x - (cursorSize / 2), Screen.height - cursorPos.y - (cursorSize / 2), cursorSize, cursorSize);
                GUI.DrawTexture(cursorRect, cursorTexture);
                GUI.depth = origDepth;
            }

            if (!windowBDAToolBarEnabled || !HighLogic.LoadedSceneIsFlight) return;
            SetGUIOpacity();
            if (BDArmorySettings.UI_SCALE != 1) GUIUtility.ScaleAroundPivot(BDArmorySettings.UI_SCALE * Vector2.one, WindowRectToolbar.position);
            WindowRectToolbar = GUI.Window(321, WindowRectToolbar, WindowBDAToolbar, "", BDGuiSkin.window);//"BDA Weapon Manager"
            SetGUIOpacity(false);
            GUIUtils.UseMouseEventInRect(WindowRectToolbar);
            if (showWindowGPS && ActiveWeaponManager)
            {
                //gpsWindowRect = GUI.Window(424333, gpsWindowRect, GPSWindow, "", GUI.skin.box);
                GUIUtils.UseMouseEventInRect(WindowRectGps);
                using (var coord = BDATargetManager.GPSTargetList(ActiveWeaponManager.Team).GetEnumerator())
                    while (coord.MoveNext())
                    {
                        GUIUtils.DrawTextureOnWorldPos(coord.Current.worldPos, Instance.greenDotTexture, new Vector2(8, 8), 0);
                    }
            }

            if (Time.time - dependencyLastCheckTime > (dependencyWarnings.Count() == 0 ? 60 : 5)) // Only check once per minute if no issues are found, otherwise 5s.
            {
                CheckDependencies();
            }
            if (dependencyWarnings.Count() > 0)
            {
                GUI.Label(new Rect(Screen.width / 2 - 300 + 2, Screen.height / 6 + 2, 600, 100), string.Join("\n", dependencyWarnings), redErrorShadowStyle);
                GUI.Label(new Rect(Screen.width / 2 - 300, Screen.height / 6, 600, 100), string.Join("\n", dependencyWarnings), redErrorStyle);
            }
        }

        /// <summary>
        /// Check that the dependencies are satisfied.
        /// </summary>
        /// <returns>true if they are, false otherwise.</returns>
        public bool CheckDependencies()
        {
            dependencyLastCheckTime = Time.time;
            dependencyWarnings.Clear();
            if (!ModuleManagerLoaded)
            {
                dependencyWarnings.Add("ModuleManager is missing!");
                dependencyWarnings.Add("BDArmory Lite will not work properly.");
            }
            return dependencyWarnings.Count() == 0;
        }

        float rippleHeight;
        float weaponsHeight;
        float priorityheight;
        float guardHeight;
        float TargetingHeight;
        float EngageHeight;
        float modulesHeight;
        float gpsHeight;
        bool toolMinimized = false;

        float leftIndent = 10;
        float guardLabelWidth = 90;
        float priorityLabelWidth = 120;
        float rightLabelWidth = 45;
        float contentTop = 10;
        float entryHeight = 20;
        float _buttonSize = 26;
        float _windowMargin = 4;

        Rect LabelRect(float line, float labelWidth) => new Rect(leftIndent + 3, line * entryHeight, labelWidth, entryHeight);
        Rect SliderRect(float line, float labelWidth) => new Rect(leftIndent + labelWidth + 16, (line + 0.2f) * entryHeight, columnWidth - 2 * leftIndent - labelWidth - rightLabelWidth - 28, entryHeight);
        Rect InputFieldRect(float line, float labelWidth) => new Rect(leftIndent + labelWidth + 16, line * entryHeight, columnWidth - 2 * leftIndent - labelWidth - 28, entryHeight);
        Rect RightLabelRect(float line) => new Rect(columnWidth - leftIndent - 3 - rightLabelWidth, line * entryHeight, rightLabelWidth, entryHeight);
        Rect ButtonRect(float line) => new Rect(leftIndent + 3, line * entryHeight, columnWidth - 2 * leftIndent - 16, entryHeight);

        (float, float)[] cacheGuardRange, cacheGunRange;
        void WindowBDAToolbar(int windowID)
        {
            float line = 0;
            float contentWidth = columnWidth - 2 * leftIndent;
            float windowColumns = 1;
            int buttonNumber = 0;

            GUI.DragWindow(new Rect(_windowMargin + _buttonSize, 0, columnWidth - 2 * _windowMargin - numberOfButtons * _buttonSize, _windowMargin + _buttonSize));

            line += 1.25f;
            line += 0.25f;

            //title
            GUI.Label(new Rect(_windowMargin + _buttonSize + 6, _windowMargin, columnWidth - 2 * _windowMargin - (numberOfButtons + 1) * _buttonSize - 6, _buttonSize), StringUtils.Localize("#LOC_BDArmory_WMWindow_title"), BDAStyle.Header);

            //SETTINGS BUTTON
            if (!BDKeyBinder.current &&
                GUI.Button(new Rect(columnWidth - _windowMargin - ++buttonNumber * _buttonSize, _windowMargin, _buttonSize, _buttonSize), settingsIconTexture, BDGuiSkin.button))
            {
                ToggleWindowSettings();
            }

            //infolink
            GUIStyle iStyle = infoLinkEnabled ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button;
            if (GUI.Button(new Rect(columnWidth - _windowMargin - ++buttonNumber * _buttonSize, _windowMargin, _buttonSize, _buttonSize), "i", iStyle))
            {
                infoLinkEnabled = !infoLinkEnabled;
            }

            //numeric fields
            GUIStyle nStyle = NumFieldsEnabled ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button;
            if (GUI.Button(new Rect(columnWidth - _windowMargin - ++buttonNumber * _buttonSize, _windowMargin, _buttonSize, _buttonSize), "#", nStyle))
            {
                NumFieldsEnabled = !NumFieldsEnabled;
                if (!NumFieldsEnabled)
                {
                    // Try to parse all the fields immediately so that they're up to date.
                    foreach (var field in textNumFields.Keys)
                    { textNumFields[field].tryParseValueNow(); }
                    if (ActiveWeaponManager != null)
                    {
                        foreach (var field in textNumFields.Keys)
                        {
                            try
                            {
                                var fieldInfo = typeof(MissileFire).GetField(field);
                                if (fieldInfo != null)
                                { fieldInfo.SetValue(ActiveWeaponManager, Convert.ChangeType(textNumFields[field].currentValue, fieldInfo.FieldType)); }
                                else // Check if it's a property instead of a field.
                                {
                                    var propInfo = typeof(MissileFire).GetProperty(field);
                                    propInfo.SetValue(ActiveWeaponManager, Convert.ChangeType(textNumFields[field].currentValue, propInfo.PropertyType));
                                }
                            }
                            catch (Exception e) { Debug.LogError($"[BDArmory.BDArmorySetup]: Failed to set current value of {field}: " + e.Message); }
                        }
                    }
                    // Then make any special conversions here.
                }
                else // Set the input fields to their current values.
                {
                    // Make any special conversions first.
                    // Then set each of the field values to the current slider value.   
                    if (ActiveWeaponManager != null)
                    {
                        foreach (var field in textNumFields.Keys)
                        {
                            try
                            {
                                var fieldInfo = typeof(MissileFire).GetField(field);
                                if (fieldInfo != null)
                                { textNumFields[field].SetCurrentValue(Convert.ToDouble(fieldInfo.GetValue(ActiveWeaponManager))); }
                                else // Check if it's a property instead of a field.
                                {
                                    var propInfo = typeof(MissileFire).GetProperty(field);
                                    textNumFields[field].SetCurrentValue(Convert.ToDouble(propInfo.GetValue(ActiveWeaponManager)));
                                }
                            }
                            catch (Exception e) { Debug.LogError($"[BDArmory.BDArmorySetup]: Failed to set current value of {field}: " + e.Message); }
                        }
                    }
                }
            }

            if (ActiveWeaponManager != null)
            {
                //MINIMIZE BUTTON
                toolMinimized = GUI.Toggle(new Rect(_windowMargin, _windowMargin, _buttonSize, _buttonSize), toolMinimized, "_", toolMinimized ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button);

                GUIStyle armedLabelStyle;
                Rect armedRect = new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight);
                if (ActiveWeaponManager.guardMode)
                {
                    if (GUI.Button(armedRect, "- " + StringUtils.Localize("#LOC_BDArmory_WMWindow_GuardModebtn") + " -", BDAStyle.ButtonOn))//Guard Mode
                    {
                        showGuardMenu = true;
                    }
                }
                else
                {
                    string armedText = StringUtils.Localize("#LOC_BDArmory_WMWindow_ArmedText");//"Trigger is "
                    if (ActiveWeaponManager.isArmed)
                    {
                        armedText += StringUtils.Localize("#LOC_BDArmory_WMWindow_ArmedText_ARMED");//"ARMED."
                        armedLabelStyle = BDAStyle.ButtonOn;
                    }
                    else
                    {
                        armedText += StringUtils.Localize("#LOC_BDArmory_WMWindow_ArmedText_DisArmed");//"disarmed."
                        armedLabelStyle = BDGuiSkin.button;
                    }
                    if (GUI.Button(armedRect, armedText, armedLabelStyle))
                    {
                        ActiveWeaponManager.ToggleArm();
                    }
                }

                GUIStyle teamButtonStyle = BDGuiSkin.box;
                string teamText = StringUtils.Localize("#LOC_BDArmory_WMWindow_TeamText") + $": {ActiveWeaponManager.Team.Name + (ActiveWeaponManager.Team.Neutral ? (ActiveWeaponManager.Team.Name != "Neutral" ? "(N)" : "") : "")}";//Team
                if (GUI.Button(new Rect(leftIndent + (contentWidth / 2), contentTop + (line * entryHeight), contentWidth / 2, entryHeight), teamText, teamButtonStyle))
                {
                    if (Event.current.button == 1)
                    {
                        BDTeamSelector.Instance.Open(ActiveWeaponManager, new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
                    }
                    else
                    {
                        ActiveWeaponManager.NextTeam();
                    }
                }
                line++;
                line += 0.25f;
                string weaponName = ActiveWeaponManager.selectedWeaponString;
                string selectionText = StringUtils.Localize("#LOC_BDArmory_WMWindow_selectionText", weaponName);//Weapon: <<1>>
                GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight * 1.25f), selectionText, BDGuiSkin.box);
                line += 1.25f;
                line += 0.1f;
                //if weapon can ripple, show option and slider.
                if (ActiveWeaponManager.hasLoadedRippleData && ActiveWeaponManager.canRipple)
                {
                    if (ActiveWeaponManager.selectedWeapon != null && ActiveWeaponManager.weaponIndex > 0 &&
                        (ActiveWeaponManager.selectedWeapon.GetWeaponClass() == WeaponClasses.Gun
                        || ActiveWeaponManager.selectedWeapon.GetWeaponClass() == WeaponClasses.Rocket
                        || ActiveWeaponManager.selectedWeapon.GetWeaponClass() == WeaponClasses.DefenseLaser)) //remove rocket ripple slider - moved to editor
                    {
                        string rippleText = ActiveWeaponManager.rippleFire
                            ? StringUtils.Localize("#LOC_BDArmory_WMWindow_rippleText1", ActiveWeaponManager.gunRippleRpm.ToString("0"))//"Barrage: " +  + " RPM"
                            : StringUtils.Localize("#LOC_BDArmory_WMWindow_rippleText2");//"Salvo"
                        GUIStyle rippleStyle = ActiveWeaponManager.rippleFire
                            ? BDAStyle.ButtonOn
                            : BDArmorySetup.BDGuiSkin.button;
                        if (
                            GUI.Button(
                                new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight * 1.25f),
                                rippleText, rippleStyle))
                        {
                            ActiveWeaponManager.ToggleRippleFire();
                        }
                        if (ActiveWeaponManager.rippleFire)
                        {
                            GUI.Label(new Rect(leftIndent + contentWidth / 2 + _windowMargin, contentTop + line * entryHeight, contentWidth / 4 - _windowMargin, entryHeight * 1.25f), $"{StringUtils.Localize("#LOC_BDArmory_WMWindow_barrageStagger")}: {(ActiveWeaponManager.barrageStagger > 0 ? ActiveWeaponManager.barrageStagger : 1):G1}");
                            ActiveWeaponManager.barrageStagger = BDAMath.RoundToUnit(GUI.HorizontalSlider(new Rect(leftIndent + 3 * contentWidth / 4, contentTop + (line + 0.25f) * entryHeight, contentWidth / 4, entryHeight), ActiveWeaponManager.barrageStagger, 0f, 0.1f), 0.01f);
                        }

                        rippleHeight = Mathf.Lerp(rippleHeight, 1.25f, 0.15f);
                    }
                    else
                    {
                        string rippleText = ActiveWeaponManager.rippleFire
                            ? StringUtils.Localize("#LOC_BDArmory_WMWindow_rippleText3", ActiveWeaponManager.rippleRPM.ToString("0"))//"Ripple: " +  + " RPM"
                            : StringUtils.Localize("#LOC_BDArmory_WMWindow_rippleText4");//"Ripple: OFF"
                        GUIStyle rippleStyle = ActiveWeaponManager.rippleFire
                            ? BDAStyle.ButtonOn
                            : BDArmorySetup.BDGuiSkin.button;
                        if (
                            GUI.Button(
                                new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight * 1.25f),
                                rippleText, rippleStyle))
                        {
                            ActiveWeaponManager.ToggleRippleFire();
                        }
                        if (ActiveWeaponManager.rippleFire)
                        {
                            if (!NumFieldsEnabled)
                            {
                                ActiveWeaponManager.rippleRPM = GUI.HorizontalSlider(new Rect(leftIndent + (contentWidth / 2) + 2, contentTop + (line * entryHeight) + 6.5f, (contentWidth / 2) - 2, 12),
                                    ActiveWeaponManager.rippleRPM, 100, 1600, rippleSliderStyle, rippleThumbStyle);
                            }
                            else
                            {
                                var field = textNumFields["rippleRPM"];
                                field.tryParseValue(GUI.TextField(new Rect(leftIndent + (contentWidth / 2) + 2, contentTop + (line * entryHeight) + 6.5f, (contentWidth / 2) - 2, entryHeight),
                                    field.possibleValue, 4, field.style));
                                ActiveWeaponManager.rippleRPM = (float)field.currentValue;
                            }
                        }
                        rippleHeight = Mathf.Lerp(rippleHeight, 1.25f, 0.15f);
                    }
                }
                else
                {
                    rippleHeight = Mathf.Lerp(rippleHeight, 0, 0.15f);
                }
                line += rippleHeight;
                line += 0.1f;

                if (!toolMinimized)
                {
                    showWeaponList =
                        GUI.Toggle(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 4, entryHeight),
                            showWeaponList, StringUtils.Localize("#LOC_BDArmory_WMWindow_ListWeapons"), showWeaponList ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button);//"Weapons"
                    showGuardMenu =
                        GUI.Toggle(
                            new Rect(leftIndent + (contentWidth / 4), contentTop + (line * entryHeight), contentWidth / 4,
                                entryHeight), showGuardMenu, StringUtils.Localize("#LOC_BDArmory_WMWindow_GuardMenu"),//"Guard Menu"
                            showGuardMenu ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button);
                    showPriorities =
                        GUI.Toggle(new Rect(leftIndent + (2 * contentWidth / 4), contentTop + (line * entryHeight), contentWidth / 4,
                             entryHeight), showPriorities, StringUtils.Localize("#LOC_BDArmory_WMWindow_TargetPriority"),//"Tgt priority"
                            showPriorities ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button);
                    showModules =
                        GUI.Toggle(
                            new Rect(leftIndent + (3 * contentWidth / 4), contentTop + (line * entryHeight), contentWidth / 4,
                                entryHeight), showModules, StringUtils.Localize("#LOC_BDArmory_WMWindow_ModulesToggle"),//"Modules"
                            showModules ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button);
                    line++;
                }

                float weaponLines = 0;
                if (showWeaponList && !toolMinimized)
                {
                    line += 0.25f;
                    Rect weaponListGroupRect = new Rect(5, contentTop + (line * entryHeight), columnWidth - 10, weaponsHeight * entryHeight);
                    GUI.BeginGroup(weaponListGroupRect, GUIContent.none, BDGuiSkin.box); //darker box
                    weaponLines += 0.1f;

                    for (int i = 0; i < ActiveWeaponManager.weaponArray.Length; i++)
                    {
                        GUIStyle wpnListStyle;
                        GUIStyle tgtStyle;
                        if (i == ActiveWeaponManager.weaponIndex)
                        {
                            wpnListStyle = middleLeftLabelOrange;
                            tgtStyle = targetModeStyleSelected;
                        }
                        else
                        {
                            wpnListStyle = middleLeftLabel;
                            tgtStyle = targetModeStyle;
                        }
                        string label;
                        string subLabel;
                        if (ActiveWeaponManager.weaponArray[i] != null)
                        {
                            label = ActiveWeaponManager.weaponArray[i].GetShortName();
                            subLabel = ActiveWeaponManager.weaponArray[i].GetSubLabel();
                        }
                        else
                        {
                            label = StringUtils.Localize("#LOC_BDArmory_WMWindow_NoneWeapon");//"None"
                            subLabel = string.Empty;
                        }
                        Rect weaponButtonRect = new Rect(leftIndent, (weaponLines * entryHeight), weaponListGroupRect.width - (2 * leftIndent), entryHeight);

                        GUI.Label(weaponButtonRect, subLabel, tgtStyle);

                        if (GUI.Button(weaponButtonRect, label, wpnListStyle))
                        {
                            ActiveWeaponManager.CycleWeapon(i);
                        }

                        if (i < ActiveWeaponManager.weaponArray.Length - 1)
                        {
                            GUIUtils.DrawRectangle(
                                new Rect(weaponButtonRect.x, weaponButtonRect.y + weaponButtonRect.height,
                                    weaponButtonRect.width, 1), Color.white);
                        }
                        weaponLines++;
                    }

                    weaponLines += 0.1f;
                    GUI.EndGroup();
                }
                weaponsHeight = Mathf.Lerp(weaponsHeight, weaponLines, 0.15f);
                line += weaponsHeight;

                float guardLines = 0;
                if (showGuardMenu && !toolMinimized)
                {
                    line += 0.25f;
                    GUI.BeginGroup(new Rect(5, contentTop + line * entryHeight, columnWidth - 10, guardHeight * entryHeight), GUIContent.none, BDGuiSkin.box);
                    guardLines += 0.1f;

                    string guardButtonLabel = StringUtils.Localize("#LOC_BDArmory_WMWindow_NoneWeapon", (ActiveWeaponManager.guardMode ? StringUtils.Localize("#LOC_BDArmory_Generic_On") : StringUtils.Localize("#LOC_BDArmory_Generic_Off")));//"Guard Mode " + "ON""Off"
                    if (GUI.Button(ButtonRect(guardLines), guardButtonLabel, ActiveWeaponManager.guardMode ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button))
                    {
                        ActiveWeaponManager.ToggleGuardMode();
                    }
                    guardLines += 0.25f;

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_FiringInterval"), leftLabel);//"Firing Interval"                 
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetScanInterval = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), ActiveWeaponManager.targetScanInterval, 0.5f, 60f), 0.5f);
                        GUI.Label(RightLabelRect(guardLines), ActiveWeaponManager.targetScanInterval.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetScanInterval"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetScanInterval = (float)field.currentValue;
                    }

                    string burstLabel = StringUtils.Localize("#LOC_BDArmory_WMWindow_BurstLength");//"Burst Length"
                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), burstLabel, leftLabel);
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.fireBurstLength = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), ActiveWeaponManager.fireBurstLength, 0, 10), 0.05f);
                        GUI.Label(RightLabelRect(guardLines), ActiveWeaponManager.fireBurstLength.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["fireBurstLength"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.fireBurstLength = (float)field.currentValue;
                    }

                    // extension for feature_engagementenvelope: set the firing accuracy tolarance
                    var oldAutoFireCosAngleAdjustment = ActiveWeaponManager.AutoFireCosAngleAdjustment;
                    string accuracyLabel = StringUtils.Localize("#LOC_BDArmory_WMWindow_FiringTolerance");//"Firing Angle"
                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), accuracyLabel, leftLabel);
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.AutoFireCosAngleAdjustment = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), ActiveWeaponManager.AutoFireCosAngleAdjustment, 0, 4), 0.05f);
                        GUI.Label(RightLabelRect(guardLines), ActiveWeaponManager.AutoFireCosAngleAdjustment.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["AutoFireCosAngleAdjustment"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.AutoFireCosAngleAdjustment = (float)field.currentValue;
                    }
                    if (ActiveWeaponManager.AutoFireCosAngleAdjustment != oldAutoFireCosAngleAdjustment)
                        ActiveWeaponManager.OnAFCAAUpdated(null, null);

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_FieldofView"),//"Field of View"
                        leftLabel);
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.guardAngle = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), ActiveWeaponManager.guardAngle, 10, 360), 0.1f);
                        GUI.Label(RightLabelRect(guardLines), ActiveWeaponManager.guardAngle.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["guardAngle"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.guardAngle = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_VisualRange"), leftLabel);//"Visual Range"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.guardRange = GUIUtils.HorizontalSemiLogSlider(SliderRect(guardLines, guardLabelWidth), ActiveWeaponManager.guardRange, 100, BDArmorySettings.MAX_GUARD_VISUAL_RANGE, 1, false, ref cacheGuardRange);
                        GUI.Label(RightLabelRect(guardLines), ActiveWeaponManager.guardRange < 1000 ? $"{ActiveWeaponManager.guardRange:G4}m" : $"{ActiveWeaponManager.guardRange / 1000:G4}km", leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["guardRange"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 8, field.style));
                        ActiveWeaponManager.guardRange = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_GunsRange"), leftLabel);//"Guns Range"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.gunRange = GUIUtils.HorizontalPowerSlider(SliderRect(guardLines, guardLabelWidth), ActiveWeaponManager.gunRange, 0, ActiveWeaponManager.maxGunRange, 2, 2, ref cacheGunRange);
                        GUI.Label(RightLabelRect(guardLines), ActiveWeaponManager.gunRange < 1000 ? $"{ActiveWeaponManager.gunRange:G4}m" : $"{ActiveWeaponManager.gunRange / 1000:G4}km", leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["gunRange"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 8, field.style));
                        ActiveWeaponManager.gunRange = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_MultiTargetNum"), leftLabel);//"Max Turret targets "
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.multiTargetNum = Mathf.Round(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), ActiveWeaponManager.multiTargetNum, 1, 10));
                        GUI.Label(RightLabelRect(guardLines), ActiveWeaponManager.multiTargetNum.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["multiTargetNum"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 2, field.style));
                        ActiveWeaponManager.multiTargetNum = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_MultiMissileNum"), leftLabel);//"Max Turret targets "
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.multiMissileTgtNum = Mathf.Round(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), ActiveWeaponManager.multiMissileTgtNum, 1, 10));
                        GUI.Label(RightLabelRect(guardLines), ActiveWeaponManager.multiMissileTgtNum.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["multiMissileTgtNum"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 2, field.style));
                        ActiveWeaponManager.multiMissileTgtNum = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_MissilesTgt"), leftLabel);//"Missiles/Tgt"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.maxMissilesOnTarget = Mathf.Round(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), ActiveWeaponManager.maxMissilesOnTarget, 1, MissileFire.maxAllowableMissilesOnTarget));
                        GUI.Label(RightLabelRect(guardLines), ActiveWeaponManager.maxMissilesOnTarget.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["maxMissilesOnTarget"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 2, field.style));
                        ActiveWeaponManager.maxMissilesOnTarget = (float)field.currentValue;
                    }

                    showTargetOptions = GUI.Toggle(ButtonRect(++guardLines), showTargetOptions, StringUtils.Localize("#LOC_BDArmory_Settings_Adv_Targeting"), showTargetOptions ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button);//"Advanced Targeting"
                    guardLines += 0.25f;

                    float TargetLines = 0;
                    if (showTargetOptions && showGuardMenu && !toolMinimized)
                    {
                        contentWidth = columnWidth - 30;
                        guardLines += 0.25f;
                        GUI.BeginGroup(new Rect(10, contentTop + (guardLines * entryHeight), contentWidth, (TargetingHeight + 0.25f) * entryHeight), GUIContent.none, BDGuiSkin.box);
                        TargetLines += 0.25f;
                        string CoMlabel = StringUtils.Localize("#LOC_BDArmory_TargetCOM", (ActiveWeaponManager.targetCoM ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Air; True, False
                        if (GUI.Button(new Rect(leftIndent, TargetLines * entryHeight, contentWidth - 2 * leftIndent, entryHeight), CoMlabel, ActiveWeaponManager.targetCoM ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button))
                        {
                            ActiveWeaponManager.targetCoM = !ActiveWeaponManager.targetCoM;
                            ActiveWeaponManager.StartGuardTurretFiring(); //reset weapon targeting assignments
                            if (ActiveWeaponManager.targetCoM)
                            {
                                ActiveWeaponManager.targetCommand = false;
                                ActiveWeaponManager.targetEngine = false;
                                ActiveWeaponManager.targetWeapon = false;
                                ActiveWeaponManager.targetMass = false;
                                ActiveWeaponManager.targetRandom = false;
                            }
                            if (!ActiveWeaponManager.targetCoM && (!ActiveWeaponManager.targetWeapon && !ActiveWeaponManager.targetEngine && !ActiveWeaponManager.targetCommand && !ActiveWeaponManager.targetMass && !ActiveWeaponManager.targetRandom))
                            {
                                ActiveWeaponManager.targetRandom = true;
                            }
                        }
                        TargetLines += 1.1f;
                        string Commandlabel = StringUtils.Localize("#LOC_BDArmory_Command", (ActiveWeaponManager.targetCommand ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Air; True, False
                        if (GUI.Button(new Rect(leftIndent, TargetLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Commandlabel, ActiveWeaponManager.targetCommand ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button))
                        {
                            ActiveWeaponManager.targetCommand = !ActiveWeaponManager.targetCommand;
                            ActiveWeaponManager.StartGuardTurretFiring();
                            if (ActiveWeaponManager.targetCommand)
                            {
                                ActiveWeaponManager.targetCoM = false;
                            }
                            if (!ActiveWeaponManager.targetCoM && (!ActiveWeaponManager.targetWeapon && !ActiveWeaponManager.targetEngine && !ActiveWeaponManager.targetCommand && !ActiveWeaponManager.targetMass && !ActiveWeaponManager.targetRandom))
                            {
                                ActiveWeaponManager.targetCoM = true;
                            }
                        }
                        string Engineslabel = StringUtils.Localize("#LOC_BDArmory_Engines", (ActiveWeaponManager.targetEngine ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Missile; True, False
                        if (GUI.Button(new Rect(leftIndent + (contentWidth - 2 * leftIndent) / 2, TargetLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Engineslabel, ActiveWeaponManager.targetEngine ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button))
                        {
                            ActiveWeaponManager.targetEngine = !ActiveWeaponManager.targetEngine;
                            ActiveWeaponManager.StartGuardTurretFiring();
                            if (ActiveWeaponManager.targetEngine)
                            {
                                ActiveWeaponManager.targetCoM = false;
                            }
                            if (!ActiveWeaponManager.targetCoM && (!ActiveWeaponManager.targetWeapon && !ActiveWeaponManager.targetEngine && !ActiveWeaponManager.targetCommand && !ActiveWeaponManager.targetMass && !ActiveWeaponManager.targetRandom))
                            {
                                ActiveWeaponManager.targetCoM = true;
                            }
                        }
                        TargetLines += 1.1f;
                        string Weaponslabel = StringUtils.Localize("#LOC_BDArmory_Weapons", (ActiveWeaponManager.targetWeapon ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Surface; True, False
                        if (GUI.Button(new Rect(leftIndent, TargetLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Weaponslabel, ActiveWeaponManager.targetWeapon ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button))
                        {
                            ActiveWeaponManager.targetWeapon = !ActiveWeaponManager.targetWeapon;
                            ActiveWeaponManager.StartGuardTurretFiring();
                            if (ActiveWeaponManager.targetWeapon)
                            {
                                ActiveWeaponManager.targetCoM = false;
                            }
                            if (!ActiveWeaponManager.targetCoM && (!ActiveWeaponManager.targetWeapon && !ActiveWeaponManager.targetEngine && !ActiveWeaponManager.targetCommand && !ActiveWeaponManager.targetMass && !ActiveWeaponManager.targetRandom))
                            {
                                ActiveWeaponManager.targetCoM = true;
                            }
                        }
                        string Masslabel = StringUtils.Localize("#LOC_BDArmory_Mass", (ActiveWeaponManager.targetMass ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage SLW; True, False
                        if (GUI.Button(new Rect(leftIndent + (contentWidth - 2 * leftIndent) / 2, TargetLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Masslabel, ActiveWeaponManager.targetMass ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button))
                        {
                            ActiveWeaponManager.targetMass = !ActiveWeaponManager.targetMass;
                            ActiveWeaponManager.StartGuardTurretFiring();
                            if (ActiveWeaponManager.targetMass)
                            {
                                ActiveWeaponManager.targetCoM = false;
                            }
                            if (!ActiveWeaponManager.targetCoM && (!ActiveWeaponManager.targetWeapon && !ActiveWeaponManager.targetEngine && !ActiveWeaponManager.targetCommand && !ActiveWeaponManager.targetMass && !ActiveWeaponManager.targetRandom))
                            {
                                ActiveWeaponManager.targetCoM = true;
                            }
                        }
                        TargetLines += 1.1f;
                        string Randomlabel = StringUtils.Localize("#LOC_BDArmory_Random", (ActiveWeaponManager.targetRandom ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Surface; True, False
                        if (GUI.Button(new Rect(leftIndent, TargetLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Randomlabel, ActiveWeaponManager.targetRandom ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button))
                        {
                            ActiveWeaponManager.targetRandom = !ActiveWeaponManager.targetRandom;
                            ActiveWeaponManager.StartGuardTurretFiring();
                            if (ActiveWeaponManager.targetRandom)
                            {
                                ActiveWeaponManager.targetCoM = false;
                            }
                            if (!ActiveWeaponManager.targetCoM && (!ActiveWeaponManager.targetWeapon && !ActiveWeaponManager.targetEngine && !ActiveWeaponManager.targetCommand && !ActiveWeaponManager.targetMass && !ActiveWeaponManager.targetRandom))
                            {
                                ActiveWeaponManager.targetCoM = true;
                            }
                        }
                        TargetLines += 1.1f;
                        ActiveWeaponManager.targetingString = (ActiveWeaponManager.targetCoM ? StringUtils.Localize("#LOC_BDArmory_TargetCOM") + "; " : "")
                            + (ActiveWeaponManager.targetMass ? StringUtils.Localize("#LOC_BDArmory_Mass") + "; " : "")
                            + (ActiveWeaponManager.targetCommand ? StringUtils.Localize("#LOC_BDArmory_Command") + "; " : "")
                            + (ActiveWeaponManager.targetEngine ? StringUtils.Localize("#LOC_BDArmory_Engines") + "; " : "")
                            + (ActiveWeaponManager.targetWeapon ? StringUtils.Localize("#LOC_BDArmory_Weapons") + "; " : "")
                            + (ActiveWeaponManager.targetWeapon ? StringUtils.Localize("#LOC_BDArmory_Random") + "; " : "");
                        GUI.EndGroup();
                    }
                    TargetingHeight = Mathf.Lerp(TargetingHeight, TargetLines, 0.15f);
                    guardLines += TargetingHeight;

                    showEngageList = GUI.Toggle(ButtonRect(++guardLines), showEngageList, showEngageList ? StringUtils.Localize("#LOC_BDArmory_DisableEngageOptions") : StringUtils.Localize("#LOC_BDArmory_EnableEngageOptions"), showEngageList ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button);//"Enable/Disable Engagement options"
                    guardLines += 0.25f;

                    float EngageLines = 0;
                    if (showEngageList && showGuardMenu && !toolMinimized)
                    {
                        contentWidth = columnWidth - 30;
                        guardLines += 0.25f;
                        GUI.BeginGroup(new Rect(10, contentTop + guardLines * entryHeight, contentWidth, (EngageHeight + 0.25f) * entryHeight), GUIContent.none, BDGuiSkin.box);
                        EngageLines += 0.25f;

                        string Airlabel = StringUtils.Localize("#LOC_BDArmory_EngageAir", (ActiveWeaponManager.engageAir ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Air; True, False
                        if (GUI.Button(new Rect(leftIndent, EngageLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Airlabel, ActiveWeaponManager.engageAir ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button))
                        {
                            ActiveWeaponManager.ToggleEngageAir();
                        }
                        string Missilelabel = StringUtils.Localize("#LOC_BDArmory_EngageMissile", (ActiveWeaponManager.engageMissile ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Missile; True, False
                        if (GUI.Button(new Rect(leftIndent + (contentWidth - 2 * leftIndent) / 2, EngageLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Missilelabel, ActiveWeaponManager.engageMissile ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button))
                        {
                            ActiveWeaponManager.ToggleEngageMissile();
                        }
                        EngageLines += 1.1f;
                        string Srflabel = StringUtils.Localize("#LOC_BDArmory_EngageSurface", (ActiveWeaponManager.engageSrf ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Surface; True, False
                        if (GUI.Button(new Rect(leftIndent, EngageLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Srflabel, ActiveWeaponManager.engageSrf ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button))
                        {
                            ActiveWeaponManager.ToggleEngageSrf();
                        }

                        string SLWlabel = StringUtils.Localize("#LOC_BDArmory_EngageSLW", (ActiveWeaponManager.engageSLW ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage SLW; True, False
                        if (GUI.Button(new Rect(leftIndent + (contentWidth - 2 * leftIndent) / 2, EngageLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), SLWlabel, ActiveWeaponManager.engageSLW ? BDAStyle.ButtonOn : BDArmorySetup.BDGuiSkin.button))
                        {
                            ActiveWeaponManager.ToggleEngageSLW();
                        }
                        EngageLines += 1.1f;
                        GUI.EndGroup();
                    }
                    EngageHeight = Mathf.Lerp(EngageHeight, EngageLines, 0.15f);
                    guardLines += EngageHeight;
                    GUI.EndGroup();
                    ++guardLines;
                }
                guardHeight = Mathf.Lerp(guardHeight, guardLines, 0.15f);
                line += guardHeight;

                float priorityLines = 0;
                if (showPriorities && !toolMinimized)
                {
                    line += 0.25f;
                    GUI.BeginGroup(new Rect(5, contentTop + line * entryHeight, columnWidth - 10, priorityheight * entryHeight), GUIContent.none, BDGuiSkin.box);
                    priorityLines += 0.1f;

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetBias"), leftLabel);//"current target bias"                 
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetBias = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetBias, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetBias.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetBias"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetBias = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetProximity"), leftLabel); //target proximity"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightRange = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightRange, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightRange.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightRange"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightRange = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetPreference"), leftLabel); //target Air preference"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightAirPreference = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightAirPreference, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightAirPreference.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightAirPreference"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightAirPreference = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAngletoTarget"), leftLabel); //target angle"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightATA = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightATA, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightATA.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightATA"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightATA = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAngleDist"), leftLabel); //Angle over Distance"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightAoD = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightAoD, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightAoD.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightAoD"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightAoD = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAccel"), leftLabel); //target accel"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightAccel = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightAccel, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightAccel.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightAccel"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightAccel = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetClosingTime"), leftLabel); //target closing time"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightClosureTime = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightClosureTime, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightClosureTime.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightClosureTime"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightClosureTime = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetgunNumber"), leftLabel); //target weapon num."
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightWeaponNumber = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightWeaponNumber, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightWeaponNumber.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightWeaponNumber"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightWeaponNumber = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetMass"), leftLabel); //target mass"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightMass = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightMass, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightMass.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightMass"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightMass = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_TargetPriority_TargetDmg"), leftLabel); //target Damage"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightDamage = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightDamage, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightDamage.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightDamage"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightDamage = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAllies"), leftLabel); //target mass"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightFriendliesEngaging = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightFriendliesEngaging, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightFriendliesEngaging.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightFriendliesEngaging"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightFriendliesEngaging = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetThreat"), leftLabel); //target proximity"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightThreat = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightThreat, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightThreat.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightThreat"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightThreat = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_defendTeammate"), leftLabel); //defend teammate"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightProtectTeammate = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightProtectTeammate, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightProtectTeammate.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightProtectTeammate"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightProtectTeammate = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_defendVIP"), leftLabel); //target proximity"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightProtectVIP = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightProtectVIP, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightProtectVIP.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightProtectVIP"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightProtectVIP = (float)field.currentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetVIP"), leftLabel); //target proximity"
                    if (!NumFieldsEnabled)
                    {
                        ActiveWeaponManager.targetWeightAttackVIP = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), ActiveWeaponManager.targetWeightAttackVIP, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), ActiveWeaponManager.targetWeightAttackVIP.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightAttackVIP"];
                        field.tryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        ActiveWeaponManager.targetWeightAttackVIP = (float)field.currentValue;
                    }

                    priorityLines += 1.1f;
                    GUI.EndGroup();
                }
                priorityheight = Mathf.Lerp(priorityheight, priorityLines, 0.15f);
                line += priorityheight;

                float moduleLines = 0;
                if (showModules && !toolMinimized)
                {
                    line += 0.25f;
                    GUI.BeginGroup(
                        new Rect(5, contentTop + (line * entryHeight), columnWidth - 10, numberOfModules * entryHeight),
                        GUIContent.none, BDGuiSkin.box);
                    moduleLines += 0.1f;

                    numberOfModules = 0;
                    //RWR
                    if (ActiveWeaponManager.rwr)
                    {
                        numberOfModules++;
                        bool isEnabled = ActiveWeaponManager.rwr.displayRWR;
                        string label = StringUtils.Localize("#LOC_BDArmory_WMWindow_RadarWarning");//"Radar Warning Receiver"
                        Rect rwrRect = new Rect(leftIndent, +(moduleLines * entryHeight), columnWidth - 2 * leftIndent, entryHeight);
                        if (GUI.Button(rwrRect, label, isEnabled ? centerLabelOrange : centerLabel))
                        {
                            if (isEnabled)
                            {
                                //ActiveWeaponManager.rwr.DisableRWR();
                                ActiveWeaponManager.rwr.displayRWR = false;
                            }
                            else
                            {
                                //ActiveWeaponManager.rwr.EnableRWR();
                                ActiveWeaponManager.rwr.displayRWR = true;
                            }
                        }
                        moduleLines++;
                    }

                    //TGP
                    using (List<ModuleTargetingCamera>.Enumerator mtc = ActiveWeaponManager.targetingPods.GetEnumerator())
                        while (mtc.MoveNext())
                        {
                            if (mtc.Current == null) continue;
                            numberOfModules++;
                            bool isEnabled = (mtc.Current.cameraEnabled);
                            bool isActive = (mtc.Current == ModuleTargetingCamera.activeCam);
                            GUIStyle moduleStyle = isEnabled ? centerLabelOrange : centerLabel; // = mtc
                            string label = mtc.Current.part.partInfo.title;
                            if (isActive)
                            {
                                moduleStyle = centerLabelRed;
                                label = $"[{label}]";
                            }
                            if (GUI.Button(new Rect(leftIndent, +(moduleLines * entryHeight), columnWidth - 2 * leftIndent, entryHeight),
                                label, moduleStyle))
                            {
                                if (isActive)
                                {
                                    mtc.Current.ToggleCamera();
                                }
                                else
                                {
                                    mtc.Current.EnableCamera();
                                }
                            }
                            moduleLines++;
                        }

                    //RADAR
                    using (List<ModuleRadar>.Enumerator mr = ActiveWeaponManager.radars.GetEnumerator())
                        while (mr.MoveNext())
                        {
                            if (mr.Current == null) continue;
                            numberOfModules++;
                            GUIStyle moduleStyle = mr.Current.radarEnabled ? centerLabelBlue : centerLabel;
                            string label = mr.Current.radarName;
                            if (GUI.Button(new Rect(leftIndent, +(moduleLines * entryHeight), columnWidth - 2 * leftIndent, entryHeight),
                                label, moduleStyle))
                            {
                                mr.Current.Toggle();
                            }
                            moduleLines++;
                        }
                    using (List<ModuleIRST>.Enumerator mr = ActiveWeaponManager.irsts.GetEnumerator())
                        while (mr.MoveNext())
                        {
                            if (mr.Current == null) continue;
                            numberOfModules++;
                            GUIStyle moduleStyle = mr.Current.irstEnabled ? centerLabelBlue : centerLabel;
                            string label = mr.Current.IRSTName;
                            if (GUI.Button(new Rect(leftIndent, +(moduleLines * entryHeight), columnWidth - 2 * leftIndent, entryHeight),
                                label, moduleStyle))
                            {
                                mr.Current.Toggle();
                            }
                            moduleLines++;
                        }
                    //JAMMERS
                    using (List<ModuleECMJammer>.Enumerator jammer = ActiveWeaponManager.jammers.GetEnumerator())
                        while (jammer.MoveNext())
                        {
                            if (jammer.Current == null) continue;
                            if (jammer.Current.alwaysOn) continue;

                            numberOfModules++;
                            GUIStyle moduleStyle = jammer.Current.jammerEnabled ? centerLabelBlue : centerLabel;
                            string label = jammer.Current.part.partInfo.title;
                            if (GUI.Button(new Rect(leftIndent, +(moduleLines * entryHeight), columnWidth - 2 * leftIndent, entryHeight),
                                label, moduleStyle))
                            {
                                jammer.Current.Toggle();
                            }
                            moduleLines++;
                        }
                    //CLOAKS
                    using (List<ModuleCloakingDevice>.Enumerator cloak = ActiveWeaponManager.cloaks.GetEnumerator())
                        while (cloak.MoveNext())
                        {
                            if (cloak.Current == null) continue;
                            if (cloak.Current.alwaysOn) continue;

                            numberOfModules++;
                            GUIStyle moduleStyle = cloak.Current.cloakEnabled ? centerLabelBlue : centerLabel;
                            string label = cloak.Current.part.partInfo.title;
                            if (GUI.Button(new Rect(leftIndent, +(moduleLines * entryHeight), columnWidth - 2 * leftIndent, entryHeight),
                                label, moduleStyle))
                            {
                                cloak.Current.Toggle();
                            }
                            moduleLines++;
                        }

                    //Other modules
                    using (var module = ActiveWeaponManager.wmModules.GetEnumerator())
                        while (module.MoveNext())
                        {
                            if (module.Current == null) continue;

                            numberOfModules++;
                            GUIStyle moduleStyle = module.Current.Enabled ? centerLabelBlue : centerLabel;
                            string label = module.Current.Name;
                            if (GUI.Button(new Rect(leftIndent, +(moduleLines * entryHeight), columnWidth - 2 * leftIndent, entryHeight),
                                label, moduleStyle))
                            {
                                module.Current.Toggle();
                            }
                            moduleLines++;
                        }

                    //GPS coordinator
                    GUIStyle gpsModuleStyle = showWindowGPS ? centerLabelBlue : centerLabel;
                    numberOfModules++;
                    if (GUI.Button(new Rect(leftIndent, +(moduleLines * entryHeight), columnWidth - 2 * leftIndent, entryHeight),
                        StringUtils.Localize("#LOC_BDArmory_WMWindow_GPSCoordinator"), gpsModuleStyle))//"GPS Coordinator"
                    {
                        showWindowGPS = !showWindowGPS;
                    }
                    moduleLines++;

                    moduleLines += 0.1f;
                    GUI.EndGroup();
                }
                modulesHeight = Mathf.Lerp(modulesHeight, moduleLines, 0.15f);
                line += modulesHeight;

                float gpsLines = 0;
                if (showWindowGPS && !toolMinimized)
                {
                    line += 0.25f;
                    GUI.BeginGroup(new Rect(5, contentTop + (line * entryHeight), columnWidth, WindowRectGps.height));
                    WindowGPS();
                    GUI.EndGroup();
                    gpsLines = WindowRectGps.height / entryHeight;
                }
                gpsHeight = Mathf.Lerp(gpsHeight, gpsLines, 0.15f);
                line += gpsHeight;

                if (infoLinkEnabled && !toolMinimized)
                {
                    windowColumns = 2;

                    GUI.Label(new Rect(leftIndent + columnWidth, contentTop, columnWidth - (leftIndent), entryHeight), StringUtils.Localize("#LOC_BDArmory_AIWindow_infoLink"), kspTitleLabel);//"infolink"
                    GUILayout.BeginArea(new Rect(leftIndent + columnWidth, contentTop + (entryHeight * 1.5f), columnWidth - (leftIndent), toolWindowHeight - (entryHeight * 1.5f) - (2 * contentTop)));
                    using (var scrollViewScope = new GUILayout.ScrollViewScope(scrollInfoVector, GUILayout.Width(columnWidth - (leftIndent)), GUILayout.Height(toolWindowHeight - (entryHeight * 1.5f) - (2 * contentTop))))
                    {
                        scrollInfoVector = scrollViewScope.scrollPosition;
                        if (showWeaponList)
                        {
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_ListWeapons"), leftLabelBold, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Weapons
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_Weapons_Desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //weapons desc
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_Ripple_Salvo_Desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //ripple/salvo desc
                        }
                        if (showGuardMenu)
                        {
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_GuardMenu"), leftLabelBold, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Guard Mode
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_GuardTab_Desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Guard desc
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_FiringInterval_Desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //firing inverval desc
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_BurstLength_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //burst length desc
                            GUILayout.Label(FiringAngleImage);
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_FiringTolerance_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //firing angle desc
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_FieldofView_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //FoV desc
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_VisualRange_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //guard range desc
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_GunsRange_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //weapon range desc
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_MultiTargetNum_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //multiturrets desc
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_MultiMissileTgtNum_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //multiturrets desc
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_MissilesTgt_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //multimissiles desc
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_TargetType_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //subsection targeting desc
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_EngageType_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //engagement toggles desc
                        }
                        if (showPriorities)
                        {
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_Prioritues_Desc"), leftLabelBold, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt Priorities
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetBias_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt Bias
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetPreference_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt engagement Pref
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetProximity_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt dist
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAngletoTarget_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt angle
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAngleDist_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt angle/dist
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAccel_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt accel
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetClosingTime_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt closing time
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetgunNumber_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt weapons num
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetMass_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt mass
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetDmg_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt Damage
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAllies_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt allies attacking
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetThreat_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt threat
                            GUILayout.Label(StringUtils.Localize("#LOC_BDArmory_WMWindow_targetVIP_desc"), infoLinkStyle, GUILayout.Width(columnWidth - (leftIndent * 4) - 20)); //Tgt VIP
                        }

                    }
                    GUILayout.EndArea();
                }
            }
            else
            {
                GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), columnWidth - 2 * leftIndent, entryHeight),
                   StringUtils.Localize("#LOC_BDArmory_WMWindow_NoWeaponManager"), BDGuiSkin.box);// "No Weapon Manager found."
                line++;
            }
            var previousWindowHeight = toolWindowHeight;
            toolWindowWidth = Mathf.Lerp(toolWindowWidth, columnWidth * windowColumns, 0.15f);
            toolWindowHeight = Mathf.Lerp(toolWindowHeight, contentTop + (line * entryHeight) + 5, 1);
            WindowRectToolbar.height = toolWindowHeight;
            WindowRectToolbar.width = toolWindowWidth;
            numberOfButtons = buttonNumber + 1;
            GUIUtils.RepositionWindow(ref WindowRectToolbar, previousWindowHeight);
        }

        bool validGPSName = true;

        //GPS window
        public void WindowGPS()
        {
            GUI.Box(WindowRectGps, GUIContent.none, BDGuiSkin.box);
            gpsEntryCount = 0;
            Rect listRect = new Rect(gpsBorder, gpsBorder, WindowRectGps.width - (2 * gpsBorder),
                WindowRectGps.height - (2 * gpsBorder));
            GUI.BeginGroup(listRect);
            string targetLabel = $"{StringUtils.Localize("#LOC_BDArmory_WMWindow_GPSTarget")}: {ActiveWeaponManager.designatedGPSInfo.name}";//GPS Target
            GUI.Label(new Rect(0, 0, listRect.width, gpsEntryHeight), targetLabel, kspTitleLabel);

            // Expand/Collapse Target Toggle button
            if (GUI.Button(new Rect(listRect.width - gpsEntryHeight, 0, gpsEntryHeight, gpsEntryHeight), showTargets ? "-" : "+", BDGuiSkin.button))
                showTargets = !showTargets;

            gpsEntryCount += 0.85f;
            if (ActiveWeaponManager.designatedGPSCoords != Vector3d.zero)
            {
                GUI.Label(new Rect(0, gpsEntryCount * gpsEntryHeight, listRect.width - gpsEntryHeight, gpsEntryHeight),
                    BodyUtils.FormattedGeoPos(ActiveWeaponManager.designatedGPSCoords, true), BDGuiSkin.box);
                if (
                    GUI.Button(
                        new Rect(listRect.width - gpsEntryHeight, gpsEntryCount * gpsEntryHeight, gpsEntryHeight,
                            gpsEntryHeight), "X", BDGuiSkin.button))
                {
                    ActiveWeaponManager.designatedGPSInfo = new GPSTargetInfo();
                }
            }
            else
            {
                GUI.Label(new Rect(0, gpsEntryCount * gpsEntryHeight, listRect.width - gpsEntryHeight, gpsEntryHeight),
                    StringUtils.Localize("#LOC_BDArmory_WMWindow_NoTarget"), BDGuiSkin.box);//"No Target"
            }

            gpsEntryCount += 1.35f;
            int indexToRemove = -1;
            int index = 0;
            BDTeam myTeam = ActiveWeaponManager.Team;
            if (showTargets)
            {
                using (var coordinate = BDATargetManager.GPSTargetList(myTeam).GetEnumerator())
                    while (coordinate.MoveNext())
                    {
                        Color origWColor = GUI.color;
                        if (coordinate.Current.EqualsTarget(ActiveWeaponManager.designatedGPSInfo))
                        {
                            GUI.color = XKCDColors.LightOrange;
                        }

                        string label = BodyUtils.FormattedGeoPosShort(coordinate.Current.gpsCoordinates, false);
                        float nameWidth = 100;
                        if (editingGPSName && index == editingGPSNameIndex)
                        {
                            if (validGPSName && Event.current.type == EventType.KeyDown &&
                                Event.current.keyCode == KeyCode.Return)
                            {
                                editingGPSName = false;
                                hasEnteredGPSName = true;
                            }
                            else
                            {
                                Color origColor = GUI.color;
                                if (newGPSName.Contains(";") || newGPSName.Contains(":") || newGPSName.Contains(","))
                                {
                                    validGPSName = false;
                                    GUI.color = Color.red;
                                }
                                else
                                {
                                    validGPSName = true;
                                }

                                newGPSName = GUI.TextField(
                                  new Rect(0, gpsEntryCount * gpsEntryHeight, nameWidth, gpsEntryHeight), newGPSName, 12, textFieldStyle);
                                GUI.color = origColor;
                            }
                        }
                        else
                        {
                            if (GUI.Button(new Rect(0, gpsEntryCount * gpsEntryHeight, nameWidth, gpsEntryHeight),
                              coordinate.Current.name,
                              BDGuiSkin.button))
                            {
                                editingGPSName = true;
                                editingGPSNameIndex = index;
                                newGPSName = coordinate.Current.name;
                            }
                        }

                        if (
                          GUI.Button(
                            new Rect(nameWidth, gpsEntryCount * gpsEntryHeight, listRect.width - gpsEntryHeight - nameWidth,
                              gpsEntryHeight), label, BDGuiSkin.button))
                        {
                            ActiveWeaponManager.designatedGPSInfo = coordinate.Current;
                            ActiveWeaponManager.designatedGPSCoordsIndex = index;
                            editingGPSName = false;
                        }

                        if (
                          GUI.Button(
                            new Rect(listRect.width - gpsEntryHeight, gpsEntryCount * gpsEntryHeight, gpsEntryHeight,
                              gpsEntryHeight), "X", BDGuiSkin.button))
                        {
                            indexToRemove = index;
                        }

                        gpsEntryCount++;
                        index++;
                        GUI.color = origWColor;
                    }
            }

            if (hasEnteredGPSName && editingGPSNameIndex < BDATargetManager.GPSTargetList(myTeam).Count)
            {
                hasEnteredGPSName = false;
                GPSTargetInfo old = BDATargetManager.GPSTargetList(myTeam)[editingGPSNameIndex];
                if (ActiveWeaponManager.designatedGPSInfo.EqualsTarget(old))
                {
                    ActiveWeaponManager.designatedGPSInfo.name = newGPSName;
                }
                BDATargetManager.GPSTargetList(myTeam)[editingGPSNameIndex] =
                    new GPSTargetInfo(BDATargetManager.GPSTargetList(myTeam)[editingGPSNameIndex].gpsCoordinates,
                        newGPSName);
                editingGPSNameIndex = 0;
                BDATargetManager.Instance.SaveGPSTargets();
            }

            GUI.EndGroup();

            if (indexToRemove >= 0)
            {
                BDATargetManager.GPSTargetList(myTeam).RemoveAt(indexToRemove);
                BDATargetManager.Instance.SaveGPSTargets();
            }

            WindowRectGps.height = (2 * gpsBorder) + (gpsEntryCount * gpsEntryHeight);
        }

        #region Settings window
        const float settingsWidth = 460;
        const float settingsHeight = 620;
        const float settingsTitleHeight = 30;
        const float settingsLabelWidth = 190;
        const float settingsValueWidth = 56;
        Vector2 settingsScroll;
        Vector2 keyBindingsScroll;
        bool editKeys;
        bool scalingUI = false;
        float oldUIScale = 1;

        void SetupSettingsSize()
        {
            WindowRectSettings = new Rect(Screen.width / 2 - settingsWidth / 2, 100, settingsWidth, settingsHeight);
        }

        void WindowSettings(int windowID)
        {
            WindowRectSettings.width = settingsWidth;
            WindowRectSettings.height = settingsHeight;
            GUI.Label(new Rect(12, 4, 300, 22), editKeys ? "Key Bindings" : "BDArmory Lite Settings", BDAStyle.Header);
            GUI.Label(new Rect(settingsWidth - 130, 4, 96, 22), Version, new GUIStyle(BDAStyle.DimLabel) { alignment = TextAnchor.MiddleRight, fontSize = 10 });
            if (GUI.Button(new Rect(settingsWidth - 28, 5, 22, 20), "X", BDAStyle.TitleButton)) CloseSettingsWindow();
            GUI.DragWindow(new Rect(0, 0, settingsWidth - 30, settingsTitleHeight));

            GUILayout.BeginArea(new Rect(10, settingsTitleHeight, settingsWidth - 20, settingsHeight - settingsTitleHeight - 10));
            if (editKeys) KeyBindingsPanel();
            else SettingsPanel();
            GUILayout.EndArea();
            GUIUtils.UseMouseEventInRect(WindowRectSettings);
        }

        void SettingsPanel()
        {
            settingsScroll = GUILayout.BeginScrollView(settingsScroll, false, true);

            if (SettingsSection("Interface", ref BDArmorySettings.GRAPHICS_UI_SETTINGS_TOGGLE))
            {
                float uiScale = SettingSlider("UI scale", BDArmorySettings.UI_SCALE, 0.5f, 2f, 0.05f, $"{BDArmorySettings.UI_SCALE * 100:0}%");
                if (uiScale != BDArmorySettings.UI_SCALE) { BDArmorySettings.UI_SCALE = uiScale; scalingUI = true; }
                BDArmorySettings.GUI_OPACITY = SettingSlider("Window opacity", BDArmorySettings.GUI_OPACITY, 0.2f, 1f, 0.05f, $"{BDArmorySettings.GUI_OPACITY * 100:0}%");

                float uiVol = SettingSlider("UI volume", BDArmorySettings.BDARMORY_UI_VOLUME, 0f, 1f, 0.01f, $"{BDArmorySettings.BDARMORY_UI_VOLUME * 100:0}%");
                float weaponVol = SettingSlider("Weapon volume", BDArmorySettings.BDARMORY_WEAPONS_VOLUME, 0f, 1f, 0.01f, $"{BDArmorySettings.BDARMORY_WEAPONS_VOLUME * 100:0}%");
                if (uiVol != BDArmorySettings.BDARMORY_UI_VOLUME || weaponVol != BDArmorySettings.BDARMORY_WEAPONS_VOLUME)
                {
                    BDArmorySettings.BDARMORY_UI_VOLUME = uiVol;
                    BDArmorySettings.BDARMORY_WEAPONS_VOLUME = weaponVol;
                    OnVolumeChange?.Invoke();
                }

                BDArmorySettings.DRAW_AIMERS = SettingToggle("Draw gun aimers", BDArmorySettings.DRAW_AIMERS);
                if (BDArmorySettings.DRAW_AIMERS)
                {
                    BDArmorySettings.AIM_ASSIST = SettingToggle("Aim assist", BDArmorySettings.AIM_ASSIST, 1);
                    if (BDArmorySettings.AIM_ASSIST)
                        BDArmorySettings.AIM_ASSIST_MODE = SettingToggle("Reticle follows predicted impact point", BDArmorySettings.AIM_ASSIST_MODE, 2);
                }
                BDArmorySettings.SHOW_AMMO_GAUGES = SettingToggle("Ammo gauges in the staging list", BDArmorySettings.SHOW_AMMO_GAUGES);
                BDArmorySettings.SCROLL_ZOOM_PREVENTION = SettingToggle("Don't zoom the camera when scrolling over windows", BDArmorySettings.SCROLL_ZOOM_PREVENTION);
                BDArmorySettings.STRICT_WINDOW_BOUNDARIES = SettingToggle("Keep windows on screen", BDArmorySettings.STRICT_WINDOW_BOUNDARIES);
                BDArmorySettings.SHOW_CATEGORIES = SettingToggle("BDArmory part categories in the editor", BDArmorySettings.SHOW_CATEGORIES);
                if (BDArmorySettings.SHOW_CATEGORIES)
                    BDArmorySettings.AUTOCATEGORIZE_PARTS = SettingToggle("Sort parts into categories automatically", BDArmorySettings.AUTOCATEGORIZE_PARTS, 1);

                if (HighLogic.LoadedSceneIsFlight && BDTISetup.Instance != null && GUILayout.Button("Team Icons..."))
                    BDTISetup.Instance.ToggleWindow();
                if (HighLogic.LoadedSceneIsEditor && BDAEditorAnalysisWindow.Instance != null && GUILayout.Button("Radar Cross-Section Analysis..."))
                    BDAEditorAnalysisWindow.Instance.ToggleWindow();
                GUILayout.Space(4);
            }

            if (SettingsSection("Gameplay", ref BDArmorySettings.GAMEPLAY_SETTINGS_TOGGLE))
            {
                BDArmorySettings.INFINITE_AMMO = SettingToggle("Infinite ammunition", BDArmorySettings.INFINITE_AMMO);
                BDArmorySettings.INFINITE_ORDINANCE = SettingToggle("Infinite missiles and bombs (on reloadable launchers)", BDArmorySettings.INFINITE_ORDINANCE);
                BDArmorySettings.BULLET_HITS = SettingToggle("Bullet hit effects", BDArmorySettings.BULLET_HITS);
                BDArmorySettings.BULLET_DECALS = SettingToggle("Bullet hole decals", BDArmorySettings.BULLET_DECALS);
                if (BDArmorySettings.BULLET_DECALS)
                    BDArmorySettings.MAX_NUM_BULLET_DECALS = Mathf.RoundToInt(SettingSlider("Maximum decals", BDArmorySettings.MAX_NUM_BULLET_DECALS, 1f, 999f, 1f, BDArmorySettings.MAX_NUM_BULLET_DECALS.ToString(), 1));
                BDArmorySettings.EJECT_SHELLS = SettingToggle("Eject shell casings", BDArmorySettings.EJECT_SHELLS);
                if (BDArmorySettings.EJECT_SHELLS)
                    BDArmorySettings.SHELL_COLLISIONS = SettingToggle("Shell casings collide", BDArmorySettings.SHELL_COLLISIONS, 1);
                BDArmorySettings.FLARE_SMOKE = SettingToggle("Flares leave smoke trails", BDArmorySettings.FLARE_SMOKE);
                if (VolumetricExplosionFXBridge.Installed)
                    BDArmorySettings.VEFX_HIDE_BDA_EXPLOSIONS = SettingToggle("Let Volumetric Explosion FX replace explosion visuals", BDArmorySettings.VEFX_HIDE_BDA_EXPLOSIONS);
                BDArmorySettings.BULLET_WATER_DRAG = SettingToggle("Water slows bullets and rockets", BDArmorySettings.BULLET_WATER_DRAG);
                BDArmorySettings.BOMB_CLEARANCE_CHECK = SettingToggle("Check bomb clearance before release", BDArmorySettings.BOMB_CLEARANCE_CHECK);
                BDArmorySettings.TRIGGER_HOLD_TIME = SettingSlider("Trigger hold time", BDArmorySettings.TRIGGER_HOLD_TIME, 0.02f, 1f, 0.02f, $"{BDArmorySettings.TRIGGER_HOLD_TIME:0.00}s");
                GUILayout.Space(4);
            }

            if (SettingsSection("Missiles & Countermeasures", ref BDArmorySettings.MISSILE_CM_SETTING_TOGGLE))
            {
                BDArmorySettings.ASPECTED_IR_SEEKERS = SettingToggle("IR seekers see engine exhaust by aspect", BDArmorySettings.ASPECTED_IR_SEEKERS);
                BDArmorySettings.ASPECTED_RCS = SettingToggle("Radar cross-section depends on aspect", BDArmorySettings.ASPECTED_RCS);
                BDArmorySettings.FLARE_FACTOR = SettingSlider("Flare effectiveness", BDArmorySettings.FLARE_FACTOR, 0f, 3f, 0.05f, $"{BDArmorySettings.FLARE_FACTOR:0.00}");
                BDArmorySettings.CHAFF_FACTOR = SettingSlider("Chaff effectiveness", BDArmorySettings.CHAFF_FACTOR, 0f, 3f, 0.05f, $"{BDArmorySettings.CHAFF_FACTOR:0.00}");
                BDArmorySettings.SMOKE_DEFLECTION_FACTOR = SettingSlider("Smoke laser deflection", BDArmorySettings.SMOKE_DEFLECTION_FACTOR, 0f, 40f, 0.5f, $"{BDArmorySettings.SMOKE_DEFLECTION_FACTOR:0.0}");
                BDArmorySettings.APS_THRESHOLD = Mathf.RoundToInt(SettingSlider("APS minimum calibre", BDArmorySettings.APS_THRESHOLD, 1f, 356f, 1f, $"{BDArmorySettings.APS_THRESHOLD}mm"));
                GUILayout.Space(4);
            }

            if (SettingsSection("Damage", ref BDArmorySettings.SLIDER_SETTINGS_TOGGLE))
            {
                BDArmorySettings.DMG_MULTIPLIER = Mathf.RoundToInt(SettingSlider("Damage multiplier", BDArmorySettings.DMG_MULTIPLIER, 10f, 1000f, 10f, $"{BDArmorySettings.DMG_MULTIPLIER:0}%"));
                BDArmorySettings.BALLISTIC_DMG_FACTOR = SettingSlider("Ballistic damage", BDArmorySettings.BALLISTIC_DMG_FACTOR, 0f, 3f, 0.05f, $"{BDArmorySettings.BALLISTIC_DMG_FACTOR:0.00}");
                BDArmorySettings.EXP_DMG_MOD_BALLISTIC_NEW = SettingSlider("Explosive shell damage", BDArmorySettings.EXP_DMG_MOD_BALLISTIC_NEW, 0f, 1.5f, 0.05f, $"{BDArmorySettings.EXP_DMG_MOD_BALLISTIC_NEW:0.00}");
                BDArmorySettings.EXP_DMG_MOD_MISSILE = SettingSlider("Missile damage", BDArmorySettings.EXP_DMG_MOD_MISSILE, 0f, 10f, 0.25f, $"{BDArmorySettings.EXP_DMG_MOD_MISSILE:0.00}");
                BDArmorySettings.EXP_DMG_MOD_ROCKET = SettingSlider("Rocket damage", BDArmorySettings.EXP_DMG_MOD_ROCKET, 0f, 2f, 0.05f, $"{BDArmorySettings.EXP_DMG_MOD_ROCKET:0.00}");
                BDArmorySettings.BUILDING_DMG_MULTIPLIER = SettingSlider("Building damage", BDArmorySettings.BUILDING_DMG_MULTIPLIER, 0f, 10f, 0.1f, $"{BDArmorySettings.BUILDING_DMG_MULTIPLIER:0.0}");
                BDArmorySettings.HITPOINT_MULTIPLIER = SettingSlider("Part hit points", BDArmorySettings.HITPOINT_MULTIPLIER, 0.1f, 10f, 0.1f, $"{BDArmorySettings.HITPOINT_MULTIPLIER:0.0}");
                BDArmorySettings.WEAPON_FX_DURATION = SettingSlider("EMP / special effect duration", BDArmorySettings.WEAPON_FX_DURATION, 5f, 20f, 1f, $"{BDArmorySettings.WEAPON_FX_DURATION:0}s");
                GUILayout.Label("Hit point changes apply to newly loaded craft.", BDAStyle.DimLabel);
                GUILayout.Space(4);
            }

            if (SettingsSection("Battle Damage", ref BDArmorySettings.BATTLEDAMAGE_TOGGLE))
            {
                BDArmorySettings.BATTLEDAMAGE = SettingToggle("Enable battle damage", BDArmorySettings.BATTLEDAMAGE);
                if (BDArmorySettings.BATTLEDAMAGE)
                {
                    BDArmorySettings.BD_DAMAGE_CHANCE = SettingSlider("Chance per hit", BDArmorySettings.BD_DAMAGE_CHANCE, 0f, 100f, 1f, $"{BDArmorySettings.BD_DAMAGE_CHANCE:0}%", 1);
                    BDArmorySettings.BD_SUBSYSTEMS = SettingToggle("Subsystem damage", BDArmorySettings.BD_SUBSYSTEMS, 1);
                    BDArmorySettings.BD_TANKS = SettingToggle("Fuel tanks leak and burn", BDArmorySettings.BD_TANKS, 1);
                    if (BDArmorySettings.BD_TANKS)
                    {
                        BDArmorySettings.BD_TANK_LEAK_TIME = SettingSlider("Leak duration", BDArmorySettings.BD_TANK_LEAK_TIME, 0f, 100f, 1f, $"{BDArmorySettings.BD_TANK_LEAK_TIME:0}s", 2);
                        BDArmorySettings.BD_TANK_LEAK_RATE = SettingSlider("Leak rate", BDArmorySettings.BD_TANK_LEAK_RATE, 0f, 5f, 0.1f, $"{BDArmorySettings.BD_TANK_LEAK_RATE:0.0}", 2);
                    }
                    BDArmorySettings.BD_AMMOBINS = SettingToggle("Ammo boxes can explode", BDArmorySettings.BD_AMMOBINS, 1);
                    if (BDArmorySettings.BD_AMMOBINS)
                        BDArmorySettings.BD_VOLATILE_AMMO = SettingToggle("Always explode when destroyed", BDArmorySettings.BD_VOLATILE_AMMO, 2);
                    BDArmorySettings.BD_PROPULSION = SettingToggle("Engine damage", BDArmorySettings.BD_PROPULSION, 1);
                    if (BDArmorySettings.BD_PROPULSION)
                    {
                        BDArmorySettings.BD_PROP_FLOOR = SettingSlider("Minimum thrust", BDArmorySettings.BD_PROP_FLOOR, 0f, 100f, 1f, $"{BDArmorySettings.BD_PROP_FLOOR:0}%", 2);
                        BDArmorySettings.BD_PROP_FLAMEOUT = SettingSlider("Flameout below HP", BDArmorySettings.BD_PROP_FLAMEOUT, 0f, 95f, 1f, $"{BDArmorySettings.BD_PROP_FLAMEOUT:0}%", 2);
                    }
                    BDArmorySettings.BD_INTAKES = SettingToggle("Intake damage", BDArmorySettings.BD_INTAKES, 1);
                    BDArmorySettings.BD_GIMBALS = SettingToggle("Gimbal damage", BDArmorySettings.BD_GIMBALS, 1);
                    BDArmorySettings.BD_AEROPARTS = SettingToggle("Wings lose lift", BDArmorySettings.BD_AEROPARTS, 1);
                    BDArmorySettings.BD_CTRL_SRF = SettingToggle("Control surfaces can jam", BDArmorySettings.BD_CTRL_SRF, 1);
                    BDArmorySettings.BD_PILOT_KILLS = SettingToggle("Cockpit hits can kill crew", BDArmorySettings.BD_PILOT_KILLS, 1);
                    BDArmorySettings.BD_FIRES_ENABLED = SettingToggle("Fires", BDArmorySettings.BD_FIRES_ENABLED, 1);
                    if (BDArmorySettings.BD_FIRES_ENABLED)
                    {
                        BDArmorySettings.BD_FIRE_DOT = SettingToggle("Fires damage parts", BDArmorySettings.BD_FIRE_DOT, 2);
                        if (BDArmorySettings.BD_FIRE_DOT)
                            BDArmorySettings.BD_FIRE_DAMAGE = SettingSlider("Fire damage", BDArmorySettings.BD_FIRE_DAMAGE, 0f, 20f, 0.5f, $"{BDArmorySettings.BD_FIRE_DAMAGE:0.0}/s", 3);
                        BDArmorySettings.BD_FIRE_HEATDMG = SettingToggle("Fires heat parts", BDArmorySettings.BD_FIRE_HEATDMG, 2);
                        BDArmorySettings.BD_FIRE_FUELEX = SettingToggle("Fires can detonate fuel tanks", BDArmorySettings.BD_FIRE_FUELEX, 2);
                    }
                }
                GUILayout.Space(4);
            }

            if (SettingsSection("Radar & Targeting", ref BDArmorySettings.RADAR_SETTINGS_TOGGLE))
            {
                float rwrScale = SettingSlider("RWR window scale", BDArmorySettings.RWR_WINDOW_SCALE, BDArmorySettings.RWR_WINDOW_SCALE_MIN, BDArmorySettings.RWR_WINDOW_SCALE_MAX, 0.01f, $"{BDArmorySettings.RWR_WINDOW_SCALE * 100:0}%");
                if (rwrScale != BDArmorySettings.RWR_WINDOW_SCALE) ResizeRwrWindow(rwrScale);
                float radarScale = SettingSlider("Radar window scale", BDArmorySettings.RADAR_WINDOW_SCALE, BDArmorySettings.RADAR_WINDOW_SCALE_MIN, BDArmorySettings.RADAR_WINDOW_SCALE_MAX, 0.01f, $"{BDArmorySettings.RADAR_WINDOW_SCALE * 100:0}%");
                if (radarScale != BDArmorySettings.RADAR_WINDOW_SCALE) ResizeRadarWindow(radarScale);
                float targetScale = SettingSlider("Targeting camera scale", BDArmorySettings.TARGET_WINDOW_SCALE, BDArmorySettings.TARGET_WINDOW_SCALE_MIN, BDArmorySettings.TARGET_WINDOW_SCALE_MAX, 0.01f, $"{BDArmorySettings.TARGET_WINDOW_SCALE * 100:0}%");
                if (targetScale != BDArmorySettings.TARGET_WINDOW_SCALE) ResizeTargetWindow(targetScale);
                BDArmorySettings.BW_TARGET_CAM = SettingToggle("Black and white targeting camera", BDArmorySettings.BW_TARGET_CAM);
                BDArmorySettings.TARGET_WINDOW_INVERT_MOUSE_X = SettingToggle("Invert targeting camera mouse X", BDArmorySettings.TARGET_WINDOW_INVERT_MOUSE_X);
                BDArmorySettings.TARGET_WINDOW_INVERT_MOUSE_Y = SettingToggle("Invert targeting camera mouse Y", BDArmorySettings.TARGET_WINDOW_INVERT_MOUSE_Y);
                BDArmorySettings.LOGARITHMIC_RADAR_DISPLAY = SettingToggle("Logarithmic radar and RWR range", BDArmorySettings.LOGARITHMIC_RADAR_DISPLAY);
                GUILayout.Space(4);
            }

            if (SettingsSection("Debugging", ref BDArmorySettings.DEBUG_SETTINGS_TOGGLE))
            {
                BDArmorySettings.DEBUG_LINES = SettingToggle("Draw aiming and guidance lines", BDArmorySettings.DEBUG_LINES);
                BDArmorySettings.DEBUG_TELEMETRY = SettingToggle("On-screen telemetry", BDArmorySettings.DEBUG_TELEMETRY);
                BDArmorySettings.DEBUG_WEAPONS = SettingToggle("Log: guns, rockets and lasers", BDArmorySettings.DEBUG_WEAPONS);
                BDArmorySettings.DEBUG_MISSILES = SettingToggle("Log: missiles and bombs", BDArmorySettings.DEBUG_MISSILES);
                BDArmorySettings.DEBUG_DAMAGE = SettingToggle("Log: explosions and battle damage", BDArmorySettings.DEBUG_DAMAGE);
                BDArmorySettings.DEBUG_ARMOR = SettingToggle("Log: hit points and armour", BDArmorySettings.DEBUG_ARMOR);
                BDArmorySettings.DEBUG_RADAR = SettingToggle("Log: radar and IR", BDArmorySettings.DEBUG_RADAR);
                BDArmorySettings.DEBUG_AI = SettingToggle("Log: guard mode targeting", BDArmorySettings.DEBUG_AI);
                BDArmorySettings.DEBUG_OTHER = SettingToggle("Log: everything else", BDArmorySettings.DEBUG_OTHER);
                GUILayout.Space(4);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Key Bindings", GUILayout.Height(24))) editKeys = true;
            if (GUILayout.Button("Save", GUILayout.Height(24))) SaveConfig();
            GUILayout.EndHorizontal();
        }

        /// <summary>Collapsible section header. Returns true when the section is expanded.</summary>
        bool SettingsSection(string title, ref bool expanded)
        {
            GUILayout.Space(2);
            if (GUILayout.Button((expanded ? "-  " : "+  ") + title, expanded ? BDAStyle.ButtonOn : BDGuiSkin.button, GUILayout.Height(24)))
                expanded = !expanded;
            if (expanded) GUILayout.Space(3);
            return expanded;
        }

        bool SettingToggle(string label, bool value, int indent = 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(6 + 16 * indent);
            value = GUILayout.Toggle(value, label);
            GUILayout.EndHorizontal();
            return value;
        }

        float SettingSlider(string label, float value, float min, float max, float step, string valueText, int indent = 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(6 + 16 * indent);
            GUILayout.Label(label, GUILayout.Width(settingsLabelWidth - 16 * indent));
            float result = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
            GUILayout.Label(valueText, new GUIStyle(BDGuiSkin.label) { alignment = TextAnchor.MiddleRight }, GUILayout.Width(settingsValueWidth));
            GUILayout.EndHorizontal();
            if (result == value) return value;
            return Mathf.Clamp(BDAMath.RoundToUnit(result, step), min, max);
        }

        void CloseSettingsWindow()
        {
            SaveConfig();
            windowSettingsEnabled = false;
        }

        internal static void ResizeRwrWindow(float rwrScale)
        {
            BDArmorySettings.RWR_WINDOW_SCALE = rwrScale;
            RadarWarningReceiver.RwrDisplayRect = new Rect(0, 0, RadarWarningReceiver.RwrSize * rwrScale,
              RadarWarningReceiver.RwrSize * rwrScale);
            BDArmorySetup.WindowRectRwr =
              new Rect(BDArmorySetup.WindowRectRwr.x, BDArmorySetup.WindowRectRwr.y,
                RadarWarningReceiver.RwrDisplayRect.height + RadarWarningReceiver.BorderSize,
                RadarWarningReceiver.RwrDisplayRect.height + RadarWarningReceiver.BorderSize + RadarWarningReceiver.HeaderSize);
        }

        internal static void ResizeRadarWindow(float radarScale)
        {
            BDArmorySettings.RADAR_WINDOW_SCALE = radarScale;
            VesselRadarData.RadarDisplayRect =
              new Rect(VesselRadarData.BorderSize / 2, VesselRadarData.BorderSize / 2 + VesselRadarData.HeaderSize,
                VesselRadarData.RadarScreenSize * radarScale,
                VesselRadarData.RadarScreenSize * radarScale);
            WindowRectRadar =
              new Rect(WindowRectRadar.x, WindowRectRadar.y,
                VesselRadarData.RadarDisplayRect.height + VesselRadarData.BorderSize + VesselRadarData.ControlsWidth + VesselRadarData.Gap * 3,
                VesselRadarData.RadarDisplayRect.height + VesselRadarData.BorderSize + VesselRadarData.HeaderSize);
        }

        internal static void ResizeTargetWindow(float targetScale)
        {
            BDArmorySettings.TARGET_WINDOW_SCALE = targetScale;
            ModuleTargetingCamera.ResizeTargetWindow();
        }

        void KeyBindingsPanel()
        {
            keyBindingsScroll = GUILayout.BeginScrollView(keyBindingsScroll, false, true);
            int inputID = 0;
            KeyBindingsGroup("Weapons", "WEAP_", ref inputID);
            KeyBindingsGroup("Targeting Pod", "TGP_", ref inputID);
            KeyBindingsGroup("Radar", "RADAR_", ref inputID);
            KeyBindingsGroup("Windows", "GUI_", ref inputID);
#if DEBUG
            KeyBindingsGroup("Debugging", "DEBUG_", ref inputID);
#endif
            GUILayout.EndScrollView();
            GUILayout.Space(6);
            if (!BDKeyBinder.current && GUILayout.Button("Back", GUILayout.Height(24))) editKeys = false;
        }

        void KeyBindingsGroup(string title, string prefix, ref int id)
        {
            GUILayout.Space(4);
            GUILayout.Label(title, BDAStyle.Header);
            if (inputFields == null) return;
            foreach (var field in inputFields)
                if (field.Name.StartsWith(prefix, StringComparison.Ordinal))
                    KeyBindingLine(field, id++);
        }

        void KeyBindingLine(FieldInfo field, int id)
        {
            GUILayout.BeginHorizontal(BDGuiSkin.box);
            if (BDKeyBinder.IsRecordingID(id))
            {
                if (BDKeyBinder.current.AcquireInputString(out string recordedInput))
                {
                    var orig = (BDInputInfo)field.GetValue(null);
                    field.SetValue(null, new BDInputInfo(recordedInput, orig.description));
                }
                GUILayout.Label("Press a key or button...", BDAStyle.Header);
            }
            else
            {
                var inputInfo = (BDInputInfo)field.GetValue(null);
                GUILayout.Label(inputInfo.description, GUILayout.Width(170));
                GUILayout.Label(string.IsNullOrEmpty(inputInfo.inputString) ? "-" : inputInfo.inputString, BDAStyle.DimLabel, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Set", GUILayout.Width(50))) BDKeyBinder.BindKey(id);
                if (GUILayout.Button("Clear", GUILayout.Width(50))) field.SetValue(null, new BDInputInfo(inputInfo.description));
            }
            GUILayout.EndHorizontal();
        }
        #endregion Settings window

        #endregion GUI

        void HideGameUI()
        {
            GAME_UI_ENABLED = false;
            UpdateCursorState();
        }

        void ShowGameUI()
        {
            GAME_UI_ENABLED = true;
            UpdateCursorState();
        }

        internal void OnDestroy()
        {
            if (saveWindowPosition) BDAWindowSettingsField.Save();
            if (windowSettingsEnabled) SaveConfig();

            GameEvents.onHideUI.Remove(HideGameUI);
            GameEvents.onShowUI.Remove(ShowGameUI);
            GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);
            GameEvents.OnGameSettingsApplied.Remove(SaveVolumeSettings);
            GameEvents.onVesselChange.Remove(VesselChange);
            GameEvents.onGameSceneSwitchRequested.Remove(OnGameSceneSwitchRequested);
        }

        void OnVesselGoOffRails(Vessel v)
        {
            if (BDArmorySettings.DEBUG_OTHER)
            {
                Debug.Log("[BDArmory.BDArmorySetup]: Loaded vessel: " + v.vesselName + ", Velocity: " + v.Velocity() + ", packed: " + v.packed);
                //v.SetWorldVelocity(Vector3d.zero);
            }
        }

        public void SaveVolumeSettings()
        {
            SeismicChargeFX.originalShipVolume = GameSettings.SHIP_VOLUME;
            SeismicChargeFX.originalMusicVolume = GameSettings.MUSIC_VOLUME;
            SeismicChargeFX.originalAmbienceVolume = GameSettings.AMBIENCE_VOLUME;
        }


        /// <summary>
        /// On leaving flight mode. Perform some clean-up of things that might still be active.
        /// Not everything gets cleaned up by default for some reason.
        /// </summary>
        static void DisableAllBulletsAndRockets()
        {
            if (ModuleWeapon.bulletPool != null && ModuleWeapon.bulletPool.pool != null)
                foreach (var bullet in ModuleWeapon.bulletPool.pool) if (bullet != null) bullet.SetActive(false);
            if (ModuleWeapon.shellPool != null && ModuleWeapon.shellPool.pool != null)
                foreach (var shell in ModuleWeapon.shellPool.pool) if (shell != null) shell.SetActive(false);
            if (ModuleWeapon.rocketPool != null)
                foreach (var rocketPool in ModuleWeapon.rocketPool.Values)
                {
                    if (rocketPool == null || rocketPool.pool == null) continue;
                    foreach (var rocket in rocketPool.pool) if (rocket != null) rocket.SetActive(false);
                }
        }

        void OnGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> fromTo)
        {
            if (fromTo.from == GameScenes.FLIGHT && fromTo.to != GameScenes.FLIGHT)
            {
                DisableAllBulletsAndRockets();
                ExplosionFx.DisableAllExplosionFX();
                FXEmitter.DisableAllFX();
                CMDropper.DisableAllCMs();
                BulletHitFX.DisableAllFX();
                // FIXME Add in any other things that may otherwise continue doing stuff on scene changes, e.g., various FX.
            }
        }
    }
}
