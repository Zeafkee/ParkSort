import wave
from pathlib import Path

import numpy as np

REPO = Path(__file__).resolve().parents[1]
OUT = REPO / "Assets" / "_Project" / "Audio"
SR = 44100
rng = np.random.default_rng(7)


def t(seconds):
    return np.arange(int(SR * seconds)) / SR


def env(n, attack=0.005, decay=0.2, curve=4.0):
    x = np.arange(n) / SR
    a = np.clip(x / max(attack, 1e-5), 0, 1)
    d = np.exp(-curve * np.clip(x - attack, 0, None) / max(decay, 1e-5))
    return a * d


def sine(freq, seconds, phase=0.0):
    return np.sin(2 * np.pi * freq * t(seconds) + phase)


def sweep(f0, f1, seconds, shape=1.0):
    x = t(seconds)
    k = (x / seconds) ** shape
    freq = f0 + (f1 - f0) * k
    return np.sin(2 * np.pi * np.cumsum(freq) / SR)


def noise(seconds):
    return rng.uniform(-1, 1, int(SR * seconds))


def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc = (1 - a) * x[i] + a * acc
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def mix_at(dst, src, at_seconds, gain=1.0):
    i = int(at_seconds * SR)
    n = min(len(src), len(dst) - i)
    if n > 0:
        dst[i:i + n] += src[:n] * gain
    return dst


def fade_out(x, seconds=0.01):
    n = min(len(x), int(SR * seconds))
    x[-n:] *= np.linspace(1, 0, n)
    return x


def save(path, x, peak=0.85, fade=True):
    x = np.asarray(x, dtype=np.float64)
    m = np.max(np.abs(x))
    if m > 0:
        x = x / m * peak
    if fade:
        x = fade_out(x)
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())


def tone(freq, seconds, harmonics=(1.0,), decay=0.3, attack=0.003, curve=4.0):
    n = int(SR * seconds)
    x = sum(a * sine(freq * (i + 1), seconds) for i, a in enumerate(harmonics))
    return x * env(n, attack, decay, curve)


def note(semitones_from_a4):
    return 440.0 * 2 ** (semitones_from_a4 / 12)


def sfx_tap():
    n = int(SR * 0.07)
    return sweep(700, 1100, 0.07, 0.6) * env(n, 0.001, 0.03, 5)


def sfx_drive():
    seconds = 0.32
    n = int(SR * seconds)
    engine = sweep(90, 210, seconds, 0.7)
    buzz = np.sign(sweep(45, 105, seconds, 0.7)) * 0.25
    whoosh = highpass(lowpass(noise(seconds), 2600), 500) * 0.5
    shape = np.minimum(1.0, t(seconds) / 0.04) * np.exp(-5.5 * t(seconds))
    return (engine + buzz + whoosh) * shape[:n]


def sfx_bump():
    n = int(SR * 0.16)
    thud = sweep(170, 70, 0.16) * env(n, 0.001, 0.06, 5)
    knock = lowpass(noise(0.16), 1400) * env(n, 0.001, 0.025, 5) * 0.8
    return thud + knock


def sfx_park():
    x = np.zeros(int(SR * 0.3))
    n = int(SR * 0.12)
    mix_at(x, sweep(240, 110, 0.12) * env(n, 0.001, 0.05, 5), 0.0, 0.7)
    mix_at(x, tone(note(10), 0.25, (1.0, 0.0, 0.25), decay=0.09), 0.02, 0.6)
    return x


def sfx_wait():
    x = np.zeros(int(SR * 0.3))
    mix_at(x, tone(note(3), 0.2, (1.0, 0.3), decay=0.07), 0.0, 0.6)
    mix_at(x, tone(note(-2), 0.22, (1.0, 0.3), decay=0.08), 0.07, 0.6)
    return x


def sfx_garage_done():
    x = np.zeros(int(SR * 0.9))
    for i, semis in enumerate((3, 7, 10, 15)):
        mix_at(x, tone(note(semis), 0.55, (1.0, 0.3, 0.15), decay=0.2), i * 0.06, 0.45)
    sparkle = highpass(noise(0.3), 5000) * env(int(SR * 0.3), 0.01, 0.12, 4) * 0.12
    mix_at(x, sparkle, 0.2, 1.0)
    return x


def sfx_garage_open():
    n = int(SR * 0.22)
    return sweep(300, 760, 0.22, 0.6) * env(n, 0.004, 0.1, 4) + tone(note(15), 0.22, (1.0, 0.0, 0.2), decay=0.08) * 0.35


def sfx_reveal():
    x = np.zeros(int(SR * 0.3))
    n = int(SR * 0.1)
    mix_at(x, sweep(500, 1400, 0.1, 0.5) * env(n, 0.002, 0.05, 5), 0.0, 0.7)
    mix_at(x, tone(note(19), 0.2, (1.0, 0.0, 0.3), decay=0.07), 0.06, 0.5)
    return x


def sfx_click():
    n = int(SR * 0.06)
    return sweep(900, 1300, 0.06) * env(n, 0.001, 0.02, 5)


def sfx_coin():
    x = np.zeros(int(SR * 0.35))
    mix_at(x, tone(note(22), 0.12, (1.0, 0.0, 0.3), decay=0.05), 0.0, 0.6)
    mix_at(x, tone(note(27), 0.28, (1.0, 0.0, 0.3), decay=0.12), 0.06, 0.6)
    return x


def sfx_hint():
    x = np.zeros(int(SR * 0.5))
    for i, semis in enumerate((10, 15, 22)):
        mix_at(x, tone(note(semis), 0.3, (1.0, 0.0, 0.25), decay=0.1), i * 0.07, 0.5)
    return x


def sfx_win():
    x = np.zeros(int(SR * 1.9))
    for i, semis in enumerate((3, 7, 10, 15, 19)):
        mix_at(x, tone(note(semis), 0.5, (1.0, 0.3, 0.1), decay=0.2), i * 0.1, 0.5)
    for semis in (3, 10, 15, 19):
        mix_at(x, tone(note(semis), 1.3, (1.0, 0.25, 0.1), decay=0.55, curve=3), 0.55, 0.3)
    return x


def sfx_lose():
    x = np.zeros(int(SR * 1.3))
    for i, semis in enumerate((7, 3, -2)):
        mix_at(x, tone(note(semis), 0.6, (1.0, 0.4, 0.2), decay=0.25), i * 0.2, 0.5)
    n = int(SR * 0.6)
    mix_at(x, sweep(120, 55, 0.6) * env(n, 0.01, 0.3, 3), 0.55, 0.6)
    return x


def music_loop():
    bpm = 104.0
    beat = 60.0 / bpm
    bars = 8
    seconds = bars * 4 * beat
    x = np.zeros(int(SR * seconds))

    chords = [(-9, -5, -2), (-14, -10, -7), (-4, 0, 3), (-7, -3, 0)]
    for bar in range(bars):
        chord = chords[bar % len(chords)]
        root = note(chord[0] - 12)
        for b in range(4):
            start = (bar * 4 + b) * beat
            n = int(SR * beat)
            bass = (sine(root, beat) + 0.3 * sine(root * 2, beat)) * env(n, 0.008, beat * 0.4, 4)
            mix_at(x, bass, start, 0.24 if b % 2 == 0 else 0.14)
            if b % 2 == 1:
                for semis in chord:
                    mix_at(x, tone(note(semis), beat * 0.9, (1.0, 0.25, 0.1), decay=beat * 0.3), start, 0.07)

    melody = [12, None, 15, 12, 10, None, 7, None, 8, None, 12, 8, 7, None, 3, None,
              12, None, 15, 17, 19, None, 15, None, 14, 12, 10, None, 12, None, None, None]
    for i in range(bars * 8):
        semis = melody[i % len(melody)]
        if semis is None:
            continue
        mix_at(x, tone(note(semis), 0.5, (1.0, 0.0, 0.2), decay=0.16), i * beat / 2, 0.11)

    for i in range(bars * 8):
        n = int(SR * 0.05)
        hat = highpass(noise(0.05), 6000) * env(n, 0.001, 0.015, 5)
        mix_at(x, hat, i * beat / 2, 0.05 if i % 2 == 0 else 0.025)

    return x


def main():
    sfx = {
        "Tap": sfx_tap, "Drive": sfx_drive, "Bump": sfx_bump, "Park": sfx_park, "Wait": sfx_wait,
        "GarageDone": sfx_garage_done, "GarageOpen": sfx_garage_open, "Reveal": sfx_reveal, "Click": sfx_click,
        "Coin": sfx_coin, "Hint": sfx_hint, "Win": sfx_win, "Lose": sfx_lose,
    }
    for name, make in sfx.items():
        save(OUT / "SFX" / f"{name}.wav", make())
        print("sfx", name)

    save(OUT / "Music" / "Loop.wav", music_loop(), peak=0.7, fade=False)
    print("music Loop")


if __name__ == "__main__":
    main()
