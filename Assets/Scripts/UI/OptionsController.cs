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
        public Transform PanelTransform => panel.transform;
        void Awake()
        {
            if(musicVolume!=null){musicVolume.SetValueWithoutNotify(GameAudio.MusicVolume);musicVolume.onValueChanged.AddListener(GameAudio.SetMusic);}
            if(sfxVolume!=null){sfxVolume.SetValueWithoutNotify(GameAudio.EffectsVolume);sfxVolume.onValueChanged.AddListener(v=>{GameAudio.SetEffects(v);GameAudio.Play(SoundCue.Pickup);});}
            if(title!=null)title.onClick.AddListener(()=>GameManager.Instance.ReturnToTitle());
            open.onClick.AddListener(Open);close.onClick.AddListener(Close);
            reset.onClick.AddListener(()=>input.RestoreDefaults());cancel.onClick.AddListener(input.CancelListen);
            pause.onClick.AddListener(()=>GameManager.Instance.TogglePause());
            for(int i=0;i<bindingButtons.Length;i++){int index=i;bindingButtons[i].onClick.AddListener(()=>input.Listen(index));}
            panel.SetActive(false);
        }
        public void Open()
        {
            if(GameManager.Instance.State==GameState.Intro)return;
            if(GameManager.Instance.State==GameState.Playing)GameManager.Instance.TogglePause();
            input.OptionsOpen=true;open.interactable=pause.interactable=false;panel.SetActive(true);EventSystem.current?.SetSelectedGameObject(bindingButtons[0].gameObject);
        }
        public void Close()
        {PlayerPrefs.Save();input.CancelListen();input.OptionsOpen=false;open.interactable=pause.interactable=true;panel.SetActive(false);EventSystem.current?.SetSelectedGameObject(open.gameObject);}
        void Update()
        {
            var state=GameManager.Instance.State;
            open.gameObject.SetActive(state==GameState.Paused||state==GameState.Lost||state==GameState.Title);
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
