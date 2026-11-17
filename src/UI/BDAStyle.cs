using UnityEngine;

namespace BDArmory.UI
{
    /// <summary>
    /// BDArmory Lite's GUI skin: a flat, opaque dark theme in the style of ZTheme / KSP2.
    ///
    /// The skin is a copy of KSP's HighLogic.Skin that keeps every style's metrics (sizes, padding, margins),
    /// so existing hand-positioned layouts are unchanged, but replaces all backgrounds and text colours.
    /// All textures are generated here at runtime; no image assets are used.
    /// </summary>
    public static class BDAStyle
    {
        #region Palette
        public static readonly Color WindowBg = Hex(0x1B1C1F, 0.97f);
        public static readonly Color WindowBorder = Hex(0x34363B);
        public static readonly Color Panel = Hex(0x25272B);
        public static readonly Color PanelBorder = Hex(0x2F3236);
        public static readonly Color Button = Hex(0x34373C);
        public static readonly Color ButtonHover = Hex(0x40444A);
        public static readonly Color ButtonPressed = Hex(0x2A2C30);
        public static readonly Color Accent = Hex(0x2F7FD3);       // "On" state: blue, like ZTheme's toggles.
        public static readonly Color AccentHover = Hex(0x3C8FE3);
        public static readonly Color Title = Hex(0xF0A33C);        // Window titles and section headers: orange.
        public static readonly Color Good = Hex(0x3DBA4E);         // Checks and "OK" states: green.
        public static readonly Color Warning = Hex(0xE5484D);      // Errors and warnings: red.
        public static readonly Color Text = Hex(0xE6E6E6);
        public static readonly Color TextDim = Hex(0x9AA0A6);
        public static readonly Color Field = Hex(0x131417);
        public static readonly Color FieldBorder = Hex(0x3A3D42);
        #endregion

        static GUISkin _skin;
        /// <summary>The shared skin (built on first use; must be first used from the main thread).</summary>
        public static GUISkin Skin { get { if (_skin == null) Build(); return _skin; } }

        /// <summary>Button style for toggled-on / selected buttons.</summary>
        public static GUIStyle ButtonOn { get { if (_skin == null) Build(); return _buttonOn; } }
        /// <summary>Orange left-aligned bold header (window titles and section headers).</summary>
        public static GUIStyle Header { get { if (_skin == null) Build(); return _header; } }
        /// <summary>Dimmed label for secondary information.</summary>
        public static GUIStyle DimLabel { get { if (_skin == null) Build(); return _dimLabel; } }
        /// <summary>Small square button for window title bars (close, settings, etc.).</summary>
        public static GUIStyle TitleButton { get { if (_skin == null) Build(); return _titleButton; } }
        static GUIStyle _buttonOn, _header, _dimLabel, _titleButton;

        static void Build()
        {
            var baseSkin = HighLogic.Skin;
            var skin = Object.Instantiate(baseSkin);
            skin.name = "BDArmoryLite";

            var window = Tex(16, 16, 4, WindowBg, WindowBorder);
            var panel = Tex(12, 12, 3, Panel, PanelBorder);
            var button = Tex(12, 12, 3, Button, Button);
            var buttonHover = Tex(12, 12, 3, ButtonHover, ButtonHover);
            var buttonPressed = Tex(12, 12, 3, ButtonPressed, ButtonPressed);
            var accent = Tex(12, 12, 3, Accent, Accent);
            var accentHover = Tex(12, 12, 3, AccentHover, AccentHover);
            var field = Tex(12, 12, 2, Field, FieldBorder);
            var fieldFocused = Tex(12, 12, 2, Field, Accent);
            var clear = Tex(4, 4, 0, Color.clear, Color.clear);

            skin.window = Restyle(baseSkin.window, window, window, window, Title);
            skin.window.onNormal.background = skin.window.onHover.background = skin.window.onActive.background = skin.window.onFocused.background = window;
            skin.window.onNormal.textColor = Title;
            skin.window.fontStyle = FontStyle.Bold;
            skin.window.alignment = TextAnchor.UpperCenter;

            skin.box = Restyle(baseSkin.box, panel, panel, panel, Text);
            skin.button = Restyle(baseSkin.button, button, buttonHover, buttonPressed, Text);
            skin.button.onNormal.background = accent; skin.button.onHover.background = accentHover; skin.button.onActive.background = accent;
            skin.button.onNormal.textColor = skin.button.onHover.textColor = skin.button.onActive.textColor = Color.white;

            skin.label = new GUIStyle(baseSkin.label);
            SetTextColor(skin.label, Text);

            skin.textField = Restyle(baseSkin.textField, field, field, fieldFocused, Text);
            skin.textField.focused.background = fieldFocused; skin.textField.focused.textColor = Color.white;
            skin.textArea = Restyle(baseSkin.textArea, field, field, fieldFocused, Text);
            skin.textArea.focused.background = fieldFocused;

            // Toggle: a dark rounded box, green tick when on.
            var toggleOff = ToggleTex(false, false);
            var toggleOffHover = ToggleTex(false, true);
            var toggleOn = ToggleTex(true, false);
            var toggleOnHover = ToggleTex(true, true);
            skin.toggle = new GUIStyle(baseSkin.toggle);
            skin.toggle.normal.background = toggleOff; skin.toggle.hover.background = toggleOffHover; skin.toggle.active.background = toggleOffHover; skin.toggle.focused.background = toggleOff;
            skin.toggle.onNormal.background = toggleOn; skin.toggle.onHover.background = toggleOnHover; skin.toggle.onActive.background = toggleOnHover; skin.toggle.onFocused.background = toggleOn;
            skin.toggle.border = new RectOffset(17, 0, 17, 0); // Don't stretch the box; it sits at the top-left of the control.
            skin.toggle.overflow = new RectOffset(0, 0, -2, 0);
            skin.toggle.padding = new RectOffset(21, 2, 1, 1);
            SetTextColor(skin.toggle, Text);

            // Sliders: thin track, light thumb.
            skin.horizontalSlider = new GUIStyle(baseSkin.horizontalSlider);
            SetBackgrounds(skin.horizontalSlider, TrackTex(true));
            skin.horizontalSlider.border = new RectOffset(3, 3, 0, 0);
            skin.horizontalSliderThumb = new GUIStyle(baseSkin.horizontalSliderThumb);
            skin.horizontalSliderThumb.normal.background = Tex(10, 14, 3, Hex(0xD9D9D9), Hex(0xD9D9D9));
            skin.horizontalSliderThumb.hover.background = skin.horizontalSliderThumb.active.background = skin.horizontalSliderThumb.focused.background = Tex(10, 14, 3, Color.white, Color.white);
            skin.verticalSlider = new GUIStyle(baseSkin.verticalSlider);
            SetBackgrounds(skin.verticalSlider, TrackTex(false));
            skin.verticalSliderThumb = new GUIStyle(baseSkin.verticalSliderThumb);
            skin.verticalSliderThumb.normal.background = Tex(14, 10, 3, Hex(0xD9D9D9), Hex(0xD9D9D9));
            skin.verticalSliderThumb.hover.background = skin.verticalSliderThumb.active.background = Tex(14, 10, 3, Color.white, Color.white);

            // Scrollbars: slim and dark.
            var scrollTrack = Tex(8, 8, 3, Hex(0x18191C), Hex(0x18191C));
            var scrollThumb = Tex(8, 8, 3, Hex(0x4A4D53), Hex(0x4A4D53));
            var scrollThumbHover = Tex(8, 8, 3, Hex(0x5C6067), Hex(0x5C6067));
            foreach (var pair in new[] { (skin.verticalScrollbar = new GUIStyle(baseSkin.verticalScrollbar), skin.verticalScrollbarThumb = new GUIStyle(baseSkin.verticalScrollbarThumb)),
                                         (skin.horizontalScrollbar = new GUIStyle(baseSkin.horizontalScrollbar), skin.horizontalScrollbarThumb = new GUIStyle(baseSkin.horizontalScrollbarThumb)) })
            {
                SetBackgrounds(pair.Item1, scrollTrack);
                pair.Item2.normal.background = scrollThumb;
                pair.Item2.hover.background = pair.Item2.active.background = pair.Item2.focused.background = scrollThumbHover;
            }
            skin.scrollView = new GUIStyle(baseSkin.scrollView);
            SetBackgrounds(skin.scrollView, clear);

            skin.settings.selectionColor = new Color(Accent.r, Accent.g, Accent.b, 0.5f);
            skin.settings.cursorColor = Color.white;

            _buttonOn = new GUIStyle(skin.button);
            _buttonOn.normal.background = accent; _buttonOn.hover.background = accentHover; _buttonOn.active.background = accent; _buttonOn.focused.background = accent;
            SetTextColor(_buttonOn, Color.white);

            _header = new GUIStyle(skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            SetTextColor(_header, Title);

            _dimLabel = new GUIStyle(skin.label);
            SetTextColor(_dimLabel, TextDim);

            _titleButton = new GUIStyle(skin.button) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(2, 2, 2, 2), fontStyle = FontStyle.Bold };

            _skin = skin;
        }

        #region Helpers
        static Color Hex(int rgb, float alpha = 1f) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);

        static GUIStyle Restyle(GUIStyle source, Texture2D normal, Texture2D hover, Texture2D active, Color textColor)
        {
            var style = new GUIStyle(source);
            style.normal.background = normal; style.hover.background = hover; style.active.background = active; style.focused.background = normal;
            style.onNormal.background = normal; style.onHover.background = hover; style.onActive.background = active; style.onFocused.background = normal;
            int b = Mathf.Max(3, normal.width / 2 - 1);
            style.border = new RectOffset(b, b, b, b);
            SetTextColor(style, textColor);
            return style;
        }

        static void SetBackgrounds(GUIStyle style, Texture2D tex)
        {
            style.normal.background = style.hover.background = style.active.background = style.focused.background = tex;
            style.onNormal.background = style.onHover.background = style.onActive.background = style.onFocused.background = tex;
        }

        static void SetTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
            style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = color;
        }

        static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.ARGB32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            return tex;
        }

        /// <summary>Anti-aliased rounded rectangle with a 1px border.</summary>
        static Texture2D Tex(int w, int h, float radius, Color fill, Color border)
        {
            var tex = NewTex(w, h);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; ++y)
                for (int x = 0; x < w; ++x)
                {
                    float d = RoundedRectDistance(x + 0.5f, y + 0.5f, w, h, radius); // <0 inside
                    float outer = Mathf.Clamp01(0.5f - d);       // Coverage of the whole shape.
                    float inner = Mathf.Clamp01(0.5f - (d + 1f)); // Coverage of the fill (inset by the 1px border).
                    var c = Color.Lerp(border, fill, inner);
                    c.a *= outer;
                    pixels[y * w + x] = c;
                }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static float RoundedRectDistance(float px, float py, float w, float h, float r)
        {
            float qx = Mathf.Abs(px - w / 2f) - (w / 2f - r);
            float qy = Mathf.Abs(py - h / 2f) - (h / 2f - r);
            float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        static Texture2D TrackTex(bool horizontal)
        {
            int w = horizontal ? 8 : 12, h = horizontal ? 12 : 8;
            var tex = NewTex(w, h);
            var pixels = new Color[w * h];
            var track = Hex(0x45484E);
            for (int y = 0; y < h; ++y)
                for (int x = 0; x < w; ++x)
                {
                    int across = horizontal ? y : x, size = horizontal ? h : w;
                    bool onTrack = across >= size / 2 - 2 && across < size / 2 + 2;
                    pixels[y * w + x] = onTrack ? track : Color.clear;
                }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>18x18 texture with a 14x14 checkbox in the top-left; a green tick when on.</summary>
        static Texture2D ToggleTex(bool on, bool hover)
        {
            const int size = 18, box = 14;
            var boxTex = Tex(box, box, 3, on ? Good : (hover ? ButtonHover : Button), on ? Good : FieldBorder);
            var tex = NewTex(size, size);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; ++i) pixels[i] = Color.clear;
            for (int y = 0; y < box; ++y)
                for (int x = 0; x < box; ++x)
                    pixels[(y + size - box) * size + x] = boxTex.GetPixel(x, y); // Texture y=0 is the bottom row; place the box at the top.
            if (on) // Tick: two strokes, drawn with a small brush.
            {
                Vector2 a = new Vector2(3.5f, 7.5f), b = new Vector2(6f, 4.5f), c = new Vector2(10.5f, 10f);
                for (int y = 0; y < box; ++y)
                    for (int x = 0; x < box; ++x)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        float d = Mathf.Min(SegmentDistance(p, a, b), SegmentDistance(p, b, c));
                        float cov = Mathf.Clamp01(1.6f - d);
                        int idx = (y + size - box) * size + x;
                        pixels[idx] = Color.Lerp(pixels[idx], Color.white, cov);
                    }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            Object.Destroy(boxTex);
            return tex;
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + t * ab)).magnitude;
        }
        #endregion
    }
}
