using System;
using Doomscroll.Core.Entities;
using Doomscroll.Core.Events;
using Doomscroll.Core.Services;
using Doomscroll.Presentation.UI;
using Godot;

namespace Doomscroll.Presentation.DeskScene
{
    /// <summary>
    /// First-time tips: a post-it that explains a mechanic the first time it matters (dying battery, full outlet,
    /// blurry eyes, a boring niche, the boss pinging). Each tip shows once per save and can be turned off or
    /// reset in Settings. Tips only read the night's state; they never change it.
    /// </summary>
    public partial class DeskSceneController
    {
        private const float TipSeconds = 9f;
        private const float TipWidth = 400f;

        private PanelContainer _tipToast = null!;
        private GlowGlyph _tipIcon = null!;
        private Label _tipTitle = null!;
        private Label _tipBody = null!;
        private float _tipTimer;
        private Tween? _tipTween;

        private void BuildTipToast()
        {
            _tipToast = GlowStyle.StickerPanel(GlowStyle.Mythic, 8, 5, 16, 12);
            _tipToast.MouseFilter = MouseFilterEnum.Stop;
            _tipToast.Visible = false;
            _tipToast.MouseDefaultCursorShape = CursorShape.PointingHand;
            _tipToast.TooltipText = "Click to dismiss";
            _tipToast.GuiInput += e =>
            {
                if (e is InputEventMouseButton { Pressed: true }) HideTip();
            };

            var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            box.AddThemeConstantOverride("separation", 4);
            var header = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            header.AddThemeConstantOverride("separation", 10);
            _tipIcon = new GlowGlyph { TileColor = Colors.White, CustomMinimumSize = new Vector2(34, 34), SizeFlagsVertical = SizeFlags.ShrinkCenter };
            header.AddChild(_tipIcon);
            _tipTitle = GlowStyle.MakeLabel("", GlowStyle.Display, 17, GlowStyle.Ink);
            _tipTitle.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            header.AddChild(_tipTitle);
            box.AddChild(header);
            _tipBody = GlowStyle.MakeLabel("", GlowStyle.Body, 17, GlowStyle.Ink);
            _tipBody.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            box.AddChild(_tipBody);
            var dismiss = GlowStyle.MakeLabel("click to dismiss", GlowStyle.Body, 13, new Color(GlowStyle.Ink, 0.6f));
            dismiss.HorizontalAlignment = HorizontalAlignment.Right;
            box.AddChild(dismiss);
            _tipToast.AddChild(box);
            // Right side of the desk, under the laptop: it never covers the phone, the rack or the plug.
            Place(this, _tipToast, HAnchor.Right, 1360f, 738f, TipWidth, 0f);
            _tipBody.CustomMinimumSize = new Vector2(TipWidth - 32f, 0f);
        }

        /// <summary>Checks the night for a mechanic the player has not been told about yet (one tip at a time).</summary>
        private void UpdateTips(float dt, in PhoneStats primaryStats)
        {
            if (_tipToast.Visible)
            {
                _tipTimer -= dt;
                if (_tipTimer <= 0f) HideTip();
                return;
            }
            if (!_settings.Tips || _session == null || _session.IsFinished || _session.ElapsedSeconds < 2f) return;

            var primary = _session.Phones[0];
            float batteryPct = primary.CurrentBattery / Math.Max(1f, primaryStats.MaxBatterySeconds);
            double blurLoss = 1.0 - RunFormulas.BlurDopamineMultiplier(_balanceConfig, _session.Eyes.GetNormalizedBlur(LensBlurReduction));
            float boredom = primary.CurrentVideo == VideoCategory.Standard ? _nicheTolerance.Boredom(primary.CurrentNiche) : 0f;

            // In priority order; a tip that was already seen never blocks the next one.
            if (_session.Laptop.IsAlertActive && TryShowTip("boss", "briefcase", "THE BOSS IS PINGING",
                    "Press UP (or REPLY) before the timer runs out, or tonight's salary drops.")) return;
            if (!_isCharging && primary.HasBattery && batteryPct < 0.35f && TryShowTip("battery", "battery", "PHONE #1 IS DYING",
                    "Press C or the PLUG IN sticker (left, by the outlet) to charge. The night is longer than one battery. No overclock while plugged in.")) return;
            if (_electricalOverloadPercent >= 45f && !_isBlackoutActive && TryShowTip("outlet", "plug", "THE OUTLET IS FILLING UP",
                    "Rack phones, the charger, the fan and high brightness share one outlet. At 100% the breaker trips. Unplug something.")) return;
            if (blurLoss >= 0.08 && TryShowTip("blur", "glasses", "BLURRY = LESS DOPAMINE",
                    $"Tired eyes cut phone #1's dopamine (now -{blurLoss * 100:0}%). Eye drops, an energy drink or blue-light glasses help. Cat likes rest your eyes.")) return;
            if (boredom >= 0.15f) TryShowTip("boredom", "brain", $"BORED OF {primary.CurrentNiche.ToString().ToUpperInvariant()}",
                    $"The same niche pays less and less (now -{boredom * 100:0}%). Other niches recover while you watch them, and likes steer what the feed shows.");
        }

        /// <summary>Shows the tip unless this save has seen it; returns whether it is now on screen.</summary>
        private bool TryShowTip(string id, string glyph, string title, string body)
        {
            if (_saveData.SeenTips.Contains(id)) return false;
            _saveData.SeenTips.Add(id);

            _tipIcon.Set(glyph, Colors.White);
            _tipTitle.Text = title;
            _tipBody.Text = body;
            ApplyCurrentTextScale(_tipToast);
            _tipToast.Visible = true;
            _tipTimer = TipSeconds;
            Tilt(_tipToast, 1.5f);
            _audio?.PlaySfx("sfx_boss_ping", 1.5f);

            _tipTween?.Kill();
            if (!ReducedMotion)
            {
                _tipToast.Scale = new Vector2(0.8f, 0.8f);
                _tipTween = CreateTween();
                _tipTween.TweenProperty(_tipToast, "scale", Vector2.One, 0.25).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            }
            return true;
        }

        private void HideTip()
        {
            _tipTween?.Kill();
            _tipToast.Visible = false;
            _tipToast.Scale = Vector2.One;
        }

        /// <summary>The shop's post-it: a hint that fits where the run is.</summary>
        private string ShopNoteText()
        {
            var creator = _balanceConfig.GetUpgrade("CRT_01");
            bool creatorOpen = creator.UnlockNight is not int unlock || _saveData.CurrentDay >= unlock;
            if (creatorOpen && GetUpgradeLevel("CRT_01") == 0) return "creator mode is open: turn spare dopamine into rent money";
            if (_saveData.CurrentDay <= 2) return "snacks last one night only. buy them right before you need them";
            if (_economy.DaysUntilBill(_saveData.CurrentDay) == 1 || _economy.IsBillDay(_saveData.CurrentDay + 1)) return "bill after the next night. keep some cash";
            return "bought tonight = on your desk tomorrow night";
        }
    }
}
