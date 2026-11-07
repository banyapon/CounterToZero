"""Deterministic original synth cues and synchronized 96 BPM stems. No external samples."""
import math, random, wave, array
from pathlib import Path
RATE=22050
OUT=Path(__file__).resolve().parents[1]/'Assets/GameAssets/Audio/Polish'
OUT.mkdir(parents=True,exist_ok=True)
rng=random.Random(77)
def tone(f,t):return math.sin(math.tau*f*t)+.18*math.sin(math.tau*f*2*t)
def save(name,data):
    peak=max(.001,max(abs(x) for x in data)); gain=min(1,.7/peak)
    pcm=array.array('h',(int(max(-1,min(1,x*gain))*32767) for x in data))
    with wave.open(str(OUT/(name+'.wav')),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(RATE);w.writeframes(pcm.tobytes())
def notes(name,seq,step,tail=.18):
    length=step*len(seq)+tail;data=[0.]*(int(length*RATE))
    for n,freq in enumerate(seq):
        start=int(n*step*RATE)
        for j in range(int((step+tail)*RATE)):
            if start+j>=len(data):break
            t=j/RATE;env=min(1,t/.006)*math.exp(-t/(step*.65))
            data[start+j]+=.26*tone(freq,t)*env
    save(name,data)
notes('Pickup',[784,988],.065,.08)
notes('Heal',[659,784,988,1318],.11,.2)
notes('Lucky',[659,784,988,1318,1568],.09,.3)
notes('Warning',[880,660,880],.12,.1)
notes('Death',[330,277,220,165,110],.13,.25)
notes('Phase',[440,554,659,880],.12,.24)
for name,length in [('Shoot',.09),('Dash',.23),('Hit',.2)]:
    data=[];smooth=0
    for i in range(int(length*RATE)):
        t=i/RATE;u=t/length;noise=rng.uniform(-1,1);smooth=.75*smooth+.25*noise;env=min(1,t/.003)*(1-u)**2
        if name=='Shoot':value=.28*math.sin(math.tau*(700*t-2600*t*t))+.1*noise
        elif name=='Dash':value=.6*smooth+.1*math.sin(math.tau*(300*t-400*t*t))
        else:value=.5*noise+.3*math.sin(math.tau*75*t)
        data.append(value*env)
    save(name,data)
# Exactly 16 beats = ten seconds; all stems loop at the same sample count.
beat=.625;count=RATE*10
bed=[0.]*count;pulse=[0.]*count;danger=[0.]*count
roots=[82.4069,65.4064,55,61.7354]
for i in range(count):
    t=i/RATE;bar=int(t/2.5);local=t%2.5;root=roots[bar]
    env=min(1,local/.12)*min(1,(2.5-local)/.2)
    bed[i]=.11*env*(tone(root,t)+.35*tone(root*2**(3/12),t)+.3*tone(root*1.5,t))
    b=t%beat;kick=.32*math.sin(math.tau*(48*b+4*(1-math.exp(-b*25))))*math.exp(-b*18)
    h=t%(beat/2);hat=.055*rng.uniform(-1,1)*math.exp(-h*65)
    pulse[i]=kick+hat
    note=int(t/(beat/2));f=root*4*[1,1.5,2,1.5,1.25,1.5,2,2.5][note%8]
    q=t%(beat/2);env2=min(1,q/.004)*math.exp(-q*16)
    danger[i]=.13*env2*(math.sin(math.tau*f*q)+.2*math.sin(math.tau*f*3*q))
for name,data in [('MusicBed',bed),('MusicPulse',pulse),('MusicDanger',danger)]:save(name,data)
print('Wrote 9 cues and 3 synchronized music stems to',OUT)
