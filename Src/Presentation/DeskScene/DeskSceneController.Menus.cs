using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Doomscroll.Core.Events;
using Doomscroll.Core.Services;
using Doomscroll.Presentation.UI;
using Godot;

namespace Doomscroll.Presentation.DeskScene
{
    /// <summary>
    /// Everything that is not the night itself, all shown as phone screens in the 3 AM Glow style:
    /// the lock-screen main menu, the Settings and Album apps, the "Screen Time" night report and the PRYM shop.
    /// Layouts follow the design canvas (1600x900) scaled by 1.2 to the 1920x1080 base.
    /// </summary>
    public partial class DeskSceneController
    {
        // One phone size for the lock screen, the apps and the report; the shop phone is a bit wider.
        private const float PhoneWidth = 504f;
        private const float PhoneHeight = 1008f;
        private const float PhoneTop = 36f;
        private const float ShopPhoneWidth = 552f;
        private const float CenteredPhoneX = 960f - PhoneWidth / 2f;
        private const float ReportPhoneShopX = 150f;
        private const float ShopPhoneX = 720f;

        private static readonly Color LockedLine = Color.FromHtml("#8F88BF");

        private GameSettings _settings = null!;

        // Lock screen. _titleOverlay and _titleContinueBtn keep their old names (playtest tools use them).
        private Label _lockDateLabel = null!;
        private Label _lockSaveKicker = null!;
        private Label _lockSaveTitle = null!;
        private Label _lockSaveDetail = null!;
        private Label _lockContinueSub = null!;
        private Label _lockMomText = null!;
        private Label _lockDoomText = null!;
        private Label _lockScreenTimeText = null!;
        private Label _lockTipA = null!;
        private Label _lockTipB = null!;
        private LinkButton _lockNewGameButton = null!;
        private Control _lockConfirm = null!;
        private Control[] _lockOptionalBubbles = Array.Empty<Control>();
        private bool _lockDragging;
        private Vector2 _lockDragStart;

        // Settings / Album apps, drawn above everything (also above the pause menu).
        private Control _appLayer = null!;
        private Control _appScreen = null!;
        private bool _appReturnsToPause;
        private int _albumTab;

        // Night report ("Screen Time") and PRYM shop.
        private Control _reportPhone = null!;
        private Control _shopPhone = null!;
        private Control _shopSide = null!;
        private Button _openShopButton = null!;
        private Label _shopNote = null!;
        private PanelContainer _reportHero = null!;
        private Label _reportHeroBig = null!;
        private Label _reportHeroText = null!;
        private VBoxContainer _reportRows = null!;
        private Label _reportFootnote = null!;
        private Label _shopDopamineChip = null!;
        private Label _shopCashChip = null!;
        private Label _shopGrassChip = null!;
        private PanelContainer _shopGrassChipPanel = null!;
        private readonly Dictionary<UpgradeCategory, Button> _shopTabButtons = new();
        private HFlowContainer _arrivingFlow = null!;
        private Label _arrivingEmpty = null!;
        private readonly List<string> _boughtTonight = new();
        private bool _confirmingPrestige;
        private bool _shopPhaseShown;

        // Per-night numbers for the report (gameplay never reads them).
        private long _nightStartVideos;
        private int _nightFevers;
        private int _nightBestStreak;

        // ------------------------------------------------------------------ shared phone pieces

        /// <summary>Ink phone body with a rounded, clipped screen. Returns the screen; <paramref name="body"/> is the outer shell.</summary>
        private static Panel BuildPhoneShell(Control parent, float x, float width, Color screenColor, out Panel body)
        {
            body = new Panel { MouseFilter = MouseFilterEnum.Ignore };
            var bodyBox = GlowStyle.Flat(GlowStyle.Ink, 65);
            bodyBox.ShadowColor = new Color(GlowStyle.ScreenGlow, 0.2f);
            bodyBox.ShadowSize = 80;
            body.AddThemeStyleboxOverride("panel", bodyBox);
            Place(parent, body, HAnchor.Center, x, PhoneTop, width, PhoneHeight);

            var screen = new Panel { ClipChildren = ClipChildrenMode.AndDraw };
            screen.AddThemeStyleboxOverride("panel", GlowStyle.Flat(screenColor, 53));
            Inset(body, screen, 14, 14, 14, 14);
            return screen;
        }

        /// <summary>Adds <paramref name="child"/> stretched over <paramref name="parent"/> with the given margins.</summary>
        private static T Inset<T>(Control parent, T child, float left, float top, float right, float bottom) where T : Control
        {
            child.SetAnchorsPreset(LayoutPreset.FullRect);
            child.OffsetLeft = left;
            child.OffsetTop = top;
            child.OffsetRight = -right;
            child.OffsetBottom = -bottom;
            parent.AddChild(child);
            return child;
        }

        private static VBoxContainer AppColumn(Control screen, int separation, float top = 60f)
        {
            var column = new VBoxContainer();
            column.AddThemeConstantOverride("separation", separation);
            return Inset(screen, column, 24, top, 24, 28);
        }

        private static Label WrapLabel(string text, Font font, int size, Color color)
        {
            var label = GlowStyle.MakeLabel(text, font, size, color);
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            return label;
        }

        private static PanelContainer Chip(string text, Color bg, out Label label, int fontSize = 17)
        {
            var panel = GlowStyle.StickerPanel(bg, 12, 0, 10, 3);
            label = GlowStyle.MakeLabel(text, GlowStyle.Display, fontSize, GlowStyle.Ink);
            panel.AddChild(label);
            return panel;
        }

        /// <summary>Sticker button on a light phone screen: a pale chip when disabled instead of the dark desk one.</summary>
        private static void StyleAppButton(Button button, Color bg, Font font, int fontSize, int radius = 16, int padX = 14, int padY = 10)
        {
            GlowStyle.StyleStickerButton(button, bg, GlowStyle.Ink, font, fontSize, radius, padX, padY);
            button.AddThemeStyleboxOverride("disabled", GlowStyle.Sticker(GlowStyle.AppLine, radius, 3, 0, padX, padY));
            button.AddThemeColorOverride("font_disabled_color", GlowStyle.AppMuted);
        }

        private static Button IconTextButton(Control content)
        {
            var button = new Button { Flat = true, MouseDefaultCursorShape = CursorShape.PointingHand };
            foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "focus", "disabled" })
            {
                button.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
            }
            content.MouseFilter = MouseFilterEnum.Ignore;
            button.AddChild(content);
            button.CustomMinimumSize = content.GetCombinedMinimumSize();
            content.Resized += () => button.CustomMinimumSize = content.GetCombinedMinimumSize();
            return button;
        }

        private static PanelContainer PostIt(Control parent, float x, float y, float width, Color color, float degrees, out Label text)
        {
            var note = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            note.AddThemeStyleboxOverride("panel", GlowStyle.Sticker(color, 0, 3, 4, 19, 17));
            text = WrapLabel("", GlowStyle.Mono, 34, GlowStyle.Ink);
            text.CustomMinimumSize = new Vector2(width - 44, 0);
            note.AddChild(text);
            Place(parent, note, HAnchor.Right, x, y, width, 0);
            Tilt(note, degrees);
            return note;
        }

        /// <summary>
        /// Containers reset their children's rotation, so a tilted sticker inside one sits in a plain holder
        /// that takes the sticker's minimum size.
        /// </summary>
        private static Control Tilted(Control child, float degrees)
        {
            var holder = new Control
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = child.SizeFlagsHorizontal,
                SizeFlagsVertical = child.SizeFlagsVertical
            };
            child.SetAnchorsPreset(LayoutPreset.FullRect);
            holder.AddChild(child);
            holder.CustomMinimumSize = child.GetCombinedMinimumSize();
            child.MinimumSizeChanged += () => holder.CustomMinimumSize = child.GetCombinedMinimumSize();
            Tilt(child, degrees);
            return holder;
        }

        private void ApplyCurrentTextScale(Node root)
        {
            if (Math.Abs(GlowStyle.TextScale - 1f) > 0.01f) GlowStyle.ApplyTextScale(root);
        }

        // ------------------------------------------------------------------ settings

        private void LoadSettings()
        {
            _settings = GameSettings.Load(_saveData.SafeScreamerMode);
            GlowStyle.TextScale = _settings.TextScale;
            _settings.ApplyDisplay();
        }

        private void ApplyAudioSettings()
        {
            _audio.SetMusicVolume(_settings.MusicVolume);
            _audio.SetSfxVolume(_settings.SfxVolume);
        }

        private void SaveSettings() => _settings.Save();

        // ------------------------------------------------------------------ lock screen (main menu)

        private void BuildLockScreen()
        {
            _titleOverlay = new ColorRect { Color = Color.FromHtml("#100D22"), MouseFilter = MouseFilterEnum.Stop };
            _titleOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(_titleOverlay);

            var glow = new TextureRect
            {
                Texture = RadialGlow(new Color(GlowStyle.ScreenGlow, 0.16f)),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                MouseFilter = MouseFilterEnum.Ignore
            };
            Place(_titleOverlay, glow, HAnchor.Center, 480, 0, 960, 1080);

            BuildLockLogoColumn();
            BuildLockPhone();

            PostIt(_titleOverlay, 1344, 228, 276, GlowStyle.Mythic, 4f, out _lockTipA);
            PostIt(_titleOverlay, 1416, 516, 252, GlowStyle.PostItPink, -3f, out _lockTipB);
            var safeHint = Chip("SAFE SCARE MODE IN SETTINGS", GlowStyle.Eyes, out _, 17);
            Place(_titleOverlay, safeHint, HAnchor.Right, 1356, 768, 0, 0);
            Tilt(safeHint, 2f);
        }

        private static GradientTexture2D RadialGlow(Color center)
        {
            var gradient = new Gradient();
            gradient.SetColor(0, center);
            gradient.SetColor(1, new Color(center, 0f));
            gradient.SetOffset(1, 0.62f);
            return new GradientTexture2D
            {
                Gradient = gradient,
                Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(0.5f, 0.5f),
                FillTo = new Vector2(0.5f, 0f),
                Width = 256,
                Height = 256
            };
        }

        private void BuildLockLogoColumn()
        {
            var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            column.AddThemeConstantOverride("separation", 6);
            Place(_titleOverlay, column, HAnchor.Left, 108, 160, 580, 820);

            foreach (var (word, color, tilt) in new[] { ("DOOM", GlowStyle.Dopamine, -3f), ("SCROLL", GlowStyle.Eyes, 2f) })
            {
                var label = GlowStyle.MakeLabel(word, GlowStyle.Display, 125, color, 6);
                GlowStyle.AddTextShadow(label, GlowStyle.Ink, 6);
                label.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
                column.AddChild(Tilted(label, tilt));
            }

            var incremental = Chip("INCREMENTAL", GlowStyle.Cash, out _, 26);
            incremental.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            column.AddChild(Tilted(incremental, -5f));

            column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 50), MouseFilter = MouseFilterEnum.Ignore });

            var saveCard = GlowStyle.StickerPanel(GlowStyle.Wall, 17, 4, 22, 17);
            saveCard.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            saveCard.CustomMinimumSize = new Vector2(408, 0);
            var saveBox = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            saveBox.AddThemeConstantOverride("separation", 4);
            _lockSaveKicker = GlowStyle.MakeLabel("LAST SAVE", GlowStyle.Display, 16, GlowStyle.Mythic);
            _lockSaveTitle = GlowStyle.MakeLabel("", GlowStyle.BodyBlack, 24, GlowStyle.Paper);
            _lockSaveDetail = WrapLabel("", GlowStyle.Body, 18, GlowStyle.Muted);
            saveBox.AddChild(_lockSaveKicker);
            saveBox.AddChild(_lockSaveTitle);
            saveBox.AddChild(_lockSaveDetail);
            saveCard.AddChild(saveBox);
            column.AddChild(saveCard);

            string version = ProjectSettings.GetSetting("application/config/version", "").AsString();
            var footer = GlowStyle.MakeLabel(string.IsNullOrEmpty(version) ? "made at 3 AM" : $"v{version} · made at 3 AM", GlowStyle.Mono, 26, LockedLine);
            column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 14), MouseFilter = MouseFilterEnum.Ignore });
            column.AddChild(footer);
        }

        private void BuildLockPhone()
        {
            var screen = BuildPhoneShell(_titleOverlay, CenteredPhoneX, PhoneWidth, GlowStyle.LockSky, out _);
            screen.GuiInput += OnLockScreenGuiInput;

            // Wallpaper: a planet, the moon and a few stars.
            AddDisc(screen, 416, 180, 108, Color.FromHtml("#2B2560"));
            AddDisc(screen, 422, 157, 42, Color.FromHtml("#F3EFD6"));
            foreach (var (sx, sy, size) in new[] { (40f, 70f, 5f), (120f, 34f, 4f), (60f, 236f, 3f), (300f, 36f, 3f), (250f, 250f, 3f) })
            {
                var star = new ColorRect { Color = Color.FromHtml("#F3EFD6"), MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(sx, sy), Size = new Vector2(size, size) };
                screen.AddChild(star);
            }

            var clock = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            clock.AddThemeConstantOverride("separation", -6);
            clock.SetAnchorsPreset(LayoutPreset.TopWide);
            clock.OffsetTop = 58;
            clock.OffsetBottom = 240;
            screen.AddChild(clock);
            _lockDateLabel = GlowStyle.MakeLabel("", GlowStyle.Body, 22, new Color(1, 1, 1, 0.85f));
            _lockDateLabel.HorizontalAlignment = HorizontalAlignment.Center;
            clock.AddChild(_lockDateLabel);
            // The night always starts at midnight, so the lock screen sits one minute before it.
            var time = GlowStyle.MakeLabel("11:59", GlowStyle.BodyBlack, 132, Colors.White);
            time.HorizontalAlignment = HorizontalAlignment.Center;
            clock.AddChild(time);

            var notifications = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            notifications.AddThemeConstantOverride("separation", 12);
            notifications.SetAnchorsPreset(LayoutPreset.TopWide);
            notifications.OffsetLeft = 19;
            notifications.OffsetRight = -19;
            notifications.OffsetTop = 283;
            screen.AddChild(notifications);
            _lockMomText = AddNotification(notifications, LetterTile("M", GlowStyle.Dopamine), "Mom", "now");
            var slacc = AddNotification(notifications, LetterTile("S", Color.FromHtml("#7F77DD")), "Slacc", "2m");
            slacc.Text = "BOSS: standup at 9 SHARP";
            _lockDoomText = AddNotification(notifications, GlyphTile("play", GlowStyle.Fever), "Doomscroll", "5m");
            // Bigger text sizes keep only Mom and Screen Time so the stack never runs into the buttons.
            _lockOptionalBubbles = new[] { BubbleOf(slacc), BubbleOf(_lockDoomText) };
            _lockScreenTimeText = AddNotification(notifications, GlyphTile("clock", GlowStyle.Eyes), "Screen Time", "1h");

            var bottom = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End };
            bottom.AddThemeConstantOverride("separation", 16);
            bottom.SetAnchorsPreset(LayoutPreset.BottomWide);
            bottom.OffsetLeft = 26;
            bottom.OffsetRight = -26;
            bottom.OffsetTop = -330;
            bottom.OffsetBottom = -36;
            bottom.GrowVertical = GrowDirection.Begin;
            screen.AddChild(bottom);

            _titleContinueBtn = new Button { CustomMinimumSize = new Vector2(0, 112), MouseDefaultCursorShape = CursorShape.PointingHand };
            GlowStyle.StyleStickerButton(_titleContinueBtn, GlowStyle.Dopamine, GlowStyle.Ink, GlowStyle.Display, 26, 22, 14, 12);
            _titleContinueBtn.AddThemeStyleboxOverride("normal", GlowStyle.Sticker(GlowStyle.Dopamine, 22, 3, 6, 14, 12));
            var swipeContent = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
            swipeContent.AddThemeConstantOverride("separation", 0);
            var chevron = new GlowGlyph { Glyph = "chevron_up", ShowTile = false, GlyphScale = 1f, CustomMinimumSize = new Vector2(30, 22), SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
            swipeContent.AddChild(chevron);
            var swipeTitle = GlowStyle.MakeLabel("SWIPE UP TO SCROLL", GlowStyle.Display, 26, GlowStyle.Ink);
            swipeTitle.HorizontalAlignment = HorizontalAlignment.Center;
            swipeContent.AddChild(swipeTitle);
            _lockContinueSub = GlowStyle.MakeLabel("", GlowStyle.BodyBlack, 16, GlowStyle.Ink);
            _lockContinueSub.HorizontalAlignment = HorizontalAlignment.Center;
            swipeContent.AddChild(_lockContinueSub);
            Inset(_titleContinueBtn, swipeContent, 0, 6, 0, 6);
            _titleContinueBtn.Pressed += ContinueFromLockScreen;
            bottom.AddChild(_titleContinueBtn);

            var apps = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
            apps.AddThemeConstantOverride("separation", 30);
            apps.AddChild(LockAppButton("gear", "Settings", () => OpenApp(BuildSettingsApp, fromPause: false)));
            apps.AddChild(LockAppButton("trophy", "Album", () => OpenApp(BuildAlbumApp, fromPause: false)));
            apps.AddChild(LockAppButton("power", "Quit", () =>
            {
                SaveProgress();
                GetTree().Quit();
            }));
            bottom.AddChild(apps);

            _lockNewGameButton = new LinkButton { Text = "Start a new game", Underline = LinkButton.UnderlineMode.Always, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
            _lockNewGameButton.AddThemeFontOverride("font", GlowStyle.Body);
            _lockNewGameButton.AddThemeFontSizeOverride("font_size", 17);
            _lockNewGameButton.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.75f));
            _lockNewGameButton.AddThemeColorOverride("font_hover_color", Colors.White);
            _lockNewGameButton.Pressed += () => _lockConfirm.Visible = true;
            bottom.AddChild(_lockNewGameButton);

            var homeBar = GlowStyle.FlatPanel(new Color(1, 1, 1, 0.7f), 3);
            homeBar.SetAnchorsPreset(LayoutPreset.CenterBottom);
            homeBar.OffsetLeft = -78;
            homeBar.OffsetRight = 78;
            homeBar.OffsetTop = -18;
            homeBar.OffsetBottom = -12;
            screen.AddChild(homeBar);

            BuildNewGameConfirm(screen);
        }

        private static Control BubbleOf(Label message) => (Control)message.GetParent().GetParent().GetParent();

        private static void AddDisc(Control parent, float cx, float cy, float radius, Color color)
        {
            var disc = GlowStyle.FlatPanel(color, (int)radius);
            disc.Position = new Vector2(cx - radius, cy - radius);
            disc.Size = new Vector2(radius * 2f, radius * 2f);
            parent.AddChild(disc);
        }

        private static Control LetterTile(string letter, Color color)
        {
            var tile = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(41, 41) };
            tile.AddThemeStyleboxOverride("panel", GlowStyle.Flat(color, 11, 0, 0, 2));
            var label = GlowStyle.MakeLabel(letter, GlowStyle.Display, 19, GlowStyle.Ink);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            tile.AddChild(label);
            return tile;
        }

        private static Control GlyphTile(string glyph, Color color, float size = 41f) => new GlowGlyph
        {
            Glyph = glyph,
            TileColor = color,
            CustomMinimumSize = new Vector2(size, size),
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };

        /// <summary>One white notification bubble; returns its message label.</summary>
        private static Label AddNotification(Control list, Control icon, string app, string when)
        {
            var bubble = GlowStyle.FlatPanel(new Color(1, 1, 1, 0.92f), 22, 14, 12);
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 12);
            icon.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            row.AddChild(icon);

            var text = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            text.AddThemeConstantOverride("separation", 0);
            var header = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            var name = GlowStyle.MakeLabel(app, GlowStyle.BodyBlack, 16, GlowStyle.Ink);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            header.AddChild(name);
            header.AddChild(GlowStyle.MakeLabel(when, GlowStyle.Body, 16, GlowStyle.AppMuted));
            text.AddChild(header);
            var message = WrapLabel("", GlowStyle.Body, 18, GlowStyle.Ink);
            text.AddChild(message);
            row.AddChild(text);

            bubble.AddChild(row);
            list.AddChild(bubble);
            return message;
        }

        private static Button LockAppButton(string glyph, string caption, Action onPressed)
        {
            var content = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            content.AddThemeConstantOverride("separation", 5);
            content.AddChild(new GlowGlyph
            {
                Glyph = glyph,
                TileColor = new Color(1, 1, 1, 0.16f),
                GlyphColor = Colors.White,
                Round = true,
                Border = false,
                GlyphScale = 0.46f,
                CustomMinimumSize = new Vector2(62, 62),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter
            });
            var label = GlowStyle.MakeLabel(caption, GlowStyle.Body, 16, Colors.White);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            content.AddChild(label);
            var button = IconTextButton(content);
            button.TooltipText = caption;
            button.Pressed += onPressed;
            return button;
        }

        private void BuildNewGameConfirm(Control screen)
        {
            _lockConfirm = new ColorRect { Color = new Color(GlowStyle.Ink, 0.8f), Visible = false, MouseFilter = MouseFilterEnum.Stop };
            Inset(screen, _lockConfirm, 0, 0, 0, 0);

            var card = GlowStyle.StickerPanel(GlowStyle.AppPaper, 22, 5, 22, 20);
            card.SetAnchorsPreset(LayoutPreset.Center);
            card.GrowHorizontal = GrowDirection.Both;
            card.GrowVertical = GrowDirection.Both;
            card.CustomMinimumSize = new Vector2(400, 0);
            _lockConfirm.AddChild(card);

            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 14);
            card.AddChild(box);
            box.AddChild(GlowStyle.MakeLabel("Start over?", GlowStyle.BodyBlack, 30, GlowStyle.Ink));
            box.AddChild(WrapLabel("This deletes your nights, upgrades, Grass Points, Mythics and trophies. Your settings stay.", GlowStyle.Body, 18, GlowStyle.AppText));

            var wipe = new Button { Text = "DELETE AND START" };
            StyleAppButton(wipe, GlowStyle.Danger, GlowStyle.Display, 20);
            wipe.Pressed += StartNewGame;
            box.AddChild(wipe);

            var keep = new Button { Text = "Keep my save" };
            StyleAppButton(keep, Colors.White, GlowStyle.BodyBlack, 18);
            keep.Pressed += () => _lockConfirm.Visible = false;
            box.AddChild(keep);
        }

        private void OnLockScreenGuiInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } mb) return;
            if (mb.Pressed)
            {
                _lockDragging = true;
                _lockDragStart = mb.Position;
            }
            else if (_lockDragging)
            {
                _lockDragging = false;
                if (_lockDragStart.Y - mb.Position.Y >= 60f && !_lockConfirm.Visible) ContinueFromLockScreen();
            }
        }

        private void ContinueFromLockScreen()
        {
            _lockConfirm.Visible = false;
            _titleOverlay.Visible = false;
            _audio?.PlaySfx("sfx_complete", 1.15f);
        }

        private void ShowLockScreen()
        {
            RefreshLockScreen();
            _titleOverlay.Visible = true;
        }

        private void StartNewGame()
        {
            _lockConfirm.Visible = false;
            _session?.Abort();
            _saveData = new SaveData();
            SaveProgress();
            _audio?.PlaySfx("sfx_blackout");
            StartNewDayTurn();
            ContinueFromLockScreen();
        }

        private static readonly string[] MomMessages =
        {
            "are you sleeping?? it is late",
            "your cousin got promoted. just saying",
            "did you eat something today",
            "call me tomorrow ok? not at 3am",
            "i saw you online at 4am"
        };

        private void RefreshLockScreen()
        {
            if (_lockDateLabel == null) return;
            var victory = _balanceConfig.Victory;
            bool hasSave = _saveService.HasSave;
            bool freshRun = _saveData.CurrentDay == 1 && _saveData.TotalRunDopamine <= 0 && !_saveData.NightCompleted;

            _lockDateLabel.Text = DateTime.Now.ToString("dddd, MMMM d", CultureInfo.InvariantCulture);
            _lockContinueSub.Text = _saveData.NightCompleted
                ? "Back to the shop"
                : freshRun ? $"Start night {_saveData.CurrentDay}" : $"Continue night {_saveData.CurrentDay}";

            if (hasSave)
            {
                _lockSaveKicker.Text = "LAST SAVE";
                _lockSaveTitle.Text = _saveData.EndlessModeUnlocked
                    ? $"Night {_saveData.CurrentDay} · endless"
                    : $"Night {_saveData.CurrentDay} of {victory.Day}";
                string grass = _saveData.TotalPrestiges switch
                {
                    0 => "never touched grass",
                    1 => "touched grass once",
                    _ => $"touched grass {_saveData.TotalPrestiges} times"
                };
                _lockSaveDetail.Text = $"{GlowStyle.FormatCash(Math.Floor(_saveData.CurrentMoney))} · {_saveData.UnlockedMythicIds.Count}/{MythicCatalog.Count} Mythics · {grass}";
            }
            else
            {
                _lockSaveKicker.Text = "NEW PHONE";
                _lockSaveTitle.Text = "No save yet";
                _lockSaveDetail.Text = "Battery 100%. That will not last.";
            }
            _lockNewGameButton.Visible = hasSave;
            foreach (var bubble in _lockOptionalBubbles) bubble.Visible = GlowStyle.TextScale < 1.2f;

            _lockMomText.Text = MomMessages[(_saveData.CurrentDay - 1) % MomMessages.Length];
            _lockDoomText.Text = _saveData.UnlockedMythicIds.Count > 0
                ? $"{_saveData.UnlockedMythicIds.Count} of {MythicCatalog.Count} Mythic clips found. Keep looking"
                : "3 new videos picked just for you";
            long videos = _saveData.LifetimeVideosWatched;
            double minutes = videos * _balanceConfig.Phone.VideoSeconds / 60.0;
            _lockScreenTimeText.Text = videos > 0
                ? $"{videos:N0} videos so far. That is {(int)(minutes / 60)}h {(int)(minutes % 60)}m of your life."
                : "You averaged 9h 41m a day. Up 520%.";

            var tips = LockTips();
            int first = _fxRng.Next(tips.Length);
            _lockTipA.Text = tips[first];
            _lockTipB.Text = tips[(first + 1 + _fxRng.Next(tips.Length - 1)) % tips.Length];
        }

        private string[] LockTips() => new[]
        {
            "first time? wait for the green line, THEN swipe.",
            "ESC pauses. the boss never does.",
            $"{_balanceConfig.Combo.SwipesToFever} good swipes in a row = FEVER. x{FeverX} everything.",
            "too many phones on one outlet = blackout.",
            "the battery won't last the night. press C to plug in.",
            "blurry eyes = less dopamine. eye drops exist.",
            $"the bill comes every {_balanceConfig.Bill.EveryDays} nights. keep cash.",
            "touching grass resets the run. you come back stronger.",
            "double-click the phone to like. likes train the feed."
        };

        // ------------------------------------------------------------------ phone apps (settings, album)

        private void BuildAppLayer()
        {
            _appLayer = new ColorRect
            {
                Color = new Color(GlowStyle.Ink, 0.72f),
                MouseFilter = MouseFilterEnum.Stop,
                Visible = false,
                // Settings also open from the pause menu, while the tree is paused.
                ProcessMode = ProcessModeEnum.Always
            };
            _appLayer.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(_appLayer);
            _appScreen = BuildPhoneShell(_appLayer, CenteredPhoneX, PhoneWidth, GlowStyle.AppPaper, out _);
        }

        private void OpenApp(Action<VBoxContainer> build, bool fromPause)
        {
            _appReturnsToPause = fromPause;
            if (fromPause) _pauseOverlay.Visible = false;
            RebuildApp(build);
            _appLayer.Visible = true;
        }

        private void RebuildApp(Action<VBoxContainer> build)
        {
            foreach (Node child in _appScreen.GetChildren())
            {
                _appScreen.RemoveChild(child);
                child.QueueFree();
            }
            var column = AppColumn(_appScreen, 14, 56);
            build(column);
            ApplyCurrentTextScale(_appScreen);
        }

        private void CloseApp()
        {
            _appLayer.Visible = false;
            if (_appReturnsToPause) _pauseOverlay.Visible = true;
            else RefreshLockScreen();
        }

        private void AddAppHeader(VBoxContainer column, string title, string? subtitle)
        {
            var back = new Button
            {
                Text = "Back",
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                ShortcutInTooltip = false,
                Shortcut = new Shortcut { Events = new Godot.Collections.Array { new InputEventKey { Keycode = Key.Escape } } }
            };
            StyleAppButton(back, Colors.White, GlowStyle.BodyBlack, 16, 12, 12, 4);
            back.Pressed += CloseApp;
            column.AddChild(back);

            column.AddChild(GlowStyle.MakeLabel(title, GlowStyle.BodyBlack, 34, GlowStyle.Ink));
            if (subtitle != null) column.AddChild(WrapLabel(subtitle, GlowStyle.Body, 16, GlowStyle.AppMuted));
        }

        private static VBoxContainer AddScrollArea(VBoxContainer column, int separation)
        {
            var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
            column.AddChild(scroll);
            var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            content.AddThemeConstantOverride("separation", separation);
            scroll.AddChild(content);
            return content;
        }

        private static VBoxContainer AddSettingsSection(VBoxContainer content, string title)
        {
            content.AddChild(GlowStyle.MakeLabel(title, GlowStyle.Display, 14, GlowStyle.AppMuted));
            var card = GlowStyle.FlatPanel(Colors.White, 18, 16, 12, 3);
            card.MouseFilter = MouseFilterEnum.Pass;
            content.AddChild(card);
            var rows = new VBoxContainer();
            rows.AddThemeConstantOverride("separation", 12);
            card.AddChild(rows);
            return rows;
        }

        private static HBoxContainer SettingsRow(VBoxContainer rows, string label, string? hint, Control control)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 12);
            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            text.AddThemeConstantOverride("separation", 0);
            text.AddChild(GlowStyle.MakeLabel(label, GlowStyle.BodyBlack, 18, GlowStyle.Ink));
            if (hint != null) text.AddChild(WrapLabel(hint, GlowStyle.Body, 14, GlowStyle.AppMuted));
            row.AddChild(text);
            control.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(control);
            rows.AddChild(row);
            return row;
        }

        private static Button Toggle(bool value, Action<bool> changed)
        {
            var toggle = new Button { ToggleMode = true, ButtonPressed = value, CustomMinimumSize = new Vector2(76, 0) };
            void Restyle(bool on)
            {
                toggle.Text = on ? "ON" : "OFF";
                StyleAppButton(toggle, on ? GlowStyle.Cash : GlowStyle.AppLine, GlowStyle.Display, 15, 12, 10, 4);
            }
            Restyle(value);
            toggle.Toggled += on =>
            {
                Restyle(on);
                changed(on);
            };
            return toggle;
        }

        private void AddVolumeRow(VBoxContainer rows, string label, float value, Color fill, Action<float> changed)
        {
            var percent = GlowStyle.MakeLabel($"{value * 100:0}%", GlowStyle.Display, 15, GlowStyle.Ink);
            SettingsRow(rows, label, null, percent);
            var slider = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = value, CustomMinimumSize = new Vector2(0, 30) };
            GlowStyle.StyleSlider(slider, fill);
            slider.ValueChanged += v =>
            {
                percent.Text = $"{v * 100:0}%";
                changed((float)v);
            };
            slider.DragEnded += _ => SaveSettings();
            rows.AddChild(slider);
        }

        private void BuildSettingsApp(VBoxContainer column)
        {
            AddAppHeader(column, "Settings", null);
            var content = AddScrollArea(column, 10);

            var sound = AddSettingsSection(content, "SOUND");
            AddVolumeRow(sound, "Music", _settings.MusicVolume, GlowStyle.Dopamine, v =>
            {
                _settings.MusicVolume = v;
                _audio.SetMusicVolume(v);
            });
            AddVolumeRow(sound, "Sound effects", _settings.SfxVolume, GlowStyle.Eyes, v =>
            {
                _settings.SfxVolume = v;
                _audio.SetSfxVolume(v);
            });

            var screen = AddSettingsSection(content, "SCREEN");
            SettingsRow(screen, "Fullscreen", null, Toggle(_settings.Fullscreen, on =>
            {
                _settings.Fullscreen = on;
                _settings.ApplyDisplay();
                SaveSettings();
            }));
            SettingsRow(screen, "VSync", "Off can feel snappier, but may tear.", Toggle(_settings.Vsync, on =>
            {
                _settings.Vsync = on;
                _settings.ApplyDisplay();
                SaveSettings();
            }));

            var sizes = new HBoxContainer();
            sizes.AddThemeConstantOverride("separation", 6);
            foreach (float scale in GameSettings.TextScales)
            {
                var option = new Button { Text = $"{scale * 100:0}%" };
                StyleAppButton(option, Math.Abs(_settings.TextScale - scale) < 0.01f ? GlowStyle.Mythic : Colors.White, GlowStyle.Display, 14, 10, 8, 4);
                option.Pressed += () =>
                {
                    _settings.TextScale = scale;
                    GlowStyle.TextScale = scale;
                    GlowStyle.ApplyTextScale(this);
                    SaveSettings();
                    RebuildApp(BuildSettingsApp);
                };
                sizes.AddChild(option);
            }
            SettingsRow(screen, "Text size", null, sizes);

            var comfort = AddSettingsSection(content, "COMFORT");
            SettingsRow(comfort, "Reduced motion", "No shaking, flying numbers or pulsing.", Toggle(_settings.ReducedMotion, on =>
            {
                _settings.ReducedMotion = on;
                SaveSettings();
            }));
            SettingsRow(comfort, "Safe Screamer", "Screamers become a calm card. Same rewards.", Toggle(_settings.SafeScreamer, on =>
            {
                _settings.SafeScreamer = on;
                SaveSettings();
            }));
            SettingsRow(comfort, "First-time tips", "Post-its that explain the battery, outlet, blur, boredom and boss once.", Toggle(_settings.Tips, on =>
            {
                _settings.Tips = on;
                SaveSettings();
            }));
            var replay = new Button { Text = _saveData.SeenTips.Count > 0 ? "Show tips again" : "All tips are fresh", Disabled = _saveData.SeenTips.Count == 0 };
            StyleAppButton(replay, Colors.White, GlowStyle.BodyBlack, 15, 12, 12, 5);
            replay.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            replay.Pressed += () =>
            {
                _saveData.SeenTips.Clear();
                SaveProgress();
                RebuildApp(BuildSettingsApp);
            };
            comfort.AddChild(replay);

            content.AddChild(WrapLabel("Saved on this computer. A new game keeps these.", GlowStyle.Body, 15, GlowStyle.AppMuted));
        }

        private void BuildAlbumApp(VBoxContainer column)
        {
            AddAppHeader(column, "Album",
                $"{_saveData.UnlockedMythicIds.Count}/{MythicCatalog.Count} Mythics · {_saveData.UnlockedAchievementIds.Count}/{AchievementCatalog.Length} trophies");

            var tabs = new HBoxContainer();
            tabs.AddThemeConstantOverride("separation", 6);
            string[] names = { "Mythics", "Trophies", "Stats" };
            for (int i = 0; i < names.Length; i++)
            {
                int tab = i;
                var button = new Button { Text = names[i] };
                StyleAppButton(button, _albumTab == i ? GlowStyle.Mythic : Colors.White, GlowStyle.BodyBlack, 16, 12, 12, 5);
                button.Pressed += () =>
                {
                    _albumTab = tab;
                    RebuildApp(BuildAlbumApp);
                };
                tabs.AddChild(button);
            }
            column.AddChild(tabs);

            var content = AddScrollArea(column, 10);
            if (_albumTab == 0) FillAlbumMythics(content);
            else if (_albumTab == 1) FillAlbumTrophies(content);
            else FillAlbumStats(content);
        }

        private static PanelContainer AlbumCard(Control content, Control icon, string title, string body, bool unlocked, string? chip, Color chipColor)
        {
            var card = new PanelContainer();
            card.AddThemeStyleboxOverride("panel", unlocked
                ? GlowStyle.Sticker(Colors.White, 16, 3, 4, 12, 10)
                : GlowStyle.Flat(GlowStyle.AppPaper, 16, 12, 10, 3, LockedLine));
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 12);
            row.AddChild(icon);
            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            text.AddThemeConstantOverride("separation", 2);
            text.AddChild(WrapLabel(title, GlowStyle.BodyBlack, 17, unlocked ? GlowStyle.Ink : GlowStyle.AppText));
            text.AddChild(WrapLabel(body, GlowStyle.Body, 14, unlocked ? GlowStyle.AppText : GlowStyle.AppMuted));
            if (chip != null) text.AddChild(WrapLabel(chip, GlowStyle.BodyBlack, 14, chipColor));
            row.AddChild(text);
            card.AddChild(row);
            content.AddChild(card);
            return card;
        }

        private void FillAlbumMythics(VBoxContainer content)
        {
            int pity = _balanceConfig.Feed.MythicPityVideos;
            string bonus = $"+{_balanceConfig.Mythics.DopamineBonusEach * 100:0.##}% dopamine";
            for (int i = 0; i < MythicCatalog.Count; i++)
            {
                var mythic = MythicCatalog[i];
                bool found = HasMythic(mythic.Id);
                string title = mythic.Title;
                int colon = title.IndexOf(": ", StringComparison.Ordinal);
                if (colon >= 0) title = title[(colon + 2)..];
                AlbumCard(content,
                    GlyphTile(found ? "star" : "lock", found ? GlowStyle.Mythic : GlowStyle.AppLine, 52),
                    found ? $"#{i + 1} · {title}" : $"#{i + 1} · ???",
                    found ? $"\"{mythic.Lore}\"" : $"Not found yet. They show up at random; one is guaranteed every {pity} videos (now {_saveData.MythicPityCounter}).",
                    found,
                    found ? $"{mythic.Buff} · {bonus}" : null,
                    GlowStyle.AppGood);
            }
        }

        private void FillAlbumTrophies(VBoxContainer content)
        {
            string bonus = $"+{_balanceConfig.Achievements.DopamineBonusEach * 100:0.##}% dopamine";
            foreach (var trophy in AchievementCatalog)
            {
                bool unlocked = _saveData.UnlockedAchievementIds.Contains(trophy.Id);
                AlbumCard(content,
                    GlyphTile(unlocked ? trophy.Icon : "lock", unlocked ? GlowStyle.Cash : GlowStyle.AppLine, 52),
                    trophy.Name, trophy.Goal, unlocked, unlocked ? bonus : null, GlowStyle.AppGood);
            }
        }

        private void FillAlbumStats(VBoxContainer content)
        {
            var niches = _balanceConfig.Niches;
            double memesSeconds = niches.Memes.VideoSeconds ?? _balanceConfig.Phone.VideoSeconds;
            var career = AddSettingsSection(content, "CAREER");
            foreach (var (label, value) in new[]
            {
                ("Videos watched", _saveData.LifetimeVideosWatched.ToString("N0", CultureInfo.InvariantCulture)),
                ("Early swipes", _saveData.LifetimeEarlySwipes.ToString("N0", CultureInfo.InvariantCulture)),
                ("Boss replies", _saveData.LifetimeBossAlertsReplied.ToString("N0", CultureInfo.InvariantCulture)),
                ("Screamers survived", _saveData.LifetimeScreamersSurvived.ToString("N0", CultureInfo.InvariantCulture)),
                ("Times touched grass", _saveData.TotalPrestiges.ToString(CultureInfo.InvariantCulture)),
                ("Mythic pity", $"{_saveData.MythicPityCounter}/{_balanceConfig.Feed.MythicPityVideos}"),
                ("Trophy bonus", $"+{_saveData.UnlockedAchievementIds.Count * _balanceConfig.Achievements.DopamineBonusEach * 100:0.##}%"),
                ("Campaign", _saveData.EndlessModeUnlocked ? "won · endless" : $"night {_saveData.CurrentDay} of {_balanceConfig.Victory.Day}")
            })
            {
                SettingsRow(career, label, null, GlowStyle.MakeLabel(value, GlowStyle.Display, 15, GlowStyle.Ink));
            }

            var nicheCard = AddSettingsSection(content, "WHAT EACH NICHE DOES");
            SettingsRow(nicheCard, "Cats", $"A like restores {niches.Cats.LikeEyeRestore:0.##}% eye freshness.", new Control());
            SettingsRow(nicheCard, "Gaming", $"+{(niches.Gaming.Dopamine - 1.0) * 100:0.##}% dopamine, tires your eyes faster.", new Control());
            SettingsRow(nicheCard, "ASMR", $"-{(1.0 - niches.Asmr.Fatigue) * 100:0.##}% eye fatigue while it plays.", new Control());
            SettingsRow(nicheCard, "Memes", $"Only {memesSeconds:0.#}s long ({(1.0 - memesSeconds / _balanceConfig.Phone.VideoSeconds) * 100:0}% faster).", new Control());
        }

        // ------------------------------------------------------------------ night report + PRYM shop

        private void BuildNightPhones()
        {
            _nightModal = new ColorRect
            {
                Color = new Color(GlowStyle.Ink, 0.55f),
                MouseFilter = MouseFilterEnum.Stop,
                Visible = false
            };
            _nightModal.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(_nightModal);

            BuildReportPhone();
            BuildShopPhone();
            BuildShopSide();
        }

        private void BuildReportPhone()
        {
            var screen = BuildPhoneShell(_nightModal, CenteredPhoneX, PhoneWidth, GlowStyle.AppPaper, out var body);
            _reportPhone = body;
            var column = AppColumn(screen, 16, 64);

            var header = new VBoxContainer();
            header.AddThemeConstantOverride("separation", 0);
            header.AddChild(GlowStyle.MakeLabel("Screen Time", GlowStyle.BodyBlack, 34, GlowStyle.Ink));
            _nightSummaryLabel = WrapLabel("", GlowStyle.Body, 17, GlowStyle.AppMuted);
            header.AddChild(_nightSummaryLabel);
            column.AddChild(header);

            _reportHero = new PanelContainer();
            var heroRow = new HBoxContainer();
            heroRow.AddThemeConstantOverride("separation", 14);
            _reportHeroBig = GlowStyle.MakeLabel("", GlowStyle.Display, 46, GlowStyle.Ink);
            _reportHeroBig.VerticalAlignment = VerticalAlignment.Center;
            heroRow.AddChild(_reportHeroBig);
            _reportHeroText = WrapLabel("", GlowStyle.BodyBlack, 17, GlowStyle.Ink);
            _reportHeroText.VerticalAlignment = VerticalAlignment.Center;
            heroRow.AddChild(_reportHeroText);
            _reportHero.AddChild(heroRow);
            column.AddChild(Tilted(_reportHero, -1.5f));

            var rowsCard = new PanelContainer();
            rowsCard.AddThemeStyleboxOverride("panel", GlowStyle.Flat(Colors.White, 19, 0, 2, 3));
            _reportRows = new VBoxContainer();
            _reportRows.AddThemeConstantOverride("separation", 0);
            rowsCard.AddChild(_reportRows);
            column.AddChild(rowsCard);

            _reportFootnote = WrapLabel("", GlowStyle.Body, 17, GlowStyle.AppMuted);
            column.AddChild(_reportFootnote);

            column.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });

            _openShopButton = new Button { Text = "OPEN THE SHOP" };
            StyleAppButton(_openShopButton, GlowStyle.Mythic, GlowStyle.Display, 24, 16, 14, 12);
            _openShopButton.Pressed += () => ShowShopPhase(animate: true);
            column.AddChild(_openShopButton);

            _prestigeButton = new Button { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            StyleAppButton(_prestigeButton, GlowStyle.Grass, GlowStyle.BodyBlack, 19, 16, 14, 10);
            _prestigeButton.Pressed += OnPrestigePressed;
            column.AddChild(_prestigeButton);
        }

        private void AddReportRow(string label, string value, Color valueColor, bool last = false)
        {
            var row = new PanelContainer();
            var box = GlowStyle.Flat(new Color(0, 0, 0, 0), 0, 16, 11);
            if (!last)
            {
                box.BorderColor = GlowStyle.AppLine;
                box.BorderWidthBottom = 2;
            }
            row.AddThemeStyleboxOverride("panel", box);
            var line = new HBoxContainer();
            line.AddThemeConstantOverride("separation", 10);
            line.AddChild(WrapLabel(label, GlowStyle.Body, 18, GlowStyle.Ink));
            var amount = GlowStyle.MakeLabel(value, GlowStyle.BodyBlack, 18, valueColor);
            amount.HorizontalAlignment = HorizontalAlignment.Right;
            line.AddChild(amount);
            row.AddChild(line);
            _reportRows.AddChild(row);
        }

        private void BuildShopPhone()
        {
            var screen = BuildPhoneShell(_nightModal, ShopPhoneX, ShopPhoneWidth, GlowStyle.AppCream, out var body);
            _shopPhone = body;
            var column = AppColumn(screen, 12, 56);
            column.OffsetLeft = 20;
            column.OffsetRight = -20;

            var header = new HFlowContainer();
            header.AddThemeConstantOverride("v_separation", 6);
            var brand = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            brand.AddThemeConstantOverride("separation", -4);
            brand.AddChild(GlowStyle.MakeLabel("PRYM", GlowStyle.Display, 36, GlowStyle.Ink));
            brand.AddChild(GlowStyle.MakeLabel("next-night delivery", GlowStyle.Body, 16, GlowStyle.AppMuted));
            header.AddChild(brand);
            var chips = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkEnd };
            chips.AddThemeConstantOverride("separation", 6);
            chips.AddChild(Chip("", GlowStyle.Dopamine, out _shopDopamineChip));
            chips.AddChild(Chip("", GlowStyle.Cash, out _shopCashChip));
            _shopGrassChipPanel = Chip("", GlowStyle.Grass, out _shopGrassChip);
            chips.AddChild(_shopGrassChipPanel);
            header.AddChild(chips);
            column.AddChild(header);

            var tabs = new HFlowContainer();
            tabs.AddThemeConstantOverride("h_separation", 6);
            tabs.AddThemeConstantOverride("v_separation", 6);
            foreach (UpgradeCategory category in Enum.GetValues<UpgradeCategory>())
            {
                var tab = new Button { Text = category.ToString() };
                tab.Pressed += () =>
                {
                    _currentNightTab = category;
                    RefreshNightTabContent();
                };
                _shopTabButtons[category] = tab;
                tabs.AddChild(tab);
            }
            column.AddChild(tabs);

            var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
            column.AddChild(scroll);
            _shopButtonsContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _shopButtonsContainer.AddThemeConstantOverride("separation", 12);
            scroll.AddChild(_shopButtonsContainer);

            _nextDayButton = new Button { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            StyleAppButton(_nextDayButton, GlowStyle.Eyes, GlowStyle.Display, 24, 16, 14, 12);
            _nextDayButton.Pressed += OnStartNightPressed;
            column.AddChild(_nextDayButton);
        }

        private void BuildShopSide()
        {
            _shopSide = new Control { MouseFilter = MouseFilterEnum.Ignore };
            _shopSide.SetAnchorsPreset(LayoutPreset.FullRect);
            _nightModal.AddChild(_shopSide);

            PostIt(_shopSide, 1352, 120, 360, GlowStyle.Mythic, 3f, out _shopNote);

            var arriving = GlowStyle.StickerPanel(GlowStyle.Wall, 16, 4, 18, 16);
            Place(_shopSide, arriving, HAnchor.Right, 1340, 360, 432, 0);
            var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            box.AddThemeConstantOverride("separation", 12);
            arriving.AddChild(box);
            box.AddChild(GlowStyle.MakeLabel("ARRIVING TOMORROW", GlowStyle.Display, 18, GlowStyle.Mythic));
            _arrivingFlow = new HFlowContainer { MouseFilter = MouseFilterEnum.Ignore };
            _arrivingFlow.AddThemeConstantOverride("h_separation", 10);
            _arrivingFlow.AddThemeConstantOverride("v_separation", 12);
            box.AddChild(_arrivingFlow);
            _arrivingEmpty = WrapLabel("Nothing yet. Your cart is as empty as your sleep schedule.", GlowStyle.Body, 17, GlowStyle.Muted);
            box.AddChild(_arrivingEmpty);
        }

        private void RefreshArriving()
        {
            foreach (Node child in _arrivingFlow.GetChildren()) child.QueueFree();
            Color[] colors = { GlowStyle.Cash, GlowStyle.Fever, GlowStyle.Eyes, GlowStyle.Dopamine, GlowStyle.Mythic };
            for (int i = 0; i < _boughtTonight.Count; i++)
            {
                var sticker = Chip(_boughtTonight[i].ToUpperInvariant(), colors[i % colors.Length], out _, 14);
                _arrivingFlow.AddChild(Tilted(sticker, i % 2 == 0 ? -3f : 2f));
            }
            _arrivingEmpty.Visible = _boughtTonight.Count == 0;
            ApplyCurrentTextScale(_arrivingFlow);
        }

        /// <summary>Opens the night phones: the report first, the shop after "Open the shop" (or straight away).</summary>
        private void ShowNightPhones(bool shopFirst)
        {
            _boughtTonight.Clear();
            _confirmingPrestige = false;
            _shopNote.Text = ShopNoteText();
            RefreshArriving();
            RefreshPrestigeButton();
            UpdateNightFooterButtons();
            RefreshNightTabContent();
            if (shopFirst) ShowShopPhase(animate: false);
            else ShowReportPhase();
            ApplyCurrentTextScale(_nightModal);
            _nightModal.Visible = true;
        }

        private void ShowReportPhase()
        {
            _shopPhaseShown = false;
            _reportPhone.Position = new Vector2(CenteredPhoneX, _reportPhone.Position.Y);
            _shopPhone.Visible = false;
            _shopSide.Visible = false;
            _openShopButton.Visible = true;
        }

        private void ShowShopPhase(bool animate)
        {
            if (_shopPhaseShown) return;
            _shopPhaseShown = true;
            _openShopButton.Visible = false;
            _shopPhone.Visible = true;
            _shopSide.Visible = true;
            float reportX = ReportPhoneShopX;
            if (!animate || ReducedMotion)
            {
                _reportPhone.Position = new Vector2(reportX, _reportPhone.Position.Y);
                _shopPhone.Modulate = Colors.White;
                _shopSide.Modulate = Colors.White;
                return;
            }

            _shopPhone.Modulate = new Color(1, 1, 1, 0);
            _shopSide.Modulate = new Color(1, 1, 1, 0);
            var tween = CreateTween().SetParallel();
            tween.TweenProperty(_reportPhone, "position:x", reportX, 0.35).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(_shopPhone, "modulate:a", 1.0, 0.3).SetDelay(0.12);
            tween.TweenProperty(_shopSide, "modulate:a", 1.0, 0.3).SetDelay(0.2);
            _audio?.PlaySfx("sfx_swipe", 1.1f);
        }

        /// <summary>Fills the Screen Time report for the night that just ended.</summary>
        private void FillNightReport(TurnEndReason reason, double salary, float attendance, bool isBillDay, double billAmount)
        {
            int day = _saveData.CurrentDay;
            string endedAt = reason == TurnEndReason.DawnReached ? "dawn at 6:00 AM" : $"battery died at {NightClockText()}";
            _nightSummaryLabel.Text = $"Night {day} report · {endedAt}";

            long videos = _saveData.LifetimeVideosWatched - _nightStartVideos;
            long lastNight = _saveData.LastNightVideos;
            if (_isBillUnpaidLock)
            {
                SetReportHero(GlowStyle.Danger, "CUT", "Power cut. The bill won. Touch grass to start a new run.");
            }
            else if (lastNight <= 0)
            {
                SetReportHero(GlowStyle.Dopamine, videos.ToString(CultureInfo.InvariantCulture), "videos tonight. The algorithm is learning you.");
            }
            else
            {
                double change = (videos - lastNight) * 100.0 / lastNight;
                SetReportHero(GlowStyle.Dopamine, $"{(change >= 0 ? "+" : "")}{change:0}%", change >= 0
                    ? "more scrolling than last night. Proud of you?"
                    : "less scrolling than last night. The algorithm misses you.");
            }
            _saveData.LastNightVideos = videos;

            double dopamine = _saveData.TotalRunDopamine - (_nightStartRunState?.RunDopamine ?? _saveData.TotalRunDopamine);
            ClearReportRows();
            AddReportRow("Videos watched", videos.ToString("N0", CultureInfo.InvariantCulture), GlowStyle.Ink);
            AddReportRow("Best streak", $"x{_nightBestStreak} · {_nightFevers} {(_nightFevers == 1 ? "fever" : "fevers")}", GlowStyle.Ink);
            AddReportRow("Dopamine", "+" + GlowStyle.FormatBig(Math.Round(dopamine)), GlowStyle.Ink);
            var topNiche = new[] { VideoNiche.Cats, VideoNiche.Gaming, VideoNiche.Asmr, VideoNiche.Memes }
                .OrderByDescending(n => _algorithm.GetNormalizedShare(n)).First();
            AddReportRow("Your feed is now", $"{_algorithm.GetNormalizedShare(topNiche) * 100:0}% {topNiche.ToString().ToLowerInvariant()}", GlowStyle.Ink);
            AddReportRow(RunFormulas.BossMuted(_saveData) ? "Salary · do not disturb" : $"Salary · {attendance * 100f:0}% attendance",
                "+" + GlowStyle.FormatCash(salary), GlowStyle.AppGood);
            if (isBillDay)
            {
                AddReportRow(_isBillUnpaidLock ? "Electric bill · UNPAID" : "Electric bill", "-" + GlowStyle.FormatCash(Math.Round(billAmount, 2)), GlowStyle.AppBad, last: true);
            }
            else
            {
                AddReportRow("Electric bill", $"night {day + _economy.DaysUntilBill(day)} · ~{GlowStyle.FormatCash(Math.Round(billAmount))}", GlowStyle.AppMuted, last: true);
            }

            int nextBill = isBillDay ? day + _balanceConfig.Bill.EveryDays : day + _economy.DaysUntilBill(day);
            _reportFootnote.Text = $"Next bill on night {nextBill}. Mythic pity: {_saveData.MythicPityCounter} of {_balanceConfig.Feed.MythicPityVideos}.";
        }

        /// <summary>Report shown when a saved game reopens straight into the shop (the night's numbers are gone).</summary>
        private void FillResumedReport()
        {
            int day = _saveData.CurrentDay;
            ClearReportRows();
            if (_isBillUnpaidLock)
            {
                _nightSummaryLabel.Text = $"Night {day} report · saved";
                SetReportHero(GlowStyle.Danger, "CUT", "Power cut. The bill is still unpaid. Touch grass to start a new run.");
            }
            else if (_saveData.TotalRunDopamine <= 0)
            {
                _nightSummaryLabel.Text = "New run · saved";
                SetReportHero(GlowStyle.Grass, $"{_saveData.GrassPoints} GP", "Fresh run. Spend Grass Points, then start night 1.");
                _currentNightTab = UpgradeCategory.Grass;
            }
            else
            {
                _nightSummaryLabel.Text = $"Night {day} report · saved";
                SetReportHero(GlowStyle.Dopamine, $"{_saveData.LastNightVideos}", "videos last night. Shop, then start the next one.");
            }
            AddReportRow("Run dopamine", GlowStyle.FormatBig(Math.Round(_saveData.TotalRunDopamine)), GlowStyle.Ink);
            AddReportRow("Cash", GlowStyle.FormatCash(_saveData.CurrentMoney), GlowStyle.AppGood, last: true);
            _reportFootnote.Text = $"Next bill on night {day + _economy.DaysUntilBill(day)}. Mythic pity: {_saveData.MythicPityCounter} of {_balanceConfig.Feed.MythicPityVideos}.";
        }

        private void SetReportHero(Color color, string big, string text)
        {
            _reportHero.AddThemeStyleboxOverride("panel", GlowStyle.Sticker(color, 16, 3, 4, 14, 12));
            _reportHeroBig.Text = big;
            _reportHeroText.Text = text;
        }

        private void ClearReportRows()
        {
            foreach (Node child in _reportRows.GetChildren())
            {
                _reportRows.RemoveChild(child);
                child.QueueFree();
            }
        }

        private string NightClockText()
        {
            float ratio = Math.Clamp(_session.ElapsedSeconds / (float)_balanceConfig.Night.DurationSeconds, 0f, 1f);
            int totalMinutes = (int)(ratio * 360f);
            int hour = totalMinutes / 60;
            return $"{(hour == 0 ? 12 : hour)}:{totalMinutes % 60:00} AM";
        }

        private void RefreshPrestigeButton()
        {
            bool voluntary = !_isBillUnpaidLock;
            int baseReward = _economy.CalculateGrassPointsReward(_saveData.TotalRunDopamine, _saveData.CurrentDay);
            int bonus = voluntary ? _economy.VoluntaryGrassBonus(baseReward) : 0;
            int grassPreview = baseReward + bonus;
            bool freshRun = _saveData.TotalRunDopamine <= 0;
            _prestigeButton.Visible = !freshRun;
            string early = bonus > 0 ? $" (+{bonus} for going early)" : "";
            _prestigeButton.Text = _confirmingPrestige
                ? $"Tap again: reset the run for +{grassPreview} GP"
                : $"Touch grass now · +{grassPreview} GP{early}";
        }

        private void OnPrestigePressed()
        {
            // Touching grass wipes the run, so it takes a second tap (unless the bill forces it anyway).
            if (!_confirmingPrestige && !_isBillUnpaidLock)
            {
                _confirmingPrestige = true;
                RefreshPrestigeButton();
                return;
            }
            _confirmingPrestige = false;

            int earned = _prestigeReset.ExecuteTouchGrass(_saveData, voluntary: !_isBillUnpaidLock);
            _saveData.PowerCutPending = false;
            _audio?.PlaySfx("sfx_mythic");
            CheckAchievements();
            SaveProgress();
            _currentNightTab = UpgradeCategory.Grass;
            _isBillUnpaidLock = false;
            RefreshNightTabContent();
            _nextDayButton.Disabled = false;
            _nextDayButton.Text = "START NIGHT 1 · NEW RUN";
            _nightSummaryLabel.Text = $"Touched grass · total {_saveData.GrassPoints} GP";
            SetReportHero(GlowStyle.Grass, $"+{earned}", $"Grass {(earned == 1 ? "Point" : "Points")}. The sun was weird. Spend them, then start night 1.");
            _prestigeButton.Visible = false;
            ShowShopPhase(animate: true);
        }

        private void OnStartNightPressed()
        {
            if (!_isBillUnpaidLock)
            {
                if (_saveData.TotalRunDopamine > 0)
                {
                    _saveData.CurrentDay++;
                }
                _saveData.NightCompleted = false;
                SaveProgress();
                StartNewDayTurn();
            }
        }

        private void UpdateNightFooterButtons()
        {
            int nextNight = _saveData.TotalRunDopamine > 0 ? _saveData.CurrentDay + 1 : _saveData.CurrentDay;
            _nextDayButton.Disabled = _isBillUnpaidLock;
            _nextDayButton.Text = _isBillUnpaidLock ? "POWER CUT · TOUCH GRASS" : $"START NIGHT {nextNight}";
        }

        private void RefreshNightTabContent()
        {
            foreach (Node child in _shopButtonsContainer.GetChildren())
            {
                child.QueueFree();
            }

            foreach (var (category, tab) in _shopTabButtons)
            {
                bool active = category == _currentNightTab;
                Color bg = active
                    ? (category == UpgradeCategory.Grass ? GlowStyle.Grass : GlowStyle.Dopamine)
                    : Colors.White;
                StyleAppButton(tab, bg, GlowStyle.BodyBlack, 16, 12, 11, 5);
            }

            _shopDopamineChip.Text = GlowStyle.FormatBig(Math.Floor(_saveData.CurrentDopamine));
            _shopCashChip.Text = GlowStyle.FormatCash(Math.Floor(_saveData.CurrentMoney * 100) / 100);
            _shopGrassChip.Text = $"{_saveData.GrassPoints} GP";
            _shopGrassChipPanel.Visible = _currentNightTab == UpgradeCategory.Grass || _saveData.GrassPoints > 0;

            foreach (var upgrade in _balanceConfig.UpgradesIn(_currentNightTab))
            {
                _shopButtonsContainer.AddChild(BuildShopCard(upgrade));
                if (upgrade.Id == "CRT_01" && GetUpgradeLevel("CRT_01") > 0) _shopButtonsContainer.AddChild(BuildCreatorPostCard());
            }

            if (_currentNightTab == UpgradeCategory.Desk) _shopButtonsContainer.AddChild(BuildVictoryCard());
            if (_currentNightTab == UpgradeCategory.Rack) AddRackRoles();

            ApplyCurrentTextScale(_shopPhone);
        }

        private static Color CategoryColor(UpgradeCategory? category) => category switch
        {
            UpgradeCategory.Mind => GlowStyle.Dopamine,
            UpgradeCategory.Desk => GlowStyle.Eyes,
            UpgradeCategory.Snacks => GlowStyle.Mythic,
            UpgradeCategory.Rack => GlowStyle.Violet,
            _ => GlowStyle.Grass
        };

        private static Color CurrencyColor(UpgradeCurrency currency) => currency switch
        {
            UpgradeCurrency.Cash => GlowStyle.Cash,
            UpgradeCurrency.Grass => GlowStyle.Grass,
            _ => GlowStyle.Dopamine
        };

        private PanelContainer BuildShopCard(UpgradeDefinition upgrade)
        {
            bool isGrass = upgrade.Currency == UpgradeCurrency.Grass;
            int level = isGrass ? GetGrassLevel(upgrade.Id) : GetUpgradeLevel(upgrade.Id);
            int maxLevel = upgrade.MaxLevel ?? int.MaxValue;
            bool maxed = level >= maxLevel;
            bool locked = upgrade.UnlockNight is int unlock && _saveData.CurrentDay < unlock;

            if (locked)
            {
                return LockedCard(upgrade.Name, $"Unlocks on night {upgrade.UnlockNight}. {upgrade.Desc}");
            }

            double cost = isGrass
                ? Math.Round(_economy.CalculateUpgradeCost(upgrade, level))
                : Math.Round(_economy.CalculateUpgradeCost(upgrade, level), 1);
            double wallet = upgrade.Currency switch
            {
                UpgradeCurrency.Cash => _saveData.CurrentMoney,
                UpgradeCurrency.Grass => _saveData.GrassPoints,
                _ => _saveData.CurrentDopamine
            };
            bool affordable = wallet >= cost;

            string levelText = upgrade.PerNight
                ? (maxed ? "ready for tonight" : "tonight only")
                : maxLevel == 1
                    ? (maxed ? "owned" : "")
                    : upgrade.MaxLevel.HasValue ? $"{level}/{maxLevel}" : $"lv {level}";

            string? effectLine = null;
            if (UpgradeEffects.At(_balanceConfig, upgrade, level) is UpgradeEffect now)
            {
                effectLine = maxed
                    ? $"{now.Label}: {now.Value}"
                    : $"{now.Label}: {now.Value} → {UpgradeEffects.At(_balanceConfig, upgrade, level + 1)!.Value.Value}";
            }

            string? shortLine = null;
            if (!maxed && !affordable)
            {
                double missing = cost - wallet;
                shortLine = upgrade.Currency switch
                {
                    UpgradeCurrency.Cash => $"{GlowStyle.FormatCash(Math.Ceiling(missing * 100) / 100)} short",
                    UpgradeCurrency.Grass => $"{missing:0} GP short",
                    _ => $"{GlowStyle.FormatBig(Math.Ceiling(missing))} dopamine short"
                };
            }

            string price = cost <= 0 ? "FREE" : upgrade.Currency switch
            {
                UpgradeCurrency.Cash => GlowStyle.FormatCash(cost),
                UpgradeCurrency.Grass => $"{cost:0} GP",
                _ => GlowStyle.FormatBig(cost)
            };
            var buy = new Button { Text = maxed ? (upgrade.PerNight ? "READY" : "MAX") : price, Disabled = maxed || !affordable, TooltipText = upgrade.Desc };
            StyleAppButton(buy, CurrencyColor(upgrade.Currency), GlowStyle.Display, 16, 12, 10, 8);
            buy.Pressed += () => BuyFromShop(upgrade);

            return ShopCard(upgrade.Icon, CategoryColor(upgrade.Category), upgrade.Name, levelText, upgrade.Desc, effectLine, shortLine, buy);
        }

        /// <summary>Creator mode's action: turn part of the dopamine wallet into cash, once per night.</summary>
        private PanelContainer BuildCreatorPostCard()
        {
            double cost = RunFormulas.CreatorPostCost(_balanceConfig, _saveData);
            double cash = RunFormulas.CreatorPostCash(_balanceConfig, _saveData);
            bool canPost = RunFormulas.CanPostVideo(_saveData) && cost > 0;
            double share = _balanceConfig.GetUpgrade("CRT_01").Param("dopamineShare");
            var post = new Button { Text = canPost ? "POST" : "POSTED", Disabled = !canPost };
            StyleAppButton(post, GlowStyle.Cash, GlowStyle.Display, 16, 12, 10, 8);
            post.Pressed += () =>
            {
                if (!RunFormulas.CanPostVideo(_saveData)) return;
                double spend = RunFormulas.CreatorPostCost(_balanceConfig, _saveData);
                double earn = RunFormulas.CreatorPostCash(_balanceConfig, _saveData);
                _saveData.CurrentDopamine -= spend;
                _saveData.CurrentMoney += earn;
                _saveData.LastPostNight = _saveData.CurrentDay;
                _audio?.PlaySfx("sfx_mythic", 1.2f);
                _boughtTonight.Add($"Sponsor {GlowStyle.FormatCash(earn)}");
                SaveProgress();
                RefreshArriving();
                RefreshNightTabContent();
                RefreshActiveHud(BuildPhoneStats(isPrimary: true, SecondaryRole.Standard));
            };
            string effect = canPost
                ? $"-{GlowStyle.FormatBig(cost)} dopamine → +{GlowStyle.FormatCash(cash)}"
                : "Already posted after this night. Come back tomorrow.";
            return ShopCard("camera", GlowStyle.Cash, "Post tonight's video", canPost ? "1 left" : "done",
                $"Spend {share * 100:0}% of your dopamine. The sponsor pays more every night.", effect, null, post);
        }

        private PanelContainer ShopCard(string icon, Color tile, string name, string levelText, string desc, string? effect, string? shortLine, Control action)
        {
            var card = new PanelContainer();
            card.AddThemeStyleboxOverride("panel", GlowStyle.Sticker(Colors.White, 16, 3, 4, 12, 12));
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 12);
            card.AddChild(row);

            var glyph = GlyphTile(icon, tile, 58);
            glyph.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(glyph);

            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            text.AddThemeConstantOverride("separation", 1);
            var title = new HBoxContainer();
            title.AddThemeConstantOverride("separation", 8);
            var nameLabel = WrapLabel(name, GlowStyle.BodyBlack, 18, GlowStyle.Ink);
            title.AddChild(nameLabel);
            if (levelText.Length > 0) title.AddChild(GlowStyle.MakeLabel(levelText, GlowStyle.Body, 15, GlowStyle.AppMuted));
            text.AddChild(title);
            text.AddChild(WrapLabel(desc, GlowStyle.Body, 14, GlowStyle.AppMuted));
            if (effect != null) text.AddChild(WrapLabel(effect, GlowStyle.BodyBlack, 15, GlowStyle.AppText));
            if (shortLine != null) text.AddChild(GlowStyle.MakeLabel(shortLine, GlowStyle.BodyBlack, 14, GlowStyle.AppBad));
            row.AddChild(text);

            action.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(action);
            return card;
        }

        private static PanelContainer LockedCard(string name, string condition)
        {
            var card = new PanelContainer();
            card.AddThemeStyleboxOverride("panel", GlowStyle.Flat(GlowStyle.AppCream, 16, 12, 12, 3, LockedLine));
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 12);
            card.AddChild(row);
            var glyph = new GlowGlyph
            {
                Glyph = "lock",
                TileColor = GlowStyle.AppLine,
                BorderColor = LockedLine,
                GlyphColor = GlowStyle.AppMuted,
                CustomMinimumSize = new Vector2(58, 58),
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };
            row.AddChild(glyph);
            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            text.AddThemeConstantOverride("separation", 2);
            text.AddChild(WrapLabel(name, GlowStyle.BodyBlack, 18, GlowStyle.AppText));
            text.AddChild(WrapLabel(condition, GlowStyle.Body, 14, GlowStyle.AppMuted));
            row.AddChild(text);
            return card;
        }

        private PanelContainer BuildVictoryCard()
        {
            var victory = _balanceConfig.Victory;
            const string name = "Fiber-Optic Neural Link";
            const string desc = "Plug your brain into the feed. Wins the campaign and unlocks endless mode.";
            if (!_saveData.EndlessModeUnlocked && _saveData.CurrentDay < victory.Day)
            {
                return LockedCard(name, $"Unlocks on night {victory.Day}. {desc}");
            }

            bool owned = _saveData.EndlessModeUnlocked;
            bool affordable = _saveData.CurrentMoney >= victory.MoneyCost;
            var buy = new Button { Text = owned ? "MAX" : GlowStyle.FormatCash(victory.MoneyCost), Disabled = owned || !affordable };
            StyleAppButton(buy, GlowStyle.Cash, GlowStyle.Display, 16, 12, 10, 8);
            buy.Pressed += () =>
            {
                if (_saveData.CurrentMoney >= victory.MoneyCost && !_saveData.EndlessModeUnlocked)
                {
                    _saveData.CurrentMoney -= victory.MoneyCost;
                    _saveData.EndlessModeUnlocked = true;
                    SaveProgress();
                    _audio?.PlaySfx("sfx_mythic");
                    _boughtTonight.Add("Neural link");
                    RefreshArriving();
                    RefreshNightTabContent();
                    _nightSummaryLabel.Text = $"You made it to night {victory.Day}. Endless mode is unlocked.";
                    SetReportHero(GlowStyle.Mythic, "WIN", "Campaign complete. The feed never ends now. Neither do you.");
                }
            };
            string? shortLine = owned || affordable ? null : $"{GlowStyle.FormatCash(Math.Ceiling((victory.MoneyCost - _saveData.CurrentMoney) * 100) / 100)} short";
            return ShopCard("link", GlowStyle.Mythic, name, owned ? "owned" : "", desc, null, shortLine, buy);
        }

        private void BuyFromShop(UpgradeDefinition upgrade)
        {
            bool bought;
            if (upgrade.Currency == UpgradeCurrency.Grass)
            {
                int level = GetGrassLevel(upgrade.Id);
                int cost = (int)Math.Round(_economy.CalculateUpgradeCost(upgrade, level));
                bought = _saveData.GrassPoints >= cost && level < (upgrade.MaxLevel ?? int.MaxValue);
                if (bought)
                {
                    _saveData.GrassPoints -= cost;
                    _saveData.GrassUpgradeLevels[upgrade.Id] = level + 1;
                    _audio?.PlaySfx("sfx_crit_like");
                }
            }
            else
            {
                bool isMoney = upgrade.Currency == UpgradeCurrency.Cash;
                double pool = isMoney ? _saveData.CurrentMoney : _saveData.CurrentDopamine;
                bought = _buyUpgrade.TryBuyUpgrade(upgrade, ref pool, _saveData.RunUpgradeLevels);
                if (bought)
                {
                    if (isMoney) _saveData.CurrentMoney = pool;
                    else _saveData.CurrentDopamine = pool;
                    _audio?.PlaySfx("sfx_like", 1.15f);
                }
            }

            if (!bought) return;
            _boughtTonight.Add(upgrade.Name);
            SaveProgress();
            RefreshActiveHud(BuildPhoneStats(isPrimary: true, SecondaryRole.Standard));
            RefreshArriving();
            RefreshPrestigeButton();
            RefreshNightTabContent();
        }

        private static string RoleName(SecondaryRole role) => role switch
        {
            SecondaryRole.AdShield => "Ad Shield",
            SecondaryRole.CryptoMiner => "Crypto Miner",
            SecondaryRole.MythicRadar => "Mythic Radar",
            _ => "Scroller"
        };

        private string RoleDescription(SecondaryRole role)
        {
            var roles = _balanceConfig.SecondaryRoles;
            return role switch
            {
                SecondaryRole.AdShield => $"Ads are {roles.AdShieldAdMultiplier * 100:0}% as likely on it. {roles.DopamineFactor * 100:0}% dopamine.",
                SecondaryRole.CryptoMiner => $"{GlowStyle.FormatCash(roles.CryptoMinerCashPerVideo)} per video, only {roles.CryptoMinerDopamineFactor * 100:0}% dopamine.",
                SecondaryRole.MythicRadar => $"Mythic chance {roles.MythicRadarMythicChance * 100:0.#}% instead of {_balanceConfig.Feed.MythicChance * 100:0.#}%.",
                _ => $"Plain scrolling at {roles.DopamineFactor * 100:0}% dopamine."
            };
        }

        private void AddRackRoles()
        {
            int phones = RunFormulas.SecondaryPhoneCount(_balanceConfig, _saveData);
            _shopButtonsContainer.AddChild(GlowStyle.MakeLabel("PHONE ROLES", GlowStyle.Display, 14, GlowStyle.AppMuted));
            if (phones == 0)
            {
                _shopButtonsContainer.AddChild(WrapLabel("No rack phones yet. Buy one above and give it a job here.", GlowStyle.Body, 15, GlowStyle.AppMuted));
                return;
            }
            _shopButtonsContainer.AddChild(WrapLabel("Tap a role to change it. Saved for every night.", GlowStyle.Body, 15, GlowStyle.AppMuted));

            int roleCount = Enum.GetValues<SecondaryRole>().Length;
            for (int slot = 0; slot < phones; slot++)
            {
                int phoneSlot = slot;
                SecondaryRole role = _saveData.RackRole(slot);
                var change = new Button { Text = RoleShortName(role), TooltipText = "Change this phone's role" };
                StyleAppButton(change, RoleColor(role), GlowStyle.Display, 14, 12, 10, 8);
                change.Pressed += () =>
                {
                    var next = (SecondaryRole)(((int)_saveData.RackRole(phoneSlot) + 1) % roleCount);
                    _saveData.SetRackRole(phoneSlot, next);
                    if (phoneSlot + 1 < _session.Phones.Count) _session.Phones[phoneSlot + 1].Role = next;
                    _audio?.PlaySfx("sfx_swipe", 1.15f);
                    SaveProgress();
                    RebuildSecondaryRackUI();
                    RefreshNightTabContent();
                };
                _shopButtonsContainer.AddChild(ShopCard("phone", RoleColor(role), $"Phone #{slot + 2}", RoleName(role), RoleDescription(role), null, null, change));
            }
        }
    }
}
