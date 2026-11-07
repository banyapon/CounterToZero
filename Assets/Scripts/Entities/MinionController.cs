using UnityEngine;

namespace CountToZero
{
    public sealed class MinionController : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] TextMesh label;
        float age,fireAt,health;
        public bool Active {get;private set;}
        public void Spawn(Vector3 position){transform.position=position;age=0;fireAt=2.5f;health=3;Active=true;gameObject.SetActive(true);}
        public void Clear(){Active=false;gameObject.SetActive(false);}
        public void Hit(float damage)
        {
            health-=damage;
            if(health<=0){var g=GameManager.Instance;g.Score.Award(200);g.Encounters.DropHealth(transform.position);Clear();}
        }
        void Update()
        {
            var g=GameManager.Instance;if(!Active||g.State!=GameState.Playing)return;
            age+=Time.deltaTime;sprite.color=age<1?ArenaVisuals.Gold:ArenaVisuals.Violet;
            label.text=age<1?"INCOMING":"SENTINEL";if(age<1)return;
            Vector3 direction=(g.Player.transform.position-transform.position).normalized;
            Vector3 step=(direction+Vector3.Cross(Vector3.up,direction)*Mathf.Sin(age*2)*.45f)*Time.deltaTime*1.8f;
            transform.position=g.Encounters.Move(transform.position,step);
            if(Vector3.Distance(g.Player.transform.position,transform.position)<.65f)g.Player.Hurt();
            if(age>=fireAt){fireAt=age+2.7f;g.Bullets.Fire(transform.position,direction,true,3.2f);}
            if(age>24)Clear();
        }
    }
}
