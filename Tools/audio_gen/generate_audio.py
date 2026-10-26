"""
Procedural Audio Generator for Doomscroll Incremental.
Generates a looping 3:00 AM Lo-Fi Synthwave background track and 8 crisp arcade SFX
as standard 16-bit PCM WAV files in Assets/Audio/.
"""
import math
import os
import random
import struct
import wave

SAMPLE_RATE = 44100
OUT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Audio"))


def write_wav(filename: str, samples: list[float]) -> None:
    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, filename)
    with wave.open(path, "w") as wf:
        wf.setnchannels(1)
        wf.setsampwidth(2)
        wf.setframerate(SAMPLE_RATE)
        buf = bytearray()
        for s in samples:
            clamped = max(-0.98, min(0.98, s))
            buf.extend(struct.pack("<h", int(clamped * 32767)))
        wf.writeframes(bytes(buf))
    print(f"Generated {path} ({len(samples) / SAMPLE_RATE:.2f}s)")


def generate_bgm_lofi_night() -> None:
    """
    12.8-second seamless loop at 75 BPM (16 beats = 4 bars of 4/4).
    Warm Rhodes-style chords (Fmaj7 -> Em7 -> Dm7 -> Cmaj7) + sub-bass + soft lo-fi groove + vinyl rain texture.
    """
    bpm = 75.0
    beat_sec = 60.0 / bpm  # 0.8s per beat
    total_beats = 16
    duration = total_beats * beat_sec  # 12.8s
    total_samples = int(duration * SAMPLE_RATE)

    chords = [
        # Bar 1: Fmaj7 (F3, A3, C4, E4)
        (174.61, [174.61, 220.00, 261.63, 329.63]),
        # Bar 2: Em7 (E3, G3, B3, D4)
        (164.81, [164.81, 196.00, 246.94, 293.66]),
        # Bar 3: Dm7 (D3, F3, A3, C4)
        (146.83, [146.83, 174.61, 220.00, 261.63]),
        # Bar 4: Cmaj7 (C3, E3, G3, B3)
        (130.81, [130.81, 164.81, 196.00, 246.94]),
    ]

    melody_notes = [
        523.25, 659.25, 587.33, 523.25,
        493.88, 587.33, 523.25, 440.00,
        440.00, 523.25, 493.88, 392.00,
        392.00, 440.00, 392.00, 329.63,
    ]

    rng = random.Random(333)
    samples = [0.0] * total_samples

    for i in range(total_samples):
        t = i / SAMPLE_RATE
        bar_idx = min(3, int(t / (beat_sec * 4)))
        bar_t = t - (bar_idx * beat_sec * 4)
        beat_idx = int(t / beat_sec) % total_beats
        beat_t = (t % beat_sec)

        bass_freq, chord_freqs = chords[bar_idx]

        # 1. Warm electric piano chord pad with gentle tremolo
        tremolo = 0.88 + 0.12 * math.sin(2.0 * math.pi * 3.75 * t)
        chord_env = math.exp(-bar_t * 0.55) * (1.0 - math.exp(-bar_t * 18.0))
        chord_val = 0.0
        for f in chord_freqs:
            # Slight detune for warm lo-fi chorus feel
            chord_val += math.sin(2.0 * math.pi * (f * 0.999) * t)
            chord_val += 0.35 * math.sin(2.0 * math.pi * (f * 2.0) * t) * math.exp(-bar_t * 1.4)
        chord_val = (chord_val / len(chord_freqs)) * 0.24 * chord_env * tremolo

        # 2. Deep warm sub-bass
        bass_env = math.exp(-beat_t * 2.2) * (1.0 - math.exp(-beat_t * 45.0))
        bass_val = math.sin(2.0 * math.pi * (bass_freq * 0.5) * t) * 0.26 * bass_env

        # 3. Soft bell-like late-night melody plucks on each beat
        mel_freq = melody_notes[beat_idx]
        mel_env = math.exp(-beat_t * 4.5) * (1.0 - math.exp(-beat_t * 60.0))
        mel_val = (
            math.sin(2.0 * math.pi * mel_freq * t)
            + 0.25 * math.sin(2.0 * math.pi * (mel_freq * 2.0) * t)
        ) * 0.12 * mel_env

        # 4. Lo-fi soft kick on beats 0, 2 and gentle snare/hat on 1, 3
        beat_in_bar = beat_idx % 4
        perc_val = 0.0
        if beat_in_bar in (0, 2) and beat_t < 0.18:
            kick_freq = 52.0 + 95.0 * math.exp(-beat_t * 32.0)
            perc_val += math.sin(2.0 * math.pi * kick_freq * beat_t) * math.exp(-beat_t * 16.0) * 0.28
        elif beat_in_bar in (1, 3) and beat_t < 0.12:
            perc_val += (rng.uniform(-1.0, 1.0) * 0.10) * math.exp(-beat_t * 28.0)

        # Eighth-note soft brushed hi-hat
        half_beat_t = t % (beat_sec * 0.5)
        if half_beat_t < 0.045:
            perc_val += rng.uniform(-1.0, 1.0) * 0.035 * math.exp(-half_beat_t * 65.0)

        # 5. Subtle 3 AM vinyl warmth
        vinyl = rng.uniform(-1.0, 1.0) * 0.006

        samples[i] = chord_val + bass_val + mel_val + perc_val + vinyl

    write_wav("bgm_lofi_night.wav", samples)


def generate_sfx_swipe() -> None:
    dur = 0.12
    n = int(dur * SAMPLE_RATE)
    rng = random.Random(10)
    samples = []
    for i in range(n):
        t = i / SAMPLE_RATE
        env = math.sin(math.pi * (t / dur)) ** 1.5
        freq = 240.0 + 480.0 * (t / dur)
        tone = math.sin(2.0 * math.pi * freq * t) * 0.25
        air = rng.uniform(-1.0, 1.0) * 0.18
        samples.append((tone + air) * env)
    write_wav("sfx_swipe.wav", samples)


def generate_sfx_like() -> None:
    dur = 0.18
    n = int(dur * SAMPLE_RATE)
    samples = []
    for i in range(n):
        t = i / SAMPLE_RATE
        freq = 523.25 if t < 0.07 else 783.99  # C5 -> G5
        local_t = t if t < 0.07 else (t - 0.07)
        env = math.exp(-local_t * 18.0)
        val = (math.sin(2.0 * math.pi * freq * t) + 0.3 * math.sin(2.0 * math.pi * freq * 2.0 * t)) * 0.35 * env
        samples.append(val)
    write_wav("sfx_like.wav", samples)


def generate_sfx_crit_like() -> None:
    dur = 0.32
    n = int(dur * SAMPLE_RATE)
    notes = [523.25, 659.25, 783.99, 1046.50]  # C5, E5, G5, C6
    samples = []
    for i in range(n):
        t = i / SAMPLE_RATE
        idx = min(3, int(t / 0.065))
        local_t = t - idx * 0.065
        freq = notes[idx]
        env = math.exp(-local_t * 14.0)
        val = (math.sin(2.0 * math.pi * freq * t) + 0.4 * math.sin(2.0 * math.pi * freq * 2.0 * t)) * 0.38 * env
        samples.append(val)
    write_wav("sfx_crit_like.wav", samples)


def generate_sfx_complete() -> None:
    dur = 0.20
    n = int(dur * SAMPLE_RATE)
    samples = []
    for i in range(n):
        t = i / SAMPLE_RATE
        freq = 440.0 if t < 0.08 else 659.25  # A4 -> E5
        env = math.exp(-t * 12.0)
        val = math.sin(2.0 * math.pi * freq * t) * 0.28 * env
        samples.append(val)
    write_wav("sfx_complete.wav", samples)


def generate_sfx_mythic() -> None:
    dur = 0.65
    n = int(dur * SAMPLE_RATE)
    freqs = [523.25, 659.25, 783.99, 987.77, 1174.66]  # Cmaj9 shimmer
    samples = []
    for i in range(n):
        t = i / SAMPLE_RATE
        env = math.exp(-t * 3.5) * (1.0 - math.exp(-t * 35.0))
        val = sum(math.sin(2.0 * math.pi * f * t) for f in freqs) / len(freqs)
        samples.append(val * 0.45 * env)
    write_wav("sfx_mythic.wav", samples)


def generate_sfx_boss_ping() -> None:
    dur = 0.28
    n = int(dur * SAMPLE_RATE)
    samples = []
    for i in range(n):
        t = i / SAMPLE_RATE
        freq = 880.0 if t < 0.12 else 698.46  # A5 -> F5 urgent knock
        local_t = t if t < 0.12 else (t - 0.12)
        env = math.exp(-local_t * 16.0)
        val = (math.sin(2.0 * math.pi * freq * t) + 0.5 * math.sin(2.0 * math.pi * freq * 1.5 * t)) * 0.36 * env
        samples.append(val)
    write_wav("sfx_boss_ping.wav", samples)


def generate_sfx_screamer() -> None:
    dur = 0.45
    n = int(dur * SAMPLE_RATE)
    rng = random.Random(99)
    samples = []
    for i in range(n):
        t = i / SAMPLE_RATE
        env = math.exp(-t * 5.0)
        # Dramatic dissonant synth stinger + sub impact (non-ear-piercing, streamer-friendly)
        val = (
            math.sin(2.0 * math.pi * 110.0 * t)
            + math.sin(2.0 * math.pi * 155.56 * t)
            + math.sin(2.0 * math.pi * 233.08 * t)
            + rng.uniform(-0.4, 0.4)
        ) * 0.24 * env
        samples.append(val)
    write_wav("sfx_screamer.wav", samples)


def generate_sfx_blackout() -> None:
    dur = 0.50
    n = int(dur * SAMPLE_RATE)
    samples = []
    for i in range(n):
        t = i / SAMPLE_RATE
        freq = max(45.0, 260.0 * (1.0 - (t / dur) * 0.82))
        env = math.exp(-t * 3.8)
        val = (math.sin(2.0 * math.pi * freq * t) + 0.4 * math.sin(2.0 * math.pi * (freq * 0.5) * t)) * 0.38 * env
        samples.append(val)
    write_wav("sfx_blackout.wav", samples)


if __name__ == "__main__":
    generate_bgm_lofi_night()
    generate_sfx_swipe()
    generate_sfx_like()
    generate_sfx_crit_like()
    generate_sfx_complete()
    generate_sfx_mythic()
    generate_sfx_boss_ping()
    generate_sfx_screamer()
    generate_sfx_blackout()
