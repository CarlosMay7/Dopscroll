using System;
using Godot;

namespace Doomscroll.Presentation.DeskScene
{
    /// <summary>
    /// Player preferences, stored apart from the save in user://settings.cfg so starting a new game keeps them.
    /// </summary>
    public sealed class GameSettings
    {
        public const string Path = "user://settings.cfg";
        public static readonly float[] TextScales = { 1.0f, 1.25f, 1.5f };

        private const string Section = "settings";

        public float MusicVolume { get; set; } = 0.8f;
        public float SfxVolume { get; set; } = 0.8f;
        public bool Fullscreen { get; set; }
        public bool Vsync { get; set; } = true;
        public float TextScale { get; set; } = 1.0f;
        /// <summary>No screen shake, flying numbers, pulses or slide-ins.</summary>
        public bool ReducedMotion { get; set; }
        /// <summary>Screamer videos become a calm "adrenaline" card (same rewards, no jumpscare).</summary>
        public bool SafeScreamer { get; set; }
        /// <summary>First-time tips on the desk (battery, outlet, blur, boredom, boss).</summary>
        public bool Tips { get; set; } = true;

        /// <summary>Loads the file; when there is none yet, Safe Screamer is seeded from the old save flag.</summary>
        public static GameSettings Load(bool legacySafeScreamer)
        {
            var settings = new GameSettings();
            var file = new ConfigFile();
            if (file.Load(Path) != Error.Ok)
            {
                settings.SafeScreamer = legacySafeScreamer;
                return settings;
            }

            settings.MusicVolume = Math.Clamp((float)file.GetValue(Section, "music_volume", settings.MusicVolume), 0f, 1f);
            settings.SfxVolume = Math.Clamp((float)file.GetValue(Section, "sfx_volume", settings.SfxVolume), 0f, 1f);
            settings.Fullscreen = (bool)file.GetValue(Section, "fullscreen", settings.Fullscreen);
            settings.Vsync = (bool)file.GetValue(Section, "vsync", settings.Vsync);
            settings.TextScale = NearestTextScale((float)file.GetValue(Section, "text_scale", settings.TextScale));
            settings.ReducedMotion = (bool)file.GetValue(Section, "reduced_motion", settings.ReducedMotion);
            settings.SafeScreamer = (bool)file.GetValue(Section, "safe_screamer", legacySafeScreamer);
            settings.Tips = (bool)file.GetValue(Section, "tips", settings.Tips);
            return settings;
        }

        public void Save()
        {
            var file = new ConfigFile();
            file.SetValue(Section, "music_volume", MusicVolume);
            file.SetValue(Section, "sfx_volume", SfxVolume);
            file.SetValue(Section, "fullscreen", Fullscreen);
            file.SetValue(Section, "vsync", Vsync);
            file.SetValue(Section, "text_scale", TextScale);
            file.SetValue(Section, "reduced_motion", ReducedMotion);
            file.SetValue(Section, "safe_screamer", SafeScreamer);
            file.SetValue(Section, "tips", Tips);
            Error err = file.Save(Path);
            if (err != Error.Ok) GD.PushWarning($"[settings] Could not write {Path}: {err}");
        }

        /// <summary>Window mode and vsync. Skipped headless, where there is no window to change.</summary>
        public void ApplyDisplay()
        {
            if (DisplayServer.GetName() == "headless") return;
            DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
            DisplayServer.WindowSetVsyncMode(Vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        }

        private static float NearestTextScale(float value)
        {
            float best = TextScales[0];
            foreach (float scale in TextScales)
            {
                if (Math.Abs(scale - value) < Math.Abs(best - value)) best = scale;
            }
            return best;
        }
    }
}
