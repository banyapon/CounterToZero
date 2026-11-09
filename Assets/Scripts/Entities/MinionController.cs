using TMPro;
using UnityEngine;

namespace CountToZero
{
    public sealed class MinionController : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] TMP_Text label;
        [SerializeField] PixelDeathRing deathRing;
        float age,fireAt,health;
        public int ShotsFired {get;private set;}
        public static bool CanShootAtLevel(int level)=>level>=6;
        public static int ShotCountAtLevel(int level)=>level>=25?5:level>=15?3:1;
        public static float ShotIntervalAtLevel(int level)=>Mathf.Max(1.2f,2.7f-(level-6)*.045f);
        public float Health => Mathf.Max(0,health);
        public bool Active {get;private set;}
        public void Spawn(Vector3 position){deathRing?.Clear();GetComponent<RobotAppearance>()?.Choose();transform.position=position;age=0;ShotsFired=0;fireAt=2.5f;health=3+Mathf.Min(4,(Campaign.Data.level-1)/8);Active=true;gameObject.SetActive(true);}
        public void Clear(bool defeated=false){if(!defeated)deathRing?.Clear();Active=false;gameObject.SetActive(false);}
        public void Hit(float damage)
        {
            if(!Active || damage<=0)return;
            health-=damage;
            if(health<=0){var g=GameManager.Instance;g.Score.Award(200);GameAudio.Play(SoundCue.Explosion);g.Encounters.DropHealth(transform.position);deathRing?.Play(sprite.transform.position);Clear(true);}
        }
        void Update()
        {
            var g=GameManager.Instance;if(!Active||g.State!=GameState.Playing)return;
            age+=Time.deltaTime;sprite.color=age<1?ArenaVisuals.Gold:Color.white;
            label.text=age<1?"INCOMING":"SENTINEL";if(age<1)return;
            Vector3 direction=(g.Player.transform.position-transform.position).normalized;
            Vector3 step=(direction+Vector3.Cross(Vector3.up,direction)*Mathf.Sin(age*2)*.45f)*Time.deltaTime*Mathf.Min(3.4f,1.8f+Campaign.Data.level*.035f);
            transform.position=g.Encounters.Move(transform.position,step);
            if(Vector3.Distance(g.Player.transform.position,transform.position)<.4f)g.Player.Hurt(false,"Sentinel contact");
            int level=Campaign.Active?Campaign.Data.level:1;
            if(CanShootAtLevel(level) && age>=fireAt)
            {
                fireAt=age+ShotIntervalAtLevel(level);
                int count=ShotCountAtLevel(level);
                float speed=Mathf.Min(5.5f,3.2f+(level-6)*.025f);
                for(int i=0;i<count;i++){g.Bullets.Fire(transform.position,Quaternion.Euler(0,(i-(count-1)*.5f)*12,0)*direction,true,speed);ShotsFired++;}
            }
            if(age>24)Clear();
        }
    }
}
