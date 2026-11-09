using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace CountToZero
{
    public sealed class BulletPattern : MonoBehaviour
    {
        sealed class Bullet
        {
            public GameObject Object;
            public SpriteRenderer Sprite;
            public Vector3 Position, Velocity;
            public bool Enemy, Homing, Grazed;
            public float Age;
        }
        readonly List<Bullet> active=new List<Bullet>(700);
        [SerializeField] GameObject projectilePrefab;
        [SerializeField] Transform poolRoot;
        ObjectPool<Bullet> pool;
        void Awake()
        {
            pool=new ObjectPool<Bullet>(Create,null,b=>b.Object.SetActive(false),null,true,700,700);
            var warm=new Bullet[700]; for(int i=0;i<warm.Length;i++) warm[i]=pool.Get();
            foreach(var b in warm) pool.Release(b);
        }
        Bullet Create()
        {
            var go=Instantiate(projectilePrefab,poolRoot);
            var sprite=go.GetComponentInChildren<SpriteRenderer>();
            go.SetActive(false); return new Bullet {Object=go,Sprite=sprite};
        }
        public void Fire(Vector3 origin,Vector3 direction,bool enemy,float speed,bool homing=false)
        {
            if(active.Count>=680) return;
            var b=pool.Get(); b.Position=origin; b.Velocity=direction*speed;
            b.Enemy=enemy; b.Homing=homing; b.Grazed=false; b.Age=0;
            b.Sprite.color=enemy?ArenaVisuals.Red:homing?ArenaVisuals.Green:ArenaVisuals.Cyan;
            b.Sprite.transform.localScale=Vector3.one*(enemy?.27f:.22f);
            b.Object.transform.position=origin; b.Object.SetActive(true); active.Add(b);
        }
        public void Ring(Vector3 origin,int count,float angle,float speed)
        {
            for(int i=0;i<count;i++)
            {
                float a=(angle+360f*i/count)*Mathf.Deg2Rad;
                Fire(origin,new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),true,speed);
            }
        }
        void Release(int i) { pool.Release(active[i]); active.RemoveAt(i); }
        public void Clear() { for(int i=active.Count-1;i>=0;i--) Release(i); }
        public void ClearEnemy() { for(int i=active.Count-1;i>=0;i--) if(active[i].Enemy) Release(i); }
        // Swept XZ collision avoids tunneling during fast shots and low frame rates.
        public static float SegmentDistance(Vector3 p,Vector3 a,Vector3 b)
        {
            Vector3 ab=b-a; float t=ab.sqrMagnitude<.00001f?0:Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude);
            return Vector3.Distance(p,a+ab*t);
        }
        void Update()
        {
            var g=GameManager.Instance; if(g.State!=GameState.Playing)return;
            for(int i=active.Count-1;i>=0;i--)
            {
                // A collision may clear the pool through a phase transition.
                if(i>=active.Count) continue;
                var b=active[i]; b.Age+=Time.deltaTime;
                if(b.Homing) b.Velocity=Vector3.RotateTowards(b.Velocity,((BattleExpansion.Instance!=null?BattleExpansion.Instance.BossAim:g.Boss.transform.position)-b.Position).normalized*16,Time.deltaTime*6,0);
                Vector3 old=b.Position; b.Position+=b.Velocity*Time.deltaTime; b.Object.transform.position=b.Position;
                if(b.Age>9 || Mathf.Abs(b.Position.x)>11.5f || Mathf.Abs(b.Position.z)>8.5f) {Release(i);continue;}
                if(g.Encounters.Intercept(old,b.Position,b.Enemy,b.Homing?1.5f:1f)){Release(i);continue;}
                if(b.Enemy)
                {
                    if(BattleExpansion.Instance!=null && BattleExpansion.Instance.HitAssistant(old,b.Position)){Release(i);continue;}
                    float distance=SegmentDistance(g.Player.transform.position,old,b.Position);
                    if(distance<.18f) {Release(i);g.Player.Hurt();}
                    else if(distance<.85f && !b.Grazed) {b.Grazed=true;g.Score.Graze();}
                }
                else
                {
                    if(BattleExpansion.Instance!=null && BattleExpansion.Instance.HitSegment(old,b.Position,b.Homing?1.5f:1)){Release(i);continue;}
                    bool consumed=false;
                    for(int n=g.Orbs.Active.Count-1;n>=0;n--)
                    {
                        var orb=g.Orbs.Active[n];
                        if(orb.Reward==0 && SegmentDistance(orb.transform.position,old,b.Position)<.48f)
                        {orb.Shot();consumed=true;break;}
                    }
                    if(consumed) {Release(i);continue;}
                    if(SegmentDistance(g.Boss.transform.position,old,b.Position)<1.05f)
                    {Release(i);g.DamageBoss(b.Homing?.65f:.22f);}
                }
                if(g.State!=GameState.Playing) break;
            }
        }
    }
}
