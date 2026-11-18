using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System;
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
        static GameScenes InScene = GameScenes.CREDITS; // The scene the instance was instantiated in (for duplicate detection).
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
        public static GUIStyle SelectedButtonStyle; // Uses many of the same settings as the ButtonStyle, but with the box colour scheme.
        public static GUIStyle CloseButtonStyle; // Use this for " X" buttons on windows using BDGuiSkin. Note: the extra space helps with alignment.
        public static GUIStyle CloseButtonStyleDark; // Use this for " X" buttons on windows using GUI.skin

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

        // Note: Use OnGUIWM instead for stuff in OnGUI.
        public MissileFire ActiveWeaponManager
        {
            get
            {
                var activeVessel = HighLogic.LoadedSceneIsFlight ? FlightGlobals.ActiveVessel : null;
                if (activeVessel == null || !activeVessel.loaded || activeVessel.packed) _activeWeaponManager = null;
                else if (_activeWeaponManager == null || !_activeWeaponManager.IsPrimaryWM || _activeWeaponManager.vessel != activeVessel)
                {
                    _activeWeaponManager = (activeVessel != null && activeVessel.loaded) ? activeVessel.ActiveController().WM : null;
                    if (_activeWeaponManager != null && _activeWeaponManager.vessel != activeVessel) _activeWeaponManager = null;
                    if (_activeWeaponManager != null) ConfigTextFields(_activeWeaponManager);
                }
                return _activeWeaponManager;
            }
        }
        MissileFire _activeWeaponManager;
        public MissileFire OnGUIWM; // Separate instance to make sure we only update the active WM once for OnGUI rendering. This is updated in LateUpdate.
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

        static float _SystemMaxMemory = 0;
        public static float SystemMaxMemory
        {
            get
            {
                if (_SystemMaxMemory == 0)
                {
                    _SystemMaxMemory = SystemInfo.systemMemorySize / 1024; // System Memory in GB.
                    if (BDArmorySettings.QUIT_MEMORY_USAGE_THRESHOLD > _SystemMaxMemory + 1) BDArmorySettings.QUIT_MEMORY_USAGE_THRESHOLD = _SystemMaxMemory + 1;
                }
                return _SystemMaxMemory;
            }
        }

        #region Textures

        public static string textureDir = "BDArmoryLite/Textures/";

        bool cursorOnGUI = false;
        bool drawCursor;
        Texture2D cursorTexture = GameDatabase.Instance.GetTexture(textureDir + "aimer", false);
        bool temporarilyShowMouse = false;

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

        public const float largeGreenCircleScale = 256f / 230f; // Circle is 230 pixels in diameter vs. 256 texture size

        private Texture2D gct;

        public Texture2D greenCircleTexture
        {
            get { return gct ? gct : gct = GameDatabase.Instance.GetTexture(textureDir + "greenCircle2", false); }
        }

        public const float greenCircleScale = 128f / 64f; // Circle is 64 pixels in diameter vs. 128 texture size

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

        private Texture2D gC;

        public Texture2D greenCross
        {
            get
            {
                return gC ? gC : gC = GameDatabase.Instance.GetTexture(textureDir + "greenCross", false);
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
            if (Instance != null)
            {
                if (InScene == HighLogic.LoadedScene)
                {
                    // In some scenes (such as the editors) addons get instantiated multiple times if using KSPAddon.Startup.EveryScene.
                    // This avoids the duplicate instance from messing with our syncing to KSP's settings.
                    Destroy(this);
                    return;
                }
                Destroy(Instance);
            }
            Instance = this;
            InScene = HighLogic.LoadedScene;
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
                if (assembly.FullName.Split([','])[0] == "BDArmory")
                {
                    var info = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute));
                    Version = info != null ? info.InformationalVersion : assembly.GetName().Version.ToString();
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

            ModuleManagerLoaded = ModuleManager.CheckForModuleManager();

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
            centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = new GUIStyleState { textColor = Color.white }
            };

            settingsTitleStyle = new GUIStyle(centerLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            centerLabelRed = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = new GUIStyleState { textColor = Color.red }
            };

            centerLabelOrange = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = new GUIStyleState { textColor = XKCDColors.BloodOrange }
            };

            centerLabelBlue = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = new GUIStyleState { textColor = XKCDColors.AquaBlue }
            };

            leftLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                normal = new GUIStyleState { textColor = Color.white }
            };

            leftLabelBold = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState { textColor = Color.white }
            };

            infoLinkStyle = new GUIStyle(BDGuiSkin.label)
            {
                alignment = TextAnchor.UpperLeft
            };
            infoLinkStyle.normal.textColor = Color.white;

            middleLeftLabel = new GUIStyle(leftLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };

            middleLeftLabelOrange = new GUIStyle(middleLeftLabel);
            middleLeftLabelOrange.normal.textColor = XKCDColors.BloodOrange;

            targetModeStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 9,
                normal = new GUIStyleState { textColor = Color.white }
            };

            targetModeStyleSelected = new GUIStyle(targetModeStyle);
            targetModeStyleSelected.normal.textColor = XKCDColors.BloodOrange;

            waterMarkStyle = new GUIStyle(middleLeftLabel);
            waterMarkStyle.normal.textColor = XKCDColors.LightBlueGrey;

            leftLabelRed = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                normal = new GUIStyleState { textColor = Color.red }
            };

            rightLabelRed = new GUIStyle
            {
                alignment = TextAnchor.UpperRight,
                normal = new GUIStyleState { textColor = Color.red }
            };

            leftLabelGray = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                normal = new GUIStyleState { textColor = Color.gray }
            };

            rippleSliderStyle = new GUIStyle(BDGuiSkin.horizontalSlider);
            rippleThumbStyle = new GUIStyle(BDGuiSkin.horizontalSliderThumb);
            rippleSliderStyle.fixedHeight = rippleThumbStyle.fixedHeight = 0;

            kspTitleLabel = new GUIStyle
            {
                font = BDGuiSkin.window.font,
                fontSize = BDGuiSkin.window.fontSize,
                fontStyle = BDGuiSkin.window.fontStyle,
                alignment = TextAnchor.UpperCenter,
                normal = new GUIStyleState { textColor = BDGuiSkin.window.normal.textColor }
            };

            redErrorStyle = new GUIStyle(BDGuiSkin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 24,
                alignment = TextAnchor.UpperCenter
            };
            redErrorStyle.normal.textColor = Color.red;

            redErrorShadowStyle = new GUIStyle(redErrorStyle);
            redErrorShadowStyle.normal.textColor = new Color(0, 0, 0, 0.75f);

            textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                alignment = TextAnchor.MiddleRight
            };

            ButtonStyle = new GUIStyle(BDGuiSkin.button);
            SelectedButtonStyle = new GUIStyle(BDGuiSkin.box) // Copy most of the ButtonStyle, but keep the box style backgrounds and font styles.
            {
                alignment = ButtonStyle.alignment,
                // border = ButtonStyle.border,
                clipping = ButtonStyle.clipping,
                contentOffset = ButtonStyle.contentOffset,
                fixedHeight = ButtonStyle.fixedHeight,
                fixedWidth = ButtonStyle.fixedWidth,
                fontSize = ButtonStyle.fontSize,
                imagePosition = ButtonStyle.imagePosition,
                margin = ButtonStyle.margin,
                overflow = ButtonStyle.overflow,
                padding = ButtonStyle.padding,
                richText = ButtonStyle.richText,
                stretchHeight = ButtonStyle.stretchHeight,
                stretchWidth = ButtonStyle.stretchWidth,
                wordWrap = ButtonStyle.wordWrap
            };
            CloseButtonStyle = new GUIStyle(ButtonStyle) { alignment = TextAnchor.MiddleCenter };
            CloseButtonStyle.hover.textColor = Color.red;
            CloseButtonStyleDark = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, padding = new(0, 0, 0, 0) };
            CloseButtonStyleDark.hover.textColor = Color.red;

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
            else { ToggleWindowSettings(); }
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
            if (!scalingUI) BDArmorySettings.PREVIOUS_UI_SCALE = BDArmorySettings.UI_SCALE_ACTUAL;

            if (HighLogic.LoadedSceneIsFlight)
            {
                if (missileWarning && Time.time - missileWarningTime > 1.5f) missileWarning = false;
                if (BDInputUtils.GetKeyDown(BDInputSettingsFields.GUI_WM_TOGGLE)) windowBDAToolBarEnabled = !windowBDAToolBarEnabled;
#if DEBUG
                if (BDInputUtils.GetKeyDown(BDInputSettingsFields.DEBUG_CLEAR_DEV_CONSOLE)) Debug.ClearDeveloperConsole();
#endif
                if (temporarilyShowMouse != (temporarilyShowMouse = BDInputUtils.GetKey(BDInputSettingsFields.TEMPORARILY_SHOW_MOUSE))) UpdateCursorState();
                if (cursorOnGUI && !GUIUtils.CheckMouseIsOnGui()) UpdateCursorState();
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
            if (temporarilyShowMouse)
            {
                drawCursor = false;
                Cursor.visible = true;
                return;
            }
            var weaponManager = ActiveWeaponManager;
            if (weaponManager == null)
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
                cursorOnGUI = GUIUtils.CheckMouseIsOnGui();
                if (!MapView.MapIsEnabled && !cursorOnGUI && !PauseMenu.isOpen)
                {
                    if (weaponManager.selectedWeapon != null && weaponManager.weaponIndex > 0 &&
                        !weaponManager.guardMode)
                    {
                        if (weaponManager.selectedWeapon.GetWeaponClass() == WeaponClasses.Gun ||
                            weaponManager.selectedWeapon.GetWeaponClass() == WeaponClasses.Rocket ||
                            weaponManager.selectedWeapon.GetWeaponClass() == WeaponClasses.DefenseLaser)
                        {
                            ModuleWeapon mw = weaponManager.selectedWeapon.GetWeaponModule();
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

                    if (!weaponManager.guardMode && weaponManager.isHMDEnabled)
                    {
                        Cursor.visible = false;
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
                Instance.UpdateCursorState();
            }
        }

        public void ConfigTextFields(MissileFire weaponManager)
        {
            textNumFields = new Dictionary<string, NumericInputField> {
                { "rippleRPM", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.rippleRPM, 0, 1600) },
                { "targetScanInterval", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetScanInterval, 0.5f, 60f) },
                { "fireBurstLength", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.fireBurstLength, 0, 10) },
                { "AutoFireCosAngleAdjustment", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.AutoFireCosAngleAdjustment, 0, 4) },
                { "guardAngle", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.guardAngle, 10, 360) },
                { "guardRange", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.guardRange, 100, BDArmorySettings.MAX_GUARD_VISUAL_RANGE) },
                { "gunRange", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.gunRange, 0, weaponManager.maxGunRange) },
                { "multiTargetNum", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.multiTargetNum, 1, 10) },
                { "multiMissileTgtNum", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.multiMissileTgtNum, 1, 10) },
                { "maxMissilesOnTarget", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.maxMissilesOnTarget, 1, MissileFire.maxAllowableMissilesOnTarget) },

                { "targetBias", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetBias, 0, 10) },
                { "targetWeightRange", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightRange, -10, 10) },
                { "targetWeightAirPreference", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightAirPreference, -10, 10) },
                { "targetWeightATA", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightATA, -10, 10) },
                { "targetWeightAoD", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightAoD, -10, 10) },
                { "targetWeightAccel", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightAccel,-10, 10) },
                { "targetWeightClosureTime", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightClosureTime, -10, 10) },
                { "targetWeightWeaponNumber", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightWeaponNumber, -10, 10) },
                { "targetWeightMass", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightMass,-10, 10) },
                { "targetWeightDamage", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightDamage,-10, 10) },
                { "targetWeightFriendliesEngaging", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightFriendliesEngaging, -10, 10) },
                { "targetWeightThreat", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightThreat, -10, 10) },
                { "targetWeightProtectTeammate", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightProtectTeammate, -10, 10) },
                { "targetWeightProtectVIP", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightProtectVIP, -10, 10) },
                { "targetWeightAttackVIP", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightAttackVIP, -10, 10) },
                { "targetWeightUncontrolled", gameObject.AddComponent<NumericInputField>().Initialise(0, weaponManager.targetWeightUncontrolled, -10, 10) },
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

        void LateUpdate()
        {
            OnGUIWM = ActiveWeaponManager;
        }
        void OnGUI()
        {
            if (!GAME_UI_ENABLED) return;
            GUI.skin = BDGuiSkin;
            if (!stylesConfigured) ConfigureStyles();
            if (windowSettingsEnabled)
            {
                var guiMatrix = GUI.matrix; // Store and restore the GUI.matrix so we can apply a different scaling for the WM window.
                if (scalingUI && Mouse.Left.GetButtonUp()) scalingUI = false; // Don't rescale the settings window until the mouse is released otherwise it messes with the slider.
                if (!scalingUI) oldUIScale = BDArmorySettings.UI_SCALE_ACTUAL;
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
            if (BDArmorySettings.UI_SCALE_ACTUAL != 1) GUIUtility.ScaleAroundPivot(BDArmorySettings.UI_SCALE_ACTUAL * Vector2.one, WindowRectToolbar.position);
            WindowRectToolbar = GUI.Window(321, WindowRectToolbar, WindowBDAToolbar, "", BDGuiSkin.window);//"BDA Weapon Manager"
            SetGUIOpacity(false);
            GUIUtils.UseMouseEventInRect(WindowRectToolbar);
            if (showWindowGPS && OnGUIWM)
            {
                //gpsWindowRect = GUI.Window(424333, gpsWindowRect, GPSWindow, "", GUI.skin.box);
                GUIUtils.UseMouseEventInRect(WindowRectGps);
                using (var coord = BDATargetManager.GPSTargetList(OnGUIWM.Team).GetEnumerator())
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
            GUIStyle iStyle = infoLinkEnabled ? BDAStyle.ButtonOn : BDGuiSkin.button;
            if (GUI.Button(new Rect(columnWidth - _windowMargin - ++buttonNumber * _buttonSize, _windowMargin, _buttonSize, _buttonSize), "i", iStyle))
            {
                infoLinkEnabled = !infoLinkEnabled;
            }

            //numeric fields
            GUIStyle nStyle = NumFieldsEnabled ? BDAStyle.ButtonOn : BDGuiSkin.button;
            if (GUI.Button(new Rect(columnWidth - _windowMargin - ++buttonNumber * _buttonSize, _windowMargin, _buttonSize, _buttonSize), "#", nStyle))
            {
                NumFieldsEnabled = !NumFieldsEnabled;
                if (!NumFieldsEnabled)
                {
                    // Try to parse all the fields immediately so that they're up to date.
                    foreach (var field in textNumFields.Keys)
                    { textNumFields[field].tryParseValueNow(); }
                    if (OnGUIWM != null)
                    {
                        foreach (var field in textNumFields.Keys)
                        {
                            try
                            {
                                var fieldInfo = typeof(MissileFire).GetField(field);
                                if (fieldInfo != null)
                                { fieldInfo.SetValue(OnGUIWM, Convert.ChangeType(textNumFields[field].CurrentValue, fieldInfo.FieldType)); }
                                else // Check if it's a property instead of a field.
                                {
                                    var propInfo = typeof(MissileFire).GetProperty(field);
                                    propInfo.SetValue(OnGUIWM, Convert.ChangeType(textNumFields[field].CurrentValue, propInfo.PropertyType));
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
                    if (OnGUIWM != null)
                    {
                        foreach (var field in textNumFields.Keys)
                        {
                            try
                            {
                                var fieldInfo = typeof(MissileFire).GetField(field);
                                if (fieldInfo != null)
                                { textNumFields[field].SetCurrentValue(Convert.ToDouble(fieldInfo.GetValue(OnGUIWM))); }
                                else // Check if it's a property instead of a field.
                                {
                                    var propInfo = typeof(MissileFire).GetProperty(field);
                                    textNumFields[field].SetCurrentValue(Convert.ToDouble(propInfo.GetValue(OnGUIWM)));
                                }
                            }
                            catch (Exception e) { Debug.LogError($"[BDArmory.BDArmorySetup]: Failed to set current value of {field}: " + e.Message); }
                        }
                    }
                }
            }

            if (OnGUIWM != null)
            {
                //MINIMIZE BUTTON
                toolMinimized = GUI.Toggle(new Rect(_windowMargin, _windowMargin, _buttonSize, _buttonSize), toolMinimized, "_", toolMinimized ? BDAStyle.ButtonOn : ButtonStyle);

                GUIStyle armedLabelStyle;
                Rect armedRect = new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight);
                if (OnGUIWM.guardMode)
                {
                    if (GUI.Button(armedRect, "- " + StringUtils.Localize("#LOC_BDArmory_WMWindow_GuardModebtn") + " -", BDAStyle.ButtonOn))//Guard Mode
                    {
                        showGuardMenu = true;
                    }
                }
                else
                {
                    string armedText = StringUtils.Localize("#LOC_BDArmory_WMWindow_ArmedText");//"Trigger is "
                    if (OnGUIWM.isArmed)
                    {
                        armedText += StringUtils.Localize("#LOC_BDArmory_WMWindow_ArmedText_ARMED");//"ARMED."
                        armedLabelStyle = BDAStyle.ButtonOn;
                    }
                    else
                    {
                        armedText += StringUtils.Localize("#LOC_BDArmory_WMWindow_ArmedText_DisArmed");//"disarmed."
                        armedLabelStyle = ButtonStyle;
                    }
                    if (GUI.Button(armedRect, armedText, armedLabelStyle))
                    {
                        OnGUIWM.ToggleArm();
                    }
                }

                GUIStyle teamButtonStyle = SelectedButtonStyle;
                string teamText = StringUtils.Localize("#LOC_BDArmory_WMWindow_TeamText") + $": {OnGUIWM.Team.Name + (OnGUIWM.Team.Neutral ? (OnGUIWM.Team.Name != "Neutral" ? "(N)" : "") : "")}";//Team
                if (GUI.Button(new Rect(leftIndent + (contentWidth / 2), contentTop + (line * entryHeight), contentWidth / 2, entryHeight), teamText, teamButtonStyle))
                {
                    if (Event.current.button == 1)
                    {
                        BDTeamSelector.Instance.Open(OnGUIWM, new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
                    }
                    else
                    {
                        OnGUIWM.NextTeam();
                    }
                }
                line++;
                line += 0.25f;
                string weaponName = OnGUIWM.selectedWeaponString;
                string selectionText = StringUtils.Localize("#LOC_BDArmory_WMWindow_selectionText", weaponName);//Weapon: <<1>>
                GUI.Label(new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth, entryHeight * 1.25f), selectionText, SelectedButtonStyle);
                line += 1.25f;
                line += 0.1f;
                //if weapon can ripple, show option and slider.
                if (OnGUIWM.hasLoadedRippleData && OnGUIWM.canRipple)
                {
                    if (OnGUIWM.selectedWeapon != null && OnGUIWM.weaponIndex > 0 &&
                        (OnGUIWM.selectedWeapon.GetWeaponClass() == WeaponClasses.Gun
                        || OnGUIWM.selectedWeapon.GetWeaponClass() == WeaponClasses.Rocket
                        || OnGUIWM.selectedWeapon.GetWeaponClass() == WeaponClasses.DefenseLaser)) //remove rocket ripple slider - moved to editor
                    {
                        string rippleText = OnGUIWM.rippleFire
                            ? StringUtils.Localize("#LOC_BDArmory_WMWindow_rippleText1", OnGUIWM.gunRippleRpm.ToString("0"))//"Barrage: " +  + " RPM"
                            : StringUtils.Localize("#LOC_BDArmory_WMWindow_rippleText2");//"Salvo"
                        GUIStyle rippleStyle = OnGUIWM.rippleFire
                            ? BDAStyle.ButtonOn : ButtonStyle;
                        if (
                            GUI.Button(
                                new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight * 1.25f),
                                rippleText, rippleStyle))
                        {
                            OnGUIWM.ToggleRippleFire();
                        }
                        if (OnGUIWM.rippleFire)
                        {
                            GUI.Label(new Rect(leftIndent + contentWidth / 2 + _windowMargin, contentTop + line * entryHeight, contentWidth / 4 - _windowMargin, entryHeight * 1.25f), $"{StringUtils.Localize("#LOC_BDArmory_WMWindow_barrageStagger")}: {(OnGUIWM.barrageStagger > 0 ? OnGUIWM.barrageStagger : 1):G1}");
                            OnGUIWM.barrageStagger = BDAMath.RoundToUnit(GUI.HorizontalSlider(new Rect(leftIndent + 3 * contentWidth / 4, contentTop + (line + 0.25f) * entryHeight, contentWidth / 4, entryHeight), OnGUIWM.barrageStagger, 0f, 0.1f), 0.01f);
                        }

                        rippleHeight = Mathf.Lerp(rippleHeight, 1.25f, 0.15f);
                    }
                    else
                    {
                        string rippleText = OnGUIWM.rippleFire
                            ? StringUtils.Localize("#LOC_BDArmory_WMWindow_rippleText3", OnGUIWM.rippleRPM.ToString("0"))//"Ripple: " +  + " RPM"
                            : StringUtils.Localize("#LOC_BDArmory_WMWindow_rippleText4");//"Ripple: OFF"
                        GUIStyle rippleStyle = OnGUIWM.rippleFire
                            ? BDAStyle.ButtonOn : ButtonStyle;
                        if (
                            GUI.Button(
                                new Rect(leftIndent, contentTop + (line * entryHeight), contentWidth / 2, entryHeight * 1.25f),
                                rippleText, rippleStyle))
                        {
                            OnGUIWM.ToggleRippleFire();
                        }
                        if (OnGUIWM.rippleFire)
                        {
                            if (!NumFieldsEnabled)
                            {
                                OnGUIWM.rippleRPM = GUI.HorizontalSlider(new Rect(leftIndent + (contentWidth / 2) + 2, contentTop + (line * entryHeight) + 6.5f, (contentWidth / 2) - 2, 12),
                                    OnGUIWM.rippleRPM, 100, 1600, rippleSliderStyle, rippleThumbStyle);
                            }
                            else
                            {
                                var field = textNumFields["rippleRPM"];
                                field.TryParseValue(GUI.TextField(new Rect(leftIndent + (contentWidth / 2) + 2, contentTop + (line * entryHeight) + 6.5f, (contentWidth / 2) - 2, entryHeight),
                                    field.possibleValue, 4, field.style));
                                OnGUIWM.rippleRPM = (float)field.CurrentValue;
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
                            showWeaponList, StringUtils.Localize("#LOC_BDArmory_WMWindow_ListWeapons"), showWeaponList ? BDAStyle.ButtonOn : ButtonStyle);//"Weapons"
                    showGuardMenu =
                        GUI.Toggle(
                            new Rect(leftIndent + (contentWidth / 4), contentTop + (line * entryHeight), contentWidth / 4,
                                entryHeight), showGuardMenu, StringUtils.Localize("#LOC_BDArmory_WMWindow_GuardMenu"),//"Guard Menu"
                            showGuardMenu ? BDAStyle.ButtonOn : ButtonStyle);
                    showPriorities =
                        GUI.Toggle(new Rect(leftIndent + (2 * contentWidth / 4), contentTop + (line * entryHeight), contentWidth / 4,
                             entryHeight), showPriorities, StringUtils.Localize("#LOC_BDArmory_WMWindow_TargetPriority"),//"Tgt priority"
                            showPriorities ? BDAStyle.ButtonOn : ButtonStyle);
                    showModules =
                        GUI.Toggle(
                            new Rect(leftIndent + (3 * contentWidth / 4), contentTop + (line * entryHeight), contentWidth / 4,
                                entryHeight), showModules, StringUtils.Localize("#LOC_BDArmory_WMWindow_ModulesToggle"),//"Modules"
                            showModules ? BDAStyle.ButtonOn : ButtonStyle);
                    line++;
                }

                float weaponLines = 0;
                if (showWeaponList && !toolMinimized)
                {
                    line += 0.25f;
                    Rect weaponListGroupRect = new Rect(5, contentTop + (line * entryHeight), columnWidth - 10, weaponsHeight * entryHeight);
                    GUI.BeginGroup(weaponListGroupRect, GUIContent.none, SelectedButtonStyle); //darker box
                    weaponLines += 0.1f;

                    for (int i = 0; i < OnGUIWM.weaponArray.Length; i++)
                    {
                        GUIStyle wpnListStyle;
                        GUIStyle tgtStyle;
                        if (i == OnGUIWM.weaponIndex)
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
                        if (OnGUIWM.weaponArray[i] != null)
                        {
                            label = OnGUIWM.weaponArray[i].GetShortName();
                            subLabel = OnGUIWM.weaponArray[i].GetSubLabel();
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
                            OnGUIWM.CycleWeapon(i);
                        }

                        if (i < OnGUIWM.weaponArray.Length - 1)
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
                    GUI.BeginGroup(new Rect(5, contentTop + line * entryHeight, columnWidth - 10, guardHeight * entryHeight), GUIContent.none, SelectedButtonStyle);
                    guardLines += 0.1f;

                    string guardButtonLabel = StringUtils.Localize("#LOC_BDArmory_WMWindow_GuardMode", OnGUIWM.guardMode ? StringUtils.Localize("#LOC_BDArmory_Generic_On") : StringUtils.Localize("#LOC_BDArmory_Generic_Off"));//"Guard Mode " + "ON""Off"
                    if (GUI.Button(ButtonRect(guardLines), guardButtonLabel, OnGUIWM.guardMode ? BDAStyle.ButtonOn : ButtonStyle))
                    {
                        OnGUIWM.ToggleGuardMode();
                    }
                    guardLines += 0.25f;

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_FiringInterval"), leftLabel);//"Firing Interval"                 
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetScanInterval = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), OnGUIWM.targetScanInterval, 0.5f, 60f), 0.5f);
                        GUI.Label(RightLabelRect(guardLines), OnGUIWM.targetScanInterval.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetScanInterval"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetScanInterval = (float)field.CurrentValue;
                    }

                    string burstLabel = StringUtils.Localize("#LOC_BDArmory_WMWindow_BurstLength");//"Burst Length"
                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), burstLabel, leftLabel);
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.fireBurstLength = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), OnGUIWM.fireBurstLength, 0, 10), 0.05f);
                        GUI.Label(RightLabelRect(guardLines), OnGUIWM.fireBurstLength.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["fireBurstLength"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.fireBurstLength = (float)field.CurrentValue;
                    }

                    // extension for feature_engagementenvelope: set the firing accuracy tolarance
                    var oldAutoFireCosAngleAdjustment = OnGUIWM.AutoFireCosAngleAdjustment;
                    string accuracyLabel = StringUtils.Localize("#LOC_BDArmory_WMWindow_FiringTolerance");//"Firing Angle"
                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), accuracyLabel, leftLabel);
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.AutoFireCosAngleAdjustment = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), OnGUIWM.AutoFireCosAngleAdjustment, 0, 4), 0.05f);
                        GUI.Label(RightLabelRect(guardLines), OnGUIWM.AutoFireCosAngleAdjustment.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["AutoFireCosAngleAdjustment"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.AutoFireCosAngleAdjustment = (float)field.CurrentValue;
                    }
                    if (OnGUIWM.AutoFireCosAngleAdjustment != oldAutoFireCosAngleAdjustment)
                        OnGUIWM.OnAFCAAUpdated(null, null);

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_FieldofView"),//"Field of View"
                        leftLabel);
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.guardAngle = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), OnGUIWM.guardAngle, 10, 360), 0.1f);
                        GUI.Label(RightLabelRect(guardLines), OnGUIWM.guardAngle.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["guardAngle"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.guardAngle = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_VisualRange"), leftLabel);//"Visual Range"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.guardRange = GUIUtils.HorizontalSemiLogSlider(SliderRect(guardLines, guardLabelWidth), OnGUIWM.guardRange, 100, BDArmorySettings.MAX_GUARD_VISUAL_RANGE, 1, false, false, ref cacheGuardRange);
                        GUI.Label(RightLabelRect(guardLines), OnGUIWM.guardRange < 1000 ? $"{OnGUIWM.guardRange:G4}m" : $"{OnGUIWM.guardRange / 1000:G4}km", leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["guardRange"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 8, field.style));
                        OnGUIWM.guardRange = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_GunsRange"), leftLabel);//"Guns Range"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.gunRange = GUIUtils.HorizontalPowerSlider(SliderRect(guardLines, guardLabelWidth), OnGUIWM.gunRange, 0, OnGUIWM.maxGunRange, 2, 2, ref cacheGunRange);
                        GUI.Label(RightLabelRect(guardLines), OnGUIWM.gunRange < 1000 ? $"{OnGUIWM.gunRange:G4}m" : $"{OnGUIWM.gunRange / 1000:G4}km", leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["gunRange"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 8, field.style));
                        OnGUIWM.gunRange = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_MultiTargetNum"), leftLabel);//"Max Turret targets "
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.multiTargetNum = Mathf.Round(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), OnGUIWM.multiTargetNum, 1, 10));
                        GUI.Label(RightLabelRect(guardLines), OnGUIWM.multiTargetNum.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["multiTargetNum"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 2, field.style));
                        OnGUIWM.multiTargetNum = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_MultiMissileNum"), leftLabel);//"Max Turret targets "
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.multiMissileTgtNum = Mathf.Round(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), OnGUIWM.multiMissileTgtNum, 1, 10));
                        GUI.Label(RightLabelRect(guardLines), OnGUIWM.multiMissileTgtNum.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["multiMissileTgtNum"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 2, field.style));
                        OnGUIWM.multiMissileTgtNum = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++guardLines, guardLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_MissilesTgt"), leftLabel);//"Missiles/Tgt"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.maxMissilesOnTarget = Mathf.Round(GUI.HorizontalSlider(SliderRect(guardLines, guardLabelWidth), OnGUIWM.maxMissilesOnTarget, 1, MissileFire.maxAllowableMissilesOnTarget));
                        GUI.Label(RightLabelRect(guardLines), OnGUIWM.maxMissilesOnTarget.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["maxMissilesOnTarget"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(guardLines, guardLabelWidth), field.possibleValue, 2, field.style));
                        OnGUIWM.maxMissilesOnTarget = (float)field.CurrentValue;
                    }

                    showTargetOptions = GUI.Toggle(ButtonRect(++guardLines), showTargetOptions, StringUtils.Localize("#LOC_BDArmory_Settings_Adv_Targeting"), showTargetOptions ? BDAStyle.ButtonOn : ButtonStyle);//"Advanced Targeting"
                    guardLines += 0.25f;

                    float TargetLines = 0;
                    if (showTargetOptions && showGuardMenu && !toolMinimized)
                    {
                        contentWidth = columnWidth - 30;
                        guardLines += 0.25f;
                        GUI.BeginGroup(new Rect(10, contentTop + (guardLines * entryHeight), contentWidth, (TargetingHeight + 0.25f) * entryHeight), GUIContent.none, SelectedButtonStyle);
                        TargetLines += 0.25f;
                        string CoMlabel = StringUtils.Localize("#LOC_BDArmory_TargetCOM", (OnGUIWM.targetCoM ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Air; True, False
                        if (GUI.Button(new Rect(leftIndent, TargetLines * entryHeight, contentWidth - 2 * leftIndent, entryHeight), CoMlabel, OnGUIWM.targetCoM ? BDAStyle.ButtonOn : ButtonStyle))
                        {
                            OnGUIWM.targetCoM = !OnGUIWM.targetCoM;
                            OnGUIWM.StartGuardTurretFiring(); //reset weapon targeting assignments
                            if (OnGUIWM.targetCoM)
                            {
                                OnGUIWM.targetCommand = false;
                                OnGUIWM.targetEngine = false;
                                OnGUIWM.targetWeapon = false;
                                OnGUIWM.targetMass = false;
                                OnGUIWM.targetRandom = false;
                            }
                            if (!OnGUIWM.targetCoM && (!OnGUIWM.targetWeapon && !OnGUIWM.targetEngine && !OnGUIWM.targetCommand && !OnGUIWM.targetMass && !OnGUIWM.targetRandom))
                            {
                                OnGUIWM.targetRandom = true;
                            }
                        }
                        TargetLines += 1.1f;
                        string Commandlabel = StringUtils.Localize("#LOC_BDArmory_Command", (OnGUIWM.targetCommand ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Air; True, False
                        if (GUI.Button(new Rect(leftIndent, TargetLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Commandlabel, OnGUIWM.targetCommand ? BDAStyle.ButtonOn : ButtonStyle))
                        {
                            OnGUIWM.targetCommand = !OnGUIWM.targetCommand;
                            OnGUIWM.StartGuardTurretFiring();
                            if (OnGUIWM.targetCommand)
                            {
                                OnGUIWM.targetCoM = false;
                            }
                            if (!OnGUIWM.targetCoM && (!OnGUIWM.targetWeapon && !OnGUIWM.targetEngine && !OnGUIWM.targetCommand && !OnGUIWM.targetMass && !OnGUIWM.targetRandom))
                            {
                                OnGUIWM.targetCoM = true;
                            }
                        }
                        string Engineslabel = StringUtils.Localize("#LOC_BDArmory_Engines", (OnGUIWM.targetEngine ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Missile; True, False
                        if (GUI.Button(new Rect(leftIndent + (contentWidth - 2 * leftIndent) / 2, TargetLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Engineslabel, OnGUIWM.targetEngine ? BDAStyle.ButtonOn : ButtonStyle))
                        {
                            OnGUIWM.targetEngine = !OnGUIWM.targetEngine;
                            OnGUIWM.StartGuardTurretFiring();
                            if (OnGUIWM.targetEngine)
                            {
                                OnGUIWM.targetCoM = false;
                            }
                            if (!OnGUIWM.targetCoM && (!OnGUIWM.targetWeapon && !OnGUIWM.targetEngine && !OnGUIWM.targetCommand && !OnGUIWM.targetMass && !OnGUIWM.targetRandom))
                            {
                                OnGUIWM.targetCoM = true;
                            }
                        }
                        TargetLines += 1.1f;
                        string Weaponslabel = StringUtils.Localize("#LOC_BDArmory_Weapons", (OnGUIWM.targetWeapon ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Surface; True, False
                        if (GUI.Button(new Rect(leftIndent, TargetLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Weaponslabel, OnGUIWM.targetWeapon ? BDAStyle.ButtonOn : ButtonStyle))
                        {
                            OnGUIWM.targetWeapon = !OnGUIWM.targetWeapon;
                            OnGUIWM.StartGuardTurretFiring();
                            if (OnGUIWM.targetWeapon)
                            {
                                OnGUIWM.targetCoM = false;
                            }
                            if (!OnGUIWM.targetCoM && (!OnGUIWM.targetWeapon && !OnGUIWM.targetEngine && !OnGUIWM.targetCommand && !OnGUIWM.targetMass && !OnGUIWM.targetRandom))
                            {
                                OnGUIWM.targetCoM = true;
                            }
                        }
                        string Masslabel = StringUtils.Localize("#LOC_BDArmory_Mass", (OnGUIWM.targetMass ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage SLW; True, False
                        if (GUI.Button(new Rect(leftIndent + (contentWidth - 2 * leftIndent) / 2, TargetLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Masslabel, OnGUIWM.targetMass ? BDAStyle.ButtonOn : ButtonStyle))
                        {
                            OnGUIWM.targetMass = !OnGUIWM.targetMass;
                            OnGUIWM.StartGuardTurretFiring();
                            if (OnGUIWM.targetMass)
                            {
                                OnGUIWM.targetCoM = false;
                            }
                            if (!OnGUIWM.targetCoM && (!OnGUIWM.targetWeapon && !OnGUIWM.targetEngine && !OnGUIWM.targetCommand && !OnGUIWM.targetMass && !OnGUIWM.targetRandom))
                            {
                                OnGUIWM.targetCoM = true;
                            }
                        }
                        TargetLines += 1.1f;
                        string Randomlabel = StringUtils.Localize("#LOC_BDArmory_Random", (OnGUIWM.targetRandom ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Surface; True, False
                        if (GUI.Button(new Rect(leftIndent, TargetLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Randomlabel, OnGUIWM.targetRandom ? BDAStyle.ButtonOn : ButtonStyle))
                        {
                            OnGUIWM.targetRandom = !OnGUIWM.targetRandom;
                            OnGUIWM.StartGuardTurretFiring();
                            if (OnGUIWM.targetRandom)
                            {
                                OnGUIWM.targetCoM = false;
                            }
                            if (!OnGUIWM.targetCoM && (!OnGUIWM.targetWeapon && !OnGUIWM.targetEngine && !OnGUIWM.targetCommand && !OnGUIWM.targetMass && !OnGUIWM.targetRandom))
                            {
                                OnGUIWM.targetCoM = true;
                            }
                        }
                        TargetLines += 1.1f;
                        OnGUIWM.targetingString = (OnGUIWM.targetCoM ? StringUtils.Localize("#LOC_BDArmory_TargetCOM") + "; " : "")
                            + (OnGUIWM.targetMass ? StringUtils.Localize("#LOC_BDArmory_Mass") + "; " : "")
                            + (OnGUIWM.targetCommand ? StringUtils.Localize("#LOC_BDArmory_Command") + "; " : "")
                            + (OnGUIWM.targetEngine ? StringUtils.Localize("#LOC_BDArmory_Engines") + "; " : "")
                            + (OnGUIWM.targetWeapon ? StringUtils.Localize("#LOC_BDArmory_Weapons") + "; " : "")
                            + (OnGUIWM.targetWeapon ? StringUtils.Localize("#LOC_BDArmory_Random") + "; " : "");
                        GUI.EndGroup();
                    }
                    TargetingHeight = Mathf.Lerp(TargetingHeight, TargetLines, 0.15f);
                    guardLines += TargetingHeight;

                    showEngageList = GUI.Toggle(ButtonRect(++guardLines), showEngageList, showEngageList ? StringUtils.Localize("#LOC_BDArmory_DisableEngageOptions") : StringUtils.Localize("#LOC_BDArmory_EnableEngageOptions"), showEngageList ? BDAStyle.ButtonOn : ButtonStyle);//"Enable/Disable Engagement options"
                    guardLines += 0.25f;

                    float EngageLines = 0;
                    if (showEngageList && showGuardMenu && !toolMinimized)
                    {
                        contentWidth = columnWidth - 30;
                        guardLines += 0.25f;
                        GUI.BeginGroup(new Rect(10, contentTop + guardLines * entryHeight, contentWidth, (EngageHeight + 0.25f) * entryHeight), GUIContent.none, SelectedButtonStyle);
                        EngageLines += 0.25f;

                        string Airlabel = StringUtils.Localize("#LOC_BDArmory_EngageAir", (OnGUIWM.engageAir ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Air; True, False
                        if (GUI.Button(new Rect(leftIndent, EngageLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Airlabel, OnGUIWM.engageAir ? BDAStyle.ButtonOn : ButtonStyle))
                        {
                            OnGUIWM.ToggleEngageAir();
                        }
                        string Missilelabel = StringUtils.Localize("#LOC_BDArmory_EngageMissile", (OnGUIWM.engageMissile ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Missile; True, False
                        if (GUI.Button(new Rect(leftIndent + (contentWidth - 2 * leftIndent) / 2, EngageLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Missilelabel, OnGUIWM.engageMissile ? BDAStyle.ButtonOn : ButtonStyle))
                        {
                            OnGUIWM.ToggleEngageMissile();
                        }
                        EngageLines += 1.1f;
                        string Srflabel = StringUtils.Localize("#LOC_BDArmory_EngageSurface", (OnGUIWM.engageSrf ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage Surface; True, False
                        if (GUI.Button(new Rect(leftIndent, EngageLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), Srflabel, OnGUIWM.engageSrf ? BDAStyle.ButtonOn : ButtonStyle))
                        {
                            OnGUIWM.ToggleEngageSrf();
                        }

                        string SLWlabel = StringUtils.Localize("#LOC_BDArmory_EngageSLW", (OnGUIWM.engageSLW ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true")));//"Engage SLW; True, False
                        if (GUI.Button(new Rect(leftIndent + (contentWidth - 2 * leftIndent) / 2, EngageLines * entryHeight, (contentWidth - 2 * leftIndent) / 2, entryHeight), SLWlabel, OnGUIWM.engageSLW ? BDAStyle.ButtonOn : ButtonStyle))
                        {
                            OnGUIWM.ToggleEngageSLW();
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
                    GUI.BeginGroup(new Rect(5, contentTop + line * entryHeight, columnWidth - 10, priorityheight * entryHeight), GUIContent.none, SelectedButtonStyle);
                    priorityLines += 0.1f;

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetBias"), leftLabel);//"current target bias"                 
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetBias = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetBias, 0, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetBias.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetBias"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetBias = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetProximity"), leftLabel); //target proximity"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightRange = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightRange, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightRange.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightRange"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightRange = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetPreference"), leftLabel); //target Air preference"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightAirPreference = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightAirPreference, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightAirPreference.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightAirPreference"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightAirPreference = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAngletoTarget"), leftLabel); //target angle"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightATA = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightATA, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightATA.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightATA"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightATA = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAngleDist"), leftLabel); //Angle over Distance"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightAoD = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightAoD, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightAoD.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightAoD"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightAoD = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAccel"), leftLabel); //target accel"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightAccel = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightAccel, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightAccel.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightAccel"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightAccel = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetClosingTime"), leftLabel); //target closing time"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightClosureTime = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightClosureTime, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightClosureTime.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightClosureTime"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightClosureTime = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetgunNumber"), leftLabel); //target weapon num."
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightWeaponNumber = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightWeaponNumber, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightWeaponNumber.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightWeaponNumber"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightWeaponNumber = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetMass"), leftLabel); //target mass"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightMass = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightMass, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightMass.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightMass"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightMass = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_TargetPriority_TargetDmg"), leftLabel); //target Damage"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightDamage = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightDamage, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightDamage.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightDamage"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightDamage = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetAllies"), leftLabel); //target mass"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightFriendliesEngaging = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightFriendliesEngaging, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightFriendliesEngaging.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightFriendliesEngaging"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightFriendliesEngaging = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetThreat"), leftLabel); //target proximity"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightThreat = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightThreat, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightThreat.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightThreat"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightThreat = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_defendTeammate"), leftLabel); //defend teammate"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightProtectTeammate = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightProtectTeammate, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightProtectTeammate.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightProtectTeammate"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightProtectTeammate = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_defendVIP"), leftLabel); //target proximity"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightProtectVIP = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightProtectVIP, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightProtectVIP.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightProtectVIP"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightProtectVIP = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetVIP"), leftLabel); //target proximity"
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightAttackVIP = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightAttackVIP, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightAttackVIP.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightAttackVIP"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightAttackVIP = (float)field.CurrentValue;
                    }

                    GUI.Label(LabelRect(++priorityLines, priorityLabelWidth), StringUtils.Localize("#LOC_BDArmory_WMWindow_targetUncontrolled"), leftLabel); //target controllable
                    if (!NumFieldsEnabled)
                    {
                        OnGUIWM.targetWeightUncontrolled = BDAMath.RoundToUnit(GUI.HorizontalSlider(SliderRect(priorityLines, priorityLabelWidth), OnGUIWM.targetWeightUncontrolled, -10, 10), 0.1f);
                        GUI.Label(RightLabelRect(priorityLines), OnGUIWM.targetWeightUncontrolled.ToString(), leftLabel);
                    }
                    else
                    {
                        var field = textNumFields["targetWeightUncontrolled"];
                        field.TryParseValue(GUI.TextField(InputFieldRect(priorityLines, priorityLabelWidth), field.possibleValue, 4, field.style));
                        OnGUIWM.targetWeightUncontrolled = (float)field.CurrentValue;
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
                        GUIContent.none, SelectedButtonStyle);
                    moduleLines += 0.1f;

                    numberOfModules = 0;

                    if (OnGUIWM.radars.Count > 0)
                    {
                        numberOfModules++;
                        string Radarlabel = $"{StringUtils.Localize("#LOC_BDArmory_DynamicRadar")}: {(!OnGUIWM.DynamicRadarOverride ? StringUtils.Localize("#LOC_BDArmory_false") : StringUtils.Localize("#LOC_BDArmory_true"))}";//"Dynamic Radar vs ARMs: True, False
                        if (GUI.Button(new Rect(leftIndent, +(moduleLines * entryHeight), columnWidth - 2 * leftIndent, entryHeight), Radarlabel, OnGUIWM.DynamicRadarOverride ? ButtonStyle : BDAStyle.ButtonOn))
                        {
                            OnGUIWM.DynamicRadarOverride = !OnGUIWM.DynamicRadarOverride;
                        }
                        moduleLines += 1.1f;
                    }

                    if (OnGUIWM.HMD && OnGUIWM.hasHMD)
                    {
                        numberOfModules++;
                        bool isEnabled = OnGUIWM.isHMDEnabled;
                        string label = StringUtils.Localize("#LOC_BDArmory_WMWindow_HMD");
                        Rect HMDRect = new Rect(leftIndent, +(moduleLines * entryHeight), columnWidth - 2 * leftIndent, entryHeight);
                        if (GUI.Button(HMDRect, label, isEnabled ? centerLabelOrange : centerLabel))
                        {
                            OnGUIWM.ToggleHMD();
                        }
                        moduleLines++;
                    }

                    //RWR
                    if (OnGUIWM.rwr)
                    {
                        numberOfModules++;
                        bool isEnabled = OnGUIWM.rwr.displayRWR;
                        string label = StringUtils.Localize("#LOC_BDArmory_WMWindow_RadarWarning");//"Radar Warning Receiver"
                        Rect rwrRect = new Rect(leftIndent, +(moduleLines * entryHeight), columnWidth - 2 * leftIndent, entryHeight);
                        if (GUI.Button(rwrRect, label, isEnabled ? centerLabelOrange : centerLabel))
                        {
                            if (isEnabled)
                            {
                                //OnGUIWM.rwr.DisableRWR();
                                OnGUIWM.rwr.displayRWR = false;
                            }
                            else
                            {
                                //OnGUIWM.rwr.EnableRWR();
                                OnGUIWM.rwr.displayRWR = true;
                            }
                        }
                        moduleLines++;
                    }

                    //TGP
                    using (List<ModuleTargetingCamera>.Enumerator mtc = OnGUIWM.targetingPods.GetEnumerator())
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
                    using (List<ModuleRadar>.Enumerator mr = OnGUIWM.radars.GetEnumerator())
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
                    using (List<ModuleIRST>.Enumerator mr = OnGUIWM.irsts.GetEnumerator())
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
                    using (List<ModuleECMJammer>.Enumerator jammer = OnGUIWM.jammers.GetEnumerator())
                        while (jammer.MoveNext())
                        {
                            if (jammer.Current == null) continue;
                            if (jammer.Current.isMissileECM) continue;
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
                    using (List<ModuleCloakingDevice>.Enumerator cloak = OnGUIWM.cloaks.GetEnumerator())
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
                    using (var module = OnGUIWM.wmModules.GetEnumerator())
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
                   StringUtils.Localize("#LOC_BDArmory_WMWindow_NoWeaponManager"), SelectedButtonStyle);// "No Weapon Manager found."
                line++;
            }
#if DEBUG
            if (GUI.Button(new Rect(leftIndent, contentTop + (line++ * entryHeight), contentWidth, entryHeight * 1.25f), "Double click to QUIT", Time.realtimeSinceStartup - quitTimer > 1 ? ButtonStyle : BDAStyle.ButtonOn)) // Big QUIT button for debug mode. Double click within 1s to quit.
            {
                if (Time.realtimeSinceStartup - quitTimer < 1)
                {
                    SaveConfig();
                    TournamentAutoResume.AutoQuit(0);
                }
                quitTimer = Time.realtimeSinceStartup;
            }
#endif
            var previousWindowHeight = toolWindowHeight;
            toolWindowWidth = Mathf.Lerp(toolWindowWidth, columnWidth * windowColumns, 0.15f);
            toolWindowHeight = Mathf.Lerp(toolWindowHeight, contentTop + (line * entryHeight) + 5, 1);
            WindowRectToolbar.height = toolWindowHeight;
            WindowRectToolbar.width = toolWindowWidth;
            numberOfButtons = buttonNumber + 1;
            GUIUtils.DragWindow();
            GUIUtils.RepositionWindow(ref WindowRectToolbar, previousWindowHeight);
        }
#if DEBUG
        float quitTimer = 0;
#endif

        bool validGPSName = true;

        //GPS window
        public void WindowGPS()
        {
            GUI.Box(WindowRectGps, GUIContent.none, SelectedButtonStyle);
            gpsEntryCount = 0;
            Rect listRect = new Rect(gpsBorder, gpsBorder, WindowRectGps.width - (2 * gpsBorder),
                WindowRectGps.height - (2 * gpsBorder));
            GUI.BeginGroup(listRect);
            string targetLabel = $"{StringUtils.Localize("#LOC_BDArmory_WMWindow_GPSTarget")}: {OnGUIWM.designatedGPSInfo.name}";//GPS Target
            GUI.Label(new Rect(0, 0, listRect.width, gpsEntryHeight), targetLabel, kspTitleLabel);

            // Expand/Collapse Target Toggle button
            if (GUI.Button(new Rect(listRect.width - gpsEntryHeight, 0, gpsEntryHeight, gpsEntryHeight), showTargets ? "-" : "+", ButtonStyle))
                showTargets = !showTargets;

            gpsEntryCount += 0.85f;
            if (OnGUIWM.designatedGPSCoords != Vector3d.zero)
            {
                GUI.Label(new Rect(0, gpsEntryCount * gpsEntryHeight, listRect.width - gpsEntryHeight, gpsEntryHeight),
                    BodyUtils.FormattedGeoPos(OnGUIWM.designatedGPSCoords, true), SelectedButtonStyle);
                if (
                    GUI.Button(
                        new Rect(listRect.width - gpsEntryHeight, gpsEntryCount * gpsEntryHeight, gpsEntryHeight,
                            gpsEntryHeight), " X", ButtonStyle))
                {
                    OnGUIWM.designatedGPSInfo = new GPSTargetInfo();
                }
            }
            else
            {
                GUI.Label(new Rect(0, gpsEntryCount * gpsEntryHeight, listRect.width - gpsEntryHeight, gpsEntryHeight),
                    StringUtils.Localize("#LOC_BDArmory_WMWindow_NoTarget"), SelectedButtonStyle);//"No Target"
            }

            gpsEntryCount += 1.35f;
            int indexToRemove = -1;
            int index = 0;
            BDTeam myTeam = OnGUIWM.Team;
            if (showTargets)
            {
                using (var coordinate = BDATargetManager.GPSTargetList(myTeam).GetEnumerator())
                    while (coordinate.MoveNext())
                    {
                        Color origWColor = GUI.color;
                        if (coordinate.Current.EqualsTarget(OnGUIWM.designatedGPSInfo))
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
                              ButtonStyle))
                            {
                                editingGPSName = true;
                                editingGPSNameIndex = index;
                                newGPSName = coordinate.Current.name;
                            }
                        }

                        if (
                          GUI.Button(
                            new Rect(nameWidth, gpsEntryCount * gpsEntryHeight, listRect.width - gpsEntryHeight - nameWidth,
                              gpsEntryHeight), label, ButtonStyle))
                        {
                            OnGUIWM.designatedGPSInfo = coordinate.Current;
                            OnGUIWM.designatedGPSCoordsIndex = index;
                            editingGPSName = false;
                        }

                        if (
                          GUI.Button(
                            new Rect(listRect.width - gpsEntryHeight, gpsEntryCount * gpsEntryHeight, gpsEntryHeight,
                              gpsEntryHeight), "X", ButtonStyle))
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
                if (OnGUIWM.designatedGPSInfo.EqualsTarget(old))
                {
                    OnGUIWM.designatedGPSInfo.name = newGPSName;
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
                BDArmorySettings.UI_SCALE_FOLLOWS_STOCK = SettingToggle($"UI scale follows KSP's ({GameSettings.UI_SCALE * 100:0}%)", BDArmorySettings.UI_SCALE_FOLLOWS_STOCK);
                if (!BDArmorySettings.UI_SCALE_FOLLOWS_STOCK)
                {
                    float uiScale = SettingSlider("UI scale", BDArmorySettings.UI_SCALE, 0.5f, 2f, 0.05f, $"{BDArmorySettings.UI_SCALE * 100:0}%", 1);
                    if (uiScale != BDArmorySettings.UI_SCALE) { BDArmorySettings.UI_SCALE = uiScale; scalingUI = true; }
                }
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
                if (BDArmorySettings.PERFORMANCE_OPTIONS != (BDArmorySettings.PERFORMANCE_OPTIONS = SettingToggle("Visual effects (explosions, smoke, flares)", BDArmorySettings.PERFORMANCE_OPTIONS)))
                    ResetFXPools();
                if (BDArmorySettings.PERFORMANCE_OPTIONS)
                {
                    BDArmorySettings.WATER_HIT_FX = SettingToggle("Water splashes", BDArmorySettings.WATER_HIT_FX, 1);
                    if (BDArmorySettings.LIGHTFX != (BDArmorySettings.LIGHTFX = SettingToggle("Explosion light flashes", BDArmorySettings.LIGHTFX, 1)))
                        ResetFXPools();
                }


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
                BDArmorySettings.INFINITE_COUNTERMEASURES = SettingToggle("Infinite countermeasures", BDArmorySettings.INFINITE_COUNTERMEASURES);
                BDArmorySettings.WEAPONS_RESPECT_CROSSFEED = SettingToggle("Weapons draw ammo only through crossfeed", BDArmorySettings.WEAPONS_RESPECT_CROSSFEED);
                if (BDArmorySettings.FLARE_SMOKE != (BDArmorySettings.FLARE_SMOKE = SettingToggle("Flares leave smoke trails", BDArmorySettings.FLARE_SMOKE)))
                    if (CMDropper.flarePool != null) CMDropper.ResetFlarePool();
                if (VolumetricExplosionFXBridge.Installed)
                    BDArmorySettings.VEFX_HIDE_BDA_EXPLOSIONS = SettingToggle("Let Volumetric Explosion FX replace explosion visuals", BDArmorySettings.VEFX_HIDE_BDA_EXPLOSIONS);
                BDArmorySettings.BULLET_WATER_DRAG = SettingToggle("Water slows bullets and rockets", BDArmorySettings.BULLET_WATER_DRAG);
                BDArmorySettings.BOMB_CLEARANCE_CHECK = SettingToggle("Check bomb clearance before release", BDArmorySettings.BOMB_CLEARANCE_CHECK);
                BDArmorySettings.TRIGGER_HOLD_TIME = SettingSlider("Missile launch hold time", BDArmorySettings.TRIGGER_HOLD_TIME, 0.02f, 1f, 0.02f, $"{BDArmorySettings.TRIGGER_HOLD_TIME:0.00}s");
                BDArmorySettings.LAUNCH_WARNING = SettingToggle("Missile launch warning (beeps and a bar while holding fire)", BDArmorySettings.LAUNCH_WARNING, 1);
                GUILayout.Space(4);
            }

            if (SettingsSection("Missiles & Countermeasures", ref BDArmorySettings.MISSILE_CM_SETTING_TOGGLE))
            {
                BDArmorySettings.ASPECTED_IR_SEEKERS = SettingToggle("IR seekers see engine exhaust by aspect", BDArmorySettings.ASPECTED_IR_SEEKERS);
                BDArmorySettings.ASPECTED_RCS = SettingToggle("Radar cross-section depends on aspect", BDArmorySettings.ASPECTED_RCS);
                BDArmorySettings.RADAR_NOTCHING = SettingToggle("Radar notching (beaming defeats doppler radar)", BDArmorySettings.RADAR_NOTCHING);
                if (BDArmorySettings.RADAR_NOTCHING)
                {
                    BDArmorySettings.RADAR_NOTCHING_FACTOR = SettingSlider("Notch effectiveness", BDArmorySettings.RADAR_NOTCHING_FACTOR, 0f, 1f, 0.05f, $"{BDArmorySettings.RADAR_NOTCHING_FACTOR:0.00}", 1);
                    BDArmorySettings.RADAR_NOTCHING_SCR_FACTOR = SettingSlider("Notch clutter factor", BDArmorySettings.RADAR_NOTCHING_SCR_FACTOR, 0f, 0.5f, 0.005f, $"{BDArmorySettings.RADAR_NOTCHING_SCR_FACTOR:0.000}", 1);
                }
                BDArmorySettings.CHECK_WATER_TERRAIN = SettingToggle("Water blocks radar and IR line of sight", BDArmorySettings.CHECK_WATER_TERRAIN);
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
                    BDArmorySettings.BD_DAMAGE_PENETRATION = SettingSlider("Penetration needed", BDArmorySettings.BD_DAMAGE_PENETRATION, 0f, 1f, 0.01f, $"{BDArmorySettings.BD_DAMAGE_PENETRATION:0.00}", 1);
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
                BDArmorySettings.DEBUG_ARMOR = SettingToggle("Log: armour penetration", BDArmorySettings.DEBUG_ARMOR);
                BDArmorySettings.DEBUG_HP = SettingToggle("Log: hit points", BDArmorySettings.DEBUG_HP);
                BDArmorySettings.DEBUG_APS = SettingToggle("Log: point defence", BDArmorySettings.DEBUG_APS);
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

        static void ResetFXPools()
        {
            if (CMDropper.flarePool != null) CMDropper.ResetFlarePool();
            if (ExplosionFx.explosionFXPools != null) ExplosionFx.explosionFXPools.Clear();
            if (NukeFX.nukePool != null) NukeFX.nukePool.Clear();
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
            KeyBindingsGroup("Mouse", "TEMPORARILY_", ref inputID);
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

        private static Vector2 _displayViewerPosition = Vector2.zero;

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
        void OnGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> fromTo)
        {
            if (windowSettingsEnabled) ToggleWindowSettings(); // Close the settings window so that the following settings changes don't propagate back into the settings window.
            if (fromTo.from == GameScenes.FLIGHT && fromTo.to != GameScenes.FLIGHT)
            {
                DisableAllFXAndProjectiles();
            }
        }

        public static void DisableAllBulletsAndRockets()
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

        public static void DisableAllFXAndProjectiles()
        {
            DisableAllBulletsAndRockets();
            ExplosionFx.DisableAllExplosionFX();
            NukeFX.DisableAllExplosionFX();
            FXEmitter.DisableAllFX();
            CMDropper.DisableAllCMs();
            BulletHitFX.DisableAllFX();
        }
    }
}
