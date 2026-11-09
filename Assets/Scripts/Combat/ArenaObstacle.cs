using UnityEngine;

namespace CountToZero
{
    public sealed class ArenaObstacle : MonoBehaviour
    {
        [SerializeField] Transform cube;
        [SerializeField] GameObject warning;
        [SerializeField] SpriteRenderer fire;      // second variant: falling pixel fireball
        [SerializeField] Sprite[] fireFrames;
        public bool Fire {get;private set;}
        float age, life;
        bool falling;
        Renderer blockRenderer;
        MaterialPropertyBlock properties;
        public static Color ElectricColor(float seconds)
        {
            int phase=Mathf.FloorToInt(seconds*6)%3;
            return phase==0?ArenaVisuals.Violet:phase==1?Color.white:Color.black;
        }
        void Awake(){blockRenderer=cube.GetComponent<Renderer>();properties=new MaterialPropertyBlock();}
        void ElectricFlash()
        {
            if(Fire){AnimateFire();return;}
            if(blockRenderer==null)return;
            var color=ElectricColor(age);
            blockRenderer.GetPropertyBlock(properties);properties.SetColor("_BaseColor",color);properties.SetColor("_Color",color);blockRenderer.SetPropertyBlock(properties);
        }
        public bool Active {get;private set;}
        public bool Solid => Active && age>=1.5f;
        public Vector3 Position => transform.position;
        public void Summon(Vector3 position,bool drop)
        {
            transform.position=position;falling=drop;age=0;life=drop?6:8;
            var hazards=GameManager.Instance!=null?GameManager.Instance.Hazards:null;
            if(hazards!=null && hazards.Scrolling)life=Mathf.Max(life,(position.z+10)/hazards.ScrollSpeed+1.5f);
            Fire=CanBurn&&Random.value<.5f;
            Active=true;gameObject.SetActive(true);
            warning.SetActive(true);cube.localPosition=Vector3.up*(drop?9:.8f);ShowBody(drop);if(Fire)AnimateFire();
        }
        public void Clear(){Active=false;gameObject.SetActive(false);}
        bool CanBurn=>fire!=null&&fireFrames!=null&&fireFrames.Length>0;
        // The body (cube or fireball) shares the cube's drop height so both variants fall the same way.
        void ShowBody(bool visible){cube.gameObject.SetActive(visible&&!Fire);if(fire!=null){fire.gameObject.SetActive(visible&&Fire);fire.transform.localPosition=cube.localPosition+Vector3.up*.2f;}}
        void AnimateFire()
        {
            var g=GameManager.Instance;
            fire.transform.localPosition=cube.localPosition+Vector3.up*.2f;
            if(g!=null&&g.View!=null)fire.transform.rotation=g.View.transform.rotation;
            fire.sprite=fireFrames[Mathf.FloorToInt(age*10)%fireFrames.Length];
        }
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
            float before=age;age+=Time.deltaTime;ElectricFlash();
            if(g.Hazards!=null && g.Hazards.Scrolling && !g.Training)
            {
                transform.position+=Vector3.back*g.Hazards.ScrollSpeed*Time.deltaTime;
                if(Solid && Blocks(g.Player.transform.position,.22f))
                {
                    var pushed=g.Player.transform.position;pushed.z=Position.z-1.1f;
                    g.Player.transform.position=pushed;
                    if(pushed.z<=-7.3f || g.View.WorldToViewportPoint(pushed).y<=.035f)g.Player.Crush();
                }
                if(Position.z < -9){Clear();return;}
            }
            if(age<1.5f)
            {
                warning.transform.localScale=Vector3.one*(1.9f+Mathf.Sin(age*15)*.1f);
                if(falling)cube.localPosition=Vector3.up*Mathf.Lerp(9,.8f,Mathf.Clamp01((age-1)/.5f));
            }
            else
            {
                warning.SetActive(false);cube.localPosition=Vector3.up*.8f;ShowBody(true);
                if(before<1.5f && Blocks(g.Player.transform.position))
                {
                    if(falling)g.Player.Hurt(false,Fire?"Falling fireball":"Falling cube");
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
