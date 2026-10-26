using System;
using System.Collections.Generic;
using Godot;

namespace Doomscroll.Presentation.UI
{
    /// <summary>
    /// Lightweight 60 FPS particle emitter for Dopamine sparks, Like hearts, and Mythic stars.
    /// </summary>
    public partial class DopamineParticleSystem : Control
    {
        private struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public Color Color;
            public float Size;
            public float Life;
            public float MaxLife;
        }

        private readonly List<Particle> _particles = new();
        private readonly Random _rng = new();

        public void EmitBurst(Vector2 origin, Color baseColor, int count = 18, float speedScale = 1.0f)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(_rng.NextDouble() * Math.PI * 2.0);
                float speed = (45f + (float)_rng.NextDouble() * 115f) * speedScale;
                float life = 0.45f + (float)_rng.NextDouble() * 0.45f;

                _particles.Add(new Particle
                {
                    Position = origin,
                    Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed - 35f),
                    Color = baseColor.Lightened((float)_rng.NextDouble() * 0.25f),
                    Size = 3.0f + (float)_rng.NextDouble() * 3.5f,
                    Life = life,
                    MaxLife = life
                });
            }
        }

        public override void _Process(double delta)
        {
            if (_particles.Count == 0) return;

            float dt = (float)delta;
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Life -= dt;
                if (p.Life <= 0f)
                {
                    _particles.RemoveAt(i);
                    continue;
                }

                p.Velocity.Y += 160f * dt; // gentle gravity
                p.Position += p.Velocity * dt;
                _particles[i] = p;
            }

            QueueRedraw();
        }

        public override void _Draw()
        {
            foreach (var p in _particles)
            {
                float alpha = Mathf.Clamp(p.Life / p.MaxLife, 0f, 1f);
                Color c = new Color(p.Color.R, p.Color.G, p.Color.B, alpha);
                DrawCircle(p.Position, p.Size * alpha, c);
            }
        }
    }
}
