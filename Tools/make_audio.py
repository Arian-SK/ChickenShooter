"""Synthesize the arcade music loops and short effects used by Chicken Shooter."""
from pathlib import Path
import wave
import numpy as np

RATE = 24000
OUT = Path(__file__).resolve().parents[1] / "Assets/Cluck/Audio"
OUT.mkdir(parents=True, exist_ok=True)
random = np.random.default_rng(273)


def save(name, samples):
    samples = np.asarray(samples)
    pcm = np.int16(np.clip(samples, -.98, .98) * 32767)
    with wave.open(str(OUT / (name + ".wav")), "wb") as file:
        file.setnchannels(1 if samples.ndim == 1 else samples.shape[1])
        file.setsampwidth(2)
        file.setframerate(RATE)
        file.writeframes(pcm.astype("<i2").tobytes())


def time(seconds):
    return np.arange(round(RATE * seconds)) / RATE


def tone(note, seconds, fall=5, bright=.22):
    t = time(seconds)
    freq = 440 * 2 ** ((note - 69) / 12)
    phase = 2 * np.pi * freq * t
    env = np.minimum(t / .013, 1) * np.exp(-fall * t) * np.clip((seconds - t) / .045, 0, 1)
    return (np.sin(phase) + bright * np.sin(2 * phase) + .11 * np.sin(3 * phase)) * env


def beat_loop(name, bpm, progression, motif, swing, accent):
    beat = 60 / bpm
    samples = np.zeros((round(16 * beat * RATE), 2))

    def add(at, sound, gain, pan=0):
        start = round(at * beat * RATE)
        stop = min(start + len(sound), len(samples))
        samples[start:stop, 0] += sound[:stop-start] * gain * (1 - pan)
        samples[start:stop, 1] += sound[:stop-start] * gain * (1 + pan)

    for bar, (root, third) in enumerate(progression):
        for i, offset in enumerate(motif):
            when = bar * 4 + i / 2 + (swing if i % 2 else 0)
            if offset is not None:
                chord = (root + 24, root + 24 + third, root + 31, root + 36, root + 38)
                add(when, tone(chord[offset], beat * .52, 7, accent), .13, -.24 if i % 2 else .24)
        for note in (root + 12, root + 12 + third, root + 19):
            add(bar * 4, tone(note, 3.95 * beat, .95, .09), .064)
        for i in range(4):
            add(bar * 4 + i, tone(root + (7 if i == 3 else 0), beat * .72, 8), .21)
            if i % 2 == 0:
                t = time(.17)
                kick = np.sin(2 * np.pi * (43*t + 8*(1-np.exp(-23*t)))) * np.exp(-29*t)
                add(bar * 4 + i, kick, .21)
            t = time(.058)
            hat = random.normal(0, .6, len(t)) * np.exp(-75*t)
            add(bar * 4 + i + .5, hat, .065, .35)
    samples[:64] *= np.linspace(0, 1, 64)[:, None]
    samples[-128:] *= np.linspace(1, 0, 128)[:, None]
    save(name, samples)


beat_loop("Menu", 98, [(45,3),(41,4),(48,4),(43,4)], [0,2,1,None,3,2,1,None], .04, .12)
beat_loop("Earth Orbit", 112, [(48,4),(43,4),(45,3),(41,4)], [0,1,2,3,None,2,4,2], .03, .22)
beat_loop("Jovian Belt", 119, [(43,3),(46,4),(41,3),(45,3)], [2,None,0,3,2,1,None,4], .06, .30)
beat_loop("The Deep", 126, [(40,3),(36,4),(43,4),(38,3)], [0,3,2,4,1,3,2,None], .01, .37)


def sweep(name, duration, start, finish, gain, noise=0, fall=14):
    t = time(duration)
    phase = 2 * np.pi * (start*t + .5 * (finish-start) / duration * t*t)
    env = np.minimum(t / .003, 1) * np.exp(-fall*t) * np.clip((duration-t)/.024, 0, 1)
    signal = np.sin(phase) + .18 * np.sin(2.03 * phase) + noise * random.normal(0, .58, len(t))
    save(name, signal * gain * env)


sweep("Laser", .093, 940, 410, .38, .10, 34)
sweep("Egg", .14, 430, 180, .36, .10, 16)
sweep("Impact", .095, 220, 80, .43, .6, 27)
sweep("Asteroid Break", .27, 137, 44, .59, 1.25, 15)
sweep("Chicken Down", .22, 510, 140, .43, .45, 14)
sweep("Player Hit", .32, 290, 65, .53, .60, 13)
sweep("Bomb", .71, 159, 35, .62, 1.2, 6)


def chime(name, notes):
    samples = np.zeros(round((.34 + .075 * len(notes)) * RATE))
    for i, note in enumerate(notes):
        sound = tone(note, .26, 9)
        at = round(i * .075 * RATE)
        samples[at:at+len(sound)] += sound * .31
    save(name, samples)


chime("Gift", [72, 79, 84])
chime("Round Clear", [67, 72, 76, 79])
