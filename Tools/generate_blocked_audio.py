"""Original short metallic rejection cue, without external samples."""
import math, wave, array, random
from pathlib import Path
rate=22050; rng=random.Random(83); pcm=[]
for i in range(int(rate*.26)):
 t=i/rate
 value=0
 for onset,frequency in [(0,1450),(.075,920)]:
  u=t-onset
  if u>=0:
   value+=(math.sin(math.tau*frequency*u)+.45*math.sin(math.tau*frequency*2.71*u)+.18*rng.uniform(-1,1))*math.exp(-u*35)*min(1,u/.0015)*.3
 pcm.append(int(max(-.9,min(.9,value))*32767))
path=Path('Assets/GameAssets/Audio/Polish/Blocked.wav')
with wave.open(str(path),'wb') as f:
 f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(array.array('h',pcm).tobytes())
print(path)
