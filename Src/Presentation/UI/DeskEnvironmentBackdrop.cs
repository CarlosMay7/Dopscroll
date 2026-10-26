using System;
using Godot;

namespace Doomscroll.Presentation.UI
{
    /// <summary>
    /// The 3 AM room behind the UI, drawn in the "3 AM Glow" style: a dark wall, a window whose sky
    /// goes from moon to dawn over the night, post-its, the desk lit only by the phone, the power strip,
    /// the alarm clock and the hardware the player bought. Coordinates are authored for the 1920x1080
    /// base viewport; left, center and right groups follow their edge when the window is wider.
    /// </summary>
    public partial class DeskEnvironmentBackdrop : Control
    {
        public const float BaseWidth = 1920f;
        public const float DeskTop = 732f;

        public float NightProgress { get; set; }
        public bool IsBlackout { get; set; }
        public int SpeedLevel { get; set; } = 1;
        public bool HasDeskFan { get; set; }
        public bool HasPowerBank { get; set; }
        public bool HasCoffeeMug { get; set; }
        public bool HasDrinkingBird { get; set; }
        public bool HasPowerStrip { get; set; }
        public bool FeverActive { get; set; }
        public bool ReducedMotion { get; set; }
        public string ClockText { get; set; } = "12:00";
        /// <summary>Phone #1's charger cable runs from the power strip into the phone.</summary>
        public bool ChargerPlugged { get; set; }
        /// <summary>Plugged in and the room has power: the cable glows and current flows.</summary>
        public bool ChargerLive { get; set; }

        private static readonly Color NightSky = Color.FromHtml("#141A44");
        private float _time;

        public override void _Process(double delta)
        {
            _time += (float)delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            Vector2 size = Size;
            if (size.X <= 10 || size.Y <= 10) return;

            float cx = (size.X - BaseWidth) * 0.5f;
            float rx = size.X - BaseWidth;
            Color glow = SpeedLevel >= 5 ? GlowStyle.Dopamine : SpeedLevel >= 2 ? GlowStyle.Violet : GlowStyle.ScreenGlow;

            DrawRect(new Rect2(0, 0, size.X, DeskTop), GlowStyle.Wall);
            DrawSoftGlow(new Vector2(960 + cx, 470), 470f, glow, 0.13f, 1f);
            DrawWindow(new Rect2(96, 89, 432, 300));
            DrawPostIt(new Vector2(546, 146), new Vector2(172, 140), 5f, GlowStyle.Mythic, "REPLY TO\nBOSS OR\nNO RENT!!");
            DrawPostIt(new Vector2(1416 + rx, 150), new Vector2(172, 132), -4f, GlowStyle.PostItPink, "touch grass?\n(later)");

            DrawRect(new Rect2(0, DeskTop, size.X, size.Y - DeskTop), GlowStyle.Desk);
            DrawRect(new Rect2(0, DeskTop, size.X, 4), GlowStyle.Ink);
            DrawRect(new Rect2(0, DeskTop + 4, size.X, 12), GlowStyle.DeskEdge);
            DrawSoftGlow(new Vector2(960 + cx, 1010), 500f, glow, 0.20f, 0.42f);

            DrawPowerStrip(new Rect2(139, 862, 360, 44));
            DrawChargerCable(cx);
            if (HasDeskFan) DrawFan(new Vector2(612, 792));
            if (HasDrinkingBird) DrawBird(new Vector2(1352 + rx, 860));
            DrawClock(new Rect2(1421 + rx, 811, 228, 110));
            if (HasCoffeeMug) DrawMug(new Vector2(1704 + rx, 828));
            if (HasPowerBank) DrawPowerBank(new Rect2(300, 770, 120, 44));

            if (IsBlackout)
            {
                DrawRect(new Rect2(0, 0, size.X, size.Y), new Color(0f, 0f, 0f, 0.55f));
            }

            if (FeverActive)
            {
                float alpha = 0.11f + (ReducedMotion ? 0f : 0.04f * MathF.Sin(_time * 6f));
                DrawRect(new Rect2(0, 0, size.X, size.Y), new Color(GlowStyle.Dopamine, alpha));
            }
        }

        private void DrawSoftGlow(Vector2 center, float radius, Color color, float maxAlpha, float yScale)
        {
            const int layers = 14;
            DrawSetTransform(center, 0f, new Vector2(1f, yScale));
            for (int i = 0; i < layers; i++)
            {
                float r = radius * (1f - i / (float)layers);
                DrawCircle(Vector2.Zero, r, new Color(color, maxAlpha / layers));
            }
            DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        }

        private void DrawWindow(Rect2 frame)
        {
            Rect2 sky = frame.Grow(-14f);
            float dawn = Mathf.SmoothStep(0.45f, 1f, NightProgress);
            DrawRect(sky, NightSky.Lerp(GlowStyle.Dawn, dawn));

            float starAlpha = 1f - Mathf.SmoothStep(0.2f, 0.6f, NightProgress);
            if (starAlpha > 0.01f)
            {
                for (int i = 0; i < 9; i++)
                {
                    var p = new Vector2(sky.Position.X + 24 + (i * 47) % (sky.Size.X - 48), sky.Position.Y + 18 + (i * 61) % (sky.Size.Y * 0.55f));
                    float twinkle = ReducedMotion ? 1f : 0.6f + 0.4f * MathF.Sin(_time * 2f + i);
                    DrawRect(new Rect2(p, new Vector2(3, 3)), new Color(GlowStyle.Paper, starAlpha * twinkle));
                }
            }

            float moonAlpha = 1f - Mathf.SmoothStep(0.5f, 0.85f, NightProgress);
            if (moonAlpha > 0.01f)
            {
                DrawCircle(new Vector2(sky.End.X - 84, sky.Position.Y + 64), 32f, new Color(Color.FromHtml("#F3EFD6"), moonAlpha));
            }

            float sunRise = Mathf.SmoothStep(0.62f, 1f, NightProgress);
            if (sunRise > 0.01f)
            {
                var sunCenter = new Vector2(sky.Position.X + sky.Size.X * 0.4f, sky.End.Y + 46f - 110f * sunRise);
                var points = new Vector2[28];
                for (int i = 0; i < points.Length; i++)
                {
                    float a = i * MathF.Tau / points.Length;
                    var p = sunCenter + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 46f;
                    points[i] = new Vector2(p.X, Math.Min(p.Y, sky.End.Y));
                }
                DrawColoredPolygon(points, GlowStyle.Mythic);
            }

            for (int i = 0; i < 5; i++)
            {
                DrawRect(new Rect2(sky.Position.X, sky.Position.Y + 18 + i * 52, sky.Size.X, 13), new Color(GlowStyle.Ink, 0.55f));
            }

            DrawRect(new Rect2(frame.Position, new Vector2(frame.Size.X, 14)), GlowStyle.Desk);
            DrawRect(new Rect2(frame.Position.X, sky.End.Y, frame.Size.X, 14), GlowStyle.Desk);
            DrawRect(new Rect2(frame.Position, new Vector2(14, frame.Size.Y)), GlowStyle.Desk);
            DrawRect(new Rect2(sky.End.X, frame.Position.Y, 14, frame.Size.Y), GlowStyle.Desk);
            DrawRect(frame, GlowStyle.Ink, false, 3f);
            DrawRect(sky, GlowStyle.Ink, false, 3f);

            var sill = new Rect2(frame.Position.X - 16, frame.End.Y, frame.Size.X + 32, 18);
            DrawRect(sill, GlowStyle.DeskEdge);
            DrawRect(sill, GlowStyle.Ink, false, 3f);

            if (dawn > 0.05f)
            {
                DrawSoftGlow(frame.GetCenter() + new Vector2(0, 40), 340f, GlowStyle.Dawn, 0.10f * dawn, 0.8f);
            }
        }

        private void DrawPostIt(Vector2 position, Vector2 size, float degrees, Color color, string text)
        {
            DrawSetTransform(position + size * 0.5f, Mathf.DegToRad(degrees), Vector2.One);
            var rect = new Rect2(-size * 0.5f, size);
            DrawRect(new Rect2(rect.Position + new Vector2(0, 4), rect.Size), GlowStyle.Ink);
            DrawRect(rect, color);
            DrawRect(rect, GlowStyle.Ink, false, 3f);
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                DrawString(GlowStyle.Mono, rect.Position + new Vector2(14, 40 + i * 30), lines[i], HorizontalAlignment.Left, size.X - 20, 32, GlowStyle.Ink);
            }
            DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        }

        private void DrawPowerStrip(Rect2 r)
        {
            DrawRect(r, GlowStyle.Ink);
            for (int i = 0; i < 4; i++)
            {
                DrawRect(new Rect2(r.Position.X + 64 + i * 72, r.Position.Y + 14, 44, 16), GlowStyle.DeskEdge);
            }
            Color led = IsBlackout
                ? (MathF.Sin(_time * 10f) > 0 ? GlowStyle.Danger : GlowStyle.DeskEdge)
                : HasPowerStrip ? GlowStyle.Grass : GlowStyle.Fever;
            DrawCircle(new Vector2(r.Position.X + 28, r.Position.Y + r.Size.Y * 0.5f), 7f, led);
        }

        /// <summary>
        /// The charger: plugged into the strip's last socket. Unplugged, its end lies on the desk by the plug sticker;
        /// plugged in, it runs under the phone and glows, with current dots flowing towards the phone.
        /// </summary>
        private void DrawChargerCable(float cx)
        {
            var socket = new Vector2(441, 878);
            Vector2 end, c1, c2;
            if (ChargerPlugged)
            {
                end = new Vector2(960 + cx, 1062);
                c1 = socket + new Vector2(30, 190);
                c2 = new Vector2(800 + cx, 1080);
            }
            else
            {
                end = new Vector2(486, 1044);
                c1 = socket + new Vector2(10, 110);
                c2 = end + new Vector2(-46, 18);
            }

            var points = new Vector2[28];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = Bezier(socket, c1, c2, end, i / (float)(points.Length - 1));
            }

            Color core = ChargerLive ? GlowStyle.Eyes : ChargerPlugged ? GlowStyle.Muted : GlowStyle.DeskEdge.Lightened(0.25f);
            DrawPolyline(points, GlowStyle.Ink, 12f, true);
            DrawPolyline(points, core, 6f, true);
            DrawRect(new Rect2(socket.X - 14, socket.Y - 12, 28, 22), GlowStyle.Ink);
            DrawRect(new Rect2(socket.X - 9, socket.Y - 7, 18, 12), core);

            if (!ChargerPlugged)
            {
                // The loose USB end, waiting on the desk.
                DrawRect(new Rect2(end.X - 4, end.Y - 11, 26, 22), GlowStyle.Ink);
                DrawRect(new Rect2(end.X + 22, end.Y - 5, 10, 10), GlowStyle.Muted);
                return;
            }

            if (ChargerLive && !ReducedMotion)
            {
                for (int k = 0; k < 4; k++)
                {
                    float t = (_time * 0.55f + k / 4f) % 1f;
                    DrawCircle(Bezier(socket, c1, c2, end, t), 4.5f, Colors.White);
                }
            }
        }

        private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            float u = 1f - t;
            return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
        }

        private void DrawFan(Vector2 c)
        {
            DrawRect(new Rect2(c.X - 8, c.Y + 34, 16, 40), GlowStyle.Ink);
            DrawRect(new Rect2(c.X - 34, c.Y + 70, 68, 12), GlowStyle.Ink);
            DrawCircle(c, 44f, GlowStyle.Ink);
            DrawCircle(c, 39f, GlowStyle.DeskEdge);
            float spin = IsBlackout || ReducedMotion ? 0.4f : _time * 14f;
            for (int b = 0; b < 3; b++)
            {
                float a = spin + b * MathF.Tau / 3f;
                DrawLine(c, c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 32f, GlowStyle.Eyes, 9f, true);
            }
            DrawCircle(c, 8f, GlowStyle.Ink);
        }

        private void DrawBird(Vector2 baseCenter)
        {
            DrawRect(new Rect2(baseCenter.X - 26, baseCenter.Y + 30, 52, 12), GlowStyle.Ink);
            DrawLine(baseCenter + new Vector2(-14, 30), baseCenter + new Vector2(-6, -10), GlowStyle.Ink, 4f);
            DrawLine(baseCenter + new Vector2(14, 30), baseCenter + new Vector2(6, -10), GlowStyle.Ink, 4f);
            float tilt = ReducedMotion ? 0.2f : MathF.Sin(_time * 3.5f) * 0.5f;
            var pivot = baseCenter + new Vector2(0, -10);
            var head = pivot + new Vector2(MathF.Sin(tilt) * 46f, -MathF.Cos(tilt) * 46f);
            DrawLine(pivot, head, GlowStyle.Ink, 9f, true);
            DrawLine(pivot, head, GlowStyle.Danger, 5f, true);
            DrawCircle(head, 13f, GlowStyle.Ink);
            DrawCircle(head, 10f, GlowStyle.Eyes);
            DrawCircle(pivot, 9f, GlowStyle.Ink);
        }

        private void DrawClock(Rect2 r)
        {
            DrawRect(new Rect2(r.Position + new Vector2(0, 5), r.Size), new Color(0f, 0f, 0f, 0.35f));
            DrawRect(r, GlowStyle.Ink);
            Color digits = IsBlackout && MathF.Sin(_time * 6f) < 0f ? new Color(GlowStyle.Danger, 0.25f) : GlowStyle.Danger;
            DrawString(GlowStyle.Mono, r.Position + new Vector2(16, 84), ClockText, HorizontalAlignment.Left, r.Size.X - 60, 84, digits);
            DrawString(GlowStyle.Mono, r.Position + new Vector2(r.Size.X - 50, 84), "AM", HorizontalAlignment.Left, 48, 28, digits);
        }

        private void DrawMug(Vector2 p)
        {
            var body = new Rect2(p, new Vector2(88, 100));
            DrawArc(body.Position + new Vector2(body.Size.X + 2, body.Size.Y * 0.45f), 22f, -1.4f, 1.4f, 16, GlowStyle.Ink, 9f, true);
            DrawRect(body, GlowStyle.Dawn);
            DrawRect(body, GlowStyle.Ink, false, 3f);
            if (!ReducedMotion)
            {
                float sway = MathF.Sin(_time * 3f) * 5f;
                DrawLine(p + new Vector2(28, -8), p + new Vector2(28 + sway, -38), new Color(GlowStyle.Paper, 0.45f), 4f, true);
                DrawLine(p + new Vector2(58, -8), p + new Vector2(58 - sway, -42), new Color(GlowStyle.Paper, 0.45f), 4f, true);
            }
        }

        private void DrawPowerBank(Rect2 r)
        {
            DrawRect(r, GlowStyle.Ink);
            for (int i = 0; i < 4; i++)
            {
                DrawRect(new Rect2(r.Position.X + 14 + i * 24, r.Position.Y + 14, 16, r.Size.Y - 28), GlowStyle.Cash);
            }
        }
    }
}
