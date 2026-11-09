"""Original 'lucky jackpot' cue for exact matches: rising major arpeggio of bell tones
plus a coin shimmer and a low thump. Pure synthesis, no external samples."""
import math, wave, array, random
from pathlib import Path

rate = 44100
rng = random.Random(777)
length = 1.25
pcm = []
# C6 E6 G6 C7 then a held E7 sparkle: instantly reads as "win".
notes = [(0.00, 1046.5), (0.07, 1318.5), (0.14, 1568.0), (0.21, 2093.0), (0.30, 2637.0)]
for i in range(int(rate * length)):
    t = i / rate
    v = 0.0
    # sub thump on impact
    v += math.sin(math.tau * 70 * t) * math.exp(-t * 18) * 0.55
    for onset, f in notes:
        u = t - onset
        if u < 0:
            continue
        env = min(1, u / 0.003) * math.exp(-u * (3.2 if f > 2500 else 6.0))
        # bell: fundamental + inharmonic partials
        v += (math.sin(math.tau * f * u) + 0.42 * math.sin(math.tau * f * 2.76 * u) + 0.2 * math.sin(math.tau * f * 5.4 * u)) * env * 0.17
    # coin shimmer: sparse high clicks fading out
    if rng.random() < 0.012 * math.exp(-t * 2.2):
        v += rng.uniform(-0.5, 0.5)
    v += math.sin(math.tau * 4186 * t + 6 * math.sin(math.tau * 9 * t)) * math.exp(-t * 3.5) * 0.04 * (t > 0.3)
    pcm.append(int(max(-0.95, min(0.95, v)) * 32767))

path = Path('Assets/GameAssets/Audio/Polish/LuckyJackpot.wav')
with wave.open(str(path), 'wb') as f:
    f.setnchannels(1); f.setsampwidth(2); f.setframerate(rate)
    f.writeframes(array.array('h', pcm).tobytes())
print(path)
