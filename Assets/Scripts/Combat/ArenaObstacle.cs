using UnityEngine;

namespace CountToZero
{
    public sealed class ArenaObstacle : MonoBehaviour
    {
        [SerializeField] Transform cube;
        [SerializeField] GameObject warning;
        float age, life;
        bool falling;
        public bool Active {get;private set;}
        public bool Solid => Active && age>=1.5f;
        public Vector3 Position => transform.position;
        public void Summon(Vector3 position,bool drop)
        {
            transform.position=position;falling=drop;age=0;life=drop?6:8;Active=true;gameObject.SetActive(true);
            warning.SetActive(true);cube.gameObject.SetActive(drop);cube.localPosition=Vector3.up*(drop?9:.8f);
        }
        public void Clear(){Active=false;gameObject.SetActive(false);}
        public bool Blocks(Vector3 point,float radius=.32f)
        {return Solid && Mathf.Abs(point.x-Position.x)<.8f+radius && Mathf.Abs(point.z-Position.z)<.8f+radius;}
        public bool HitsSegment(Vector3 a,Vector3 b)
        {
            if(!Solid)return false;
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.2f));
            for(int i=0;i<=steps;i++)if(Blocks(Vector3.Lerp(a,b,(float)i/steps),.08f))return true;
            return false;
        }
        void Update()
        {
            var g=GameManager.Instance;if(!Active||g.State!=GameState.Playing)return;
            float before=age;age+=Time.deltaTime;
            if(age<1.5f)
            {
                warning.transform.localScale=Vector3.one*(1.9f+Mathf.Sin(age*15)*.1f);
                if(falling)cube.localPosition=Vector3.up*Mathf.Lerp(9,.8f,Mathf.Clamp01((age-1)/.5f));
            }
            else
            {
                warning.SetActive(false);cube.gameObject.SetActive(true);cube.localPosition=Vector3.up*.8f;
                if(before<1.5f && Blocks(g.Player.transform.position))
                {
                    if(falling)g.Player.Hurt(false,"Falling cube");
                    // Always leave the player outside a newly solid block, even while stunned.
                    Vector3 away=g.Player.transform.position-Position;if(away.sqrMagnitude<.01f)away=Vector3.back;
                    away=away.normalized*1.65f;var safe=Position+away;
                    safe.x=Mathf.Clamp(safe.x,-10.3f,10.3f);safe.z=Mathf.Clamp(safe.z,-7.3f,7.3f);
                    if(!g.Encounters.Blocked(safe))g.Player.transform.position=safe;
                    else Clear();
                }
            }
            if(age>life)Clear();
        }
    }
}
