"""Generate the bakery's sound set.

Every clip that ships with Baka Bake Bakery is synthesised here rather than sourced, so the
project keeps its rule that only owner-supplied or clearly licensed audio ships. Run this and
the clips land in Assets/_BakaBakeBakery/Resources/Audio.

    python Tools/generate_audio.py

The palette is deliberately domestic: wooden taps, a small brass bell, a music box, and a room
that hums quietly under all of it. Nothing bright, nothing synthetic-sounding, nothing loud.
"""

from __future__ import annotations

import math
import pathlib
import wave

import numpy as np

RATE = 44100
OUT = pathlib.Path(__file__).resolve().parent.parent / "Assets/_BakaBakeBakery/Resources/Audio"

rng = np.random.default_rng(20260801)


def t(duration: float) -> np.ndarray:
    return np.linspace(0.0, duration, int(RATE * duration), endpoint=False)


def env(samples: np.ndarray, attack: float, decay: float, power: float = 2.0) -> np.ndarray:
    """Percussive envelope: a quick rise, then a curved fall."""
    n = len(samples)
    a = max(1, int(RATE * attack))
    out = np.ones(n)
    out[:a] = np.linspace(0.0, 1.0, a)
    fall = np.linspace(0.0, 1.0, max(1, n - a))
    out[a:] = (1.0 - fall) ** power
    d = max(1, int(RATE * decay))
    if d < n:
        out[-d:] *= np.linspace(1.0, 0.0, d)
    return out


def tone(freq: float, duration: float, harmonics=(1.0, 0.32, 0.11), detune: float = 0.0) -> np.ndarray:
    x = t(duration)
    wave_out = np.zeros_like(x)
    for index, amount in enumerate(harmonics, start=1):
        wave_out += amount * np.sin(2 * math.pi * freq * index * (1.0 + detune) * x)
    return wave_out / max(1e-6, sum(harmonics))


def noise(duration: float) -> np.ndarray:
    return rng.uniform(-1.0, 1.0, int(RATE * duration))


def lowpass(signal: np.ndarray, cutoff: float) -> np.ndarray:
    """One-pole filter. Rounds off every edge so nothing in the mix feels brittle."""
    alpha = math.exp(-2.0 * math.pi * cutoff / RATE)
    out = np.empty_like(signal)
    carry = 0.0
    for i, value in enumerate(signal):
        carry = (1.0 - alpha) * value + alpha * carry
        out[i] = carry
    return out


def normalise(signal: np.ndarray, peak: float = 0.72) -> np.ndarray:
    highest = float(np.max(np.abs(signal))) or 1.0
    return signal / highest * peak


def fade_edges(signal: np.ndarray, seconds: float = 0.006) -> np.ndarray:
    n = max(1, int(RATE * seconds))
    if len(signal) <= 2 * n:
        return signal
    signal = signal.copy()
    signal[:n] *= np.linspace(0.0, 1.0, n)
    signal[-n:] *= np.linspace(1.0, 0.0, n)
    return signal


def write(name: str, signal: np.ndarray, peak: float = 0.72) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    data = np.clip(fade_edges(normalise(signal, peak)), -1.0, 1.0)
    pcm = (data * 32767).astype("<i2")
    with wave.open(str(OUT / f"{name}.wav"), "wb") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(RATE)
        handle.writeframes(pcm.tobytes())
    print(f"  {name}.wav  {len(data) / RATE:.2f}s")


def loop_seam(signal: np.ndarray, seconds: float = 0.35) -> np.ndarray:
    """Cross-fade the tail over the head so a looping clip has no click at the seam."""
    n = int(RATE * seconds)
    if len(signal) <= 2 * n:
        return signal
    head, tail = signal[:n].copy(), signal[-n:].copy()
    ramp = np.linspace(0.0, 1.0, n)
    body = signal[:-n].copy()
    body[:n] = head * ramp + tail * (1.0 - ramp)
    return body


# --- one-shots -------------------------------------------------------------------------------

def ui_tap() -> np.ndarray:
    """A fingertip on a wooden counter."""
    body = lowpass(noise(0.05), 2100) * env(t(0.05), 0.001, 0.02, 3.0)
    thump = tone(196, 0.05, (1.0, 0.15)) * env(t(0.05), 0.001, 0.02, 3.0) * 0.5
    return body + thump


def knead() -> np.ndarray:
    """Dough meeting the board: soft, floury, low."""
    hit = lowpass(noise(0.22), 900) * env(t(0.22), 0.004, 0.09, 2.4)
    low = tone(110, 0.22, (1.0, 0.2)) * env(t(0.22), 0.002, 0.1, 2.6) * 0.7
    return hit * 0.8 + low


def oven_door() -> np.ndarray:
    """A heavy door and the breath of warm air that follows it."""
    x = t(0.65)
    creak = np.sin(2 * math.pi * np.cumsum(np.linspace(150, 96, len(x))) / RATE)
    creak *= env(x, 0.02, 0.25, 1.6) * 0.35
    breath = lowpass(noise(0.65), 620) * env(x, 0.09, 0.32, 1.3)
    return creak + breath * 0.9


def bake_ready() -> np.ndarray:
    """Small brass bell over the oven. Two struck partials, no shimmer."""
    x = t(1.5)
    out = np.zeros_like(x)
    for freq, amount in ((784.0, 1.0), (1174.7, 0.42), (1568.0, 0.16)):
        out += amount * np.sin(2 * math.pi * freq * x) * np.exp(-x * (2.2 + freq / 900))
    return lowpass(out, 5200)


def shop_bell() -> np.ndarray:
    """The little bell above the door when a neighbour walks in."""
    x = t(0.9)
    out = np.zeros_like(x)
    for freq, amount, decay in ((1046.5, 1.0, 5.0), (1318.5, 0.5, 6.0), (2093.0, 0.2, 8.0)):
        out += amount * np.sin(2 * math.pi * freq * x) * np.exp(-x * decay)
    ring = 1.0 + 0.06 * np.sin(2 * math.pi * 5.5 * x)
    return lowpass(out * ring, 6000)


def coin() -> np.ndarray:
    """Two coins into a tin. Warm, not metallic-bright."""
    out = np.zeros(int(RATE * 0.45))
    for offset, freq in ((0.0, 1244.5), (0.075, 1661.2)):
        start = int(RATE * offset)
        x = t(0.3)
        hit = np.sin(2 * math.pi * freq * x) * np.exp(-x * 16)
        hit += 0.4 * np.sin(2 * math.pi * freq * 1.5 * x) * np.exp(-x * 22)
        out[start:start + len(hit)] += hit
    return lowpass(out, 5200)


def discovery() -> np.ndarray:
    """Mila's little fanfare: a rising fifth on a music box."""
    out = np.zeros(int(RATE * 1.4))
    for offset, freq in ((0.0, 523.25), (0.11, 659.25), (0.22, 783.99), (0.33, 1046.5)):
        start = int(RATE * offset)
        x = t(1.0)
        note = np.sin(2 * math.pi * freq * x) * np.exp(-x * 4.6)
        note += 0.3 * np.sin(2 * math.pi * freq * 2 * x) * np.exp(-x * 7.5)
        out[start:start + len(note)] += note * 0.7
    return lowpass(out, 6500)


def day_bell() -> np.ndarray:
    """The shutter going up, or coming down. Lower and rounder than the counter bell."""
    x = t(2.0)
    out = np.zeros_like(x)
    for freq, amount in ((392.0, 1.0), (587.3, 0.36), (784.0, 0.14)):
        out += amount * np.sin(2 * math.pi * freq * x) * np.exp(-x * (1.5 + freq / 1200))
    return lowpass(out, 4200)


# --- beds ------------------------------------------------------------------------------------

def room_tone() -> np.ndarray:
    """Twelve seconds of a warm kitchen: oven hum, and the room around it."""
    duration = 12.0
    x = t(duration)
    hum = 0.0
    for freq, amount in ((52.0, 1.0), (104.0, 0.3), (156.0, 0.12)):
        hum += amount * np.sin(2 * math.pi * freq * x + rng.uniform(0, 6.28))
    hum *= 0.32 + 0.06 * np.sin(2 * math.pi * 0.09 * x)

    air = lowpass(noise(duration), 340) * 0.55
    crackle = np.zeros_like(x)
    for _ in range(26):
        start = rng.integers(0, len(x) - 3000)
        pop = lowpass(noise(0.05), 1500) * env(t(0.05), 0.001, 0.02, 3.0)
        crackle[start:start + len(pop)] += pop * rng.uniform(0.05, 0.16)

    return loop_seam(hum * 0.5 + air + crackle)


def music_home() -> np.ndarray:
    """A slow music-box loop. Four chords, one voice, nothing in a hurry."""
    beat = 1.05
    chords = [
        (261.63, 329.63, 392.00),   # C
        (220.00, 261.63, 329.63),   # Am
        (174.61, 220.00, 261.63),   # F
        (196.00, 246.94, 293.66),   # G
    ]
    melody = [523.25, 659.25, 587.33, 523.25, 440.00, 523.25, 587.33, 493.88]
    duration = beat * 2 * len(chords)
    out = np.zeros(int(RATE * duration))

    for index, chord in enumerate(chords):
        start = int(RATE * index * beat * 2)
        x = t(beat * 2)
        pad = np.zeros_like(x)
        for freq in chord:
            pad += np.sin(2 * math.pi * freq * x) * np.exp(-x * 0.85)
        out[start:start + len(pad)] += pad * 0.16

    for index, freq in enumerate(melody):
        start = int(RATE * index * beat)
        x = t(beat * 1.6)
        note = np.sin(2 * math.pi * freq * x) * np.exp(-x * 3.1)
        note += 0.22 * np.sin(2 * math.pi * freq * 2 * x) * np.exp(-x * 5.4)
        note += 0.08 * np.sin(2 * math.pi * freq * 3 * x) * np.exp(-x * 8.0)
        end = min(len(out), start + len(note))
        out[start:end] += note[:end - start] * 0.4

    return loop_seam(lowpass(out, 5200), 0.5)


def main() -> None:
    print(f"Writing bakery audio to {OUT}")
    write("ui_tap", ui_tap(), 0.42)
    write("knead", knead(), 0.55)
    write("oven_door", oven_door(), 0.5)
    write("bake_ready", bake_ready(), 0.58)
    write("shop_bell", shop_bell(), 0.5)
    write("coin", coin(), 0.55)
    write("discovery", discovery(), 0.6)
    write("day_bell", day_bell(), 0.58)
    write("room_tone", room_tone(), 0.3)
    write("music_home", music_home(), 0.38)
    print("Done.")


if __name__ == "__main__":
    main()
