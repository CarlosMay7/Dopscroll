using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace Doomscroll.Presentation.DeskScene
{
    /// <summary>
    /// Manages the 3:00 AM Lo-Fi Synthwave background music loop and all arcade SFX.
    /// Reads WAV files directly from Assets/Audio/ into AudioStreamWav so audio works
    /// immediately even before Godot Editor generates .import metadata.
    /// </summary>
    public sealed partial class DeskAudioManager : Node
    {
        private const float MusicBaseDb = -6.0f;
        private const float SfxBaseDb = -4.0f;

        /// <summary>0..1 slider value from the settings; 0 stops the music.</summary>
        public float MusicVolume { get; private set; } = 0.8f;
        /// <summary>0..1 slider value from the settings; 0 mutes every sound effect.</summary>
        public float SfxVolume { get; private set; } = 0.8f;

        private AudioStreamPlayer _bgmPlayer = null!;
        private readonly List<AudioStreamPlayer> _sfxPool = new();
        private readonly Dictionary<string, AudioStreamWav> _streams = new();
        private int _sfxPoolIndex;

        public override void _Ready()
        {
            _bgmPlayer = new AudioStreamPlayer
            {
                VolumeDb = MusicBaseDb + Mathf.LinearToDb(MusicVolume)
            };
            AddChild(_bgmPlayer);

            for (int i = 0; i < 6; i++)
            {
                var p = new AudioStreamPlayer { VolumeDb = SfxBaseDb + Mathf.LinearToDb(SfxVolume) };
                AddChild(p);
                _sfxPool.Add(p);
            }

            LoadAudioAssets();
            StartMusic();
        }

        private void LoadAudioAssets()
        {
            LoadWavFile("bgm_lofi_night", loop: true);
            LoadWavFile("sfx_swipe", loop: false);
            LoadWavFile("sfx_like", loop: false);
            LoadWavFile("sfx_crit_like", loop: false);
            LoadWavFile("sfx_complete", loop: false);
            LoadWavFile("sfx_mythic", loop: false);
            LoadWavFile("sfx_boss_ping", loop: false);
            LoadWavFile("sfx_screamer", loop: false);
            LoadWavFile("sfx_blackout", loop: false);
        }

        private void LoadWavFile(string name, bool loop)
        {
            string resPath = $"res://Assets/Audio/{name}.wav";
            string globalPath = ProjectSettings.GlobalizePath(resPath);
            if (!File.Exists(globalPath)) return;

            byte[] rawBytes = File.ReadAllBytes(globalPath);
            if (rawBytes.Length <= 44) return;

            int pcmLength = rawBytes.Length - 44;
            byte[] pcmData = new byte[pcmLength];
            Buffer.BlockCopy(rawBytes, 44, pcmData, 0, pcmLength);

            var stream = new AudioStreamWav
            {
                Format = AudioStreamWav.FormatEnum.Format16Bits,
                MixRate = 44100,
                Stereo = false,
                Data = pcmData,
                LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled,
                LoopBegin = 0,
                LoopEnd = pcmLength / 2
            };

            _streams[name] = stream;
        }

        public void StartMusic()
        {
            if (_streams.TryGetValue("bgm_lofi_night", out var bgm))
            {
                _bgmPlayer.Stream = bgm;
                if (MusicVolume > 0f && !_bgmPlayer.Playing)
                {
                    _bgmPlayer.Play();
                }
            }
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp(volume, 0f, 1f);
            if (_bgmPlayer == null) return;
            if (MusicVolume <= 0f)
            {
                _bgmPlayer.Stop();
                return;
            }
            _bgmPlayer.VolumeDb = MusicBaseDb + Mathf.LinearToDb(MusicVolume);
            if (_bgmPlayer.Stream != null && !_bgmPlayer.Playing) _bgmPlayer.Play();
        }

        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp(volume, 0f, 1f);
            float db = SfxBaseDb + Mathf.LinearToDb(Math.Max(SfxVolume, 0.0001f));
            foreach (var player in _sfxPool) player.VolumeDb = db;
        }

        public void UpdateDynamicMusicPitch(float targetPitch)
        {
            if (_bgmPlayer != null)
            {
                _bgmPlayer.PitchScale = Mathf.Lerp(_bgmPlayer.PitchScale, targetPitch, 0.1f);
            }
        }

        public void PlaySfx(string sfxName, float pitchScale = 1.0f)
        {
            if (SfxVolume <= 0f) return;
            if (!_streams.TryGetValue(sfxName, out var stream)) return;

            var player = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Count;
            player.Stream = stream;
            player.PitchScale = pitchScale;
            player.Play();
        }
    }
}
