using System;
using System.Collections.Generic;
using System.IO;
using Doomscroll.Core.Common;
using Doomscroll.Core.Entities;
using Doomscroll.Core.Events;
using Doomscroll.Core.Services;
using Doomscroll.Core.UseCases;
using Doomscroll.Presentation.UI;
using Godot;

namespace Doomscroll.Presentation.DeskScene
{
    public enum RandomEventType
    {
        None,
        WifiLagSpike,
        ShadySponsorDeal,
        GoldenAlgorithmWave
    }

    /// <summary>
    /// Root presentation controller for DeskScene.tscn.
    /// Bridges the pure C# Core domain layer with Godot 4 Neon UI, 60 FPS procedural phone graphics,
    /// diegetic 3:00 AM desk props, particle bursts, live algorithm radar, and Lo-Fi Synthwave audio.
    /// </summary>
    public partial class DeskSceneController : Control
    {
        private float PrimaryTransitionSeconds => (float)_balanceConfig.Phone.PrimaryTransitionSeconds;
        private float PrimaryScrollCooldownSeconds => (float)_balanceConfig.Phone.PrimaryScrollCooldownSeconds;
        /// <summary>Fever dopamine multiplier for UI text ("3").</summary>
        private string FeverX => _balanceConfig.Combo.FeverDopamineMultiplier.ToString("0.##");
        /// <summary>Price of the next Secondary Phone, rounded like the Night Shop shows it.</summary>
        private string NextSecondaryPhoneCost =>
            Math.Round(_economy.CalculateUpgradeCost(_balanceConfig.GetUpgrade("HW_01"), GetUpgradeLevel("HW_01")), 1).ToString("0.##");

        private static readonly string[] CatTitles =
        {
            "POV: Orange cat discovers 3 AM zoomies",
            "Tiny kitten refuses to stop meowing at wall",
            "Loaf check: 10/10 purrfect geometry",
            "Cat vs. laser pointer at 240 FPS",
            "Cat staring into the 4th dimension at 3:15 AM",
            "Orange cat with exactly one shared braincell",
            "Cat knocks over glass while maintaining eye contact",
            "My cat judging my life choices at night"
        };

        private static readonly string[] GamingTitles =
        {
            "Frame-perfect speedrun skip explained in 4s",
            "Insane 1v5 clutch on 1 HP no armor",
            "Why this retro boss was impossible in 1998",
            "Building a 10,000-item factory in 10 seconds",
            "Subway Surfers gameplay underneath court trial audio",
            "Why speedrunners pause 4,000 times in 10 seconds",
            "I spent 100 hours building an unnecessary redstone toaster",
            "NPC dialogue that aged like spoiled milk"
        };

        private static readonly string[] AsmrTitles =
        {
            "Kinetic sand slicing with obsidian knife",
            "Mechanical keyboard thock test (lubed switches)",
            "Rain on car roof at 3:00 AM binaural",
            "Hydraulic press vs. 500 layers of foil",
            "ASMR chewing literal honeycomb at 400% volume",
            "Whispering dark secrets of corporate accounting",
            "Cutting extremely kinetic soap until dawn",
            "Typing aggressively on mechanical keyboard with blue switches"
        };

        private static readonly string[] MemeTitles =
        {
            "Me explaining my sleep schedule to my boss",
            "When the alarm says 3 hours and 12 minutes left",
            "Bro really thought he could watch just one more",
            "Brain at 4 AM: remember that awkward thing?",
            "Me explaining lore to my dog at 3:30 AM",
            "POV: alarm rings in 2 hours and you're watching this",
            "The algorithm knowing my deepest regrets",
            "Brain at 10 PM: Sleep. Brain at 3 AM: Quantum physics"
        };

        private static readonly string[] AdTitles =
        {
            "SPONSORED: Only 1% can pass Level 3! Download NOW!",
            "AD: Tired of paying bills? Try Crypto-Toaster 3000!",
            "PROMO: Hot singles in your local server rack!"
        };

        private List<MythicDefinition> MythicCatalog => _balanceConfig.Mythics.Catalog;

        // Title is what the log and the toast print; Name and Goal are the Album's wording.
        private static readonly (string Id, string Title, string Name, string Goal, string Icon)[] AchievementCatalog =
        {
            ("ACH_0", "⚡ Short Attention Master", "Short Attention Master", "Do 10 early swipes after the green line.", "brain"),
            ("ACH_1", "🔥 Viral Trendsetter", "Viral Trendsetter", "Like the same niche 5 times in a row.", "flame"),
            ("ACH_2", "💼 Slack Ninja", "Slacc Ninja", "Reply to the boss on time 3 times.", "briefcase"),
            ("ACH_3", "📱 Phone Farm", "Phone Farm", "Own 2 rack phones.", "phone"),
            ("ACH_4", "☕ Late Night Setup", "Late Night Setup", "Own the desk fan and the triple espresso.", "cup"),
            ("ACH_5", "💀 Jumpscare Veteran", "Jumpscare Veteran", "Survive 3 screamers.", "skull"),
            ("ACH_6", "🏆 3:00 AM Witness", "3:00 AM Witness", "Find your first Mythic clip.", "star"),
            ("ACH_7", "🌱 Touched Grass", "Touched Grass", "Touch grass for the first time.", "sprout")
        };

        // Core services & state
        private readonly Random _rng = new();
        // Cosmetic-only randomness (shake, audio pitch, popup jitter) so visuals never shift gameplay rolls.
        private readonly Random _fxRng = new();
        private EventBus _eventBus = null!;
        private EconomyService _economy = null!;
        private AlgorithmGenerator _algorithm = null!;
        private SaveDataService _saveService = null!;
        private BuyUpgradeUseCase _buyUpgrade = null!;
        private PrestigeResetUseCase _prestigeReset = null!;
        private BalanceConfig _balanceConfig = null!;
        private SaveData _saveData = null!;
        private TurnSession _session = null!;
        private readonly PhoneStats[] _secondaryStatsByRole = new PhoneStats[Enum.GetValues<SecondaryRole>().Length];
        // Run currencies at the start of the current night; mid-night saves persist these instead of live values.
        private (double Dopamine, double RunDopamine, double Money)? _nightStartRunState;
        private DeskAudioManager _audio = null!;

        private BrightnessMode _brightness = BrightnessMode.Normal;
        private UpgradeCategory _currentNightTab = UpgradeCategory.Mind;
        private bool _isHoldingBoost;
        private float _scrollCooldownTimer;
        private float _autoScrollTimerPrimary;
        private readonly float[] _autoScrollTimersSecondary = new float[8];
        private string _currentVideoTitle = string.Empty;
        private float _screenShakeTimer;
        private bool _isBillUnpaidLock;
        private bool _wasBossAlertActive;
        private int _viralStreakCount;
        private VideoNiche _lastLikedNiche = VideoNiche.None;

        // 3:00 AM Random Interactive Desk Events & Achievement Toast
        private RandomEventType _activeRandomEvent = RandomEventType.None;
        private bool _nightRandomEventRolled;
        private float _goldenWaveTimer;
        private bool _sponsorAdPenaltyActive;
        private float _achievementToastTimer;

        // Fever Mode & Swipe Combo Streak
        private int _feverComboStreak;
        private float _feverModeTimer;

        // Annoying Smartphone Spam Popup
        private bool _isSpamPopupActive;
        private float _spamPopupCooldown = 14.0f;
        private PanelContainer _spamPopupPanel = null!;
        private Label _spamPopupLabel = null!;

        // Mouse swipe & double-tap tracking on primary phone
        private bool _isDraggingPhone;
        private Vector2 _dragStartPos;
        private double _lastPhoneClickTimeSec;

        // Room Electrical Overload & Blackout state
        private float _electricalOverloadPercent;
        private bool _isBlackoutActive;

        // Phone #1 on the charger (key C): refills, loads the outlet, no overclock.
        private bool _isCharging;
        // Niche boredom on phone #1, reset every night.
        private NicheTolerance _nicheTolerance = null!;

        // Automated Playtest Bot state (F9 or --playtest CLI)
        private bool _isPlaytestBotActive;
        private bool _isPlaytestAutoQuit;
        private float _botLikeTimer;
        private float _botSwipeTimer;
        private float _botDeskActionTimer;
        private float _botModalTimer;
        private float _botScreenshotTimer;
        private float _botTotalRunTime;
        private float _botMaxRuntime = 25.0f;
        private int _botScreenshotIndex;

        // Lock screen (main menu) and pause overlay. Menus, report and shop live in DeskSceneController.Menus.cs.
        private ColorRect _titleOverlay = null!;
        private Button _titleContinueBtn = null!;
        private ColorRect _pauseOverlay = null!;
        private Label _pauseHintLabel = null!;

        // Night report + PRYM shop
        private ColorRect _nightModal = null!;
        private Label _nightSummaryLabel = null!;
        private VBoxContainer _shopButtonsContainer = null!;
        private Button _nextDayButton = null!;
        private Button _prestigeButton = null!;

        public override void _Ready()
        {
            InitializeCoreServices();
            LoadSettings();
            _audio = new DeskAudioManager();
            AddChild(_audio);
            ApplyAudioSettings();
            BuildDeskInterface();
            BuildPauseMenu();
            BuildAppLayer();
            StartNewDayTurn();
            DisableButtonFocus(this, _nightModal, _titleOverlay, _pauseOverlay, _appLayer);
            if (_saveData.NightCompleted)
            {
                ResumeNightShop();
            }
            // Show real values behind the lock screen before the first frame runs.
            RefreshActiveHud(BuildPhoneStats(isPrimary: true, SecondaryRole.Standard));
            RefreshLockScreen();
            ApplyCurrentTextScale(this);

            string[] cmdArgs = OS.GetCmdlineArgs();
            string[] userArgs = OS.GetCmdlineUserArgs();
            if (Array.Exists(cmdArgs, a => a == "--playtest") || Array.Exists(userArgs, a => a == "--playtest"))
            {
                _titleOverlay.Visible = false;
                _isPlaytestBotActive = true;
                _isPlaytestAutoQuit = true;
                _botMaxRuntime = 25.0f;
                LogEvent("[color=#00ffff]🤖 Automated Playtest Bot started via CLI (--playtest)[/color]");
                GD.Print("[PLAYTEST BOT] Automated Playtest Bot activated. Will run for 25s, click/swipe/buy, capture screenshots, and quit.");
            }
        }

        private void InitializeCoreServices()
        {
            _eventBus = new EventBus();
            _balanceConfig = LoadBalanceConfig();
            _economy = new EconomyService(_balanceConfig);
            _algorithm = new AlgorithmGenerator(_rng);

            string savePath = ProjectSettings.GlobalizePath("user://save_v1.json");
            _saveService = new SaveDataService(new FileSaveStorage(savePath));
            _buyUpgrade = new BuyUpgradeUseCase(_economy);
            _prestigeReset = new PrestigeResetUseCase(_economy, _balanceConfig);
            _saveData = _saveService.LoadOrCreate();

            _eventBus.Subscribe<VideoCompletedEvent>(OnVideoCompleted);
            _eventBus.Subscribe<VideoLikedEvent>(OnVideoLiked);
            _eventBus.Subscribe<ScreamerTriggeredEvent>(OnScreamerTriggered);
            _eventBus.Subscribe<PhoneBatteryDepletedEvent>(OnPhoneBatteryDepleted);
            _eventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
        }

        private const string BalanceConfigPath = "res://Data/balance.json";

        /// <summary>
        /// Loads Data/balance.json through Godot's FileAccess, so it also works from an exported .pck
        /// (the export preset must include "Data/*.json"). A missing or invalid file stops the game with
        /// a clear error instead of silently running on different numbers.
        /// </summary>
        private static BalanceConfig LoadBalanceConfig()
        {
            if (!Godot.FileAccess.FileExists(BalanceConfigPath))
            {
                throw new InvalidOperationException($"{BalanceConfigPath} was not found. In an exported build, add 'Data/*.json' to the export filter.");
            }

            BalanceConfig cfg = BalanceConfig.Parse(Godot.FileAccess.GetFileAsString(BalanceConfigPath));
            var problems = cfg.Validate();
            if (problems.Count > 0)
            {
                foreach (string problem in problems) GD.PushError($"[balance.json] {problem}");
                throw new InvalidOperationException($"{BalanceConfigPath} is invalid: {string.Join("; ", problems)}");
            }

            return cfg;
        }

        public override void _Process(double delta)
        {
            // Clamp hitches (e.g. dragging the window) so a single frame cannot skip seconds of the night.
            float dt = Math.Min((float)delta, 0.1f);

            if (_scrollCooldownTimer > 0f)
            {
                _scrollCooldownTimer = Math.Max(0f, _scrollCooldownTimer - dt);
            }

            if (_screenShakeTimer > 0f)
            {
                _screenShakeTimer -= dt;
                Position = new Vector2(_fxRng.Next(-8, 9), _fxRng.Next(-8, 9));
                if (_screenShakeTimer <= 0f) Position = Vector2.Zero;
            }

            if (_achievementToastTimer > 0f)
            {
                _achievementToastTimer -= dt;
                if (_achievementToastTimer <= 0f && _achievementToastPanel != null)
                {
                    _achievementToastPanel.Visible = false;
                }
            }

            // The lock screen freezes the whole night, timers included.
            if (_titleOverlay != null && _titleOverlay.Visible)
            {
                _audio?.UpdateDynamicMusicPitch(0.95f);
                return;
            }

            if (_goldenWaveTimer > 0f)
            {
                _goldenWaveTimer = Math.Max(0f, _goldenWaveTimer - dt);
            }

            if (_feverModeTimer > 0f)
            {
                _feverModeTimer = Math.Max(0f, _feverModeTimer - dt);
            }

            if (_screenGraphic != null)
            {
                _screenGraphic.IsFeverActive = _feverModeTimer > 0f;
            }

            if (_session == null || _session.IsFinished)
            {
                _audio?.UpdateDynamicMusicPitch(0.92f);
                if (_isPlaytestBotActive)
                {
                    UpdatePlaytestBot(dt);
                }
                return;
            }

            if (!_isSpamPopupActive)
            {
                _spamPopupCooldown -= dt;
                if (_spamPopupCooldown <= 0f)
                {
                    _spamPopupCooldown = (float)_balanceConfig.Night.SpamPopup.DelaySeconds + (float)_rng.NextDouble() * (float)_balanceConfig.Night.SpamPopup.DelayRandomSeconds;
                    TriggerSpamPopup();
                }
            }

            UpdateSpeedInputState();
            UpdateElectricalOverload(dt);
            UpdateNightRandomEvents();

            var primary = _session.Phones[0];
            float targetPitch;
            if (_isBlackoutActive)
            {
                targetPitch = 0.76f;
            }
            else if (_feverModeTimer > 0f)
            {
                targetPitch = 1.25f;
            }
            else
            {
                targetPitch = primary.CurrentSpeed switch
                {
                    SpeedMode.X5 => 1.15f,
                    SpeedMode.X2 => 1.07f,
                    _ => 1.0f
                };
            }
            _audio?.UpdateDynamicMusicPitch(targetPitch);

            PhoneStats primaryStats = BuildPhoneStats(isPrimary: true, SecondaryRole.Standard);
            for (int r = 0; r < _secondaryStatsByRole.Length; r++)
            {
                _secondaryStatsByRole[r] = BuildPhoneStats(isPrimary: false, (SecondaryRole)r);
            }

            float eyeFatiguePerSec = RunFormulas.EyeFatiguePerSecond(
                _balanceConfig, _saveData, _brightness, _isBlackoutActive, primary.CurrentVideo, primary.CurrentNiche);
            float coffeeSlow = RunFormulas.CoffeeFatigueSlow(_balanceConfig, _saveData);
            float clockScale = RunFormulas.LaptopIntervalScale(_balanceConfig, _saveData);
            bool hasBird = GetUpgradeLevel("HW_06") > 0 && !_isBlackoutActive;

            float chargeRate = _isCharging && !_isBlackoutActive ? RunFormulas.ChargePerSecond(_balanceConfig) : 0f;
            _session.Tick(dt, in primaryStats, _secondaryStatsByRole, eyeFatiguePerSec, coffeeSlow, clockScale, hasBird,
                chargeRate, RunFormulas.BossMuted(_saveData));

            if (_session.Laptop.IsAlertActive && !_wasBossAlertActive)
            {
                _audio?.PlaySfx("sfx_boss_ping");
            }
            _wasBossAlertActive = _session.Laptop.IsAlertActive;

            HandleAutoScroll(dt);
            CheckAchievements();
            RefreshActiveHud(in primaryStats);
            UpdateTips(dt, in primaryStats);

            if (_isPlaytestBotActive)
            {
                UpdatePlaytestBot(dt);
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventKey escKey && escKey.Pressed && !escKey.Echo && escKey.Keycode == Key.Escape)
            {
                if (_titleOverlay.Visible)
                {
                    // The lock screen is the menu: Esc only backs out of the "start over?" question.
                    _lockConfirm.Visible = false;
                }
                else
                {
                    SetPaused(true);
                }
                GetViewport().SetInputAsHandled();
                return;
            }

            if (@event is InputEventKey f9Key && f9Key.Pressed && !f9Key.Echo && f9Key.Keycode == Key.F9)
            {
                if (_titleOverlay != null && _titleOverlay.Visible) _titleOverlay.Visible = false;
                _isPlaytestBotActive = !_isPlaytestBotActive;
                LogEvent(_isPlaytestBotActive
                    ? "[color=#00ffff]🤖 AUTO-PLAYTEST BOT ENABLED (Press F9 to pause)[/color]"
                    : "[color=#aaaaaa]🤖 Auto-Playtest Bot paused.[/color]");
                return;
            }

            if (_titleOverlay != null && _titleOverlay.Visible)
            {
                if (@event is InputEventKey titleKey && titleKey.Pressed && !titleKey.Echo && !_lockConfirm.Visible && !_appLayer.Visible &&
                    (titleKey.Keycode == Key.Enter || titleKey.Keycode == Key.KpEnter || titleKey.Keycode == Key.Space || titleKey.Keycode == Key.Up))
                {
                    _titleContinueBtn.EmitSignal(BaseButton.SignalName.Pressed);
                    GetViewport().SetInputAsHandled();
                }
                return;
            }
            if (_session == null || _session.IsFinished) return;

            if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
            {
                if (keyEvent.Keycode == Key.Space || keyEvent.Keycode == Key.Down || keyEvent.Keycode == Key.S)
                {
                    TryManualPrimaryScroll();
                }
                else if (keyEvent.Keycode == Key.Right || keyEvent.Keycode == Key.D)
                {
                    PerformPrimaryLike();
                }
                else if (keyEvent.Keycode == Key.Up || keyEvent.Keycode == Key.W)
                {
                    HandleUpArrowDeskAction();
                }
                else if (keyEvent.Keycode == Key.C)
                {
                    ToggleCharger();
                }
                else if (keyEvent.Keycode >= Key.Key2 && keyEvent.Keycode <= Key.Key7)
                {
                    int slotId = (int)(keyEvent.Keycode - Key.Key1) + 1;
                    InteractWithSecondaryPhone(slotId);
                }
            }
            else if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
            {
                if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
                {
                    TryManualPrimaryScroll();
                }
            }
        }

        public override void _Notification(int what)
        {
            // Closing the window with the X must save just like the in-game Quit buttons.
            if (what == NotificationWMCloseRequest && _saveService != null && _saveData != null)
            {
                SaveProgress();
            }
        }

        /// <summary>
        /// Persists the save. While a night is in progress, run currencies are written as they were
        /// at the start of the night, so quitting mid-night cannot bank a partial night: the night is
        /// replayed instead. Permanent unlocks (achievements, mythics, settings) are kept as-is.
        /// </summary>
        private void SaveProgress()
        {
            bool nightInProgress = _session != null && !_session.IsFinished && _nightStartRunState.HasValue;
            if (!nightInProgress)
            {
                _saveService.Save(_saveData);
                return;
            }

            var dusk = _nightStartRunState!.Value;
            double liveDopamine = _saveData.CurrentDopamine;
            double liveRunDopamine = _saveData.TotalRunDopamine;
            double liveMoney = _saveData.CurrentMoney;
            try
            {
                _saveData.CurrentDopamine = dusk.Dopamine;
                _saveData.TotalRunDopamine = dusk.RunDopamine;
                _saveData.CurrentMoney = dusk.Money;
                _saveService.Save(_saveData);
            }
            finally
            {
                _saveData.CurrentDopamine = liveDopamine;
                _saveData.TotalRunDopamine = liveRunDopamine;
                _saveData.CurrentMoney = liveMoney;
            }
        }

        private void BuildPauseMenu()
        {
            _pauseOverlay = new ColorRect
            {
                AnchorRight = 1f,
                AnchorBottom = 1f,
                Color = new Color(GlowStyle.Ink, 0.78f),
                MouseFilter = MouseFilterEnum.Stop,
                Visible = false,
                // Keeps the menu interactive while the rest of the scene tree is paused.
                ProcessMode = ProcessModeEnum.Always
            };
            AddChild(_pauseOverlay);

            var center = new CenterContainer { AnchorRight = 1f, AnchorBottom = 1f };
            _pauseOverlay.AddChild(center);

            var panel = GlowStyle.StickerPanel(GlowStyle.Wall, 22, 6, 34, 28);
            panel.MouseFilter = MouseFilterEnum.Pass;
            center.AddChild(Tilted(panel, -1f));

            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 14);
            panel.AddChild(box);

            var title = GlowStyle.MakeLabel("PAUSED", GlowStyle.Display, 48, GlowStyle.Paper, 5);
            GlowStyle.AddTextShadow(title, GlowStyle.Ink, 5);
            title.HorizontalAlignment = HorizontalAlignment.Center;
            box.AddChild(title);

            _pauseHintLabel = GlowStyle.MakeLabel("", GlowStyle.Body, 18, GlowStyle.Muted);
            _pauseHintLabel.HorizontalAlignment = HorizontalAlignment.Center;
            box.AddChild(_pauseHintLabel);

            Button PauseButton(string text, Color bg, Action onPressed)
            {
                var button = new Button { Text = text, CustomMinimumSize = new Vector2(420, 0) };
                GlowStyle.StyleStickerButton(button, bg, GlowStyle.Ink, GlowStyle.Display, 22, 16, 16, 12);
                button.Pressed += onPressed;
                box.AddChild(button);
                return button;
            }

            var resumeBtn = PauseButton("RESUME", GlowStyle.Eyes, () => SetPaused(false));
            resumeBtn.ShortcutInTooltip = false;
            resumeBtn.Shortcut = new Shortcut { Events = new Godot.Collections.Array { new InputEventKey { Keycode = Key.Escape } } };
            PauseButton("SETTINGS", GlowStyle.Paper, () => OpenApp(BuildSettingsApp, fromPause: true));
            PauseButton("MAIN MENU", GlowStyle.Paper, () =>
            {
                SaveProgress();
                SetPaused(false);
                ShowLockScreen();
            });
            PauseButton("SAVE & QUIT", GlowStyle.Danger, () =>
            {
                SaveProgress();
                GetTree().Quit();
            });
        }

        private void SetPaused(bool paused)
        {
            if (_pauseOverlay == null) return;
            if (paused)
            {
                bool nightInProgress = _session != null && !_session.IsFinished && !_titleOverlay.Visible;
                _pauseHintLabel.Text = nightInProgress
                    ? "Quitting now drops tonight's dopamine and cash.\nThe night is saved when it ends."
                    : "Your progress is saved.";
            }
            _pauseOverlay.Visible = paused;
            GetTree().Paused = paused;
        }

        /// <summary>
        /// Gameplay buttons must never take keyboard focus: otherwise Space / arrow keys press or move
        /// between buttons instead of scrolling and liking. Menus (title, night shop, pause) keep focus.
        /// </summary>
        private static void DisableButtonFocus(Node root, params Node[] skip)
        {
            foreach (Node child in root.GetChildren())
            {
                if (Array.IndexOf(skip, child) >= 0) continue;
                if (child is BaseButton button) button.FocusMode = FocusModeEnum.None;
                DisableButtonFocus(child, skip);
            }
        }

        private void OnPhoneCanvasGuiInput(InputEvent @event)
        {
            if (_session == null || _session.IsFinished) return;

            if (@event is InputEventMouseButton mb)
            {
                if (mb.ButtonIndex == MouseButton.Left)
                {
                    if (mb.Pressed)
                    {
                        _isDraggingPhone = true;
                        _dragStartPos = mb.Position;

                        double now = Time.GetTicksMsec() / 1000.0;
                        if (now - _lastPhoneClickTimeSec < 0.28)
                        {
                            PerformPrimaryLike();
                            _isDraggingPhone = false;
                        }
                        _lastPhoneClickTimeSec = now;
                    }
                    else if (_isDraggingPhone)
                    {
                        _isDraggingPhone = false;
                        float dy = _dragStartPos.Y - mb.Position.Y;
                        if (dy >= 36f)
                        {
                            TryManualPrimaryScroll();
                        }
                    }
                }
                else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
                {
                    TryManualPrimaryScroll();
                    AcceptEvent();
                }
            }
            else if (@event is InputEventMouseMotion mm && _isDraggingPhone)
            {
                float dy = _dragStartPos.Y - mm.Position.Y;
                if (dy >= 55f)
                {
                    _isDraggingPhone = false;
                    TryManualPrimaryScroll();
                }
            }
        }

        private void UpdateSpeedInputState()
        {
            bool shiftOrLeft = Input.IsKeyPressed(Key.Shift) || Input.IsKeyPressed(Key.Left) || Input.IsKeyPressed(Key.A);
            bool rightMouse = Input.IsMouseButtonPressed(MouseButton.Right);
            _isHoldingBoost = shiftOrLeft || rightMouse;

            var primary = _session.Phones[0];
            int speedLevel = GetUpgradeLevel("DOP_03");
            // The charger cable is too short for overclocking.
            if (_isHoldingBoost && speedLevel > 0 && !_isCharging)
            {
                primary.SetSpeed(speedLevel >= 2 ? SpeedMode.X5 : SpeedMode.X2);
            }
            else
            {
                primary.SetSpeed(SpeedMode.X1);
            }
        }

        private void UpdateElectricalOverload(float dt)
        {
            if (_isBlackoutActive) return;

            int activeSecondary = 0;
            for (int i = 1; i < _session.Phones.Count; i++)
            {
                if (_session.Phones[i].HasBattery) activeSecondary++;
            }

            var primary = _session.Phones[0];
            float loadRate = RunFormulas.OutletLoadPerSecond(_balanceConfig, _saveData, activeSecondary, _brightness, primary.CurrentSpeed, _isCharging);
            float safeThreshold = RunFormulas.SafeOutletLoad(_balanceConfig, _saveData);
            float netDelta = (loadRate - safeThreshold) * dt;

            _electricalOverloadPercent = Mathf.Clamp(_electricalOverloadPercent + netDelta, 0f, 100f);
            if (_electricalOverloadPercent >= 100f)
            {
                _isBlackoutActive = true;
                _breakerResetButton.Visible = true;
                _audio?.PlaySfx("sfx_blackout");
                LogEvent("[color=#ff4444]⚡ BLACKOUT! Wall outlet overloaded! Secondary phones paused until you click [RESET WALL BREAKER]![/color]");
                SpawnFloatingText("BLACKOUT!", GlowStyle.Danger);
            }
        }

        private void HandleAutoScroll(float dt)
        {
            var primary = _session.Phones[0];
            int autoScrollLevel = GetUpgradeLevel("DOP_02");
            if (!_isSpamPopupActive && autoScrollLevel > 0 && primary.HasBattery && !primary.IsStunnedByScreamer && !primary.IsInTransition)
            {
                PhoneStats pStats = BuildPhoneStats(isPrimary: true, SecondaryRole.Standard);
                if (primary.IsCurrentVideoCompleted || primary.IsEarlySwipeReady(pStats.AttentionThreshold) || primary.CurrentVideo == VideoCategory.AdSpam)
                {
                    _autoScrollTimerPrimary += dt;
                    float delay = RunFormulas.PrimaryAutoScrollDelay(_balanceConfig, _saveData);
                    if (_autoScrollTimerPrimary >= delay)
                    {
                        _autoScrollTimerPrimary = 0f;
                        ScrollPhone(primary);
                    }
                }
            }

            if (_isBlackoutActive) return;

            float secDelay = RunFormulas.SecondaryAutoScrollDelay(_balanceConfig, _saveData);
            for (int i = 1; i < _session.Phones.Count; i++)
            {
                var sec = _session.Phones[i];
                if (!sec.HasBattery || sec.IsStunnedByScreamer || sec.IsInTransition) continue;

                if (sec.IsCurrentVideoCompleted || sec.CurrentVideo == VideoCategory.AdSpam)
                {
                    _autoScrollTimersSecondary[i] += dt;
                    if (_autoScrollTimersSecondary[i] >= secDelay)
                    {
                        _autoScrollTimersSecondary[i] = 0f;
                        ScrollPhone(sec);
                    }
                }
            }
        }

        private PhoneStats BuildPhoneStats(bool isPrimary, SecondaryRole role)
        {
            var primary = _session.Phones[0];
            var context = new PhoneStatsContext
            {
                IsPrimary = isPrimary,
                Role = role,
                Brightness = _brightness,
                PrimarySpeed = primary.CurrentSpeed,
                PrimaryVideo = primary.CurrentVideo,
                PrimaryNiche = primary.CurrentNiche,
                WifiLagActive = _activeRandomEvent == RandomEventType.WifiLagSpike,
                BlackoutActive = _isBlackoutActive,
                FeverActive = _feverModeTimer > 0f,
                GoldenWaveActive = _goldenWaveTimer > 0f,
                ViralStreak = _viralStreakCount,
                SecondaryPhoneCount = _session.Phones.Count - 1,
                Blur = _session.Eyes.GetNormalizedBlur(LensBlurReduction),
                NicheBoredom = _nicheTolerance?.Boredom(primary.CurrentNiche) ?? 0f
            };
            return RunFormulas.BuildPhoneStats(_balanceConfig, _saveData, in context);
        }

        /// <summary>Blue-light glasses (HW_08): share of the blur (and its dopamine penalty) they remove.</summary>
        private float LensBlurReduction => GetUpgradeLevel("HW_08") > 0 ? (float)_balanceConfig.GetUpgrade("HW_08").Value : 0f;

        private void ToggleCharger()
        {
            if (_session == null || _session.IsFinished) return;
            _isCharging = !_isCharging;
            _audio?.PlaySfx(_isCharging ? "sfx_complete" : "sfx_swipe", _isCharging ? 1.3f : 0.9f);
            RefreshChargerButton();
        }

        /// <summary>Snacks and other perNight items last one night: they are used up once it ends.</summary>
        private void ConsumeNightItems()
        {
            foreach (var item in _balanceConfig.Upgrades)
            {
                if (item.PerNight) _saveData.RunUpgradeLevels.Remove(item.Id);
            }
        }

        private void StartNewDayTurn()
        {
            _nightModal.Visible = false;
            _nightStartRunState = (_saveData.CurrentDopamine, _saveData.TotalRunDopamine, _saveData.CurrentMoney);
            _isBlackoutActive = false;
            _electricalOverloadPercent = 0f;
            _breakerResetButton.Visible = false;
            _isCharging = false;
            RefreshChargerButton();
            if (_tipToast != null) HideTip();
            var tolerance = _balanceConfig.Tolerance;
            _nicheTolerance = new NicheTolerance((float)tolerance.PerVideo, (float)tolerance.RecoveryPerVideo, (float)tolerance.MaxPenalty);
            _wasBossAlertActive = false;
            _viralStreakCount = 0;
            _lastLikedNiche = VideoNiche.None;
            _activeRandomEvent = RandomEventType.None;
            _nightRandomEventRolled = false;
            _goldenWaveTimer = 0f;
            _sponsorAdPenaltyActive = false;
            _feverComboStreak = 0;
            _feverModeTimer = 0f;
            _isSpamPopupActive = false;
            _spamPopupCooldown = (float)_balanceConfig.Night.SpamPopup.FirstDelaySeconds + (float)_rng.NextDouble() * (float)_balanceConfig.Night.SpamPopup.FirstDelayRandomSeconds;
            if (_spamPopupPanel != null) _spamPopupPanel.Visible = false;
            if (_randomEventButton != null) _randomEventButton.Visible = false;
            _nightStartVideos = _saveData.LifetimeVideosWatched;
            _nightFevers = 0;
            _nightBestStreak = 0;

            _algorithm.ResetWeightsForNewTurn();

            float maxBat = RunFormulas.MaxBatterySeconds(_balanceConfig, _saveData);
            int secondaryCount = RunFormulas.SecondaryPhoneCount(_balanceConfig, _saveData);
            var phones = new List<PhoneEntity>
            {
                new PhoneEntity(1, isPrimary: true, initialBatterySeconds: maxBat, _eventBus)
            };

            for (int i = 0; i < secondaryCount; i++)
            {
                var sec = new PhoneEntity(i + 2, isPrimary: false, initialBatterySeconds: maxBat, _eventBus)
                {
                    Role = _saveData.RackRole(i)
                };
                phones.Add(sec);
            }

            var eyes = new EyeState(eyeDropsCount: 0, blurStartsBelow: (float)_balanceConfig.Eyes.BlurStartsBelow);
            var laptopCfg = _balanceConfig.Laptop;
            var laptop = new WorkLaptop(
                _rng,
                minInterval: (float)laptopCfg.AlertIntervalMin,
                maxInterval: (float)laptopCfg.AlertIntervalMax,
                alertWindowSeconds: (float)laptopCfg.AlertWindowSeconds,
                auditChance: laptopCfg.AuditChance,
                auditTaps: laptopCfg.AuditTaps,
                auditExtraSeconds: (float)laptopCfg.AuditExtraSeconds);
            _session = new TurnSession(phones, eyes, laptop, nightDurationSeconds: (float)_balanceConfig.Night.DurationSeconds, _eventBus);

            foreach (var phone in _session.Phones)
            {
                ScrollPhone(phone, initialLoad: true);
            }

            RebuildSecondaryRackUI();
            RefreshLockScreen();
            LogEvent($"[color=#66ffcc]--- NIGHT {_saveData.CurrentDay} STARTED (12:00 AM) ---[/color]");
        }

        private void TryManualPrimaryScroll()
        {
            if (_isSpamPopupActive)
            {
                DismissSpamPopup();
                return;
            }

            if (_session == null || _session.IsFinished) return;
            if (_scrollCooldownTimer > 0f) return;

            var primary = _session.Phones[0];
            if (!primary.HasBattery || primary.IsStunnedByScreamer || primary.IsInTransition)
                return;

            _scrollCooldownTimer = PrimaryScrollCooldownSeconds;
            _autoScrollTimerPrimary = 0f;
            ScrollPhone(primary);
        }

        private void PerformPrimaryLike()
        {
            if (_isSpamPopupActive) return;
            if (_session == null || _session.IsFinished) return;
            var primary = _session.Phones[0];
            PhoneStats stats = BuildPhoneStats(isPrimary: true, SecondaryRole.Standard);
            double critChance = RunFormulas.CritChance(_balanceConfig, _saveData, guaranteedCrit: _goldenWaveTimer > 0f || _feverModeTimer > 0f);
            bool isCrit = critChance > 0 && _rng.NextDouble() < critChance;
            primary.TryGiveLike(in stats, isCrit, out _);
        }

        private void HandleUpArrowDeskAction()
        {
            if (_session == null || _session.IsFinished) return;

            // Priority 1: Reply to Boss on Slack / Typing Audit
            if (_session.Laptop.IsAlertActive)
            {
                TryReplyToBoss();
                return;
            }

            // Priority 2: Reset tripped Wall Breaker during Blackout
            if (_isBlackoutActive && _breakerResetButton.Visible)
            {
                _isBlackoutActive = false;
                _electricalOverloadPercent = 0f;
                _breakerResetButton.Visible = false;
                _audio?.PlaySfx("sfx_complete", 1.2f);
                LogEvent("[color=#66ff88]🔌 Wall breaker reset via [↑ Up Arrow]! Room power restored.[/color]");
                SpawnFloatingText("POWER BACK", GlowStyle.Cash);
                return;
            }

            // Priority 3: Resolve active 3:00 AM Random Interactive Desk Event
            if (_activeRandomEvent != RandomEventType.None)
            {
                ResolveActiveRandomEvent();
            }
        }

        private void TryReplyToBoss()
        {
            if (_session == null || !_session.Laptop.IsAlertActive) return;

            bool attended = _session.Laptop.TryAttendAlert();
            if (attended)
            {
                _saveData.LifetimeBossAlertsReplied++;
                if (_session.Laptop.LastAttendedWasAudit)
                {
                    double auditBonus = _balanceConfig.Laptop.AuditBonusCash;
                    _saveData.CurrentMoney = Math.Round(_saveData.CurrentMoney + auditBonus, 2);
                    _audio?.PlaySfx("sfx_crit_like", 1.25f);
                    LogEvent($"[color=#66ff88]💻 URGENT AUDIT SUBMITTED! Boss was impressed: +${auditBonus:F2} Performance Bonus![/color]");
                    SpawnFloatingText($"+{GlowStyle.FormatCash(auditBonus)} BONUS", GlowStyle.Cash, FlyTarget.Cash);
                }
                else
                {
                    _audio?.PlaySfx("sfx_complete", 1.15f);
                    LogEvent("[color=#66ff66]💻 Replied to Boss on time! Daily salary secured.[/color]");
                    SpawnFloatingText("BOSS: OK", GlowStyle.Eyes);
                }
                CheckAchievements();
            }
            else
            {
                _audio?.PlaySfx("sfx_swipe", 1.25f);
                LogEvent($"[color=#ffff66]💻 Typing report for Boss... ({_session.Laptop.AuditTapsCurrent}/{_session.Laptop.AuditTapsRequired})[/color]");
                SpawnFloatingText($"TAP {_session.Laptop.AuditTapsCurrent}/{_session.Laptop.AuditTapsRequired}", GlowStyle.Mythic);
            }
        }

        private void TriggerSpamPopup()
        {
            _isSpamPopupActive = true;
            string[] spamMessages = new[]
            {
                "⚠️ STORAGE ALMOST FULL (99.8% Memes)\nPress [↓ Next Video] or Click to Dismiss",
                "📞 INCOMING CALL: Unknown Telemarketer\nPress [↓ Next Video] or Click to Decline",
                "🔔 NOTIFICATION: Your screen time increased by +520%\nPress [↓ Next Video] or Click to Ignore",
                "🔋 BATTERY SAVER: Recommended to close app & sleep\nPress [↓ Next Video] or Click to Refuse"
            };
            if (_spamPopupLabel != null)
            {
                _spamPopupLabel.Text = spamMessages[_rng.Next(spamMessages.Length)];
            }
            if (_spamPopupPanel != null)
            {
                _spamPopupPanel.Visible = true;
            }
            _audio?.PlaySfx("sfx_blackout", 1.3f);
        }

        private void DismissSpamPopup()
        {
            if (!_isSpamPopupActive) return;
            _isSpamPopupActive = false;
            if (_spamPopupPanel != null) _spamPopupPanel.Visible = false;

            double relief = _balanceConfig.Night.SpamPopup.ReliefDopamine;
            _saveData.CurrentDopamine += relief;
            _saveData.TotalRunDopamine += relief;
            _audio?.PlaySfx("sfx_complete", 1.25f);
            LogEvent($"[color=#66ccff]🛡️ Dismissed Phone Spam Popup! +{relief:F1} Dopamine relief.[/color]");
            SpawnFloatingText($"+{GlowStyle.FormatBig(relief)}", GlowStyle.Dopamine, FlyTarget.Dopamine);
        }

        private void UpdateNightRandomEvents()
        {
            if (_session == null || _session.IsFinished || _nightRandomEventRolled) return;

            // Trigger one interactive 3:00 AM desk event around 22 seconds into the night
            var deskEvent = _balanceConfig.Night.DeskEvent;
            if (_session.ElapsedSeconds >= (float)deskEvent.TriggerSeconds)
            {
                _nightRandomEventRolled = true;
                _activeRandomEvent = (RandomEventType)_rng.Next(1, 4);
                _audio?.PlaySfx("sfx_boss_ping", 1.25f);

                if (_randomEventButton != null)
                {
                    _randomEventButton.Visible = true;
                    _randomEventButton.Text = _activeRandomEvent switch
                    {
                        RandomEventType.WifiLagSpike => $"📡 WI-FI BUFFERING (-{(1.0 - deskEvent.WifiSpeedMultiplier) * 100:F0}% Speed)! [Press ↑ UP or Click to Kick Router]",
                        RandomEventType.ShadySponsorDeal => $"🤝 SHADY BRAND DM! [Press ↑ UP or Click: +${deskEvent.SponsorCash:0.##} Cash / +Ads]",
                        _ => $"🔥 GOLDEN ALGORITHM WAVE! [Press ↑ UP or Click: {deskEvent.GoldenWaveSeconds:0.##}s x{deskEvent.GoldenWaveDopamineMultiplier:0.##} Dopamine & 100% Crit!]"
                    };
                }

                LogEvent($"[color=#ffcc44]✨ 3:00 AM DESK EVENT: {_randomEventButton?.Text}[/color]");
            }
        }

        private void ResolveActiveRandomEvent()
        {
            if (_activeRandomEvent == RandomEventType.None) return;

            var evt = _activeRandomEvent;
            _activeRandomEvent = RandomEventType.None;
            if (_randomEventButton != null) _randomEventButton.Visible = false;

            var deskEvent = _balanceConfig.Night.DeskEvent;
            switch (evt)
            {
                case RandomEventType.WifiLagSpike:
                    _saveData.CurrentDopamine += deskEvent.WifiFixDopamine;
                    _saveData.TotalRunDopamine += deskEvent.WifiFixDopamine;
                    _audio?.PlaySfx("sfx_complete", 1.2f);
                    LogEvent($"[color=#66ffcc]📡 Kicked the Wi-Fi Router! Full speed restored & +{deskEvent.WifiFixDopamine:F0} Dopamine![/color]");
                    SpawnFloatingText($"ROUTER KICKED +{GlowStyle.FormatBig(deskEvent.WifiFixDopamine)}", GlowStyle.Dopamine, FlyTarget.Dopamine);
                    break;

                case RandomEventType.ShadySponsorDeal:
                    _saveData.CurrentMoney = Math.Round(_saveData.CurrentMoney + deskEvent.SponsorCash, 2);
                    _sponsorAdPenaltyActive = true;
                    _audio?.PlaySfx("sfx_crit_like");
                    LogEvent($"[color=#66ff66]🤝 Accepted Shady Brand Deal: +${deskEvent.SponsorCash:F2} Cash! (+{deskEvent.SponsorAdPenalty * 100:F0}% Ads for the rest of tonight)[/color]");
                    SpawnFloatingText($"+{GlowStyle.FormatCash(deskEvent.SponsorCash)} BRAND DEAL", GlowStyle.Cash, FlyTarget.Cash);
                    break;

                case RandomEventType.GoldenAlgorithmWave:
                    _goldenWaveTimer = (float)deskEvent.GoldenWaveSeconds;
                    _audio?.PlaySfx("sfx_mythic");
                    LogEvent($"[color=#ffd700]🔥 GOLDEN ALGORITHM WAVE ACTIVE FOR {deskEvent.GoldenWaveSeconds:0.##}s! (x{deskEvent.GoldenWaveDopamineMultiplier:F1} Dopamine & 100% Critical Likes!)[/color]");
                    SpawnFloatingText($"GOLDEN WAVE x{deskEvent.GoldenWaveDopamineMultiplier:0.##}", GlowStyle.Mythic);
                    break;
            }
        }

        private void CheckAchievements()
        {
            TryUnlockAchievement("ACH_0", _saveData.LifetimeEarlySwipes >= 10);
            TryUnlockAchievement("ACH_1", _viralStreakCount >= 5);
            TryUnlockAchievement("ACH_2", _saveData.LifetimeBossAlertsReplied >= 3);
            TryUnlockAchievement("ACH_3", GetUpgradeLevel("HW_01") >= 2);
            TryUnlockAchievement("ACH_4", GetUpgradeLevel("HW_02") > 0 && GetUpgradeLevel("HW_05") > 0);
            TryUnlockAchievement("ACH_5", _saveData.LifetimeScreamersSurvived >= 3);
            TryUnlockAchievement("ACH_6", _saveData.UnlockedMythicIds.Count >= 1);
            TryUnlockAchievement("ACH_7", _saveData.TotalPrestiges >= 1);
        }

        private void TryUnlockAchievement(string id, bool condition)
        {
            if (!condition || _saveData.UnlockedAchievementIds.Contains(id)) return;

            _saveData.UnlockedAchievementIds.Add(id);
            SaveProgress();
            _audio?.PlaySfx("sfx_crit_like", 1.1f);

            string title = id;
            foreach (var item in AchievementCatalog)
            {
                if (item.Id == id)
                {
                    title = item.Title;
                    break;
                }
            }

            LogEvent($"[color=#ffd700]🏅 ACHIEVEMENT UNLOCKED: {title} (+{_balanceConfig.Achievements.DopamineBonusEach * 100:0.##}% Permanent Dopamine!)[/color]");
            if (_achievementToastPanel != null && _achievementToastLabel != null)
            {
                _achievementToastLabel.Text = $"🏅 ACHIEVEMENT UNLOCKED: {title} (+{_balanceConfig.Achievements.DopamineBonusEach * 100:0.##}% Permanent Dopamine!)";
                _achievementToastPanel.Visible = true;
                _achievementToastTimer = 4.2f;
            }
        }

        private void InteractWithSecondaryPhone(int phoneId)
        {
            if (_session == null || _session.IsFinished) return;
            int idx = phoneId - 1;
            if (idx < 1 || idx >= _session.Phones.Count) return;

            var phone = _session.Phones[idx];
            if (phone.CurrentVideo == VideoCategory.Mythic)
            {
                PhoneStats secStats = BuildPhoneStats(isPrimary: false, phone.Role);
                phone.TryGiveLike(in secStats, isCritical: false, out _);
            }
            ScrollPhone(phone);
        }

        private void ScrollPhone(PhoneEntity phone, bool initialLoad = false)
        {
            if (!initialLoad && (!phone.HasBattery || phone.IsStunnedByScreamer || phone.IsInTransition))
            {
                return;
            }

            int pity = _saveData.MythicPityCounter;
            var feed = _balanceConfig.Feed;
            float baseAd = RunFormulas.AdChance(_balanceConfig, _saveData, phone.IsPrimary, phone.Role, _sponsorAdPenaltyActive);
            float mythicChance = RunFormulas.MythicChance(_balanceConfig, phone.IsPrimary, phone.Role);

            var next = _algorithm.RollNextVideo(
                ref pity,
                mythicPityLimit: feed.MythicPityVideos,
                mythicChance: initialLoad ? 0f : mythicChance,
                screamerChance: initialLoad ? 0f : (float)feed.ScreamerChance,
                baseAdChance: initialLoad ? 0f : baseAd,
                pollutedAdChance: baseAd + (float)feed.PollutedAdBonus);
            _saveData.MythicPityCounter = pity;

            float videoDuration = RunFormulas.VideoSeconds(_balanceConfig, next.Category, next.Niche);

            PhoneStats preStats = BuildPhoneStats(phone.IsPrimary, phone.Role);
            VideoCategory prevCategory = phone.CurrentVideo;
            bool wasCompletedBeforeScroll = phone.IsCurrentVideoCompleted;
            bool wasEarlySwipeReady = !initialLoad && phone.IsEarlySwipeReady(preStats.AttentionThreshold);
            float timeSaved = Math.Max(0f, phone.VideoRequiredDuration - phone.VideoProgress);

            bool justTriggeredFever = false;
            if (phone.IsPrimary && !initialLoad)
            {
                bool validAdSkip = prevCategory == VideoCategory.AdSpam && !phone.HasLikedCurrentVideo;
                bool validRewardSwipe = (wasEarlySwipeReady || wasCompletedBeforeScroll) &&
                                        (prevCategory == VideoCategory.Standard || prevCategory == VideoCategory.Mythic);

                if (validAdSkip || validRewardSwipe)
                {
                    if (wasEarlySwipeReady && !wasCompletedBeforeScroll && timeSaved > 0.05f)
                    {
                        _saveData.LifetimeEarlySwipes++;
                    }

                    if (_feverModeTimer <= 0f)
                    {
                        _feverComboStreak++;
                        if (_feverComboStreak >= _balanceConfig.Combo.SwipesToFever)
                        {
                            _feverModeTimer = (float)_balanceConfig.Combo.FeverSeconds;
                            _feverComboStreak = 0;
                            justTriggeredFever = true;
                            _nightFevers++;
                        }
                    }
                }
                else if (!wasCompletedBeforeScroll && !wasEarlySwipeReady &&
                         (prevCategory == VideoCategory.Standard || prevCategory == VideoCategory.Mythic))
                {
                    if (_feverModeTimer <= 0f) _feverComboStreak = 0;
                }
            }

            // Rebuild stats after updating _feverModeTimer so the 5th combo swipe immediately receives x3.0 Fever Dopamine
            PhoneStats stats = BuildPhoneStats(phone.IsPrimary, phone.Role);

            float transitionTime = initialLoad ? 0f : (phone.IsPrimary ? PrimaryTransitionSeconds : (float)_balanceConfig.Phone.SecondaryTransitionSeconds);
            bool scrolled = phone.ScrollToNextVideo(
                next.Category,
                next.Niche,
                duration: videoDuration,
                transitionSeconds: transitionTime,
                in stats,
                screamerStunSeconds: (float)_balanceConfig.Phone.ScreamerStunSeconds,
                isInitialLoad: initialLoad);

            if (scrolled && phone.IsPrimary)
            {
                if (!initialLoad)
                {
                    if (justTriggeredFever)
                    {
                        _audio?.PlaySfx("sfx_mythic", 1.35f);
                        LogEvent($"[color=#ff00ff]🔥🔥 BRAINROT FEVER ACTIVATED! (x{_balanceConfig.Combo.FeverDopamineMultiplier:F1} Dopamine & 100% Crits for {_balanceConfig.Combo.FeverSeconds:0.##}s!) 🔥🔥[/color]");
                        _particleSystem?.EmitBurst(_videoCanvas.Size * 0.5f, new Color(1.0f, 0.25f, 0.95f), count: 32, speedScale: 1.5f);
                        if (wasCompletedBeforeScroll || prevCategory == VideoCategory.AdSpam)
                        {
                            SpawnFloatingText($"FEVER x{FeverX}!", GlowStyle.Fever);
                        }
                    }
                    else if (prevCategory == VideoCategory.AdSpam)
                    {
                        string comboSuffix = _feverComboStreak > 0 ? $" (Combo x{_feverComboStreak}/{_balanceConfig.Combo.SwipesToFever})" : "";
                        SpawnFloatingText($"AD SKIPPED{comboSuffix}", GlowStyle.Eyes);
                    }
                    else if (wasEarlySwipeReady && !wasCompletedBeforeScroll && timeSaved > 0.05f)
                    {
                        if (_feverModeTimer > 0f)
                        {
                            LogEvent($"[color=#ff66ff]🔥 FEVER SWIPE! Saved {timeSaved:F1}s & claimed x{FeverX} Dopamine![/color]");
                        }
                        else
                        {
                            LogEvent($"[color=#44ff99]⚡ Early-Swipe! Saved {timeSaved:F1}s (Combo x{_feverComboStreak}/{_balanceConfig.Combo.SwipesToFever})[/color]");
                        }
                        CheckAchievements();
                    }
                    else if (wasCompletedBeforeScroll && (prevCategory == VideoCategory.Standard || prevCategory == VideoCategory.Mythic))
                    {
                        if (_feverModeTimer > 0f)
                        {
                            SpawnFloatingText("FEVER!", GlowStyle.Fever);
                        }
                        else if (_feverComboStreak > 0)
                        {
                            SpawnFloatingText($"COMBO {_feverComboStreak}/{_balanceConfig.Combo.SwipesToFever}", GlowStyle.Mythic);
                        }
                    }
                    else if (!wasCompletedBeforeScroll && !wasEarlySwipeReady &&
                             (prevCategory == VideoCategory.Standard || prevCategory == VideoCategory.Mythic))
                    {
                        LogEvent("[color=#ff9966]⏩ Swiped before the Green Marker (0 Dopamine, Combo Reset). Wait for ⚡ DOPAMINE READY![/color]");
                        SpawnFloatingText("TOO EARLY · 0", GlowStyle.Fever);
                    }
                }

                _currentVideoTitle = PickVideoTitle(next.Category, next.Niche);
                if (!initialLoad && next.Category != VideoCategory.Screamer)
                {
                    _audio?.PlaySfx("sfx_swipe", 0.94f + (float)_fxRng.NextDouble() * 0.12f);
                }
            }
        }

        private string PickVideoTitle(VideoCategory category, VideoNiche niche)
        {
            return category switch
            {
                VideoCategory.AdSpam => AdTitles[_rng.Next(AdTitles.Length)],
                VideoCategory.Mythic => MythicCatalog[_rng.Next(MythicCatalog.Count)].Title,
                VideoCategory.Screamer => _settings.SafeScreamer
                    ? "⚡ ADRENALINE ALERT! (Safe Mode Active — Eyes 100%)"
                    : "WAIT LOOK CLOSELY AT THE BACKGROUND...",
                _ => niche switch
                {
                    VideoNiche.Cats => CatTitles[_rng.Next(CatTitles.Length)],
                    VideoNiche.Gaming => GamingTitles[_rng.Next(GamingTitles.Length)],
                    VideoNiche.Asmr => AsmrTitles[_rng.Next(AsmrTitles.Length)],
                    _ => MemeTitles[_rng.Next(MemeTitles.Length)]
                }
            };
        }

        private void OnVideoCompleted(VideoCompletedEvent evt)
        {
            _saveData.LifetimeVideosWatched++;
            if (evt.DopamineEarned > 0)
            {
                _saveData.CurrentDopamine += evt.DopamineEarned;
                _saveData.TotalRunDopamine += evt.DopamineEarned;
            }
            if (evt.PhoneId == 1 && evt.Category == VideoCategory.Standard)
            {
                _nicheTolerance.RegisterWatched(evt.Niche);
            }

            if (evt.PhoneId > 1 && evt.PhoneId - 1 < _session.Phones.Count)
            {
                var secPhone = _session.Phones[evt.PhoneId - 1];
                if (secPhone.Role == SecondaryRole.CryptoMiner)
                {
                    double mined = _balanceConfig.SecondaryRoles.CryptoMinerCashPerVideo;
                    _saveData.CurrentMoney = Math.Round(_saveData.CurrentMoney + mined, 2);
                    SpawnFloatingText($"+{GlowStyle.FormatCash(mined)}", GlowStyle.Cash, FlyTarget.Cash);
                }
            }

            Vector2 center = _videoCanvas.Size * 0.5f;

            if (evt.Category == VideoCategory.Mythic)
            {
                string mythicId = $"MYTHIC_{(_saveData.UnlockedMythicIds.Count % MythicCatalog.Count)}";
                _saveData.UnlockedMythicIds.Add(mythicId);
                _audio?.PlaySfx("sfx_mythic");
                _particleSystem?.EmitBurst(center, new Color(1.0f, 0.88f, 0.25f), count: 36, speedScale: 1.4f);
                LogEvent($"[color=#ffd700]★ 3:00 AM MYTHIC COMPLETED on Phone #{evt.PhoneId}! +{evt.DopamineEarned:F0} Dopamine![/color]");
                SpawnFloatingText($"MYTHIC +{GlowStyle.FormatBig(evt.DopamineEarned)}", GlowStyle.Mythic, FlyTarget.Dopamine);
            }
            else if (evt.PhoneId == 1 && evt.DopamineEarned > 0)
            {
                _audio?.PlaySfx("sfx_complete", 0.96f + (float)_fxRng.NextDouble() * 0.08f);
                _particleSystem?.EmitBurst(
                    center,
                    _feverModeTimer > 0f ? new Color(1.0f, 0.30f, 0.95f) : new Color(0.35f, 0.95f, 0.85f),
                    count: _feverModeTimer > 0f ? 22 : 14,
                    speedScale: _feverModeTimer > 0f ? 1.25f : 0.9f);

                string feverOrComboTag = _feverModeTimer > 0f
                    ? $" [🔥 FEVER x{FeverX}!]"
                    : (_feverComboStreak > 0 ? $" [Combo x{_feverComboStreak}/{_balanceConfig.Combo.SwipesToFever}]" : "");
                LogEvent($"[color=#7fffd4]✓ Video watched ({evt.Niche}): +{evt.DopamineEarned:F1} Dopamine{feverOrComboTag}[/color]");

                SpawnFloatingText(
                    "+" + GlowStyle.FormatBig(evt.DopamineEarned),
                    _feverModeTimer > 0f ? GlowStyle.Fever : GlowStyle.Dopamine,
                    FlyTarget.Dopamine);
            }
        }

        private void OnVideoLiked(VideoLikedEvent evt)
        {
            var feed = _balanceConfig.Feed;
            _algorithm.RegisterLike(evt.Category, evt.Niche, (float)feed.NicheLikeWeightGain, (float)feed.NicheMaxWeight, feed.AdLikePenaltyVideos);
            Vector2 center = _videoCanvas.Size * 0.5f;

            if (evt.Category == VideoCategory.AdSpam)
            {
                _viralStreakCount = 0;
                _feverComboStreak = 0;
                _audio?.PlaySfx("sfx_blackout", 1.35f);
                LogEvent($"[color=#ff5555]⚠ Liked an AD! Algorithm polluted (+{feed.PollutedAdBonus * 100:F0}% Spam for {feed.AdLikePenaltyVideos} videos)![/color]");
                SpawnFloatingText("AD LIKED! +SPAM", GlowStyle.Danger);
                return;
            }

            if (evt.Niche == _lastLikedNiche)
            {
                _viralStreakCount = Math.Min(_balanceConfig.Combo.ViralStreakMax, _viralStreakCount + 1);
            }
            else
            {
                _lastLikedNiche = evt.Niche;
                _viralStreakCount = 1;
            }
            _nightBestStreak = Math.Max(_nightBestStreak, _viralStreakCount);

            _saveData.CurrentDopamine += evt.BonusDopamine;
            _saveData.TotalRunDopamine += evt.BonusDopamine;
            _audio?.PlaySfx(evt.IsCritical ? "sfx_crit_like" : "sfx_like");
            _particleSystem?.EmitBurst(
                center,
                evt.IsCritical ? new Color(1.0f, 0.90f, 0.25f) : new Color(1.0f, 0.40f, 0.88f),
                count: evt.IsCritical ? 28 : 16,
                speedScale: evt.IsCritical ? 1.3f : 1.0f);

            string critTag = evt.IsCritical ? $" [CRIT x{_balanceConfig.Phone.CritMultiplier:0.##}!]" : "";
            string catHealTag = "";
            double eyeRestore = _balanceConfig.Niches.For(evt.Niche)?.LikeEyeRestore ?? 0.0;
            if (eyeRestore > 0)
            {
                _session.Eyes.RestorePartial((float)eyeRestore);
                catHealTag = $" (+{eyeRestore:0.##}% Eye Freshness)";
            }

            LogEvent($"[color=#ff77ff]♥ Liked {evt.Niche}{critTag}: +{evt.BonusDopamine:F1} Dopamine{catHealTag}[/color]");
            SpawnFloatingText(
                evt.IsCritical ? $"CRIT +{GlowStyle.FormatBig(evt.BonusDopamine)}" : "+" + GlowStyle.FormatBig(evt.BonusDopamine),
                evt.IsCritical ? GlowStyle.Mythic : GlowStyle.Dopamine,
                FlyTarget.Dopamine);
        }

        private void OnScreamerTriggered(ScreamerTriggeredEvent evt)
        {
            _saveData.LifetimeScreamersSurvived++;
            _session.Eyes.RestoreFull();
            if (!_settings.SafeScreamer)
            {
                _screenShakeTimer = 0.45f;
                _audio?.PlaySfx("sfx_screamer");
            }
            else
            {
                _audio?.PlaySfx("sfx_complete", 1.25f);
            }

            if (GetUpgradeLevel("DOP_11") > 0)
            {
                var stats = BuildPhoneStats(isPrimary: true, SecondaryRole.Standard);
                double bonus = Math.Round(stats.BaseDopamineReward * _balanceConfig.GetUpgrade("DOP_11").Value, 1);
                _saveData.CurrentDopamine += bonus;
                _saveData.TotalRunDopamine += bonus;
                LogEvent($"[color=#ff3333]💀 SCREAMER on Phone #{evt.PhoneId}! Jumpscare Junkie awarded +{bonus:F1} Dopamine & 100% Eyes![/color]");
                SpawnFloatingText($"JUMPSCARE +{GlowStyle.FormatBig(bonus)}", GlowStyle.Fever, FlyTarget.Dopamine);
            }
            else
            {
                LogEvent($"[color=#ff3333]💀 SCREAMER on Phone #{evt.PhoneId}! Stunned {_balanceConfig.Phone.ScreamerStunSeconds:0.##}s, but Adrenaline restored 100% Eye Freshness![/color]");
                SpawnFloatingText("ADRENALINE · EYES 100%", GlowStyle.Eyes);
            }
        }

        private void OnPhoneBatteryDepleted(PhoneBatteryDepletedEvent evt)
        {
            LogEvent($"[color=#ffaa44]🪫 Phone #{evt.PhoneId} battery reached 0%.[/color]");
        }

        private void OnTurnEnded(TurnEndedEvent evt)
        {
            float attendance = _session.Laptop.GetAttendanceRatio();
            double salaryEarned = RunFormulas.NightSalary(_balanceConfig, _saveData, attendance);
            _saveData.CurrentMoney += salaryEarned;
            _isCharging = false;
            RefreshChargerButton();
            HideTip();

            int secondaryCount = RunFormulas.SecondaryPhoneCount(_balanceConfig, _saveData);
            bool hasFan = GetUpgradeLevel("HW_02") > 0;
            int gridLvl = GetGrassLevel("GRASS_04");

            bool isBillDay = _economy.IsBillDay(_saveData.CurrentDay);
            double billAmount = _economy.CalculateElectricBill(_saveData.CurrentDay, secondaryCount, hasFan, gridLvl);

            _isBillUnpaidLock = false;
            if (isBillDay)
            {
                if (_saveData.CurrentMoney >= billAmount)
                {
                    _saveData.CurrentMoney -= billAmount;
                }
                else
                {
                    _isBillUnpaidLock = true;
                }
            }

            _saveData.NightCompleted = true;
            _saveData.PowerCutPending = _isBillUnpaidLock;
            FillNightReport(evt.Reason, salaryEarned, attendance, isBillDay, billAmount);
            ConsumeNightItems();
            SaveProgress();
            ShowNightPhones(shopFirst: false);
        }

        /// <summary>
        /// The save was written after a night ended (Night Shop phase): reopen the shop instead of
        /// replaying that night, and keep an unpaid Electricity Bill locked.
        /// </summary>
        private void ResumeNightShop()
        {
            _session.Abort();
            _isBillUnpaidLock = _saveData.PowerCutPending;
            FillResumedReport();
            ShowNightPhones(shopFirst: true);
            RefreshLockScreen();
            LogEvent("[color=#66ffcc]--- NIGHT SHOP (save resumed) ---[/color]");
        }

        private int GetUpgradeLevel(string id)
        {
            return _saveData.RunUpgradeLevels.TryGetValue(id, out int lvl) ? lvl : 0;
        }

        private int GetGrassLevel(string id)
        {
            return _saveData.GrassUpgradeLevels.TryGetValue(id, out int lvl) ? lvl : 0;
        }

        private bool HasMythic(string id)
        {
            return _saveData.UnlockedMythicIds.Contains(id);
        }

        private void LogEvent(string bbcodeLine)
        {
            if (_eventFeedLabel == null) return;
            _eventFeedLabel.AppendText(bbcodeLine + "\n");
            while (_eventFeedLabel.GetParagraphCount() > 40)
            {
                _eventFeedLabel.RemoveParagraph(0);
            }
        }

        private void BuildDeskInterface()
        {
            BuildDeskScene();
            BuildTipToast();
            BuildOverlays();
        }

        /// <summary>Night report + shop and the lock screen (both built in DeskSceneController.Menus.cs).</summary>
        private void BuildOverlays()
        {
            BuildNightPhones();
            BuildLockScreen();
        }

        private void UpdatePlaytestBot(float dt)
        {
            _botTotalRunTime += dt;
            if (_isPlaytestAutoQuit && _botTotalRunTime >= _botMaxRuntime)
            {
                CapturePlaytestScreenshot();
                SaveProgress();
                GD.Print("[PLAYTEST BOT] 25s test duration reached. Exiting gracefully.");
                GetTree().Quit();
                return;
            }

            _botScreenshotTimer += dt;
            if (_botScreenshotTimer >= 4.0f)
            {
                _botScreenshotTimer = 0f;
                CapturePlaytestScreenshot();
            }

            // 1. If Night Modal is open
            if (_nightModal.Visible)
            {
                _botModalTimer += dt;
                if (_botModalTimer >= 2.0f)
                {
                    _botModalTimer = 0f;

                    // Try to buy DOP_01 (Short Attention Span)
                    var attentionUpgrade = _balanceConfig.GetUpgrade("DOP_01");
                    int curLvl = GetUpgradeLevel("DOP_01");
                    double cost = Math.Round(_economy.CalculateUpgradeCost(attentionUpgrade, curLvl), 1);
                    if (_saveData.CurrentDopamine >= cost && curLvl < (attentionUpgrade.MaxLevel ?? int.MaxValue))
                    {
                        double pool = _saveData.CurrentDopamine;
                        if (_buyUpgrade.TryBuyUpgrade(attentionUpgrade, ref pool, _saveData.RunUpgradeLevels))
                        {
                            _saveData.CurrentDopamine = pool;
                            _audio?.PlaySfx("sfx_like", 1.15f);
                            RefreshNightTabContent();
                            LogEvent("[color=#ffff00]🤖 Bot purchased DOP_01 (Short Attention Span)![/color]");
                        }
                    }

                    if (!_nextDayButton.Disabled)
                    {
                        _nextDayButton.EmitSignal(BaseButton.SignalName.Pressed);
                        LogEvent($"[color=#66ffcc]🤖 Bot advanced to Night {_saveData.CurrentDay}![/color]");
                    }
                }
                return;
            }

            if (_session == null || _session.IsFinished) return;

            var primary = _session.Phones[0];
            PhoneStats primaryStats = BuildPhoneStats(isPrimary: true, SecondaryRole.Standard);

            if (_isSpamPopupActive)
            {
                _botDeskActionTimer += dt;
                if (_botDeskActionTimer >= 0.45f)
                {
                    _botDeskActionTimer = 0f;
                    DismissSpamPopup();
                }
                return;
            }

            // 2. Up Arrow Actions: Boss Slack Alert / Typing Audit, Wall Breaker, or 3:00 AM Random Desk Event
            if (_session.Laptop.IsAlertActive || (_isBlackoutActive && _breakerResetButton.Visible) || _activeRandomEvent != RandomEventType.None)
            {
                _botDeskActionTimer += dt;
                if (_botDeskActionTimer >= 0.42f)
                {
                    _botDeskActionTimer = 0f;
                    HandleUpArrowDeskAction();
                }
            }
            else
            {
                _botDeskActionTimer = 0f;
            }

            // 4. Video Likes: like early on Standard / Mythic videos
            if (!primary.HasLikedCurrentVideo && !primary.IsInTransition && !primary.IsStunnedByScreamer)
            {
                if (primary.CurrentVideo == VideoCategory.Standard || primary.CurrentVideo == VideoCategory.Mythic)
                {
                    _botLikeTimer += dt;
                    if (_botLikeTimer >= 0.35f)
                    {
                        _botLikeTimer = 0f;
                        PerformPrimaryLike();
                        LogEvent("[color=#ff66cc]🤖 Bot liked video![/color]");
                    }
                }
            }
            else
            {
                _botLikeTimer = 0f;
            }

            // 5. Video Swipes: AdSpam (immediate), EarlySwipeReady (early), Completed (immediate)
            if (!primary.IsInTransition && !primary.IsStunnedByScreamer)
            {
                if (primary.CurrentVideo == VideoCategory.AdSpam)
                {
                    _botSwipeTimer += dt;
                    if (_botSwipeTimer >= 0.20f)
                    {
                        _botSwipeTimer = 0f;
                        LogEvent("[color=#ffaa00]🤖 Bot swiped away Ad![/color]");
                        TryManualPrimaryScroll();
                    }
                }
                else if (primary.IsEarlySwipeReady(primaryStats.AttentionThreshold) || primary.IsCurrentVideoCompleted)
                {
                    _botSwipeTimer += dt;
                    if (_botSwipeTimer >= 0.18f)
                    {
                        _botSwipeTimer = 0f;
                        LogEvent("[color=#44ff99]🤖 Bot performed Early Swipe![/color]");
                        TryManualPrimaryScroll();
                    }
                }
                else
                {
                    _botSwipeTimer = 0f;
                }
            }
        }

        private void CapturePlaytestScreenshot()
        {
            try
            {
                string dir = ProjectSettings.GlobalizePath("res://PlaytestScreenshots");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var img = GetViewport().GetTexture().GetImage();
                if (img != null)
                {
                    string filename = Path.Combine(dir, $"playtest_{_botScreenshotIndex++:00}.png");
                    img.SavePng(filename);
                    GD.Print($"[PLAYTEST BOT] Screenshot captured: {filename}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[PLAYTEST BOT] Screenshot failed: {ex.Message}");
            }
        }
    }
}
