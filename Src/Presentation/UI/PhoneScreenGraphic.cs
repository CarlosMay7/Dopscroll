using System;
using Doomscroll.Core.Events;
using Godot;

namespace Doomscroll.Presentation.UI
{
    /// <summary>
    /// The video playing on the main phone, drawn in code in the "3 AM Glow" sticker style:
    /// chunky shapes with ink outlines for each niche (cats, gaming, ASMR, memes), ads, 3 AM Mythics,
    /// screamers, a dead battery and Fever confetti. It fills the phone screen; the art sits in the upper half.
    /// </summary>
    public partial class PhoneScreenGraphic : Control
    {
        public VideoCategory Category { get; set; } = VideoCategory.Standard;
        public VideoNiche Niche { get; set; } = VideoNiche.Cats;
        public bool IsStunned { get; set; }
        public bool HasBattery { get; set; } = true;
        public bool SafeScreamerMode { get; set; }
        public bool SplitScreenActive { get; set; }
        public bool IsFeverActive { get; set; }
        public bool ReducedMotion { get; set; }

        private static readonly Color CatFur = Color.FromHtml("#FFB15C");
        private static readonly Color[] Confetti =
        {
            GlowStyle.Dopamine, GlowStyle.Mythic, GlowStyle.Eyes, GlowStyle.Cash, GlowStyle.Fever
        };
        private float _time;

        public override void _Process(double delta)
        {
            _time += (float)delta;
            QueueRedraw();
        }

        private float T => ReducedMotion ? 0f : _time;

        public override void _Draw()
        {
            Vector2 size = Size;
            if (size.X < 10 || size.Y < 10) return;
            Vector2 center = new Vector2(size.X * 0.5f, size.Y * 0.33f);

            if (!HasBattery)
            {
                DrawDeadBattery(size * 0.5f);
                return;
            }

            for (float y = 0; y < size.Y * 0.62f; y += 8f)
            {
                DrawRect(new Rect2(0, y, size.X, 4f), new Color(1f, 1f, 1f, 0.035f));
            }

            if (IsStunned)
            {
                if (SafeScreamerMode) DrawAdrenaline(center);
                else DrawScreamer(center, size);
                return;
            }

            switch (Category)
            {
                case VideoCategory.AdSpam:
                    DrawAd(center);
                    break;
                case VideoCategory.Mythic:
                    DrawMythic(center);
                    break;
                default:
                    switch (Niche)
                    {
                        case VideoNiche.Cats: DrawCat(center); break;
                        case VideoNiche.Gaming: DrawGaming(center); break;
                        case VideoNiche.Asmr: DrawAsmr(center); break;
                        default: DrawMeme(center); break;
                    }
                    break;
            }

            if (SplitScreenActive) DrawSoapCutting(size);
            if (IsFeverActive) DrawConfetti(size);
        }

        private void Blob(Vector2 c, float r, Color fill)
        {
            DrawCircle(c, r + 5f, GlowStyle.Ink);
            DrawCircle(c, r, fill);
        }

        private void Poly(Vector2[] points, Color fill)
        {
            DrawColoredPolygon(points, fill);
            var closed = new Vector2[points.Length + 1];
            Array.Copy(points, closed, points.Length);
            closed[points.Length] = points[0];
            DrawPolyline(closed, GlowStyle.Ink, 5f, true);
        }

        private void Box(Rect2 rect, Color fill)
        {
            DrawRect(rect.Grow(5f), GlowStyle.Ink);
            DrawRect(rect, fill);
        }

        private void DrawCat(Vector2 center)
        {
            Vector2 c = center + new Vector2(0, MathF.Sin(T * 4f) * 10f);
            Poly(new[] { c + new Vector2(-74, -30), c + new Vector2(-28, -46), c + new Vector2(-62, -104) }, CatFur);
            Poly(new[] { c + new Vector2(74, -30), c + new Vector2(28, -46), c + new Vector2(62, -104) }, CatFur);
            Blob(c, 76f, CatFur);
            bool blink = !ReducedMotion && (_time % 3.2f) > 3.0f;
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 eye = c + new Vector2(side * 28f, -10f);
                if (blink)
                {
                    DrawLine(eye + new Vector2(-12, 0), eye + new Vector2(12, 0), GlowStyle.Ink, 6f, true);
                }
                else
                {
                    DrawCircle(eye, 13f, GlowStyle.Ink);
                    DrawCircle(eye + new Vector2(-4, -5), 4.5f, Colors.White);
                }
            }
            Poly(new[] { c + new Vector2(-9, 12), c + new Vector2(9, 12), c + new Vector2(0, 22) }, GlowStyle.Dopamine);
            foreach (float side in new[] { -1f, 1f })
            {
                DrawLine(c + new Vector2(side * 40, 14), c + new Vector2(side * 100, 2), GlowStyle.Ink, 5f, true);
                DrawLine(c + new Vector2(side * 40, 26), c + new Vector2(side * 100, 32), GlowStyle.Ink, 5f, true);
            }
        }

        private void DrawGaming(Vector2 center)
        {
            Vector2 boss = center + new Vector2(MathF.Sin(T * 1.6f) * 18f, -20f);
            Blob(boss, 80f, GlowStyle.Fever);
            Box(new Rect2(boss + new Vector2(-40, -26), new Vector2(26, 30)), GlowStyle.Ink);
            Box(new Rect2(boss + new Vector2(14, -26), new Vector2(26, 30)), GlowStyle.Ink);
            Box(new Rect2(boss + new Vector2(-34, 30), new Vector2(68, 14)), GlowStyle.Ink);

            Vector2 ship = center + new Vector2(MathF.Sin(T * 3.2f) * 90f, 150f);
            Poly(new[] { ship + new Vector2(0, -26), ship + new Vector2(-24, 18), ship + new Vector2(24, 18) }, GlowStyle.Eyes);
            float laser = (T * 260f) % 90f;
            DrawLine(ship + new Vector2(0, -34 - laser), ship + new Vector2(0, -58 - laser), GlowStyle.Dopamine, 7f, true);
        }

        private void DrawAsmr(Vector2 center)
        {
            for (int i = 0; i < 4; i++)
            {
                float r = (T * 40f + i * 32f) % 128f + 40f;
                float alpha = 1f - (r - 40f) / 128f;
                DrawArc(center, r, 0f, MathF.Tau, 48, new Color(GlowStyle.Eyes, alpha), 6f, true);
            }
            var head = new Rect2(center + new Vector2(-30, -60), new Vector2(60, 84));
            Box(head, GlowStyle.Paper);
            for (int i = 0; i < 4; i++)
            {
                DrawLine(head.Position + new Vector2(10, 16 + i * 16), head.Position + new Vector2(50, 16 + i * 16), GlowStyle.DeskEdge, 4f);
            }
            Box(new Rect2(center + new Vector2(-6, 28), new Vector2(12, 40)), GlowStyle.Muted);
            Box(new Rect2(center + new Vector2(-36, 66), new Vector2(72, 12)), GlowStyle.Muted);
        }

        private void DrawMeme(Vector2 center)
        {
            Blob(center, 86f, GlowStyle.Dopamine);
            for (int i = 0; i < 4; i++)
            {
                DrawRect(new Rect2(center.X - 86, center.Y + 18 + i * 16, 172, 7), Color.FromHtml("#6B1D4F"));
            }
            Vector2 shades = center + new Vector2(0, -16 - MathF.Abs(MathF.Sin(T * 5f)) * 14f);
            Box(new Rect2(shades + new Vector2(-64, -14), new Vector2(54, 30)), GlowStyle.Ink);
            Box(new Rect2(shades + new Vector2(10, -14), new Vector2(54, 30)), GlowStyle.Ink);
            DrawLine(shades + new Vector2(-72, -10), shades + new Vector2(72, -10), GlowStyle.Ink, 7f);
            DrawRect(new Rect2(shades + new Vector2(-56, -8), new Vector2(12, 8)), Colors.White);
            DrawRect(new Rect2(shades + new Vector2(18, -8), new Vector2(12, 8)), Colors.White);
        }

        private void DrawAd(Vector2 center)
        {
            float pulse = ReducedMotion ? 0f : MathF.Sin(_time * 10f) * 6f;
            var card = new Rect2(center + new Vector2(-140 - pulse, -80 - pulse), new Vector2(280 + pulse * 2, 160 + pulse * 2));
            Box(card, GlowStyle.Mythic);
            DrawString(GlowStyle.Display, card.Position + new Vector2(0, 74), "BUY NOW", HorizontalAlignment.Center, card.Size.X, 42, GlowStyle.Ink);
            DrawString(GlowStyle.BodyBlack, card.Position + new Vector2(0, 122), "limited offer!!! 99% off", HorizontalAlignment.Center, card.Size.X, 20, GlowStyle.Ink);
            Blob(card.End + new Vector2(-10, -10), 30f, GlowStyle.Danger);
            DrawString(GlowStyle.Display, card.End + new Vector2(-34, 0), "$$", HorizontalAlignment.Center, 48, 22, Colors.White);
        }

        private void DrawMythic(Vector2 center)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = T * 1.4f + i * MathF.Tau / 12f;
                Vector2 dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
                DrawLine(center + dir * 70f, center + dir * 150f, new Color(GlowStyle.Mythic, 0.85f), 9f, true);
            }
            Blob(center, 62f, GlowStyle.Mythic);
            DrawString(GlowStyle.Display, center + new Vector2(-62, 18), "???", HorizontalAlignment.Center, 124, 46, GlowStyle.Ink);
        }

        private void DrawScreamer(Vector2 center, Vector2 size)
        {
            for (int i = 0; i < 8; i++)
            {
                float y = (i * 61f + _time * 260f) % size.Y;
                DrawRect(new Rect2(0, y, size.X, 10), new Color(GlowStyle.Danger, 0.35f));
            }
            Vector2 shake = new Vector2(MathF.Sin(_time * 55f) * 8f, MathF.Cos(_time * 48f) * 7f);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 eye = center + new Vector2(side * 62f, 0) + shake;
                Blob(eye, 34f, Colors.White);
                DrawCircle(eye, 13f, GlowStyle.Danger);
            }
        }

        private void DrawAdrenaline(Vector2 center)
        {
            float r = 70f + (ReducedMotion ? 0f : MathF.Sin(_time * 8f) * 8f);
            Blob(center, r, GlowStyle.Eyes);
            DrawString(GlowStyle.Display, center + new Vector2(-r, 12), "BREATHE", HorizontalAlignment.Center, r * 2, 28, GlowStyle.Ink);
        }

        private void DrawDeadBattery(Vector2 center)
        {
            var body = new Rect2(center + new Vector2(-70, -36), new Vector2(140, 72));
            DrawRect(body, GlowStyle.DeskEdge, false, 8f);
            DrawRect(new Rect2(body.End.X, center.Y - 14, 14, 28), GlowStyle.DeskEdge);
            DrawRect(new Rect2(body.Position + new Vector2(12, 12), new Vector2(14, 48)), GlowStyle.Danger);
            DrawString(GlowStyle.Display, center + new Vector2(-110, 96), "0%", HorizontalAlignment.Center, 220, 34, GlowStyle.Muted);
        }

        private void DrawSoapCutting(Vector2 size)
        {
            var window = new Rect2(24, size.Y * 0.69f, size.X - 110, 78);
            Box(window, Color.FromHtml("#173B5C"));
            float mid = window.Position.Y + window.Size.Y * 0.5f;
            for (int i = 0; i < 6; i++)
            {
                float x = window.Position.X + 14f + ((i * 46f + T * 110f) % (window.Size.X - 40f));
                DrawRect(new Rect2(x, mid - 10f + MathF.Sin(T * 8f + i) * 8f, 18, 18), GlowStyle.Cash);
            }
            DrawString(GlowStyle.BodyBlack, window.Position + new Vector2(10, 22), "soap cutting asmr", HorizontalAlignment.Left, -1, 15, GlowStyle.Paper);
        }

        private void DrawConfetti(Vector2 size)
        {
            for (int i = 0; i < 26; i++)
            {
                float x = (i * 97f) % size.X;
                float y = (i * 53f + _time * (140f + i * 6f)) % size.Y;
                float rot = _time * 3f + i;
                DrawSetTransform(new Vector2(x, y), ReducedMotion ? 0f : rot, Vector2.One);
                DrawRect(new Rect2(-6, -4, 12, 8), Confetti[i % Confetti.Length]);
            }
            DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        }
    }
}
