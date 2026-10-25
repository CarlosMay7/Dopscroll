using System;
using Godot;

namespace Doomscroll.Presentation.UI
{
    /// <summary>
    /// A line-drawn icon on an optional sticker tile (shop cards, lock-screen apps, notification badges).
    /// Glyphs are drawn on a 24x24 grid like the SVG icons in the design canvas; unknown names draw a dot.
    /// Every name in BalanceConfig.KnownIcons must have a case here.
    /// </summary>
    public partial class GlowGlyph : Control
    {
        public string Glyph { get; set; } = "star";
        public Color TileColor { get; set; } = GlowStyle.Dopamine;
        public Color GlyphColor { get; set; } = GlowStyle.Ink;
        /// <summary>Draw the rounded tile behind the glyph (false = glyph only).</summary>
        public bool ShowTile { get; set; } = true;
        public bool Round { get; set; }
        public bool Border { get; set; } = true;
        public Color BorderColor { get; set; } = GlowStyle.Ink;
        /// <summary>Glyph size as a fraction of the control's smaller side.</summary>
        public float GlyphScale { get; set; } = 0.55f;

        private Vector2 _origin;
        private float _unit;
        private float _stroke;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public void Set(string glyph, Color tile)
        {
            Glyph = glyph;
            TileColor = tile;
            QueueRedraw();
        }

        public override void _Draw()
        {
            float side = Math.Min(Size.X, Size.Y);
            var tileRect = new Rect2((Size - new Vector2(side, side)) / 2f, new Vector2(side, side));
            if (ShowTile)
            {
                var box = new StyleBoxFlat { BgColor = TileColor, BorderColor = BorderColor, AntiAliasing = true };
                box.SetBorderWidthAll(Border ? 3 : 0);
                box.SetCornerRadiusAll(Round ? (int)(side / 2f) : (int)(side * 0.27f));
                DrawStyleBox(box, tileRect);
            }

            float glyphSide = side * GlyphScale;
            _unit = glyphSide / 24f;
            _origin = tileRect.Position + (tileRect.Size - new Vector2(glyphSide, glyphSide)) / 2f;
            _stroke = Math.Max(2f, 2.4f * _unit);
            DrawGlyph();
        }

        private Vector2 P(float x, float y) => _origin + new Vector2(x, y) * _unit;

        private void Line(params float[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = P(xy[i * 2], xy[i * 2 + 1]);
            DrawPolyline(points, GlyphColor, _stroke, true);
            foreach (var point in points) DrawCircle(point, _stroke / 2f, GlyphColor);
        }

        private void Closed(params float[] xy)
        {
            var loop = new float[xy.Length + 2];
            Array.Copy(xy, loop, xy.Length);
            loop[xy.Length] = xy[0];
            loop[xy.Length + 1] = xy[1];
            Line(loop);
        }

        private void Ring(float cx, float cy, float r) =>
            DrawArc(P(cx, cy), r * _unit, 0f, Mathf.Tau, 40, GlyphColor, _stroke, true);

        private void Arc(float cx, float cy, float r, float fromDeg, float toDeg) =>
            DrawArc(P(cx, cy), r * _unit, Mathf.DegToRad(fromDeg), Mathf.DegToRad(toDeg), 24, GlyphColor, _stroke, true);

        private void Dot(float cx, float cy, float r) => DrawCircle(P(cx, cy), r * _unit, GlyphColor);

        private void Fill(params float[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = P(xy[i * 2], xy[i * 2 + 1]);
            DrawColoredPolygon(points, GlyphColor);
        }

        private void DrawGlyph()
        {
            switch (Glyph)
            {
                case "brain":
                    Ring(8.5f, 12f, 5.5f);
                    Ring(15.5f, 12f, 5.5f);
                    Line(12f, 8f, 12f, 16f);
                    break;
                case "bot":
                    Closed(5f, 9f, 19f, 9f, 19f, 19f, 5f, 19f);
                    Dot(9f, 14f, 1.6f);
                    Dot(15f, 14f, 1.6f);
                    Line(12f, 9f, 12f, 5f);
                    Dot(12f, 4f, 1.6f);
                    break;
                case "fast":
                    Line(5f, 6f, 11f, 12f, 5f, 18f);
                    Line(12f, 6f, 18f, 12f, 12f, 18f);
                    break;
                case "chart":
                    Line(3f, 17f, 9f, 11f, 13f, 15f, 21f, 7f);
                    Line(15f, 7f, 21f, 7f, 21f, 13f);
                    break;
                case "shield":
                    Closed(12f, 3f, 19f, 6f, 19f, 12f, 12f, 21f, 5f, 12f, 5f, 6f);
                    break;
                case "star":
                    Closed(12f, 3f, 14.6f, 9f, 21f, 9.3f, 16f, 13.5f, 17.6f, 20f, 12f, 16.4f, 6.4f, 20f, 8f, 13.5f, 3f, 9.3f, 9.4f, 9f);
                    break;
                case "sync":
                    Closed(3f, 6f, 9f, 6f, 9f, 18f, 3f, 18f);
                    Closed(15f, 6f, 21f, 6f, 21f, 18f, 15f, 18f);
                    Line(10.5f, 10f, 13.5f, 10f);
                    Line(10.5f, 14f, 13.5f, 14f);
                    break;
                case "flame":
                    Closed(12f, 3f, 15.5f, 7.5f, 17f, 12f, 16f, 16.5f, 12f, 20f, 8f, 16.5f, 7f, 12.5f, 9f, 9.5f, 10.5f, 12f, 11f, 8f);
                    break;
                case "soap":
                    Closed(4f, 10f, 20f, 10f, 20f, 19f, 4f, 19f);
                    Ring(8f, 6f, 1.8f);
                    Ring(13f, 4.5f, 1.2f);
                    Line(4f, 14.5f, 20f, 14.5f);
                    break;
                case "skull":
                    Arc(12f, 11f, 7f, 150f, 390f);
                    Closed(8f, 15f, 16f, 15f, 16f, 20f, 8f, 20f);
                    Dot(9.2f, 11f, 1.8f);
                    Dot(14.8f, 11f, 1.8f);
                    break;
                case "infinity":
                    Ring(7.5f, 12f, 4f);
                    Ring(16.5f, 12f, 4f);
                    break;
                case "phone":
                    Closed(7f, 3f, 17f, 3f, 17f, 21f, 7f, 21f);
                    Line(11f, 18f, 13f, 18f);
                    break;
                case "fan":
                    Ring(12f, 12f, 8.5f);
                    Line(12f, 12f, 12f, 5.5f);
                    Line(12f, 12f, 17.6f, 15.2f);
                    Line(12f, 12f, 6.4f, 15.2f);
                    Dot(12f, 12f, 1.8f);
                    break;
                case "battery":
                    Closed(3f, 8f, 18f, 8f, 18f, 16f, 3f, 16f);
                    Line(21f, 10.5f, 21f, 13.5f);
                    Fill(6f, 10.5f, 12f, 10.5f, 12f, 13.5f, 6f, 13.5f);
                    break;
                case "cup":
                    Closed(5f, 9f, 16f, 9f, 15f, 20f, 6f, 20f);
                    Line(16f, 11f, 19f, 11.5f, 19f, 14.5f, 15.6f, 15.5f);
                    Line(8.5f, 3f, 8.5f, 6f);
                    Line(12.5f, 3f, 12.5f, 6f);
                    break;
                case "bird":
                    Ring(15f, 6.5f, 3f);
                    Line(18f, 6.5f, 21.5f, 7.5f);
                    Line(13.5f, 9.2f, 8f, 17f);
                    Line(8f, 17f, 5.5f, 21f);
                    Line(8f, 17f, 11f, 21f);
                    break;
                case "plug":
                    Closed(6f, 9f, 18f, 9f, 18f, 15f, 6f, 15f);
                    Line(10f, 4f, 10f, 9f);
                    Line(14f, 4f, 14f, 9f);
                    Line(12f, 15f, 12f, 21f);
                    break;
                case "glasses":
                    Ring(6.5f, 13f, 4f);
                    Ring(17.5f, 13f, 4f);
                    Line(10.5f, 12f, 13.5f, 12f);
                    Line(2.5f, 12f, 1.5f, 8f);
                    Line(21.5f, 12f, 22.5f, 8f);
                    break;
                case "sprout":
                    Line(12f, 21f, 12f, 11f);
                    Closed(12f, 13.5f, 7.5f, 10f, 3.5f, 11f, 6.5f, 14.5f);
                    Closed(12f, 11f, 15.5f, 6.5f, 20.5f, 6f, 18f, 11f);
                    break;
                case "envelope":
                    Closed(3f, 6f, 21f, 6f, 21f, 18f, 3f, 18f);
                    Line(3f, 6f, 12f, 13f, 21f, 6f);
                    break;
                case "briefcase":
                    Closed(3f, 8f, 21f, 8f, 21f, 19f, 3f, 19f);
                    Line(9f, 8f, 9f, 5f, 15f, 5f, 15f, 8f);
                    Line(3f, 13f, 21f, 13f);
                    break;
                case "sun":
                    Ring(12f, 12f, 4f);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.Pi / 4f;
                        Line(12f + MathF.Cos(a) * 7f, 12f + MathF.Sin(a) * 7f, 12f + MathF.Cos(a) * 9.5f, 12f + MathF.Sin(a) * 9.5f);
                    }
                    break;
                case "link":
                    Ring(8.5f, 12f, 4.5f);
                    Ring(15.5f, 12f, 4.5f);
                    break;
                case "can":
                    Closed(7f, 4f, 14f, 4f, 17f, 7f, 17f, 20f, 7f, 20f);
                    Line(10f, 11f, 14f, 11f);
                    Line(10f, 15f, 14f, 15f);
                    break;
                case "moon":
                    Arc(12f, 12f, 8f, 60f, 300f);
                    Arc(16f, 9f, 6f, 120f, 245f);
                    break;
                case "camera":
                    Closed(3f, 8f, 8f, 8f, 10f, 5f, 14f, 5f, 16f, 8f, 21f, 8f, 21f, 19f, 3f, 19f);
                    Ring(12f, 13f, 3.5f);
                    break;
                case "lock":
                    Closed(5f, 11f, 19f, 11f, 19f, 21f, 5f, 21f);
                    Line(8f, 11f, 8f, 7.5f);
                    Arc(12f, 7.5f, 4f, 180f, 360f);
                    Line(16f, 7.5f, 16f, 11f);
                    break;
                case "play":
                    Fill(7f, 4f, 20f, 12f, 7f, 20f);
                    break;
                case "clock":
                    Ring(12f, 12f, 8f);
                    Line(12f, 7f, 12f, 12f, 15f, 14f);
                    break;
                case "gear":
                    Ring(12f, 12f, 3.5f);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.Pi / 4f;
                        Line(12f + MathF.Cos(a) * 6.5f, 12f + MathF.Sin(a) * 6.5f, 12f + MathF.Cos(a) * 9.5f, 12f + MathF.Sin(a) * 9.5f);
                    }
                    break;
                case "trophy":
                    Line(8f, 21f, 16f, 21f);
                    Line(12f, 17f, 12f, 21f);
                    Closed(7f, 4f, 17f, 4f, 17f, 9f, 15.5f, 13f, 12f, 14.5f, 8.5f, 13f, 7f, 9f);
                    Line(17f, 6f, 20f, 6f, 19f, 9.5f, 16.5f, 10.5f);
                    Line(7f, 6f, 4f, 6f, 5f, 9.5f, 7.5f, 10.5f);
                    break;
                case "power":
                    Line(12f, 3f, 12f, 12f);
                    Arc(12f, 13f, 8f, -50f, 230f);
                    break;
                case "bolt":
                    Fill(13.5f, 2f, 5f, 13.5f, 11f, 13.5f, 9.5f, 22f, 19f, 10f, 13f, 10f);
                    break;
                case "chevron_up":
                    Line(3f, 16f, 12f, 8f, 21f, 16f);
                    break;
                case "back":
                    Line(15f, 4f, 7f, 12f, 15f, 20f);
                    break;
                case "check":
                    Line(4f, 12.5f, 9.5f, 18f, 20f, 6f);
                    break;
                default:
                    Dot(12f, 12f, 3f);
                    break;
            }
        }
    }
}
