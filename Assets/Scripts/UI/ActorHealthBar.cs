using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CountToZero
{
    public sealed class ActorHealthBar : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] BossController boss;
        [SerializeField] MinionController minion;
        [SerializeField] Image fill;
        [SerializeField] TMP_Text value;
        public float Fraction {get;private set;}
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            float current=player!=null?player.Health:boss!=null?boss.Health:minion!=null?minion.Health:0;
            float maximum=player!=null?5:boss!=null?99:3;
            Fraction=Mathf.Clamp01(current/maximum);fill.fillAmount=Fraction;
            value.text=Mathf.CeilToInt(Mathf.Max(0,current))+" / "+maximum.ToString("0")+" HP";
        }
    }
}
