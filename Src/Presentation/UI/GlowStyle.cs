using System;
using System.Globalization;
using Godot;

namespace Doomscroll.Presentation.UI
{
    /// <summary>
    /// "3 AM Glow" art direction: the room stays dark and calm, the phone is the only light,
    /// and every piece of UI is a sticker (3 px ink border, hard offset shadow, rounded corners).
    /// Palette, fonts and style factories live here so every screen shares them.
    /// </summary>
    public static class GlowStyle
    {
        public static readonly Color Ink = Color.FromHtml("#0E0B1F");
        public static readonly Color Room = Color.FromHtml("#17142E");
        public static readonly Color Wall = Color.FromHtml("#221E42");
        public static readonly Color Desk = Color.FromHtml("#2E2347");
        public static readonly Color DeskEdge = Color.FromHtml("#3A2D5C");
        public static readonly Color Paper = Color.FromHtml("#EDE9FF");
        public static readonly Color Muted = Color.FromHtml("#B9B3E0");
        public static readonly Color ScreenGlow = Color.FromHtml("#BDEBFF");
        public static readonly Color Dopamine = Color.FromHtml("#FF3D81");
        public static readonly Color Cash = Color.FromHtml("#B8FF3C");
        public static readonly Color Eyes = Color.FromHtml("#3CE6FF");
        public static readonly Color Mythic = Color.FromHtml("#FFD23C");
        public static readonly Color Fever = Color.FromHtml("#FF8A3D");
        public static readonly Color Danger = Color.FromHtml("#FF4D4D");
        public static readonly Color Dawn = Color.FromHtml("#FFB38A");
        public static readonly Color Grass = Color.FromHtml("#5DCB6E");
        public static readonly Color PostItPink = Color.FromHtml("#FF8FB5");
        public static readonly Color Violet = Color.FromHtml("#9F7BFF");
        public static readonly Color SlaccPurple = Color.FromHtml("#534AB7");
        public static readonly Color SlaccDesktop = Color.FromHtml("#1F6F78");
        public static readonly Color SlaccWindow = Color.FromHtml("#E9E5F5");

        /// <summary>Phone-app surfaces (lock screen cards, Screen Time, PRYM).</summary>
        public static readonly Color AppPaper = Color.FromHtml("#F4F1FF");
        public static readonly Color AppCream = Color.FromHtml("#FFF7EC");
        public static readonly Color AppLine = Color.FromHtml("#E3DEF5");
        public static readonly Color AppMuted = Color.FromHtml("#5C5680");
        public static readonly Color AppText = Color.FromHtml("#3D3866");
        public static readonly Color AppGood = Color.FromHtml("#2E6B12");
        public static readonly Color AppBad = Color.FromHtml("#C2271F");
        public static readonly Color LockSky = Color.FromHtml("#1B1640");

        /// <summary>Text size setting (1, 1.25 or 1.5). Applied by <see cref="ApplyTextScale"/>.</summary>
        public static float TextScale { get; set; } = 1f;
        private const string BaseFontSizeMeta = "glow_base_font_size";
        private const int ThemeBaseFontSize = 17;
        // Headlines and big numbers are already large; the text size setting only grows reading text.
        private const int MaxScaledFontSize = 22;

        private const string FontDir = "res://Assets/Fonts/";
        private static Font? _bungee;
        private static Font? _nunito;
        private static Font? _vt323;
        private static Font? _bodyBold;
        private static Font? _bodyBlack;

        /// <summary>Bungee: numbers, stickers, titles.</summary>
        public static Font Display => _bungee ??= LoadFontFile("Bungee-Regular.ttf");
        /// <summary>Nunito 800: phone app UI and body text.</summary>
        public static Font Body => _bodyBold ??= Weighted(Nunito, 800);
        /// <summary>Nunito 900: phone captions and handles.</summary>
        public static Font BodyBlack => _bodyBlack ??= Weighted(Nunito, 900);
        /// <summary>VT323: the laptop, the alarm clock, post-its and the log.</summary>
        public static Font Mono => _vt323 ??= LoadFontFile("VT323-Regular.ttf");

        private static Font Nunito => _nunito ??= LoadFontFile("Nunito-Variable.ttf");

        /// <summary>
        /// Loads a bundled font. Uses the imported resource when the editor has imported it,
        /// and falls back to reading the TTF directly (fresh checkouts, headless runs).
        /// </summary>
        private static Font LoadFontFile(string fileName)
        {
            string path = FontDir + fileName;
            if (Godot.FileAccess.FileExists(path + ".import"))
            {
                var imported = GD.Load<FontFile>(path);
                if (imported != null) return imported;
            }

            var dynamicFont = new FontFile();
            if (dynamicFont.LoadDynamicFont(path) == Error.Ok) return dynamicFont;

            GD.PushWarning($"[GlowStyle] Could not load {path}; using the engine fallback font.");
            return ThemeDB.FallbackFont;
        }

        private static Font Weighted(Font file, int weight)
        {
            // Variation axes are keyed by OpenType tag; "wght" drives Nunito's weight.
            long weightTag = TextServerManager.GetPrimaryInterface().NameToTag("wght");
            return new FontVariation
            {
                BaseFont = file,
                VariationOpentype = new Godot.Collections.Dictionary { { weightTag, weight } }
            };
        }

        public static Theme CreateTheme()
        {
            return new Theme
            {
                DefaultFont = Body,
                DefaultFontSize = ThemeBaseFontSize
            };
        }

        /// <summary>
        /// Scales the reading-size fonts under <paramref name="root"/> by <see cref="TextScale"/>. The size each control
        /// was built with is remembered, so calling this again after the setting changes never compounds.
        /// </summary>
        public static void ApplyTextScale(Node root)
        {
            if (root is Control control)
            {
                if (control.Theme != null)
                {
                    control.Theme.DefaultFontSize = Mathf.RoundToInt(ThemeBaseFontSize * TextScale);
                }
                string sizeName = control is RichTextLabel ? "normal_font_size" : "font_size";
                if (control.HasThemeFontSizeOverride(sizeName))
                {
                    if (!control.HasMeta(BaseFontSizeMeta))
                    {
                        control.SetMeta(BaseFontSizeMeta, control.GetThemeFontSize(sizeName));
                    }
                    int baseSize = control.GetMeta(BaseFontSizeMeta).AsInt32();
                    float scale = baseSize <= MaxScaledFontSize ? TextScale : 1f;
                    control.AddThemeFontSizeOverride(sizeName, Mathf.RoundToInt(baseSize * scale));
                }
            }

            foreach (Node child in root.GetChildren())
            {
                ApplyTextScale(child);
            }
        }

        /// <summary>A flat rounded surface (phone-app cards, notifications); no shadow, optional border.</summary>
        public static StyleBoxFlat Flat(Color bg, int radius, int padX = 0, int padY = 0, int border = 0, Color? borderColor = null)
        {
            var box = new StyleBoxFlat { BgColor = bg, BorderColor = borderColor ?? Ink, AntiAliasing = true };
            box.SetBorderWidthAll(border);
            box.SetCornerRadiusAll(radius);
            box.ContentMarginLeft = padX;
            box.ContentMarginRight = padX;
            box.ContentMarginTop = padY;
            box.ContentMarginBottom = padY;
            return box;
        }

        public static PanelContainer FlatPanel(Color bg, int radius, int padX = 0, int padY = 0, int border = 0, Color? borderColor = null)
        {
            var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            panel.AddThemeStyleboxOverride("panel", Flat(bg, radius, padX, padY, border, borderColor));
            return panel;
        }

        /// <summary>Thick ink track with a colored fill and a round sticker grabber.</summary>
        public static void StyleSlider(HSlider slider, Color fill)
        {
            var track = Flat(AppLine, 8, 0, 6, 3);
            var filled = Flat(fill, 8, 0, 6, 3);
            slider.AddThemeStyleboxOverride("slider", track);
            slider.AddThemeStyleboxOverride("grabber_area", filled);
            slider.AddThemeStyleboxOverride("grabber_area_highlight", filled);
            var knob = CircleTexture(30, Color.FromHtml("#FFFFFF"));
            slider.AddThemeIconOverride("grabber", knob);
            slider.AddThemeIconOverride("grabber_highlight", knob);
            slider.AddThemeIconOverride("grabber_disabled", knob);
        }

        private static ImageTexture CircleTexture(int size, Color fill)
        {
            var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x + 0.5f - r, y + 0.5f - r).Length();
                    Color c = d <= r - 4f ? fill : d <= r - 0.5f ? Ink : new Color(0, 0, 0, 0);
                    image.SetPixel(x, y, c);
                }
            }
            return ImageTexture.CreateFromImage(image);
        }

        /// <summary>The house sticker: flat fill, ink border, hard shadow below.</summary>
        public static StyleBoxFlat Sticker(Color bg, int radius = 14, int border = 3, int shadow = 4, int padX = 14, int padY = 8)
        {
            var box = new StyleBoxFlat
            {
                BgColor = bg,
                BorderColor = Ink,
                ShadowColor = Ink,
                ShadowSize = shadow > 0 ? 1 : 0,
                ShadowOffset = new Vector2(0, shadow),
                AntiAliasing = true
            };
            box.SetBorderWidthAll(border);
            box.SetCornerRadiusAll(radius);
            box.ContentMarginLeft = padX;
            box.ContentMarginRight = padX;
            box.ContentMarginTop = padY;
            box.ContentMarginBottom = padY;
            return box;
        }

        public static PanelContainer StickerPanel(Color bg, int radius = 14, int shadow = 4, int padX = 14, int padY = 8)
        {
            var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            panel.AddThemeStyleboxOverride("panel", Sticker(bg, radius, 3, shadow, padX, padY));
            return panel;
        }

        public static Label MakeLabel(string text, Font font, int size, Color color, int outline = 0)
        {
            var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
            label.AddThemeFontOverride("font", font);
            label.AddThemeFontSizeOverride("font_size", size);
            label.AddThemeColorOverride("font_color", color);
            if (outline > 0)
            {
                label.AddThemeColorOverride("font_outline_color", Ink);
                label.AddThemeConstantOverride("outline_size", outline);
            }
            return label;
        }

        /// <summary>Hard offset shadow behind text (the "+X" and the big counter).</summary>
        public static void AddTextShadow(Label label, Color color, int offset)
        {
            label.AddThemeColorOverride("font_shadow_color", color);
            label.AddThemeConstantOverride("shadow_offset_x", offset);
            label.AddThemeConstantOverride("shadow_offset_y", offset);
            label.AddThemeConstantOverride("shadow_outline_size", 0);
        }

        public static void StyleStickerButton(Button button, Color bg, Color fg, Font? font = null, int fontSize = 18, int radius = 14, int padX = 14, int padY = 8)
        {
            var normal = Sticker(bg, radius, 3, 4, padX, padY);
            var hover = Sticker(bg.Lightened(0.14f), radius, 3, 4, padX, padY);
            // Pressed: the sticker sinks onto its shadow.
            var pressed = Sticker(bg.Darkened(0.08f), radius, 3, 1, padX, padY);
            pressed.ContentMarginTop = padY + 3;
            pressed.ContentMarginBottom = Math.Max(0, padY - 3);
            var disabled = Sticker(DeskEdge, radius, 3, 2, padX, padY);

            button.AddThemeStyleboxOverride("normal", normal);
            button.AddThemeStyleboxOverride("hover", hover);
            button.AddThemeStyleboxOverride("pressed", pressed);
            button.AddThemeStyleboxOverride("hover_pressed", pressed);
            button.AddThemeStyleboxOverride("disabled", disabled);
            button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
            button.AddThemeColorOverride("font_color", fg);
            button.AddThemeColorOverride("font_hover_color", fg);
            button.AddThemeColorOverride("font_pressed_color", fg);
            button.AddThemeColorOverride("font_hover_pressed_color", fg);
            button.AddThemeColorOverride("font_focus_color", fg);
            button.AddThemeColorOverride("font_disabled_color", Muted);
            button.AddThemeFontOverride("font", font ?? Display);
            button.AddThemeFontSizeOverride("font_size", fontSize);
        }

        /// <summary>Styles a bar and returns its fill box so callers can recolor it without allocating.</summary>
        public static StyleBoxFlat StyleBar(ProgressBar bar, Color fill, Color? background = null, int radius = 6, int border = 2)
        {
            var bg = new StyleBoxFlat { BgColor = background ?? Ink, BorderColor = Ink, AntiAliasing = true };
            bg.SetBorderWidthAll(border);
            bg.SetCornerRadiusAll(radius);
            var fillBox = new StyleBoxFlat { BgColor = fill, BorderColor = Ink, AntiAliasing = true };
            fillBox.SetBorderWidthAll(border);
            fillBox.SetCornerRadiusAll(radius);
            bar.AddThemeStyleboxOverride("background", bg);
            bar.AddThemeStyleboxOverride("fill", fillBox);
            bar.ShowPercentage = false;
            return fillBox;
        }

        /// <summary>Big incremental numbers: 573.5, 2.15K, 21.1K, 215K, 1.24M...</summary>
        public static string FormatBig(double value)
        {
            double abs = Math.Abs(value);
            if (abs < 1000) return value.ToString("0.#", CultureInfo.InvariantCulture);

            string[] suffixes = { "K", "M", "B", "T", "Qa", "Qi" };
            int index = -1;
            while (abs >= 1000 && index < suffixes.Length - 1)
            {
                abs /= 1000;
                value /= 1000;
                index++;
            }

            string format = abs >= 100 ? "0" : abs >= 10 ? "0.0" : "0.00";
            return value.ToString(format, CultureInfo.InvariantCulture) + suffixes[index];
        }

        public static string FormatCash(double value)
        {
            return Math.Abs(value) < 10000
                ? "$" + value.ToString("0.##", CultureInfo.InvariantCulture)
                : "$" + FormatBig(value);
        }
    }
}
