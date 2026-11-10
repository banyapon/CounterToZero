using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CountToZero
{
    public sealed class OptionsController : MonoBehaviour
    {
        [SerializeField] PlayerInputRouter input;
        [SerializeField] GameObject panel;
        [SerializeField] Button open, close, reset, cancel, pause;
        [SerializeField] Button[] bindingButtons;
        [SerializeField] TMP_Text[] bindingLabels;
        [SerializeField] TMP_Text listeningLabel;
        [SerializeField] Slider musicVolume,sfxVolume;
        [SerializeField] Button title;
        Button credits;
        public Transform PanelTransform => panel.transform;
        public Button OpenButton => open;
        void Awake()
        {
            if(musicVolume!=null){musicVolume.SetValueWithoutNotify(GameAudio.MusicVolume);musicVolume.onValueChanged.AddListener(GameAudio.SetMusic);}
            if(sfxVolume!=null){sfxVolume.SetValueWithoutNotify(GameAudio.EffectsVolume);sfxVolume.onValueChanged.AddListener(v=>{GameAudio.SetEffects(v);GameAudio.Play(SoundCue.Pickup);});}
            if(title!=null)title.onClick.AddListener(()=>GameManager.Instance.ReturnToTitle());
            open.onClick.AddListener(Open);close.onClick.AddListener(Close);
            reset.onClick.AddListener(()=>input.RestoreDefaults());cancel.onClick.AddListener(input.CancelListen);
            pause.onClick.AddListener(()=>GameManager.Instance.TogglePause());
            for(int i=0;i<bindingButtons.Length;i++){int index=i;bindingButtons[i].onClick.AddListener(()=>input.Listen(index));}
            // A real opaque modal: sliced panel artwork has a transparent center.
            if(panel.TryGetComponent<Image>(out var backing)){backing.sprite=null;backing.color=new Color(.02f,.035f,.06f,.98f);}
            ArrangeFooter();
            panel.SetActive(false);
        }
        public void RegisterCredits(Button button){credits=button;ArrangeFooter();}
        void ArrangeFooter()
        {
            var buttons=new[]{reset,cancel,credits,title,close};
            for(int i=0;i<buttons.Length;i++)
            {
                if(buttons[i]==null)continue;
                var rect=(RectTransform)buttons[i].transform;
                rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
                rect.anchoredPosition=new Vector2((i-2)*220,-260);
                rect.sizeDelta=new Vector2(200,48);
                rect.localScale=Vector3.one;
                var label=buttons[i].GetComponentInChildren<TMP_Text>();
                if(label!=null){label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(8,4);label.rectTransform.offsetMax=new Vector2(-8,-4);}
            }
        }
        public void Open()
        {
            if(GameManager.Instance.State==GameState.Intro)return;
            if(GameManager.Instance.State==GameState.Playing)GameManager.Instance.TogglePause();
            input.OptionsOpen=true;open.interactable=pause.interactable=false;open.gameObject.SetActive(false);pause.gameObject.SetActive(false);panel.transform.SetAsLastSibling();panel.SetActive(true);EventSystem.current?.SetSelectedGameObject(bindingButtons[0].gameObject);
        }
        public void Close()
        {PlayerPrefs.Save();input.CancelListen();input.OptionsOpen=false;open.interactable=pause.interactable=true;panel.SetActive(false);open.gameObject.SetActive(true);pause.gameObject.SetActive(true);EventSystem.current?.SetSelectedGameObject(open.gameObject);}
        void Update()
        {
            var state=GameManager.Instance.State;
            open.gameObject.SetActive(!input.OptionsOpen&&(state==GameState.Paused||state==GameState.Lost||state==GameState.Title));
            pause.gameObject.SetActive(!input.OptionsOpen);
            pause.interactable=!input.OptionsOpen&&state!=GameState.Intro;
            if(!panel.activeSelf)return;
            for(int i=0;i<bindingLabels.Length;i++)bindingLabels[i].text=((ControlAction)i)+" : "+input.GetBinding(i);
            listeningLabel.text=input.ListenStatus;
            // Prevent the listening keystroke from submitting a focused uGUI button again.
            foreach(var button in bindingButtons)button.interactable=!input.Listening;
            if(input.PausePressed && !input.Listening)Close();
        }
    }
}
