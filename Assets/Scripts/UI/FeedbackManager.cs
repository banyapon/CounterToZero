using TMPro;
using UnityEngine;

namespace CountToZero
{
    public sealed class FeedbackManager : MonoBehaviour
    {
        public string Message { get; private set; }
        public Color MessageColor { get; private set; }
        public float MessageUntil { get; private set; }
        float hitStopUntil, shakeUntil, laserUntil;
        public Vector3 ShakeOffset => Time.unscaledTime < shakeUntil && GameManager.Instance.State == GameState.Playing ? Random.insideUnitSphere*.09f : Vector3.zero;
        [SerializeField] LineRenderer laser;
        [SerializeField] Material luckyBeamMaterial;
        LineRenderer laserGlow;
        readonly Vector3[] beamPoints=new Vector3[25];
        bool beamPending;
        Vector3 beamFrom,beamTo;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip pickup, lucky, hit;
        [SerializeField] TMP_Text floating;
        float floatUntil;
        public void ResetFeedback() {hitStopUntil=shakeUntil=laserUntil=0;MessageUntil=0;floatUntil=0;beamPending=false;if(laser!=null)laser.enabled=false;if(laserGlow!=null)laserGlow.enabled=false;}
        public void Announce(string message,Color color) {Message=message;MessageColor=color;MessageUntil=Time.unscaledTime+2.7f;}
        public void Pickup() {if(GameAudio.Instance!=null)GameAudio.Play(SoundCue.Pickup);else audioSource.PlayOneShot(pickup);}
        public void Lucky(Vector3 position,bool player)
        {
            hitStopUntil=Time.unscaledTime+.15f; shakeUntil=Time.unscaledTime+.4f;
            Time.timeScale=.1f;if(GameAudio.Instance!=null)GameAudio.Play(player?SoundCue.Lucky:SoundCue.Warning);else audioSource.PlayOneShot(lucky);
            Announce(player?"LUCKY MATCH / OVERLOAD":"OMEGA MATCH / COUNTERATTACK",player?ArenaVisuals.Green:ArenaVisuals.Red);
            floating.transform.position=position+Vector3.up*2; floating.text=player?"LUCKY!":"COUNTER!";
            floating.color=player?ArenaVisuals.Green:ArenaVisuals.Red; floatUntil=Time.unscaledTime+1;
        }
        public void Hit(Vector3 position) {shakeUntil=Time.unscaledTime+.22f;if(GameAudio.Instance!=null)GameAudio.Play(SoundCue.Hit);else audioSource.PlayOneShot(hit);}
        public void Laser(Vector3 from,Vector3 to)
        {
            beamFrom=from;beamTo=to;beamPending=true;
            if(laserGlow==null)
            {
                laserGlow=new GameObject("Lucky Beam Glow").AddComponent<LineRenderer>();
                laserGlow.transform.SetParent(laser.transform,false);
                ConfigureBeam(laser,100,.1f);ConfigureBeam(laserGlow,99,.34f);
            }
        }
        void ConfigureBeam(LineRenderer line,int order,float width)
        {
            if(luckyBeamMaterial!=null)line.sharedMaterial=luckyBeamMaterial;
            else line.sharedMaterial=laser.sharedMaterial;
            line.useWorldSpace=true;line.alignment=LineAlignment.View;line.positionCount=beamPoints.Length;
            line.widthMultiplier=1;line.startWidth=width;line.endWidth=width*.65f;
            line.sortingOrder=order;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows=false;line.enabled=false;
        }
        void UpdateBeam(GameManager g)
        {
            if(g.State==GameState.Paused){if(laserUntil>Time.unscaledTime-Time.unscaledDeltaTime)laserUntil+=Time.unscaledDeltaTime;return;}
            if(beamPending&&g.State==GameState.Playing&&!g.CutIn.Visible){beamPending=false;laserUntil=Time.unscaledTime+.75f;}
            bool visible=g.State==GameState.Playing&&!g.CutIn.Visible&&Time.unscaledTime<laserUntil;
            laser.enabled=visible;if(laserGlow!=null)laserGlow.enabled=visible;
            if(!visible)return;
            Vector3 from=(g.Player!=null?g.Player.transform.position:beamFrom)+Vector3.up*.7f;
            Vector3 to=(g.Boss!=null?g.Boss.transform.position:beamTo)+Vector3.up*1.2f;
            Vector3 side=Vector3.Cross((to-from).normalized,g.View.transform.forward).normalized;
            float tick=Mathf.Floor(Time.unscaledTime*35);
            for(int i=0;i<beamPoints.Length;i++)
            {
                float t=i/(float)(beamPoints.Length-1);
                float jitter=(Mathf.PerlinNoise(i*1.71f,tick*.63f)*2-1)*.34f*Mathf.Sin(t*Mathf.PI);
                beamPoints[i]=Vector3.Lerp(from,to,t)+side*jitter;
            }
            laser.SetPositions(beamPoints);laserGlow.SetPositions(beamPoints);
            float fade=Mathf.Clamp01((laserUntil-Time.unscaledTime)/.2f);
            laser.startColor=laser.endColor=new Color(.7f,1,.9f,fade);
            laserGlow.startColor=laserGlow.endColor=new Color(0,1,.55f,.35f*fade);
        }
        void LateUpdate()
        {
            var g=GameManager.Instance;
            Time.timeScale=g.State!=GameState.Playing?0:Time.unscaledTime<hitStopUntil?.1f:Time.unscaledTime<g.SlowUntil?.35f:1;

            UpdateBeam(g);
            floating.gameObject.SetActive(Time.unscaledTime<floatUntil);
            if(floating.gameObject.activeSelf) floating.transform.position+=Vector3.up*Time.unscaledDeltaTime*.6f;
        }
    }
}
