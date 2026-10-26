using System;
using System.Collections.Generic;
using Doomscroll.Core.Entities;
using Doomscroll.Core.Events;
using Doomscroll.Core.Services;
using Doomscroll.Presentation.UI;
using Godot;

namespace Doomscroll.Presentation.DeskScene
{
    /// <summary>
    /// The in-game desk scene in the "3 AM Glow" art direction: a dark room lit only by the phone,
    /// a big vertical phone in the center, sticker HUD, a charging rack, and the retro "Slacc 98" laptop.
    /// This partial only builds and refreshes visuals; all rules stay in the controller and the Core.
    /// Layout is authored for the 1920x1080 base viewport, with left, center and right groups anchored to their edge.
    /// </summary>
    public partial class DeskSceneController
    {
        private enum HAnchor { Left, Center, Right }
        private enum FlyTarget { None, Dopamine, Cash }

        private sealed class SecondaryPhoneRow
        {
            public int PhoneId;
            public Button RoleButton = null!;
            public MiniPhoneButton Phone = null!;
            public Label BatteryLabel = null!;
            public bool LowBattery;
        }

        private static readonly string[] FakeComments = { "real", "who is here at 3am", "first", "no thoughts", "me fr", "ratio", "go to sleep" };

        // Scene roots
        private DeskEnvironmentBackdrop _deskBackdrop = null!;
        private Control _sceneRoot = null!;
        private Control _fxLayer = null!;

        // HUD stickers
        private Label _dayClockLabel = null!;
        private Label _dopamineBadgeLabel = null!;
        private Label _cashBadgeLabel = null!;
        private PanelContainer _cashSticker = null!;
        private Label _billLabel = null!;
        private StyleBoxFlat _billStickerStyle = null!;
        private HBoxContainer _streakRow = null!;
        private Panel[] _streakPills = Array.Empty<Panel>();
        private StyleBoxFlat _pillOn = null!;
        private StyleBoxFlat _pillOff = null!;
        private Label _streakLabel = null!;
        private int _streakShown = -1;
        private ProgressBar _feverBar = null!;
        private PanelContainer _feverBanner = null!;
        private Label _feverBannerLabel = null!;
        private bool _feverVisualActive;
        private Tween? _dopamineBounce;
        private Tween? _cashBounce;

        // Eyes, brightness and power
        private Label _eyesLabel = null!;
        private Label _eyeWarningLabel = null!;
        private ProgressBar _eyeBar = null!;
        private StyleBoxFlat _eyeFill = null!;
        private Label _overloadLabel = null!;
        private ProgressBar _overloadBar = null!;
        private StyleBoxFlat _overloadFill = null!;
        private Button _breakerResetButton = null!;
        private Button _chargerButton = null!;
        private Button _brightnessButton = null!;
        private Button _eyeDropsButton = null!;

        // Pause menu toggles (built with the pause menu)

        // The phone
        private PanelContainer _phonePanel = null!;
        private StyleBoxFlat _phoneBezelStyle = null!;
        private ColorRect _videoCanvas = null!;
        private Control _screenInner = null!;
        private PhoneScreenGraphic _screenGraphic = null!;
        private DopamineParticleSystem _particleSystem = null!;
        private PanelContainer _videoBadge = null!;
        private StyleBoxFlat _videoBadgeStyle = null!;
        private Label _videoBadgeLabel = null!;
        // Why phone #1 pays less right now: blurry eyes and a boring niche.
        private HBoxContainer _effectChips = null!;
        private PanelContainer _blurChip = null!;
        private Label _blurChipLabel = null!;
        private PanelContainer _boredChip = null!;
        private Label _boredChipLabel = null!;
        private GlowGlyph _chargeBolt = null!;
        private PanelContainer _captionSticker = null!;
        private Label _videoTitleLabel = null!;
        private Label _videoHandleLabel = null!;
        private Label _videoSoundLabel = null!;
        private ColorRect _phoneBlurOverlay = null!;
        private ShaderMaterial? _phoneBlurMaterial;
        private ColorRect _eyelidOverlay = null!;
        private ShaderMaterial? _eyelidMaterial;
        private ProgressBar _videoProgressBar = null!;
        private StyleBoxFlat _videoProgressFill = null!;
        private Panel _thresholdMarker = null!;
        private ProgressBar _batteryBar = null!;
        private StyleBoxFlat _batteryFill = null!;
        private Label _batteryPctLabel = null!;
        private PanelContainer _hintSticker = null!;
        private StyleBoxFlat _hintStyle = null!;
        private Label _phoneStateLabel = null!;
        private GlowIconButton _likeButton = null!;
        private GlowIconButton _commentButton = null!;
        private GlowIconButton _shareButton = null!;
        private GlowIconButton _discButton = null!;
        private GlowIconButton _scrollButton = null!;
        private GlowIconButton _pauseButton = null!;
        private string _countsForTitle = string.Empty;

        // Rack
        private HBoxContainer _secondaryRack = null!;
        private readonly List<SecondaryPhoneRow> _secondaryRows = new();

        // Laptop
        private PanelContainer _laptopPanel = null!;
        private Label _laptopStatusLabel = null!;
        private Label _bossAvatarLabel = null!;
        private StyleBoxFlat _bossAvatarStyle = null!;
        private Label _bossChatMessageLabel = null!;
        private PanelContainer _bossChatBubblePanel = null!;
        private StyleBoxFlat _bossStripStyle = null!;
        private Label _bossAlertLabel = null!;
        private ProgressBar _bossTimerBar = null!;
        private Button _laptopReplyButton = null!;
        private int _laptopVisualState = -1;
        private readonly ProgressBar[] _algoBars = new ProgressBar[4];
        private readonly StyleBoxFlat[] _algoFills = new StyleBoxFlat[4];
        private readonly Label[] _algoLabels = new Label[4];
        private Label _algoRadarLabel = null!;
        private Button _randomEventButton = null!;

        // Log and toast
        private RichTextLabel _eventFeedLabel = null!;
        private PanelContainer _achievementToastPanel = null!;
        private Label _achievementToastLabel = null!;

        /// <summary>Settings > Reduced motion: no bounces, pulses, shakes or flying numbers.</summary>
        private bool ReducedMotion => _settings.ReducedMotion;

        private static T Place<T>(Control parent, T control, HAnchor anchor, float x, float y, float width, float height) where T : Control
        {
            float a = anchor == HAnchor.Center ? 0.5f : anchor == HAnchor.Right ? 1f : 0f;
            float baseX = anchor == HAnchor.Center ? 960f : anchor == HAnchor.Right ? 1920f : 0f;
            control.AnchorLeft = a;
            control.AnchorRight = a;
            control.AnchorTop = 0f;
            control.AnchorBottom = 0f;
            control.OffsetLeft = x - baseX;
            control.OffsetRight = x - baseX + width;
            control.OffsetTop = y;
            control.OffsetBottom = y + height;
            parent.AddChild(control);
            return control;
        }

        private static void KeepPivotCentered(Control control)
        {
            control.PivotOffset = control.Size / 2f;
            control.Resized += () => control.PivotOffset = control.Size / 2f;
        }

        private static void Tilt(Control control, float degrees)
        {
            KeepPivotCentered(control);
            control.Rotation = Mathf.DegToRad(degrees);
        }

        // ------------------------------------------------------------------ build

        private void BuildDeskScene()
        {
            Theme = GlowStyle.CreateTheme();

            _deskBackdrop = new DeskEnvironmentBackdrop { MouseFilter = MouseFilterEnum.Ignore };
            _deskBackdrop.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(_deskBackdrop);

            _sceneRoot = new Control { MouseFilter = MouseFilterEnum.Ignore };
            _sceneRoot.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(_sceneRoot);

            BuildHudStickers();
            BuildDopamineCounter();
            BuildPhone();
            BuildRack();
            BuildEyesAndPower();
            BuildLaptop();

            _fxLayer = new Control { MouseFilter = MouseFilterEnum.Ignore };
            _fxLayer.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(_fxLayer);

            _achievementToastPanel = GlowStyle.StickerPanel(GlowStyle.Mythic, 14, 4, 16, 10);
            _achievementToastPanel.Visible = false;
            _achievementToastLabel = GlowStyle.MakeLabel("", GlowStyle.BodyBlack, 19, GlowStyle.Ink);
            _achievementToastPanel.AddChild(_achievementToastLabel);
            Place(this, _achievementToastPanel, HAnchor.Left, 29, 96, 0, 0);
        }

        private void BuildHudStickers()
        {
            var night = GlowStyle.StickerPanel(GlowStyle.Eyes, 12);
            _dayClockLabel = GlowStyle.MakeLabel("NIGHT 1/30", GlowStyle.Display, 22, GlowStyle.Ink);
            night.AddChild(_dayClockLabel);
            Place(_sceneRoot, night, HAnchor.Left, 29, 22, 0, 0);
            Tilt(night, -2f);

            var right = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End };
            right.AddThemeConstantOverride("separation", 14);
            right.AnchorLeft = 1f;
            right.AnchorRight = 1f;
            right.OffsetLeft = -24f;
            right.OffsetRight = -24f;
            right.OffsetTop = 20f;
            right.GrowHorizontal = GrowDirection.Begin;
            _sceneRoot.AddChild(right);

            _cashSticker = GlowStyle.StickerPanel(GlowStyle.Cash, 12);
            _cashSticker.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            _cashBadgeLabel = GlowStyle.MakeLabel("$0", GlowStyle.Display, 26, GlowStyle.Ink);
            _cashSticker.AddChild(_cashBadgeLabel);
            right.AddChild(_cashSticker);
            Tilt(_cashSticker, -2f);

            var bill = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            _billStickerStyle = GlowStyle.Sticker(GlowStyle.Fever, 12);
            bill.AddThemeStyleboxOverride("panel", _billStickerStyle);
            _billLabel = GlowStyle.MakeLabel("BILL IN 3 NIGHTS", GlowStyle.Display, 17, GlowStyle.Ink);
            bill.AddChild(_billLabel);
            right.AddChild(bill);
            Tilt(bill, 2f);

            _pauseButton = new GlowIconButton
            {
                Icon = GlowIconButton.Glyph.Menu,
                Round = true,
                Fill = GlowStyle.Paper,
                IconSize = 30f,
                CustomMinimumSize = new Vector2(66, 66),
                TooltipText = "Pause (Esc)"
            };
            _pauseButton.Pressed += () => SetPaused(true);
            right.AddChild(_pauseButton);
        }

        private void BuildDopamineCounter()
        {
            var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            box.AddThemeConstantOverride("separation", -10);
            var caption = GlowStyle.MakeLabel("DOPAMINE", GlowStyle.Display, 18, GlowStyle.Dopamine);
            caption.HorizontalAlignment = HorizontalAlignment.Center;
            box.AddChild(caption);
            _dopamineBadgeLabel = GlowStyle.MakeLabel("0", GlowStyle.Display, 76, Colors.White, outline: 14);
            GlowStyle.AddTextShadow(_dopamineBadgeLabel, GlowStyle.Dopamine, 6);
            _dopamineBadgeLabel.HorizontalAlignment = HorizontalAlignment.Center;
            box.AddChild(_dopamineBadgeLabel);
            Place(_sceneRoot, box, HAnchor.Center, 640, 2, 640, 112);
            KeepPivotCentered(_dopamineBadgeLabel);

            _streakRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
            _streakRow.AddThemeConstantOverride("separation", 7);
            Place(_sceneRoot, _streakRow, HAnchor.Center, 640, 112, 640, 24);
            _pillOn = GlowStyle.Sticker(GlowStyle.Mythic, 7, 2, 0, 0, 0);
            _pillOff = GlowStyle.Sticker(GlowStyle.DeskEdge, 7, 2, 0, 0, 0);
            int pills = Math.Max(1, _balanceConfig.Combo.SwipesToFever);
            _streakPills = new Panel[pills];
            for (int i = 0; i < pills; i++)
            {
                var pill = new Panel { CustomMinimumSize = new Vector2(34, 14), MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter };
                pill.AddThemeStyleboxOverride("panel", _pillOff);
                _streakPills[i] = pill;
                _streakRow.AddChild(pill);
            }
            _streakRow.AddChild(new Control { CustomMinimumSize = new Vector2(4, 0), MouseFilter = MouseFilterEnum.Ignore });
            _streakLabel = GlowStyle.MakeLabel("STREAK 0/5", GlowStyle.Display, 15, GlowStyle.Mythic, outline: 6);
            _streakRow.AddChild(_streakLabel);

            _feverBar = new ProgressBar { MinValue = 0, MaxValue = 1, Step = 0, Value = 1, Visible = false, MouseFilter = MouseFilterEnum.Ignore };
            GlowStyle.StyleBar(_feverBar, GlowStyle.Fever, GlowStyle.Ink, 9);
            Place(_sceneRoot, _feverBar, HAnchor.Center, 760, 114, 400, 20);
        }

        private void BuildPhone()
        {
            _phoneBezelStyle = new StyleBoxFlat
            {
                BgColor = GlowStyle.Ink,
                BorderColor = GlowStyle.Dopamine,
                ShadowColor = new Color(GlowStyle.ScreenGlow, 0.28f),
                ShadowSize = 46,
                AntiAliasing = true
            };
            _phoneBezelStyle.SetCornerRadiusAll(58);
            _phoneBezelStyle.SetBorderWidthAll(0);
            _phoneBezelStyle.SetContentMarginAll(14);
            _phonePanel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            _phonePanel.AddThemeStyleboxOverride("panel", _phoneBezelStyle);
            Place(_sceneRoot, _phonePanel, HAnchor.Center, 732, 142, 456, 912);

            var screenMask = new Panel { ClipChildren = ClipChildrenMode.Only, MouseFilter = MouseFilterEnum.Ignore };
            var maskStyle = new StyleBoxFlat { BgColor = Colors.White, AntiAliasing = true };
            maskStyle.SetCornerRadiusAll(44);
            screenMask.AddThemeStyleboxOverride("panel", maskStyle);
            _phonePanel.AddChild(screenMask);

            _videoCanvas = new ColorRect { Color = Color.FromHtml("#2A1458"), MouseFilter = MouseFilterEnum.Stop };
            _videoCanvas.SetAnchorsPreset(LayoutPreset.FullRect);
            _videoCanvas.GuiInput += OnPhoneCanvasGuiInput;
            screenMask.AddChild(_videoCanvas);

            // Moves as one block during the swipe transition: the video art and its caption.
            _screenInner = new Control { MouseFilter = MouseFilterEnum.Ignore };
            _screenInner.SetAnchorsPreset(LayoutPreset.FullRect);
            _videoCanvas.AddChild(_screenInner);

            _screenGraphic = new PhoneScreenGraphic { MouseFilter = MouseFilterEnum.Ignore };
            _screenGraphic.SetAnchorsPreset(LayoutPreset.FullRect);
            _screenInner.AddChild(_screenGraphic);

            _captionSticker = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(26, 476) };
            _captionSticker.AddThemeStyleboxOverride("panel", GlowStyle.Sticker(Colors.White, 14, 3, 4, 14, 10));
            _videoTitleLabel = GlowStyle.MakeLabel("Loading feed...", GlowStyle.BodyBlack, 23, GlowStyle.Ink);
            _videoTitleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _videoTitleLabel.CustomMinimumSize = new Vector2(292, 0);
            _captionSticker.AddChild(_videoTitleLabel);
            _screenInner.AddChild(_captionSticker);
            Tilt(_captionSticker, -2f);

            BuildPhoneChrome();
            BuildPhoneRail();
            BuildPhoneBottom();

            _particleSystem = new DopamineParticleSystem { MouseFilter = MouseFilterEnum.Ignore };
            _particleSystem.SetAnchorsPreset(LayoutPreset.FullRect);
            _videoCanvas.AddChild(_particleSystem);

            _phoneBlurOverlay = new ColorRect { MouseFilter = MouseFilterEnum.Ignore, Color = new Color(1, 1, 1, 0) };
            _phoneBlurOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
            var blurShader = GD.Load<Shader>("res://Assets/Shaders/EyeBlur.gdshader");
            if (blurShader != null)
            {
                _phoneBlurMaterial = new ShaderMaterial { Shader = blurShader };
                _phoneBlurOverlay.Material = _phoneBlurMaterial;
            }
            // Outside the rounded clip group: the blur samples the screen texture, which a clip group cannot see.
            _phonePanel.AddChild(_phoneBlurOverlay);

            // Eyelids close over the phone screen only, so the HUD and fatigue warnings stay readable.
            _eyelidOverlay = new ColorRect { MouseFilter = MouseFilterEnum.Ignore, Color = new Color(0, 0, 0, 0) };
            _eyelidOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
            var eyelidShader = GD.Load<Shader>("res://Assets/Shaders/EyelidsVignette.gdshader");
            if (eyelidShader != null)
            {
                _eyelidMaterial = new ShaderMaterial { Shader = eyelidShader };
                _eyelidOverlay.Material = _eyelidMaterial;
            }
            _videoCanvas.AddChild(_eyelidOverlay);

            // What the player acts on stays above the eyelids: battery, swipe hint and the progress bar.
            _videoCanvas.MoveChild(_batteryBar.GetParent(), -1);
            _videoCanvas.MoveChild(_hintSticker.GetParent(), -1);
            _videoCanvas.MoveChild(_videoProgressBar, -1);

            BuildSpamPopup();

            var island = new Panel { MouseFilter = MouseFilterEnum.Ignore };
            var islandStyle = new StyleBoxFlat { BgColor = GlowStyle.Ink };
            islandStyle.SetCornerRadiusAll(15);
            island.AddThemeStyleboxOverride("panel", islandStyle);
            island.AnchorLeft = 0.5f;
            island.AnchorRight = 0.5f;
            island.OffsetLeft = -62;
            island.OffsetRight = 62;
            island.OffsetTop = 12;
            island.OffsetBottom = 42;
            _videoCanvas.AddChild(island);

            var homeBar = new ColorRect { Color = new Color(1, 1, 1, 0.7f), MouseFilter = MouseFilterEnum.Ignore };
            homeBar.AnchorLeft = 0.5f;
            homeBar.AnchorRight = 0.5f;
            homeBar.AnchorTop = 1f;
            homeBar.AnchorBottom = 1f;
            homeBar.OffsetLeft = -70;
            homeBar.OffsetRight = 70;
            homeBar.OffsetTop = -16;
            homeBar.OffsetBottom = -10;
            _videoCanvas.AddChild(homeBar);

            _feverBanner = GlowStyle.StickerPanel(GlowStyle.Fever, 18, 6, 22, 6);
            _feverBanner.Visible = false;
            _feverBannerLabel = GlowStyle.MakeLabel("BRAINROT FEVER x3", GlowStyle.Display, 46, GlowStyle.Ink);
            _feverBanner.AddChild(_feverBannerLabel);
            var bannerHolder = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
            Place(_sceneRoot, bannerHolder, HAnchor.Center, 520, 450, 880, 120);
            bannerHolder.AddChild(_feverBanner);
            Tilt(_feverBanner, -6f);

            _scrollButton = new GlowIconButton
            {
                Icon = GlowIconButton.Glyph.Down,
                Round = true,
                Fill = GlowStyle.Paper,
                Caption = "NEXT",
                IconSize = 46f,
                TooltipText = "Next video (Down / Space / Wheel)"
            };
            _scrollButton.Pressed += TryManualPrimaryScroll;
            Place(_sceneRoot, _scrollButton, HAnchor.Center, 1208, 918, 112, 132);
        }

        private void BuildPhoneChrome()
        {
            var tabs = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
            tabs.AddThemeConstantOverride("separation", 24);
            tabs.SetAnchorsPreset(LayoutPreset.TopWide);
            tabs.OffsetTop = 56;
            tabs.OffsetBottom = 90;
            tabs.AddChild(GlowStyle.MakeLabel("Following", GlowStyle.BodyBlack, 19, new Color(1, 1, 1, 0.6f)));
            var forYou = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            forYou.AddThemeConstantOverride("separation", 1);
            forYou.AddChild(GlowStyle.MakeLabel("For You", GlowStyle.BodyBlack, 19, Colors.White));
            forYou.AddChild(new ColorRect { Color = Colors.White, CustomMinimumSize = new Vector2(0, 3), MouseFilter = MouseFilterEnum.Ignore });
            tabs.AddChild(forYou);
            _videoCanvas.AddChild(tabs);

            var battery = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End };
            battery.AddThemeConstantOverride("separation", 6);
            battery.AnchorLeft = 1f;
            battery.AnchorRight = 1f;
            battery.OffsetLeft = -130;
            battery.OffsetRight = -28;
            battery.OffsetTop = 16;
            battery.OffsetBottom = 40;
            _chargeBolt = new GlowGlyph
            {
                Glyph = "bolt",
                ShowTile = false,
                GlyphColor = GlowStyle.Eyes,
                GlyphScale = 1f,
                Visible = false,
                CustomMinimumSize = new Vector2(16, 20),
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };
            battery.AddChild(_chargeBolt);
            _batteryPctLabel = GlowStyle.MakeLabel("100%", GlowStyle.Display, 14, GlowStyle.Cash);
            battery.AddChild(_batteryPctLabel);
            _batteryBar = new ProgressBar
            {
                MinValue = 0,
                MaxValue = 100,
                Value = 100,
                CustomMinimumSize = new Vector2(34, 16),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
                ShowPercentage = false
            };
            var batteryBack = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = GlowStyle.Paper, AntiAliasing = true };
            batteryBack.SetBorderWidthAll(2);
            batteryBack.SetCornerRadiusAll(4);
            _batteryFill = new StyleBoxFlat { BgColor = GlowStyle.Cash };
            _batteryFill.SetCornerRadiusAll(2);
            _batteryBar.AddThemeStyleboxOverride("background", batteryBack);
            _batteryBar.AddThemeStyleboxOverride("fill", _batteryFill);
            battery.AddChild(_batteryBar);
            _videoCanvas.AddChild(battery);

            _videoBadge = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(20, 100), Visible = false };
            _videoBadgeStyle = GlowStyle.Sticker(GlowStyle.Fever, 10, 3, 3, 10, 4);
            _videoBadge.AddThemeStyleboxOverride("panel", _videoBadgeStyle);
            _videoBadgeLabel = GlowStyle.MakeLabel("", GlowStyle.Display, 15, GlowStyle.Ink);
            _videoBadge.AddChild(_videoBadgeLabel);
            _videoCanvas.AddChild(_videoBadge);

            _effectChips = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(20, 100) };
            _effectChips.AddThemeConstantOverride("separation", 8);
            _blurChip = EffectChip(GlowStyle.Eyes, out _blurChipLabel);
            _boredChip = EffectChip(Color.FromHtml("#C9C3DD"), out _boredChipLabel);
            _effectChips.AddChild(_blurChip);
            _effectChips.AddChild(_boredChip);
            _videoCanvas.AddChild(_effectChips);
        }

        private static PanelContainer EffectChip(Color bg, out Label label)
        {
            var chip = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
            chip.AddThemeStyleboxOverride("panel", GlowStyle.Sticker(bg, 10, 3, 3, 10, 4));
            label = GlowStyle.MakeLabel("", GlowStyle.Display, 14, GlowStyle.Ink);
            chip.AddChild(label);
            return chip;
        }

        /// <summary>Chips on the video that say why phone #1 earns less right now.</summary>
        private void RefreshEffectChips(PhoneEntity primary)
        {
            double blurLoss = 1.0 - RunFormulas.BlurDopamineMultiplier(_balanceConfig, _session.Eyes.GetNormalizedBlur(LensBlurReduction));
            float boredom = primary.CurrentVideo == VideoCategory.Standard ? _nicheTolerance.Boredom(primary.CurrentNiche) : 0f;
            bool showBlur = blurLoss >= 0.01 && primary.HasBattery;
            bool showBored = boredom >= 0.05f && primary.HasBattery;
            _blurChip.Visible = showBlur;
            _boredChip.Visible = showBored;
            if (showBlur) _blurChipLabel.Text = $"BLURRY -{blurLoss * 100:0}%";
            if (showBored) _boredChipLabel.Text = $"BORED -{boredom * 100f:0}%";
            _effectChips.Position = new Vector2(20, _videoBadge.Visible ? 142 : 100);
        }

        private void BuildPhoneRail()
        {
            var rail = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
            rail.AddThemeConstantOverride("separation", 8);
            rail.AnchorLeft = 1f;
            rail.AnchorRight = 1f;
            rail.OffsetLeft = -84;
            rail.OffsetRight = -8;
            rail.OffsetTop = 330;
            rail.OffsetBottom = 750;
            _videoCanvas.AddChild(rail);

            var avatar = new Panel { CustomMinimumSize = new Vector2(56, 56), SizeFlagsHorizontal = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
            var avatarStyle = new StyleBoxFlat { BgColor = GlowStyle.Mythic, BorderColor = Colors.White, AntiAliasing = true };
            avatarStyle.SetBorderWidthAll(3);
            avatarStyle.SetCornerRadiusAll(28);
            avatar.AddThemeStyleboxOverride("panel", avatarStyle);
            rail.AddChild(avatar);

            _likeButton = new GlowIconButton { Icon = GlowIconButton.Glyph.Heart, Caption = "48.2K", IconSize = 46f, CustomMinimumSize = new Vector2(76, 82), TooltipText = "Like (Right / D / double-click)" };
            _likeButton.Pressed += () =>
            {
                PerformPrimaryLike();
                _likeButton.Pop();
            };
            rail.AddChild(_likeButton);

            _commentButton = new GlowIconButton { Icon = GlowIconButton.Glyph.Comment, Caption = "3.4K", IconSize = 42f, CustomMinimumSize = new Vector2(76, 80) };
            _commentButton.Pressed += () =>
            {
                _commentButton.Pop();
                SpawnFloatingText(FakeComments[_fxRng.Next(FakeComments.Length)], GlowStyle.Paper);
            };
            rail.AddChild(_commentButton);

            _shareButton = new GlowIconButton { Icon = GlowIconButton.Glyph.Share, Caption = "1.2K", IconSize = 42f, CustomMinimumSize = new Vector2(76, 80) };
            _shareButton.Pressed += () =>
            {
                _shareButton.Pop();
                SpawnFloatingText("sent to the group chat", GlowStyle.Eyes);
            };
            rail.AddChild(_shareButton);

            _discButton = new GlowIconButton { Icon = GlowIconButton.Glyph.Disc, IconSize = 50f, CustomMinimumSize = new Vector2(76, 60) };
            _discButton.Pressed += () => _discButton.Pop();
            rail.AddChild(_discButton);
        }

        private void BuildPhoneBottom()
        {
            _videoHandleLabel = GlowStyle.MakeLabel("@meowmentum", GlowStyle.BodyBlack, 20, Colors.White, outline: 4);
            AnchorBottomLeft(_videoHandleLabel, 22, -126, 330, -98);
            _videoCanvas.AddChild(_videoHandleLabel);

            _videoSoundLabel = GlowStyle.MakeLabel("original sound · 3 AM lofi", GlowStyle.Body, 16, new Color(1, 1, 1, 0.82f), outline: 4);
            AnchorBottomLeft(_videoSoundLabel, 22, -98, 330, -74);
            _videoCanvas.AddChild(_videoSoundLabel);

            var hintHolder = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
            hintHolder.AnchorLeft = 0f;
            hintHolder.AnchorRight = 1f;
            hintHolder.AnchorTop = 1f;
            hintHolder.AnchorBottom = 1f;
            hintHolder.OffsetTop = -186;
            hintHolder.OffsetBottom = -140;
            hintHolder.OffsetRight = -70;
            _videoCanvas.AddChild(hintHolder);
            _hintSticker = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
            _hintStyle = GlowStyle.Sticker(GlowStyle.Cash, 12, 3, 3, 12, 4);
            _hintSticker.AddThemeStyleboxOverride("panel", _hintStyle);
            _phoneStateLabel = GlowStyle.MakeLabel("", GlowStyle.Display, 16, GlowStyle.Ink);
            _hintSticker.AddChild(_phoneStateLabel);
            hintHolder.AddChild(_hintSticker);

            _videoProgressBar = new ProgressBar { MinValue = 0, MaxValue = 3.0, Step = 0, Value = 0, MouseFilter = MouseFilterEnum.Ignore };
            _videoProgressFill = GlowStyle.StyleBar(_videoProgressBar, GlowStyle.Dopamine, new Color(1, 1, 1, 0.22f), 6, 0);
            _videoProgressBar.AnchorLeft = 0f;
            _videoProgressBar.AnchorRight = 1f;
            _videoProgressBar.AnchorTop = 1f;
            _videoProgressBar.AnchorBottom = 1f;
            _videoProgressBar.OffsetLeft = 22;
            _videoProgressBar.OffsetRight = -22;
            _videoProgressBar.OffsetTop = -52;
            _videoProgressBar.OffsetBottom = -40;
            _videoCanvas.AddChild(_videoProgressBar);

            _thresholdMarker = new Panel { Size = new Vector2(9, 28), MouseFilter = MouseFilterEnum.Ignore, Visible = false };
            _thresholdMarker.AddThemeStyleboxOverride("panel", GlowStyle.Sticker(GlowStyle.Cash, 3, 2, 0, 0, 0));
            _videoProgressBar.AddChild(_thresholdMarker);
        }

        private static void AnchorBottomLeft(Control control, float left, float top, float right, float bottom)
        {
            control.AnchorLeft = 0f;
            control.AnchorRight = 0f;
            control.AnchorTop = 1f;
            control.AnchorBottom = 1f;
            control.OffsetLeft = left;
            control.OffsetTop = top;
            control.OffsetRight = right;
            control.OffsetBottom = bottom;
        }

        private void BuildSpamPopup()
        {
            _spamPopupPanel = new PanelContainer { Visible = false };
            _spamPopupPanel.AddThemeStyleboxOverride("panel", GlowStyle.Sticker(Colors.White, 18, 3, 4, 14, 12));
            _spamPopupPanel.AnchorLeft = 0f;
            _spamPopupPanel.AnchorRight = 1f;
            _spamPopupPanel.OffsetLeft = 16;
            _spamPopupPanel.OffsetRight = -16;
            _spamPopupPanel.OffsetTop = 104;
            _spamPopupPanel.OffsetBottom = 104;

            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 8);
            _spamPopupPanel.AddChild(box);

            var header = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            header.AddThemeConstantOverride("separation", 8);
            var appIcon = new Panel { CustomMinimumSize = new Vector2(26, 26), MouseFilter = MouseFilterEnum.Ignore };
            appIcon.AddThemeStyleboxOverride("panel", GlowStyle.Sticker(GlowStyle.Danger, 7, 2, 0, 0, 0));
            header.AddChild(appIcon);
            header.AddChild(GlowStyle.MakeLabel("SYSTEM · now", GlowStyle.BodyBlack, 14, Color.FromHtml("#5C5680")));
            box.AddChild(header);

            _spamPopupLabel = GlowStyle.MakeLabel("", GlowStyle.Body, 17, GlowStyle.Ink);
            _spamPopupLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            box.AddChild(_spamPopupLabel);

            var dismiss = new Button { Text = "DISMISS", SizeFlagsHorizontal = SizeFlags.ShrinkEnd };
            GlowStyle.StyleStickerButton(dismiss, GlowStyle.Ink, GlowStyle.Paper, GlowStyle.Display, 14, 10, 12, 4);
            dismiss.Pressed += DismissSpamPopup;
            box.AddChild(dismiss);

            _videoCanvas.AddChild(_spamPopupPanel);
        }

        private void BuildRack()
        {
            _secondaryRack = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Begin };
            _secondaryRack.AddThemeConstantOverride("separation", 12);
            Place(_sceneRoot, _secondaryRack, HAnchor.Left, 64, 532, 0, 0);
        }

        private void BuildEyesAndPower()
        {
            var eyes = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            eyes.AddThemeConstantOverride("separation", 10);
            Place(_sceneRoot, eyes, HAnchor.Left, 524, 890, 200, 0);

            var eyesSticker = GlowStyle.StickerPanel(GlowStyle.Eyes, 12, 4, 12, 8);
            var eyesBox = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            eyesBox.AddThemeConstantOverride("separation", 4);
            _eyesLabel = GlowStyle.MakeLabel("EYES 100%", GlowStyle.Display, 16, GlowStyle.Ink);
            eyesBox.AddChild(_eyesLabel);
            _eyeBar = new ProgressBar { MinValue = 0, MaxValue = 100, Value = 100, CustomMinimumSize = new Vector2(166, 14), MouseFilter = MouseFilterEnum.Ignore };
            _eyeFill = GlowStyle.StyleBar(_eyeBar, GlowStyle.Paper, GlowStyle.Ink, 7, 2);
            eyesBox.AddChild(_eyeBar);
            _eyeWarningLabel = GlowStyle.MakeLabel("RUB YOUR EYES!", GlowStyle.Display, 13, GlowStyle.Ink);
            _eyeWarningLabel.Visible = false;
            eyesBox.AddChild(_eyeWarningLabel);
            eyesSticker.AddChild(eyesBox);
            eyes.AddChild(Tilted(eyesSticker, 2f));

            _brightnessButton = new Button { Text = "BRIGHT: NORMAL", TooltipText = "Brightness: High drains battery faster, Low tires your eyes" };
            GlowStyle.StyleStickerButton(_brightnessButton, GlowStyle.Paper, GlowStyle.Ink, GlowStyle.Display, 14, 12, 10, 6);
            _brightnessButton.Pressed += () =>
            {
                _brightness = _brightness switch
                {
                    BrightnessMode.Normal => BrightnessMode.High,
                    BrightnessMode.High => BrightnessMode.Low,
                    _ => BrightnessMode.Normal
                };
                _brightnessButton.Text = $"BRIGHT: {_brightness.ToString().ToUpperInvariant()}";
            };
            eyes.AddChild(_brightnessButton);

            _eyeDropsButton = new Button { Text = $"EYE DROPS {GlowStyle.FormatCash(_balanceConfig.Eyes.EyeDropsCost)}" };
            GlowStyle.StyleStickerButton(_eyeDropsButton, GlowStyle.Paper, GlowStyle.Ink, GlowStyle.Display, 14, 12, 10, 6);
            _eyeDropsButton.Pressed += () =>
            {
                if (_saveData.CurrentMoney >= _balanceConfig.Eyes.EyeDropsCost && _session != null && !_session.IsFinished)
                {
                    _saveData.CurrentMoney -= _balanceConfig.Eyes.EyeDropsCost;
                    _session.Eyes.RestoreFull();
                    _audio?.PlaySfx("sfx_like", 1.3f);
                    LogEvent("[color=#66ccff]💧 Used Eye Drops: Eye Freshness restored to 100%![/color]");
                }
            };
            eyes.AddChild(_eyeDropsButton);

            var power = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            power.AddThemeConstantOverride("separation", 10);
            Place(_sceneRoot, power, HAnchor.Left, 150, 922, 0, 0);

            var outlet = GlowStyle.StickerPanel(GlowStyle.Fever, 12, 4, 12, 8);
            var outletBox = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            outletBox.AddThemeConstantOverride("separation", 4);
            _overloadLabel = GlowStyle.MakeLabel("OUTLET 0%", GlowStyle.Display, 16, GlowStyle.Ink);
            outletBox.AddChild(_overloadLabel);
            _overloadBar = new ProgressBar { MinValue = 0, MaxValue = 100, Value = 0, CustomMinimumSize = new Vector2(190, 12), MouseFilter = MouseFilterEnum.Ignore };
            _overloadFill = GlowStyle.StyleBar(_overloadBar, GlowStyle.Mythic, GlowStyle.Ink, 6, 2);
            outletBox.AddChild(_overloadBar);
            outlet.AddChild(outletBox);
            var outletRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            outletRow.AddThemeConstantOverride("separation", 14);
            outletRow.AddChild(Tilted(outlet, -2f));
            _chargerButton = new Button
            {
                TooltipText = "Charge phone #1 [C]. Adds outlet load and blocks overclock while plugged in.",
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };
            _chargerButton.Pressed += ToggleCharger;
            outletRow.AddChild(_chargerButton);
            power.AddChild(outletRow);
            RefreshChargerButton();

            _breakerResetButton = new Button { Text = "RESET BREAKER [UP]", Visible = false };
            GlowStyle.StyleStickerButton(_breakerResetButton, GlowStyle.Danger, GlowStyle.Ink, GlowStyle.Display, 15, 12, 12, 6);
            _breakerResetButton.Pressed += () =>
            {
                _isBlackoutActive = false;
                _electricalOverloadPercent = 0f;
                _breakerResetButton.Visible = false;
                _audio?.PlaySfx("sfx_complete", 1.2f);
                LogEvent("[color=#66ff88]🔌 Wall breaker reset! Room power and secondary phones restored.[/color]");
            };
            power.AddChild(_breakerResetButton);
        }

        private void BuildLaptop()
        {
            _randomEventButton = new Button { Visible = false, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            GlowStyle.StyleStickerButton(_randomEventButton, GlowStyle.Mythic, GlowStyle.Ink, GlowStyle.BodyBlack, 17, 14, 14, 6);
            _randomEventButton.Pressed += ResolveActiveRandomEvent;
            Place(_sceneRoot, _randomEventButton, HAnchor.Right, 1332, 286, 504, 58);
            KeepPivotCentered(_randomEventButton);

            _laptopPanel = GlowStyle.StickerPanel(GlowStyle.Ink, 18, 6, 14, 14);
            Place(_sceneRoot, _laptopPanel, HAnchor.Right, 1332, 352, 504, 356);
            var baseBar = new Panel { MouseFilter = MouseFilterEnum.Ignore };
            baseBar.AddThemeStyleboxOverride("panel", GlowStyle.Sticker(GlowStyle.DeskEdge, 8, 3, 3, 0, 0));
            Place(_sceneRoot, baseBar, HAnchor.Right, 1296, 700, 576, 28);

            var desktop = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            var desktopStyle = new StyleBoxFlat { BgColor = GlowStyle.SlaccDesktop };
            desktopStyle.SetCornerRadiusAll(6);
            desktopStyle.SetContentMarginAll(12);
            desktop.AddThemeStyleboxOverride("panel", desktopStyle);
            _laptopPanel.AddChild(desktop);

            var window = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            window.AddThemeStyleboxOverride("panel", GlowStyle.Sticker(GlowStyle.SlaccWindow, 3, 3, 0, 0, 0));
            desktop.AddChild(window);

            var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            column.AddThemeConstantOverride("separation", 0);
            window.AddChild(column);

            var titleBar = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            var titleStyle = new StyleBoxFlat { BgColor = GlowStyle.SlaccPurple };
            titleStyle.ContentMarginLeft = 8;
            titleStyle.ContentMarginRight = 8;
            titleStyle.ContentMarginTop = 1;
            titleStyle.ContentMarginBottom = 1;
            titleBar.AddThemeStyleboxOverride("panel", titleStyle);
            var titleRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            _laptopStatusLabel = GlowStyle.MakeLabel("Slacc 98 - #urgent-deadlines", GlowStyle.Mono, 23, Colors.White);
            _laptopStatusLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _laptopStatusLabel.ClipText = true;
            titleRow.AddChild(_laptopStatusLabel);
            titleRow.AddChild(GlowStyle.MakeLabel("x", GlowStyle.Mono, 23, Colors.White));
            titleBar.AddChild(titleRow);
            column.AddChild(titleBar);

            var bodyMargin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
            bodyMargin.AddThemeConstantOverride("margin_left", 10);
            bodyMargin.AddThemeConstantOverride("margin_right", 10);
            bodyMargin.AddThemeConstantOverride("margin_top", 8);
            bodyMargin.AddThemeConstantOverride("margin_bottom", 8);
            bodyMargin.SizeFlagsVertical = SizeFlags.ExpandFill;
            column.AddChild(bodyMargin);

            var body = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            body.AddThemeConstantOverride("separation", 7);
            bodyMargin.AddChild(body);

            var chat = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            chat.AddThemeConstantOverride("separation", 8);
            var avatar = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkBegin };
            _bossAvatarStyle = GlowStyle.Sticker(GlowStyle.SlaccPurple, 6, 2, 0, 8, 2);
            avatar.AddThemeStyleboxOverride("panel", _bossAvatarStyle);
            _bossAvatarLabel = GlowStyle.MakeLabel("B", GlowStyle.Display, 16, Colors.White);
            avatar.AddChild(_bossAvatarLabel);
            chat.AddChild(avatar);
            _bossChatMessageLabel = GlowStyle.MakeLabel("BOSS: keep up the pace.", GlowStyle.Mono, 23, GlowStyle.Ink);
            _bossChatMessageLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _bossChatMessageLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            chat.AddChild(_bossChatMessageLabel);
            body.AddChild(chat);

            _bossChatBubblePanel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
            _bossStripStyle = GlowStyle.Sticker(GlowStyle.Danger, 4, 2, 0, 8, 4);
            _bossChatBubblePanel.AddThemeStyleboxOverride("panel", _bossStripStyle);
            var strip = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            strip.AddThemeConstantOverride("separation", 4);
            _bossAlertLabel = GlowStyle.MakeLabel("", GlowStyle.Mono, 23, GlowStyle.Ink);
            strip.AddChild(_bossAlertLabel);
            _bossTimerBar = new ProgressBar { MinValue = 0, MaxValue = 1, Step = 0, Value = 1, CustomMinimumSize = new Vector2(0, 10), MouseFilter = MouseFilterEnum.Ignore };
            GlowStyle.StyleBar(_bossTimerBar, GlowStyle.Ink, Colors.White, 2, 2);
            strip.AddChild(_bossTimerBar);
            _bossChatBubblePanel.AddChild(strip);
            body.AddChild(_bossChatBubblePanel);

            _laptopReplyButton = new Button { Text = "REPLY TO BOSS [UP]", Disabled = true };
            GlowStyle.StyleStickerButton(_laptopReplyButton, GlowStyle.Paper, GlowStyle.Ink, GlowStyle.Display, 15, 10, 12, 5);
            _laptopReplyButton.Pressed += TryReplyToBoss;
            body.AddChild(_laptopReplyButton);

            var algo = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            algo.AddThemeConstantOverride("separation", 10);
            for (int i = 0; i < 4; i++)
            {
                var cell = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
                cell.AddThemeConstantOverride("separation", 2);
                _algoLabels[i] = GlowStyle.MakeLabel("", GlowStyle.Mono, 19, GlowStyle.Ink);
                cell.AddChild(_algoLabels[i]);
                _algoBars[i] = new ProgressBar { MinValue = 0, MaxValue = 100, Value = 25, CustomMinimumSize = new Vector2(0, 9), MouseFilter = MouseFilterEnum.Ignore };
                _algoFills[i] = GlowStyle.StyleBar(_algoBars[i], GlowStyle.Dopamine, Color.FromHtml("#C9C3DD"), 3, 1);
                cell.AddChild(_algoBars[i]);
                algo.AddChild(cell);
            }
            body.AddChild(algo);

            _algoRadarLabel = GlowStyle.MakeLabel("", GlowStyle.Mono, 19, GlowStyle.SlaccPurple);
            _algoRadarLabel.ClipText = true;
            body.AddChild(_algoRadarLabel);

            var terminal = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ExpandFill };
            var terminalStyle = new StyleBoxFlat { BgColor = GlowStyle.Ink };
            terminalStyle.SetCornerRadiusAll(3);
            terminalStyle.SetContentMarginAll(6);
            terminal.AddThemeStyleboxOverride("panel", terminalStyle);
            _eventFeedLabel = new RichTextLabel
            {
                BbcodeEnabled = true,
                ScrollFollowing = true,
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            _eventFeedLabel.AddThemeFontOverride("normal_font", GlowStyle.Mono);
            _eventFeedLabel.AddThemeFontSizeOverride("normal_font_size", 19);
            _eventFeedLabel.GetVScrollBar().Modulate = new Color(1, 1, 1, 0);
            terminal.AddChild(_eventFeedLabel);
            body.AddChild(terminal);
        }

        private void RefreshChargerButton()
        {
            if (_chargerButton == null) return;
            _chargerButton.Text = _isCharging ? "CHARGING [C]" : "PLUG IN [C]";
            GlowStyle.StyleStickerButton(_chargerButton, _isCharging ? GlowStyle.Eyes : GlowStyle.Paper, GlowStyle.Ink, GlowStyle.Display, 14, 12, 10, 6);
        }

        private static string RoleShortName(SecondaryRole role) => role switch
        {
            SecondaryRole.AdShield => "SHIELD",
            SecondaryRole.CryptoMiner => "MINER",
            SecondaryRole.MythicRadar => "RADAR",
            _ => "STD"
        };

        private static Color RoleColor(SecondaryRole role) => role switch
        {
            SecondaryRole.AdShield => GlowStyle.Eyes,
            SecondaryRole.CryptoMiner => GlowStyle.Mythic,
            SecondaryRole.MythicRadar => GlowStyle.Violet,
            _ => GlowStyle.Cash
        };

        private static void ApplyRoleLook(MiniPhoneButton phone, SecondaryRole role)
        {
            switch (role)
            {
                case SecondaryRole.CryptoMiner:
                    phone.ScreenColor = Color.FromHtml("#1E5E3A");
                    phone.AccentColor = GlowStyle.Cash;
                    phone.Glyph = MiniPhoneButton.RoleGlyph.Miner;
                    break;
                case SecondaryRole.AdShield:
                    phone.ScreenColor = Color.FromHtml("#1B4A78");
                    phone.AccentColor = GlowStyle.Eyes;
                    phone.Glyph = MiniPhoneButton.RoleGlyph.Shield;
                    break;
                case SecondaryRole.MythicRadar:
                    phone.ScreenColor = Color.FromHtml("#4A3A0A");
                    phone.AccentColor = GlowStyle.Mythic;
                    phone.Glyph = MiniPhoneButton.RoleGlyph.Radar;
                    break;
                default:
                    phone.ScreenColor = Color.FromHtml("#7A2E9E");
                    phone.AccentColor = GlowStyle.Dopamine;
                    phone.Glyph = MiniPhoneButton.RoleGlyph.Standard;
                    break;
            }
        }

        private void RebuildSecondaryRackUI()
        {
            BuildSecondaryRackCards();
            DisableButtonFocus(_secondaryRack);
        }

        private void BuildSecondaryRackCards()
        {
            foreach (Node child in _secondaryRack.GetChildren())
            {
                child.QueueFree();
            }
            _secondaryRows.Clear();

            for (int i = 1; i < _session.Phones.Count; i++)
            {
                var sec = _session.Phones[i];
                int phoneId = sec.Id;

                var cell = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(84, 0) };
                cell.AddThemeConstantOverride("separation", 6);

                var roleButton = new Button { Text = RoleShortName(sec.Role), TooltipText = "Change this phone's role", CustomMinimumSize = new Vector2(84, 30) };
                GlowStyle.StyleStickerButton(roleButton, RoleColor(sec.Role), GlowStyle.Ink, GlowStyle.Display, 12, 8, 4, 2);
                int slot = i - 1;
                roleButton.Pressed += () =>
                {
                    sec.Role = (SecondaryRole)(((int)sec.Role + 1) % 4);
                    _saveData.SetRackRole(slot, sec.Role);
                    _audio?.PlaySfx("sfx_swipe", 1.15f);
                    RebuildSecondaryRackUI();
                };
                cell.AddChild(roleButton);

                var phone = new MiniPhoneButton
                {
                    CustomMinimumSize = new Vector2(84, 128),
                    TooltipText = $"Phone #{phoneId}: click to skip (key {phoneId})"
                };
                ApplyRoleLook(phone, sec.Role);
                phone.Pressed += () => InteractWithSecondaryPhone(phoneId);
                cell.AddChild(phone);

                var battery = GlowStyle.MakeLabel("100%", GlowStyle.Display, 13, GlowStyle.Cash);
                battery.HorizontalAlignment = HorizontalAlignment.Center;
                cell.AddChild(battery);

                _secondaryRack.AddChild(cell);
                _secondaryRows.Add(new SecondaryPhoneRow { PhoneId = phoneId, RoleButton = roleButton, Phone = phone, BatteryLabel = battery });
            }

            int nextSlot = _session.Phones.Count + 1;
            if (nextSlot <= RunFormulas.MaxSecondaryPhones(_balanceConfig) + 1)
            {
                var dockCell = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(84, 0) };
                dockCell.AddThemeConstantOverride("separation", 6);
                dockCell.AddChild(new Control { CustomMinimumSize = new Vector2(84, 30), MouseFilter = MouseFilterEnum.Ignore });
                dockCell.AddChild(new MiniPhoneButton
                {
                    IsEmptyDock = true,
                    DockText = "$" + NextSecondaryPhoneCost,
                    CustomMinimumSize = new Vector2(84, 128),
                    MouseFilter = MouseFilterEnum.Ignore,
                    TooltipText = "Empty dock: buy a phone in the night shop"
                });
                var dockLabel = GlowStyle.MakeLabel("EMPTY", GlowStyle.Display, 12, GlowStyle.Muted);
                dockLabel.HorizontalAlignment = HorizontalAlignment.Center;
                dockCell.AddChild(dockLabel);
                _secondaryRack.AddChild(dockCell);
            }
        }

        // ------------------------------------------------------------------ refresh

        private void RefreshActiveHud(in PhoneStats primaryStats)
        {
            var primary = _session.Phones[0];
            float ratio = Mathf.Clamp(_session.ElapsedSeconds / _session.NightDurationSeconds, 0f, 1f);
            int totalMinutes = (int)(ratio * 360f);
            int hour = totalMinutes / 60;
            int displayHour = hour == 0 ? 12 : hour;
            int minute = totalMinutes % 60;
            bool fever = _feverModeTimer > 0f;
            bool calm = ReducedMotion;
            float t = Time.GetTicksMsec() / 1000f;

            _dayClockLabel.Text = _saveData.EndlessModeUnlocked
                ? $"NIGHT {_saveData.CurrentDay} · ENDLESS"
                : $"NIGHT {_saveData.CurrentDay}/{_balanceConfig.Victory.Day}";
            _dopamineBadgeLabel.Text = GlowStyle.FormatBig(_saveData.CurrentDopamine);
            _cashBadgeLabel.Text = GlowStyle.FormatCash(_saveData.CurrentMoney);

            int billEvery = _balanceConfig.Bill.EveryDays;
            int daysToBill = billEvery - ((_saveData.CurrentDay - 1) % billEvery) - 1;
            if (daysToBill == 0)
            {
                double bill = _economy.CalculateElectricBill(
                    _saveData.CurrentDay,
                    RunFormulas.SecondaryPhoneCount(_balanceConfig, _saveData),
                    GetUpgradeLevel("HW_02") > 0,
                    GetGrassLevel("GRASS_04"));
                _billLabel.Text = $"BILL TONIGHT {GlowStyle.FormatCash(bill)}";
                _billStickerStyle.BgColor = GlowStyle.Danger;
            }
            else
            {
                _billLabel.Text = daysToBill == 1 ? "BILL TOMORROW" : $"BILL IN {daysToBill} NIGHTS";
                _billStickerStyle.BgColor = GlowStyle.Fever;
            }

            // Eyes: blur costs phone #1 dopamine, so the sticker says how much.
            float freshness = _session.Eyes.CurrentFreshness;
            _eyesLabel.Text = $"EYES {freshness:0}%";
            _eyeBar.Value = freshness;
            bool tired = freshness < 25f;
            double blurLoss = 1.0 - RunFormulas.BlurDopamineMultiplier(_balanceConfig, _session.Eyes.GetNormalizedBlur(LensBlurReduction));
            _eyeWarningLabel.Visible = tired || blurLoss >= 0.005;
            _eyeWarningLabel.Text = tired ? $"RUB YOUR EYES! -{blurLoss * 100:0}%" : $"BLURRY: -{blurLoss * 100:0}% DOPA";
            _eyeFill.BgColor = tired
                ? (MathF.Sin(t * 7f) > 0f ? GlowStyle.Danger : GlowStyle.Paper)
                : GlowStyle.Paper;

            // Outlet
            _overloadLabel.Text = $"OUTLET {_electricalOverloadPercent:0}%";
            _overloadBar.Value = _electricalOverloadPercent;
            _overloadFill.BgColor = _electricalOverloadPercent >= 80f && MathF.Sin(t * 7f) > 0f ? GlowStyle.Danger : GlowStyle.Mythic;

            // Room
            _deskBackdrop.NightProgress = ratio;
            _deskBackdrop.ClockText = $"{displayHour:00}:{minute:00}";
            _deskBackdrop.IsBlackout = _isBlackoutActive;
            _deskBackdrop.SpeedLevel = (int)primary.CurrentSpeed;
            _deskBackdrop.HasDeskFan = GetUpgradeLevel("HW_02") > 0;
            _deskBackdrop.HasPowerBank = GetUpgradeLevel("HW_03") > 0;
            _deskBackdrop.HasCoffeeMug = GetUpgradeLevel("HW_05") > 0;
            _deskBackdrop.HasDrinkingBird = GetUpgradeLevel("HW_06") > 0;
            _deskBackdrop.HasPowerStrip = GetUpgradeLevel("HW_07") > 0;
            _deskBackdrop.FeverActive = fever;
            _deskBackdrop.ReducedMotion = calm;
            _deskBackdrop.ChargerPlugged = _isCharging;
            _deskBackdrop.ChargerLive = _isCharging && !_isBlackoutActive;

            // Eye fatigue shaders (phone only)
            float normBlur = _session.Eyes.GetNormalizedBlur(LensBlurReduction);
            _phoneBlurMaterial?.SetShaderParameter("blur_amount", normBlur * 2.8f);
            if (_eyelidMaterial != null)
            {
                float normFatigue = 1f - (freshness / 100f);
                _eyelidMaterial.SetShaderParameter("eyelid_closure", normFatigue * 0.85f);
            }

            // Phone battery and progress
            float batteryPct = primary.CurrentBattery / primaryStats.MaxBatterySeconds * 100f;
            _batteryBar.Value = batteryPct;
            bool charging = _isCharging && !_isBlackoutActive;
            _batteryPctLabel.Text = charging ? $"+{batteryPct:0}%" : $"{batteryPct:0}%";
            Color batteryColor = charging ? GlowStyle.Eyes : batteryPct < 20f ? GlowStyle.Danger : GlowStyle.Cash;
            _chargeBolt.Visible = charging;
            // A low battery nags the charger sticker until it is plugged in.
            bool nag = !_isCharging && batteryPct < 30f && primary.HasBattery;
            _chargerButton.Modulate = nag && (calm || MathF.Sin(t * 8f) > 0f) ? new Color(1f, 0.85f, 0.4f) : Colors.White;
            _batteryFill.BgColor = batteryColor;
            _batteryPctLabel.AddThemeColorOverride("font_color", batteryColor);

            _videoProgressBar.MaxValue = primary.VideoRequiredDuration;
            _videoProgressBar.Value = primary.IsCurrentVideoCompleted ? primary.VideoRequiredDuration : primary.VideoProgress;
            int thresholdPct = (int)MathF.Round(primaryStats.AttentionThreshold * 100f);
            bool isRewardVideo = primary.CurrentVideo == VideoCategory.Standard || primary.CurrentVideo == VideoCategory.Mythic;
            bool readyToSwipe = primary.IsEarlySwipeReady(primaryStats.AttentionThreshold) || primary.IsCurrentVideoCompleted;
            _videoProgressFill.BgColor = primary.CurrentVideo == VideoCategory.AdSpam
                ? GlowStyle.Fever
                : readyToSwipe ? GlowStyle.Cash : GlowStyle.Dopamine;
            if (_videoProgressBar.Size.X > 10f)
            {
                _thresholdMarker.Visible = isRewardVideo && thresholdPct < 100;
                float markerX = Mathf.Clamp(_videoProgressBar.Size.X * primaryStats.AttentionThreshold - 4f, 0f, _videoProgressBar.Size.X - 9f);
                _thresholdMarker.Position = new Vector2(markerX, -8f);
            }

            UpdatePhoneVisuals(primary, primaryStats.AttentionThreshold);
            RefreshEffectChips(primary);
            RefreshStreakAndFever(fever, calm, t);
            RefreshAlgorithmPanel();
            RefreshRack(primaryStats);
            RefreshLaptop();

            if (_randomEventButton.Visible)
            {
                _randomEventButton.Scale = calm ? Vector2.One : Vector2.One * (1f + 0.025f * MathF.Sin(t * 6f));
            }

            _likeButton.ReducedMotion = calm;
            _discButton.ReducedMotion = calm;
            _screenGraphic.ReducedMotion = calm;
        }

        private void RefreshStreakAndFever(bool fever, bool calm, float t)
        {
            if (fever != _feverVisualActive)
            {
                _feverVisualActive = fever;
                _phoneBezelStyle.SetBorderWidthAll(fever ? 6 : 0);
                _phoneBezelStyle.ShadowColor = fever ? new Color(GlowStyle.Dopamine, 0.55f) : new Color(GlowStyle.ScreenGlow, 0.28f);
                _phoneBezelStyle.ShadowSize = fever ? 72 : 46;
                _streakRow.Visible = !fever;
                _feverBar.Visible = fever;
                _feverBanner.Visible = fever;
                _feverBannerLabel.Text = $"BRAINROT FEVER x{FeverX}";
                if (fever && !calm)
                {
                    var pop = CreateTween();
                    pop.TweenProperty(_feverBanner, "scale", Vector2.One, 0.28).From(new Vector2(1.7f, 1.7f))
                        .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                }
            }

            if (fever)
            {
                _feverBar.Value = _feverModeTimer / Math.Max(0.01f, (float)_balanceConfig.Combo.FeverSeconds);
                if (!calm) _feverBanner.Scale = Vector2.One * (1f + 0.035f * MathF.Sin(t * 12f));
                return;
            }

            int streak = Math.Min(_feverComboStreak, _streakPills.Length);
            if (streak != _streakShown)
            {
                _streakShown = streak;
                for (int i = 0; i < _streakPills.Length; i++)
                {
                    _streakPills[i].AddThemeStyleboxOverride("panel", i < streak ? _pillOn : _pillOff);
                }
                _streakLabel.Text = $"STREAK {streak}/{_streakPills.Length}";
            }
        }

        private void RefreshAlgorithmPanel()
        {
            VideoNiche[] niches = { VideoNiche.Cats, VideoNiche.Gaming, VideoNiche.Asmr, VideoNiche.Memes };
            string[] names = { "CATS", "GAMING", "ASMR", "MEMES" };
            for (int i = 0; i < niches.Length; i++)
            {
                float share = _algorithm.GetNormalizedShare(niches[i]) * 100f;
                _algoBars[i].Value = share;
                // A boring niche fades from pink to grey and gets a sleepy "z".
                float bored = _nicheTolerance?.Boredom(niches[i]) ?? 0f;
                float fade = Mathf.Clamp(bored / Math.Max(0.01f, (float)_balanceConfig.Tolerance.MaxPenalty), 0f, 1f);
                _algoFills[i].BgColor = GlowStyle.Dopamine.Lerp(Color.FromHtml("#8F88BF"), fade);
                _algoLabels[i].Text = bored >= 0.15f ? $"{names[i]} {share:0} z" : $"{names[i]} {share:0}";
            }

            var tags = new List<string>();
            if (_viralStreakCount > 1) tags.Add($"{_lastLikedNiche.ToString().ToUpperInvariant()} STREAK x{_viralStreakCount}");
            var watching = _session.Phones[0].CurrentNiche;
            float boredom = _nicheTolerance?.Boredom(watching) ?? 0f;
            if (boredom >= 0.05f) tags.Add($"BORED OF {watching.ToString().ToUpperInvariant()} -{boredom * 100f:0}%");
            if (_goldenWaveTimer > 0f) tags.Add($"GOLDEN WAVE {_goldenWaveTimer:0.0}s");
            if (_algorithm.BadLikePenaltyRemaining > 0) tags.Add($"AD POLLUTED {_algorithm.BadLikePenaltyRemaining}");
            _algoRadarLabel.Text = tags.Count > 0 ? string.Join(" · ", tags) : "feed is learning your taste...";
        }

        private void RefreshRack(in PhoneStats primaryStats)
        {
            for (int i = 0; i < _secondaryRows.Count && i + 1 < _session.Phones.Count; i++)
            {
                var row = _secondaryRows[i];
                var sec = _session.Phones[i + 1];
                float pct = sec.CurrentBattery / primaryStats.MaxBatterySeconds * 100f;
                row.BatteryLabel.Text = sec.HasBattery ? $"{pct:0}%" : "DEAD";
                bool low = pct < 20f;
                if (low != row.LowBattery)
                {
                    row.LowBattery = low;
                    row.BatteryLabel.AddThemeColorOverride("font_color", low ? GlowStyle.Fever : GlowStyle.Cash);
                }
                row.Phone.HasBattery = sec.HasBattery;
                row.Phone.IsMythic = sec.CurrentVideo == VideoCategory.Mythic;
                row.Phone.VideoFraction = sec.VideoRequiredDuration > 0f ? sec.VideoProgress / sec.VideoRequiredDuration : 0f;
            }
        }

        private void RefreshLaptop()
        {
            var laptop = _session.Laptop;
            int state = !laptop.IsAlertActive ? 0 : laptop.IsAudit ? 2 : 1;
            if (state != _laptopVisualState)
            {
                _laptopVisualState = state;
                _bossAvatarStyle.BgColor = state == 0 ? GlowStyle.SlaccPurple : GlowStyle.Danger;
                _bossChatBubblePanel.Visible = state != 0;
                _bossStripStyle.BgColor = state == 2 ? GlowStyle.Mythic : GlowStyle.Danger;
                _laptopReplyButton.Disabled = state == 0;
                GlowStyle.StyleStickerButton(_laptopReplyButton, state == 0 ? GlowStyle.Paper : state == 2 ? GlowStyle.Mythic : GlowStyle.Danger,
                    GlowStyle.Ink, GlowStyle.Display, 15, 10, 12, 5);
            }

            float window = (float)_balanceConfig.Laptop.AlertWindowSeconds;
            switch (state)
            {
                case 0 when RunFormulas.BossMuted(_saveData):
                    _laptopStatusLabel.Text = "Slacc 98 - do not disturb";
                    _bossChatMessageLabel.Text = "DO NOT DISTURB: the boss can't reach you tonight. pay cut applies.";
                    _laptopReplyButton.Text = "MUTED";
                    break;
                case 0:
                    _laptopStatusLabel.Text = "Slacc 98 - #urgent-deadlines";
                    _bossChatMessageLabel.Text = $"BOSS: keep up the pace. attendance {laptop.GetAttendanceRatio() * 100f:0}%";
                    _laptopReplyButton.Text = "REPLY TO BOSS [UP]";
                    break;
                case 1:
                    _laptopStatusLabel.Text = "Slacc 98 - PING!";
                    _bossChatMessageLabel.Text = "BOSS: are u still working?? status update NOW";
                    _bossAlertLabel.Text = $"REPLY IN {laptop.RemainingTimer:0.0}s OR LOSE SALARY";
                    _bossTimerBar.Value = laptop.RemainingTimer / Math.Max(0.01f, window);
                    _laptopReplyButton.Text = $"REPLY NOW [UP] {laptop.RemainingTimer:0.0}s";
                    break;
                default:
                    float auditWindow = window + (float)_balanceConfig.Laptop.AuditExtraSeconds;
                    _laptopStatusLabel.Text = "Slacc 98 - AUDIT";
                    _bossChatMessageLabel.Text = "BOSS: URGENT AUDIT. compile the spreadsheet.";
                    _bossAlertLabel.Text = $"TYPE THE REPORT [UP] {laptop.AuditTapsCurrent}/{laptop.AuditTapsRequired}";
                    _bossTimerBar.Value = laptop.RemainingTimer / Math.Max(0.01f, auditWindow);
                    _laptopReplyButton.Text = $"TYPE! {laptop.AuditTapsCurrent}/{laptop.AuditTapsRequired}";
                    break;
            }
        }

        private void UpdatePhoneVisuals(PhoneEntity primary, float attentionThreshold)
        {
            if (primary.IsInTransition)
            {
                float progress = 1f - Mathf.Clamp(primary.StateTimer / PrimaryTransitionSeconds, 0f, 1f);
                _screenInner.Position = new Vector2(0f, (1f - progress) * 120f);
                _screenInner.Modulate = new Color(1f, 1f, 1f, progress);
            }
            else
            {
                _screenInner.Position = Vector2.Zero;
                _screenInner.Modulate = Colors.White;
            }

            _screenGraphic.HasBattery = primary.HasBattery;
            _screenGraphic.IsStunned = primary.IsStunnedByScreamer;
            _screenGraphic.Category = primary.CurrentVideo;
            _screenGraphic.Niche = primary.CurrentNiche;
            _screenGraphic.SafeScreamerMode = _settings.SafeScreamer;
            _screenGraphic.SplitScreenActive = GetUpgradeLevel("DOP_10") > 0;
            _likeButton.Active = primary.HasLikedCurrentVideo;

            if (!primary.HasBattery)
            {
                _videoCanvas.Color = Color.FromHtml("#0B0A14");
                ShowVideoBadge("BATTERY DEAD", GlowStyle.Danger);
                _captionSticker.Visible = false;
                _videoHandleLabel.Text = string.Empty;
                _videoSoundLabel.Text = string.Empty;
                _hintSticker.Visible = false;
                return;
            }

            if (primary.IsStunnedByScreamer)
            {
                _videoCanvas.Color = _settings.SafeScreamer ? Color.FromHtml("#173B5C") : Color.FromHtml("#5A0E14");
                ShowVideoBadge(_settings.SafeScreamer
                    ? $"ADRENALINE {primary.StateTimer:0.0}s"
                    : $"SCREAMER! STUNNED {primary.StateTimer:0.0}s", _settings.SafeScreamer ? GlowStyle.Eyes : GlowStyle.Danger);
                _captionSticker.Visible = true;
                _videoTitleLabel.Text = "eyes wide open now";
                _hintSticker.Visible = false;
                return;
            }

            string handle;
            switch (primary.CurrentVideo)
            {
                case VideoCategory.AdSpam:
                    _videoCanvas.Color = Color.FromHtml("#4A1A12");
                    ShowVideoBadge("SPONSORED · SKIP IT", GlowStyle.Fever);
                    handle = "@totally_not_an_ad";
                    break;
                case VideoCategory.Mythic:
                    _videoCanvas.Color = Color.FromHtml("#4A3A0A");
                    ShowVideoBadge($"3 AM MYTHIC · x{_balanceConfig.Phone.MythicMultiplier:0.##}", GlowStyle.Mythic);
                    handle = "@3am.archive";
                    break;
                default:
                    _videoBadge.Visible = false;
                    _videoCanvas.Color = _feverModeTimer > 0f ? Color.FromHtml("#5B1250") : primary.CurrentNiche switch
                    {
                        VideoNiche.Cats => Color.FromHtml("#4B2380"),
                        VideoNiche.Gaming => Color.FromHtml("#2A1458"),
                        VideoNiche.Asmr => Color.FromHtml("#173B5C"),
                        _ => Color.FromHtml("#6B1D4F")
                    };
                    handle = primary.CurrentNiche switch
                    {
                        VideoNiche.Cats => "@meowmentum",
                        VideoNiche.Gaming => "@retro_rage_clips",
                        VideoNiche.Asmr => "@whisper.lab",
                        _ => "@brainrot.daily"
                    };
                    break;
            }

            _captionSticker.Visible = true;
            if (_videoTitleLabel.Text != _currentVideoTitle) _videoTitleLabel.Text = _currentVideoTitle;
            _videoHandleLabel.Text = handle;
            _videoSoundLabel.Text = "original sound · 3 AM lofi";
            UpdateFakeCounts();

            if (primary.CurrentVideo == VideoCategory.AdSpam)
            {
                ShowHint("SKIP IT [DOWN]", GlowStyle.Fever);
            }
            else if (primary.IsCurrentVideoCompleted)
            {
                ShowHint("DONE · SWIPE", GlowStyle.Cash);
            }
            else if (primary.IsEarlySwipeReady(attentionThreshold))
            {
                ShowHint("DOPAMINE READY · SWIPE", GlowStyle.Cash);
            }
            else if (primary.CurrentSpeed != SpeedMode.X1)
            {
                ShowHint($"x{(int)primary.CurrentSpeed} SPEED", GlowStyle.Violet);
            }
            else
            {
                _hintSticker.Visible = false;
            }
        }

        private void ShowVideoBadge(string text, Color color)
        {
            _videoBadge.Visible = true;
            if (_videoBadgeLabel.Text != text) _videoBadgeLabel.Text = text;
            _videoBadgeStyle.BgColor = color;
        }

        private void ShowHint(string text, Color color)
        {
            _hintSticker.Visible = true;
            if (_phoneStateLabel.Text != text) _phoneStateLabel.Text = text;
            _hintStyle.BgColor = color;
        }

        /// <summary>Cosmetic like/comment/share counts, stable per video title (no gameplay randomness).</summary>
        private void UpdateFakeCounts()
        {
            if (_countsForTitle == _currentVideoTitle) return;
            _countsForTitle = _currentVideoTitle;
            int seed = 7;
            foreach (char ch in _currentVideoTitle) seed = unchecked(seed * 31 + ch);
            seed = Math.Abs(seed);
            _likeButton.Caption = $"{1 + seed % 990 / 10.0:0.0}K";
            _commentButton.Caption = $"{(seed / 7) % 97 / 10.0 + 0.2:0.0}K";
            _shareButton.Caption = $"{(seed / 13) % 49 / 10.0 + 0.1:0.0}K";
        }

        // ------------------------------------------------------------------ juice

        private void SpawnFloatingText(string text, Color color) => SpawnFloatingText(text, color, FlyTarget.None);

        /// <summary>
        /// Pops a sticker-text over the video; dopamine and cash gains then fly into their counter, which bounces.
        /// Reduced motion keeps it as a short fade in place.
        /// </summary>
        private void SpawnFloatingText(string text, Color color, FlyTarget target)
        {
            if (_fxLayer == null || _videoCanvas == null || !_videoCanvas.IsInsideTree()) return;

            var label = GlowStyle.MakeLabel(text, GlowStyle.Display, 30, color, outline: 12);
            _fxLayer.AddChild(label);
            Vector2 labelSize = label.GetMinimumSize();
            label.Size = labelSize;
            label.PivotOffset = labelSize / 2f;

            Vector2 layerOrigin = _fxLayer.GetGlobalRect().Position;
            Vector2 start = _videoCanvas.GetGlobalRect().GetCenter() - layerOrigin
                + new Vector2(_fxRng.Next(-120, 60), _fxRng.Next(-220, -60));
            label.Position = start - labelSize / 2f;

            bool calm = ReducedMotion;
            var tween = CreateTween();
            if (!calm)
            {
                label.Rotation = Mathf.DegToRad(_fxRng.Next(-9, 10));
                tween.TweenProperty(label, "scale", Vector2.One, 0.14).From(new Vector2(0.35f, 0.35f))
                    .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            }
            tween.TweenProperty(label, "position:y", label.Position.Y - 46f, 0.32).SetEase(Tween.EaseType.Out);

            Control? destination = target switch
            {
                FlyTarget.Dopamine => _dopamineBadgeLabel,
                FlyTarget.Cash => _cashSticker,
                _ => null
            };

            if (destination != null && !calm)
            {
                Vector2 end = destination.GetGlobalRect().GetCenter() - layerOrigin - labelSize / 2f;
                tween.TweenProperty(label, "position", end, 0.36).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
                tween.Parallel().TweenProperty(label, "scale", new Vector2(0.45f, 0.45f), 0.36).SetEase(Tween.EaseType.In);
                tween.TweenCallback(Callable.From(() =>
                {
                    label.QueueFree();
                    BounceCounter(target);
                }));
            }
            else
            {
                tween.TweenProperty(label, "modulate:a", 0f, 0.45);
                tween.TweenCallback(Callable.From(label.QueueFree));
            }
        }

        private void BounceCounter(FlyTarget target)
        {
            if (ReducedMotion) return;
            Control control = target == FlyTarget.Cash ? _cashSticker : _dopamineBadgeLabel;
            ref Tween? running = ref (target == FlyTarget.Cash ? ref _cashBounce : ref _dopamineBounce);
            if (running != null && running.IsValid()) running.Kill();
            control.PivotOffset = control.Size / 2f;
            var tween = CreateTween();
            tween.TweenProperty(control, "scale", new Vector2(1.18f, 1.18f), 0.07);
            tween.TweenProperty(control, "scale", Vector2.One, 0.18).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            running = tween;
        }
    }
}
