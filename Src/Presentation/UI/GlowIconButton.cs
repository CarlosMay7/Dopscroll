using System;
using Godot;

namespace Doomscroll.Presentation.UI
{
    /// <summary>
    /// Icon buttons drawn in code (no textures): the phone's social rail, the NEXT button and the pause button.
    /// Sticker look: white or colored fill with a thick ink outline; pops when <see cref="Pop"/> is called.
    /// </summary>
    public partial class GlowIconButton : BaseButton
    {
        public enum Glyph { Heart, Comment, Share, Disc, Menu, Down }

        public Glyph Icon { get; set; } = Glyph.Heart;
        public string Caption { get; set; } = string.Empty;
        /// <summary>Filled state (a liked heart).</summary>
        public bool Active { get; set; }
        /// <summary>Draws a round sticker behind the icon.</summary>
        public bool Round { get; set; }
        public Color Fill { get; set; } = GlowStyle.Paper;
        public float IconSize { get; set; } = 40f;
        public bool ReducedMotion { get; set; }

        private float _time;
        private float _pop;

        public GlowIconButton()
        {
            FocusMode = FocusModeEnum.None;
            MouseDefaultCursorShape = CursorShape.PointingHand;
        }

        public void Pop()
        {
            if (!ReducedMotion) _pop = 1f;
        }

        public override void _Process(double delta)
        {
            _time += (float)delta;
            if (_pop > 0f) _pop = Math.Max(0f, _pop - (float)delta * 4f);
            QueueRedraw();
        }

        public override void _Draw()
        {
            float captionHeight = string.IsNullOrEmpty(Caption) ? 0f : 22f;
            DrawMode mode = GetDrawMode();
            bool down = mode == DrawMode.Pressed || mode == DrawMode.HoverPressed;
            bool hover = mode == DrawMode.Hover;

            Vector2 c = new Vector2(Size.X * 0.5f, (Size.Y - captionHeight) * 0.5f);
            float s = IconSize * (1f + 0.25f * _pop) * (hover ? 1.06f : 1f);

            if (Round)
            {
                float r = Math.Min(Size.X, Size.Y - captionHeight) * 0.5f - 6f;
                DrawCircle(c + new Vector2(0, 4f), r + 3f, GlowStyle.Ink);
                if (down) c += new Vector2(0, 3f);
                DrawCircle(c, r + 3f, GlowStyle.Ink);
                DrawCircle(c, r, Fill);
            }
            else if (down)
            {
                c += new Vector2(0, 2f);
            }

            switch (Icon)
            {
                case Glyph.Heart:
                    DrawHeart(c, s);
                    break;
                case Glyph.Comment:
                    DrawComment(c, s);
                    break;
                case Glyph.Share:
                    DrawShare(c, s);
                    break;
                case Glyph.Disc:
                    DrawDisc(c, s);
                    break;
                case Glyph.Menu:
                    for (int i = -1; i <= 1; i++)
                    {
                        float y = c.Y + i * s * 0.24f;
                        DrawLine(new Vector2(c.X - s * 0.3f, y), new Vector2(c.X + s * 0.3f, y), GlowStyle.Ink, 4.5f, true);
                    }
                    break;
                case Glyph.Down:
                    DrawPolyline(new[]
                    {
                        c + new Vector2(-s * 0.32f, -s * 0.14f),
                        c + new Vector2(0, s * 0.18f),
                        c + new Vector2(s * 0.32f, -s * 0.14f)
                    }, GlowStyle.Ink, 7f, true);
                    break;
            }

            if (captionHeight > 0f)
            {
                Font font = GlowStyle.BodyBlack;
                Vector2 pos = new Vector2(0, Size.Y - 4f);
                DrawStringOutline(font, pos, Caption, HorizontalAlignment.Center, Size.X, 15, 6, GlowStyle.Ink);
                DrawString(font, pos, Caption, HorizontalAlignment.Center, Size.X, 15, Colors.White);
            }
        }

        private void DrawHeart(Vector2 c, float s)
        {
            Color fill = Active ? GlowStyle.Dopamine : Colors.White;
            Vector2 left = c + new Vector2(-s * 0.22f, -s * 0.12f);
            Vector2 right = c + new Vector2(s * 0.22f, -s * 0.12f);
            float r = s * 0.26f;
            Vector2[] tip =
            {
                c + new Vector2(-s * 0.47f, -s * 0.04f),
                c + new Vector2(s * 0.47f, -s * 0.04f),
                c + new Vector2(0, s * 0.42f)
            };
            Vector2[] tipOutline =
            {
                c + new Vector2(-s * 0.53f, -s * 0.06f),
                c + new Vector2(s * 0.53f, -s * 0.06f),
                c + new Vector2(0, s * 0.50f)
            };
            DrawCircle(left, r + 3f, GlowStyle.Ink);
            DrawCircle(right, r + 3f, GlowStyle.Ink);
            DrawColoredPolygon(tipOutline, GlowStyle.Ink);
            DrawCircle(left, r, fill);
            DrawCircle(right, r, fill);
            DrawColoredPolygon(tip, fill);
        }

        private void DrawComment(Vector2 c, float s)
        {
            float r = s * 0.40f;
            DrawColoredPolygon(new[] { c + new Vector2(-r * 0.6f, r * 0.5f), c + new Vector2(-r * 1.05f, r * 1.15f), c + new Vector2(0, r * 0.8f) }, GlowStyle.Ink);
            DrawCircle(c, r + 3f, GlowStyle.Ink);
            DrawCircle(c, r, Colors.White);
            for (int i = -1; i <= 1; i++)
            {
                DrawCircle(c + new Vector2(i * r * 0.42f, 0), s * 0.055f, GlowStyle.Ink);
            }
        }

        private void DrawShare(Vector2 c, float s)
        {
            Vector2[] shaft =
            {
                c + new Vector2(-s * 0.36f, s * 0.28f),
                c + new Vector2(-s * 0.16f, -s * 0.04f),
                c + new Vector2(s * 0.32f, -s * 0.04f)
            };
            Vector2[] head =
            {
                c + new Vector2(s * 0.12f, -s * 0.28f),
                c + new Vector2(s * 0.38f, -s * 0.04f),
                c + new Vector2(s * 0.12f, s * 0.20f)
            };
            DrawPolyline(shaft, GlowStyle.Ink, 10f, true);
            DrawPolyline(head, GlowStyle.Ink, 10f, true);
            DrawPolyline(shaft, Colors.White, 4.5f, true);
            DrawPolyline(head, Colors.White, 4.5f, true);
        }

        private void DrawDisc(Vector2 c, float s)
        {
            float r = s * 0.46f;
            float angle = ReducedMotion ? 0f : _time * 3.2f;
            DrawCircle(c, r + 3f, GlowStyle.Ink);
            DrawCircle(c, r, GlowStyle.DeskEdge);
            DrawArc(c, r * 0.72f, angle, angle + 1.9f, 16, GlowStyle.Muted, 2f, true);
            DrawArc(c, r * 0.72f, angle + MathF.PI, angle + MathF.PI + 1.9f, 16, GlowStyle.Muted, 2f, true);
            DrawCircle(c, r * 0.32f, GlowStyle.Dopamine);
            DrawCircle(c, r * 0.10f, GlowStyle.Ink);
        }
    }
}
