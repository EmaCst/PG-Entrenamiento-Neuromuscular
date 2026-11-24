import numpy as np,wave,subprocess,tempfile
from pathlib import Path
# Original 16-bar electronic training loop, 128 BPM, no samples or external tracks.
sr=22050; beat=60/128; length=64*beat;n=round(sr*length)
a=np.zeros(n);rng=np.random.default_rng(72027)
def add(s,start):
 idx=(np.arange(len(s))+round(start*sr))%n
 np.add.at(a,idx,s)
def midi(m):return 440*2**((m-69)/12)
for k in range(64):
 t=np.arange(round(.40*sr))/sr
 phase=2*np.pi*(48*t+105*.025*(1-np.exp(-t/.025)))
 kick=.8*np.sin(phase)*np.exp(-t/0.09)*(1-np.exp(-t/.002))
 add(kick,k*beat)
 if k%4 in [1,3]:
  t=np.arange(round(.18*sr))/sr;noise=rng.normal(0,1,len(t));hi=noise-np.convolve(noise,np.ones(19)/19,'same')
  clap=.13*hi*np.exp(-t/.035)*(1+np.sin(2*np.pi*65*t)**2)+.12*np.sin(2*np.pi*185*t)*np.exp(-t/.045)
  add(clap,k*beat)
for k in range(128):
 dur=.075 if k%2==0 else .14;t=np.arange(round(dur*sr))/sr
 noise=rng.normal(0,1,len(t));hi=noise-np.convolve(noise,np.ones(7)/7,'same')
 add((.035 if k%2==0 else .055)*hi*np.exp(-t/(.018 if k%2==0 else .035)),k*beat/2)
# Am / F / C / G. Short envelopes wrap around the seam without discontinuity.
chords=[(45,[69,72,76]),(41,[65,69,72]),(36,[64,67,72]),(43,[67,71,74])]
for bar in range(16):
 root,chord=chords[bar%4]
 for step in [0,.75,1.5,2,2.75,3.5]:
  t=np.arange(round(.21*sr))/sr;f=midi(root)
  bass=(np.sin(2*np.pi*f*t)+.28*np.sin(2*np.pi*2*f*t))*np.exp(-t/.08)*(1-np.exp(-t/.008))
  add(.22*bass,(bar*4+step)*beat)
 for step in range(8):
  t=np.arange(round(.38*sr))/sr;f=midi(chord[[0,1,2,1,0,2,1,2][step]])
  pluck=np.sin(2*np.pi*f*t+1.4*np.sin(2*np.pi*2*f*t)*np.exp(-t/.055))
  pluck*=np.exp(-t/.095)*(1-np.exp(-t/.004))
  add(.11*pluck,(bar*4+step*.5)*beat)
  add(.025*pluck,(bar*4+step*.5+.75)*beat)
 for offset in [.5,2.5]:
  t=np.arange(round(.30*sr))/sr;stab=np.zeros(len(t))
  for m in chord:
   f=midi(m-12);stab+=(np.sin(2*np.pi*f*t)+.12*np.sin(2*np.pi*3*f*t))
  stab*=np.exp(-t/.09)*(1-np.exp(-t/.012))
  add(.045*stab,(bar*4+offset)*beat)
a=np.tanh(a*1.15);a*=.78/max(abs(a));a-=np.mean(a)
# 64 beats at 128 BPM = precisely 30 seconds; notes/tails wrap into the start.
output=Path(__file__).resolve().parents[1]/'Assets/Resources/PraxenMusic/training_music.ogg'
output.parent.mkdir(parents=True,exist_ok=True)
work=tempfile.TemporaryDirectory()
p=Path(work.name)/'training.wav'
with wave.open(str(p),'wb') as f:
 f.setnchannels(1);f.setsampwidth(2);f.setframerate(sr);f.writeframes((np.clip(a,-1,1)*32767).astype('<i2').tobytes())
subprocess.run(['ffmpeg','-v','error','-y','-i',str(p),'-c:a','libvorbis','-q:a','4',str(output)],check=True)
print('Original music:',length,'seconds / 128 BPM / 16 bars / no third-party samples')
