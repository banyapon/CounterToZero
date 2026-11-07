using UnityEngine;

namespace CountToZero
{
    public sealed class BossController : NumberCarrier
    {
        public float Health = 99, ShieldUntil, CrashUntil;
        public string PhaseName => Game.Phase == 1 ? "THE SENTINEL" : Game.Phase == 2 ? "SPIRAL PROTOCOL" : "ENTROPY ENGINE";
        public bool Sucking => Game.Elapsed % 9 > 5 && Time.time >= CrashUntil;
        [SerializeField] BossPatternDefinition[] patterns;
        float fireAt, angle, patternUntil;
        int patternIndex;
        public string PatternName => patterns[patternIndex].displayName;
        public int PatternIndex => patternIndex;
        public void SelectPattern(int index) { patternIndex = index % patterns.Length; patternUntil = Game.Elapsed + patterns[patternIndex].duration; fireAt=Time.time+.8f; }

        [SerializeField] SpriteRenderer sprite;
        [SerializeField] TextMesh label;
        [SerializeField] Transform spawnPoint;
        Vector3 visualOrigin;
        void Awake() { visualOrigin = sprite.transform.localPosition; ResetRun(); }
        public void ResetRun()
        {
            Health = 99; ShieldUntil = CrashUntil = fireAt = angle = 0;
            SetValue(50); transform.position = spawnPoint.position; patternIndex=0;patternUntil=7;
        }
        void Shot(float degrees,float speed){Game.Bullets.Fire(transform.position,Quaternion.Euler(0,degrees,0)*Vector3.forward,true,speed);}
        void Update()
        {
            label.text = "OMEGA-99  [" + CurrentValue + "]";
            sprite.color = CrashUntil > Time.time ? ArenaVisuals.Green : ShieldUntil > Time.time ? ArenaVisuals.Violet : ArenaVisuals.Red;
            if (Game.State != GameState.Playing || Time.time < CrashUntil) return;
            if(!Campaign.Active && Game.Elapsed>=patternUntil) SelectPattern(patternIndex+1);
            var pattern=patterns[patternIndex];float t=Game.Elapsed;
            Vector3 home=spawnPoint.position;Vector2 range=pattern.movementRange;
            Vector3 destination=pattern.motion switch
            {
                BossMotion.Orbit => home+new Vector3(Mathf.Cos(t*.65f)*range.x,0,Mathf.Sin(t*.65f)*range.y),
                BossMotion.Sweep => home+new Vector3(Mathf.PingPong(t*2,range.x*2)-range.x,0,0),
                BossMotion.Pursue => Vector3.Lerp(home,Game.Player.transform.position,.45f),
                BossMotion.FigureEight => home+new Vector3(Mathf.Sin(t*.7f)*range.x,0,Mathf.Sin(t*1.4f)*range.y),
                _ => home+new Vector3(Mathf.Sin(t*.5f)*range.x,0,Mathf.Cos(t*.5f)*range.y)
            };
            destination.z=Mathf.Clamp(destination.z,-2,6.3f);
            transform.position=Vector3.MoveTowards(transform.position,destination,Time.deltaTime*3);
            sprite.transform.localPosition=visualOrigin+Vector3.up*(Mathf.Sin(t*2)*.12f);
            if(Time.time<fireAt)return;
            fireAt=Time.time+pattern.interval/((1+(Game.Phase-1)*.17f)*(Campaign.Active?Campaign.Data.difficulty:1));
            float speed=pattern.speed*(ShieldUntil>Time.time?1.5f:1)*(Campaign.Active?Mathf.Sqrt(Campaign.Data.difficulty):1);
            angle+=23;
            Vector3 aim=(Game.Player.transform.position-transform.position).normalized;
            switch(pattern.shot)
            {
                case BossShot.Ring: Game.Bullets.Ring(transform.position,pattern.count,angle,speed);break;
                case BossShot.Fan:
                    for(int i=0;i<pattern.count;i++)Game.Bullets.Fire(transform.position,Quaternion.Euler(0,(i-(pattern.count-1)*.5f)*9,0)*aim,true,speed);
                    break;
                case BossShot.Spiral:
                    Game.Bullets.Ring(transform.position,4,angle,speed);break;
                case BossShot.Cross:
                    for(int i=0;i<4;i++)for(int j=-1;j<=1;j++)Game.Bullets.Fire(transform.position,Quaternion.Euler(0,i*90+angle+j*6,0)*Vector3.forward,true,speed);
                    break;
                case BossShot.Wave:
                    for(int i=0;i<pattern.count;i++)Game.Bullets.Fire(transform.position+Vector3.right*((i-(pattern.count-1)*.5f)*.3f),Quaternion.Euler(0,Mathf.Sin(t*2)*35,0)*Vector3.back,true,speed);
                    break;
                case BossShot.TwinSpiral:
                    Game.Bullets.Ring(transform.position,3,angle,speed);Game.Bullets.Ring(transform.position,3,-angle,speed*.8f);break;
                case BossShot.AimedBurst:
                    for(int i=0;i<5;i++)Game.Bullets.Fire(transform.position,Quaternion.Euler(0,(i-2)*4,0)*aim,true,speed+i*.3f);break;
                case BossShot.Flower:
                    for(int i=0;i<18;i++)Shot(i*20+angle,speed*(1+.25f*Mathf.Sin(i*2)));break;
                case BossShot.Pinwheel:
                    for(int i=0;i<4;i++)for(int j=0;j<4;j++)Shot(i*90+angle+j*5,speed+j*.3f);break;
                case BossShot.Rain:
                    for(int i=0;i<7;i++)Game.Bullets.Fire(new Vector3(-9+i*3+Mathf.Sin(t),0,7),Vector3.back,true,speed);break;
                case BossShot.SideSweep:
                    for(int i=0;i<5;i++)Game.Bullets.Fire(new Vector3(Mathf.Sin(t)>0?-10:10,0,-6+i*3),Mathf.Sin(t)>0?Vector3.right:Vector3.left,true,speed);break;
                case BossShot.Vortex:
                    for(int i=0;i<8;i++)Shot(angle+i*45,speed*(i%2==0?1:.65f));break;
                case BossShot.DoubleFan:
                    for(int i=-2;i<=2;i++){Game.Bullets.Fire(transform.position+Vector3.left,Quaternion.Euler(0,i*12+15,0)*aim,true,speed);Game.Bullets.Fire(transform.position+Vector3.right,Quaternion.Euler(0,i*12-15,0)*aim,true,speed);}break;
                case BossShot.Lanes:
                    int gap=((int)t)%7;for(int i=0;i<7;i++)if(i!=gap)Game.Bullets.Fire(new Vector3(-9+i*3,0,7),Vector3.back,true,speed);break;
                case BossShot.Pulse:
                    Game.Bullets.Ring(transform.position,12,angle,speed);Game.Bullets.Ring(transform.position,12,angle+15,speed*.6f);break;
                case BossShot.Scissors:
                    for(int i=-4;i<=4;i++){Shot(180+Mathf.Sin(t)*50+i*3,speed);Shot(180-Mathf.Sin(t)*50+i*3,speed);}break;
                case BossShot.RotatingGate:
                    for(int i=0;i<24;i++)if(i%12>2)Shot(i*15+angle*.4f,speed);break;
                case BossShot.Scatter:
                    for(int i=0;i<9;i++)Shot(angle+i*137.5f,speed*(.7f+i*.06f));break;
                case BossShot.Finale:
                    Game.Bullets.Ring(transform.position,12,angle,speed*.75f);for(int i=-1;i<=1;i++)Game.Bullets.Fire(transform.position,Quaternion.Euler(0,i*10,0)*aim,true,speed*1.2f);break;
                case BossShot.AlternatingRing:
                    for(int i=0;i<pattern.count;i++)if(i%5!=((int)angle/23)%5)Game.Bullets.Fire(transform.position,Quaternion.Euler(0,i*360f/pattern.count,0)*Vector3.forward,true,speed);
                    break;
            }
        }
    }
}
