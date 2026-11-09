using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CountToZero
{
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] Color accent = new Color(.35f,1,.8f);
        Button button;
        Outline outline;
        Vector3 originalScale;
        bool hovering, selected, pressed;
        public bool Highlighted => (hovering || selected) && button!=null && button.IsInteractable();
        void Awake(){button=GetComponent<Button>();outline=GetComponent<Outline>();originalScale=transform.localScale;button.onClick.AddListener(Click);}
        void Click(){GameAudio.Play(SoundCue.UIClick);}
        void Enter(){if(button.IsInteractable() && !hovering && !selected)GameAudio.Play(SoundCue.UIHover);}
        public void OnPointerEnter(PointerEventData e){Enter();hovering=true;}
        public void OnPointerExit(PointerEventData e){hovering=false;pressed=false;}
        public void OnSelect(BaseEventData e){Enter();selected=true;}
        public void OnDeselect(BaseEventData e){selected=false;pressed=false;}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left && button.IsInteractable())pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        void Update()
        {
            float scale=button.IsInteractable()?(pressed?.97f:Highlighted?1.045f:1):1;
            transform.localScale=Vector3.Lerp(transform.localScale,originalScale*scale,1-Mathf.Exp(-18*Time.unscaledDeltaTime));
            if(outline!=null){outline.effectColor=new Color(accent.r,accent.g,accent.b,Highlighted?.9f:.28f);outline.effectDistance=Vector2.one*(Highlighted?2:1);}
        }
        void OnDisable(){hovering=selected=pressed=false;if(button!=null)transform.localScale=originalScale;}
        void OnDestroy(){if(button!=null)button.onClick.RemoveListener(Click);}
    }
}
