using System;
using Godot;

namespace Doomscroll.Presentation.UI
{
    /// <summary>
    /// A secondary phone standing in the desk charging rack (or an empty dock).
    /// Clicking it skips the current video (or claims a Mythic). Drawn in code in the sticker style.
    /// </summary>
    public partial class MiniPhoneButton : BaseButton
    {
        public enum RoleGlyph { Standard, Miner, Shield, Radar }

        public Color ScreenColor { get; set; } = Color.FromHtml("#7A2E9E");
        public Color AccentColor { get; set; } = GlowStyle.Dopamine;
        public RoleGlyph Glyph { get; set; } = RoleGlyph.Standard;
        public float VideoFraction { get; set; }
        public bool HasBattery { get; set; } = true;
        public bool IsMythic { get; set; }
        public bool IsEmptyDock { get; set; }
        public string DockText { get; set; } = string.Empty;

        private readonly StyleBoxFlat _bezel = new() { BgColor = GlowStyle.Ink, AntiAliasing = true };
        private readonly StyleBoxFlat _screen = new() { AntiAliasing = true };
        private float _time;

        public MiniPhoneButton()
        {
            FocusMode = FocusModeEnum.None;
            MouseDefaultCursorShape = CursorShape.PointingHand;
            _bezel.SetCornerRadiusAll(14);
            _screen.SetCornerRadiusAll(9);
        }

        public override void _Process(double delta)
        {
            _time += (float)delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            Vector2 size = Size;
            if (IsEmptyDock)
            {
                DrawDock(size);
                return;
            }

            DrawMode mode = GetDrawMode();
            float lift = mode == DrawMode.Hover ? -3f : mode == DrawMode.Pressed || mode == DrawMode.HoverPressed ? 2f : 0f;
            var outer = new Rect2(0, lift, size.X, size.Y);
            DrawStyleBox(_bezel, outer);

            var inner = outer.Grow(-6f);
            _screen.BgColor = HasBattery ? ScreenColor : Color.FromHtml("#0B0A14");
            DrawStyleBox(_screen, inner);
            if (!HasBattery) return;

            Vector2 c = inner.GetCenter() + new Vector2(0, -10f);
            if (IsMythic)
            {
                float pulse = 0.8f + 0.2f * MathF.Sin(_time * 8f);
                for (int i = 0; i < 8; i++)
                {
                    float a = _time * 1.5f + i * MathF.Tau / 8f;
                    Vector2 dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
                    DrawLine(c + dir * 10f, c + dir * 26f * pulse, GlowStyle.Mythic, 3f, true);
                }
                DrawCircle(c, 12f, GlowStyle.Ink);
                DrawCircle(c, 9f, GlowStyle.Mythic);
            }
            else
            {
                switch (Glyph)
                {
                    case RoleGlyph.Miner:
                        DrawString(GlowStyle.Display, c + new Vector2(-14f, 12f), "$", HorizontalAlignment.Left, -1, 30, AccentColor);
                        DrawString(GlowStyle.Display, c + new Vector2(6f, 20f), "$", HorizontalAlignment.Left, -1, 18, AccentColor);
                        break;
                    case RoleGlyph.Shield:
                        DrawPolyline(new[]
                        {
                            c + new Vector2(-14f, -14f), c + new Vector2(14f, -14f),
                            c + new Vector2(14f, 2f), c + new Vector2(0f, 16f),
                            c + new Vector2(-14f, 2f), c + new Vector2(-14f, -14f)
                        }, AccentColor, 4f, true);
                        break;
                    case RoleGlyph.Radar:
                        DrawArc(c, 8f, 0f, MathF.Tau, 20, AccentColor, 3f, true);
                        DrawArc(c, 16f, _time * 2f, _time * 2f + 2.2f, 16, AccentColor, 3f, true);
                        break;
                    default:
                        DrawCircle(c, 16f, GlowStyle.Ink);
                        DrawCircle(c, 13f, AccentColor);
                        break;
                }
            }

            var track = new Rect2(inner.Position.X + 6f, inner.End.Y - 12f, inner.Size.X - 12f, 5f);
            DrawRect(track, new Color(1f, 1f, 1f, 0.25f));
            DrawRect(new Rect2(track.Position, new Vector2(track.Size.X * Math.Clamp(VideoFraction, 0f, 1f), track.Size.Y)), AccentColor);
        }

        private void DrawDock(Vector2 size)
        {
            Color dash = GlowStyle.Muted;
            float r = 2f;
            Vector2 a = new Vector2(r, r);
            Vector2 b = new Vector2(size.X - r, r);
            Vector2 d = new Vector2(size.X - r, size.Y - r);
            Vector2 e = new Vector2(r, size.Y - r);
            DrawDashedLine(a, b, dash, 3f, 8f);
            DrawDashedLine(b, d, dash, 3f, 8f);
            DrawDashedLine(d, e, dash, 3f, 8f);
            DrawDashedLine(e, a, dash, 3f, 8f);
            DrawString(GlowStyle.Display, new Vector2(0, size.Y * 0.5f + 7f), DockText, HorizontalAlignment.Center, size.X, 18, dash);
        }
    }
}
