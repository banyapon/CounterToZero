using UnityEngine;

namespace CountToZero
{
    public enum SectorMode { Standard, Conveyor, Laser, ConveyorLaser }
    [DefaultExecutionOrder(10)]
    public sealed class SectorHazards : MonoBehaviour
    {
        [SerializeField] Transform[] floorLines;
        [SerializeField] LineRenderer laser;
        [SerializeField] float warningSeconds=1.2f, beamSeconds=2, restSeconds=2;
        MaterialPropertyBlock beamProperties;
        Vector3[] floorOrigins;
        Vector3 previousPlayer;
        float scroll, cycle, beamCoordinate;
        bool vertical;
        public SectorMode Mode {get;private set;}
        public bool Scrolling => Mode==SectorMode.Conveyor || Mode==SectorMode.ConveyorLaser;
        public bool HasLaser => Mode==SectorMode.Laser || Mode==SectorMode.ConveyorLaser;
        public bool BeamActive => HasLaser && cycle>=warningSeconds && cycle<warningSeconds+beamSeconds;
        public float ScrollSpeed => Scrolling ? Mathf.Min(2.2f, .8f+(Campaign.Active?Campaign.Data.level:1)*.018f) : 0;
        public string Label => Mode==SectorMode.Conveyor?"CONVEYOR / AVOID BOTTOM CRUSH":Mode==SectorMode.Laser?"LASER / 50% MAX HP + KNOCKBACK":Mode==SectorMode.ConveyorLaser?"CRUSH + LASER SECTOR":"";
        void Awake(){beamProperties=new MaterialPropertyBlock();floorOrigins=new Vector3[floorLines.Length];for(int i=0;i<floorLines.Length;i++)floorOrigins[i]=floorLines[i].position;}
        public static SectorMode SelectMode(int level,int seed)
        {
            if(level<3)return SectorMode.Standard;
            int slot=(level+(seed&0x7fffffff)%6)%6;
            return slot==1?SectorMode.Conveyor:slot==3?SectorMode.Laser:slot==5&&level>=15?SectorMode.ConveyorLaser:SectorMode.Standard;
        }
        public void ResetRun()
        {
            Configure(SelectMode(Campaign.Active?Campaign.Data.level:1,Campaign.Data.hazardSeed));
        }
        public void Configure(SectorMode mode)
        {
            Mode=mode;scroll=cycle=0;laser.enabled=false;
            if(floorOrigins!=null)for(int i=0;i<floorLines.Length;i++)floorLines[i].position=floorOrigins[i];
            previousPlayer=GameManager.Instance.Player.transform.position;ChooseBeam();
        }
        void ChooseBeam()
        {
            vertical=Random.value>.5f;beamCoordinate=Random.Range(-3.5f,3.5f);
            // Move the warning away from the player's current lane, leaving time to react.
            float playerCoordinate=vertical?GameManager.Instance.Player.transform.position.x:GameManager.Instance.Player.transform.position.z;
            if(Mathf.Abs(playerCoordinate-beamCoordinate)<1)beamCoordinate=playerCoordinate>0?-2:2;
            laser.SetPosition(0,vertical?new Vector3(beamCoordinate,.55f,-5.5f):new Vector3(-9.4f,.55f,beamCoordinate));
            laser.SetPosition(1,vertical?new Vector3(beamCoordinate,.55f,5.5f):new Vector3(9.4f,.55f,beamCoordinate));
        }
        public bool CrossesBeam(Vector3 from,Vector3 to)
        {
            float a=(vertical?from.x:from.z)-beamCoordinate,b=(vertical?to.x:to.z)-beamCoordinate;
            if(a*b>0 && Mathf.Abs(b)>.25f)return false;
            float t=Mathf.Abs(a-b)>.0001f?Mathf.Clamp01(a/(a-b)):1;
            float along=vertical?Mathf.Lerp(from.z,to.z,t):Mathf.Lerp(from.x,to.x,t);
            return Mathf.Abs(along)<(vertical?5.7f:9.6f);
        }
        void Update()
        {
            var g=GameManager.Instance;
            if(g.State!=GameState.Playing || g.Training){laser.enabled=false;previousPlayer=g.Player.transform.position;return;}
            if(Scrolling)
            {
                scroll+=ScrollSpeed*Time.deltaTime;
                for(int i=0;i<floorLines.Length;i++){var p=floorOrigins[i];p.z=Mathf.Repeat(p.z+8-scroll,16)-8;floorLines[i].position=p;}
            }
            if(HasLaser)
            {
                cycle+=Time.deltaTime;
                if(cycle>=warningSeconds+beamSeconds+restSeconds){cycle=0;ChooseBeam();}
                laser.enabled=cycle<warningSeconds+beamSeconds;
                bool live=BeamActive;
                float flicker=.55f+.45f*Mathf.Abs(Mathf.Sin(cycle*15));
                laser.startColor=laser.endColor=new Color(1,.03f,.08f,live?flicker:.25f+.3f*flicker);
                laser.widthMultiplier=live?.22f:.06f;
                laser.GetPropertyBlock(beamProperties);
                var red=new Color((live?1:.6f)*flicker,.01f,.025f,1);
                beamProperties.SetColor("_BaseColor",red);beamProperties.SetColor("_Color",red);laser.SetPropertyBlock(beamProperties);
                if(live && CrossesBeam(previousPlayer,g.Player.transform.position))
                {
                    float side=Mathf.Sign((vertical?previousPlayer.x:previousPlayer.z)-beamCoordinate);if(side==0)side=1;
                    g.Player.HitLaser((vertical?Vector3.right:Vector3.forward)*side);
                }
            }
            else laser.enabled=false;
            previousPlayer=g.Player.transform.position;
        }
    }
}
