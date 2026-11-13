using UnityEngine;

namespace CountToZero
{
    public sealed class LuckyCutIn : MonoBehaviour
    {
        [SerializeField] RectTransform panel;
        [SerializeField] CanvasGroup group;
        [SerializeField] float duration=.8f;
        [SerializeField] UnityEngine.UI.Image portrait;
        [SerializeField] Sprite[] punchFrames;
        float remaining;
        public bool Visible => remaining>0;
        public void Show(){remaining=duration;panel.gameObject.SetActive(true);}
        public void Clear(){remaining=0;panel.gameObject.SetActive(false);}
        void Awake()
        {
            if (portrait != null) portrait.rectTransform.sizeDelta = new Vector2(110, 122);
            if (panel != null) panel.sizeDelta = new Vector2(panel.sizeDelta.x, 136);
            Clear();
        }
        void Update()
        {
            if(remaining<=0)return;
            if(GameManager.Instance.State==GameState.Paused)return;
            if(GameManager.Instance.State!=GameState.Playing){Clear();return;}
            remaining-=Time.unscaledDeltaTime;
            float t=1-remaining/duration;
            if(portrait!=null && punchFrames!=null && punchFrames.Length>0){portrait.color=Color.white;portrait.sprite=punchFrames[Mathf.Min(punchFrames.Length-1,Mathf.FloorToInt(t*punchFrames.Length))];}
            group.alpha=Mathf.Min(t*10,Mathf.Max(0,(1-t)*5));
            panel.anchoredPosition=new Vector2(Mathf.Lerp(-250,0,Mathf.Clamp01(t*7)),0);
            if(remaining<=0)Clear();
        }
    }
}
