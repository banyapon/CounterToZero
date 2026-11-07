using UnityEngine;

namespace CountToZero
{
    public sealed class HealthPickup : MonoBehaviour
    {
        [SerializeField] Transform visual;
        float age;
        public bool Active {get;private set;}
        public void Spawn(Vector3 position){transform.position=position;age=0;Active=true;gameObject.SetActive(true);}
        public void Clear(){Active=false;gameObject.SetActive(false);}
        void Update()
        {
            var g=GameManager.Instance;if(!Active||g.State!=GameState.Playing)return;
            age+=Time.deltaTime;visual.localPosition=Vector3.up*(.65f+Mathf.Sin(age*3)*.12f);
            if(g.Player.Health<5 && Vector3.Distance(transform.position,g.Player.transform.position)<.75f)
            {g.Player.Heal();g.Feedback.Pickup();g.Dialogue.Say("heal");Clear();}
            if(age>24)Clear();
        }
    }
}
