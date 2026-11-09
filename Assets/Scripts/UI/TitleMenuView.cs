using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace CountToZero
{
    // Scene-authored title pages: every button and slider is editable before Play.
    public sealed class TitleMenuView : MonoBehaviour
    {
        [SerializeField] GameObject home, difficulty, options, credits, ranks;
        [SerializeField] Button newGame, normal, optionsBack, creditsBack;
        [SerializeField] Selectable ranksFocus;
        [SerializeField] Slider music, sfx;
        [SerializeField] TextAsset creditSource;
        [SerializeField] TMP_Text creditText;
        [SerializeField] ScrollRect creditScroll;
        bool creditsFromOptions;
        void Start()
        {
            music.SetValueWithoutNotify(GameAudio.MusicVolume);
            sfx.SetValueWithoutNotify(GameAudio.EffectsVolume);
            music.onValueChanged.AddListener(SetMusic);
            sfx.onValueChanged.AddListener(SetSfx);
            if(creditSource!=null)creditText.text=creditSource.text;
            creditText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(420,creditText.GetPreferredValues(creditText.text,creditText.rectTransform.rect.width,0).y+30));
        }
        void SetMusic(float value){GameAudio.SetMusic(value);PlayerPrefs.Save();}
        void SetSfx(float value){GameAudio.SetEffects(value);PlayerPrefs.Save();GameAudio.Play(SoundCue.UIClick);}
        void Show(GameObject page,Selectable selected)
        {
            home.SetActive(page==home);difficulty.SetActive(page==difficulty);options.SetActive(page==options);credits.SetActive(page==credits);
            if(ranks!=null)ranks.SetActive(page==ranks);
            if(selected!=null)EventSystem.current?.SetSelectedGameObject(selected.gameObject);
        }
        public void ShowDifficulty()=>Show(difficulty,normal);
        public void ShowOptions()=>Show(options,music);
        public void ShowRanks(){if(ranks!=null)Show(ranks,ranksFocus);}
        public void ShowCredits(){creditsFromOptions=options.activeSelf;Show(credits,creditsBack);Canvas.ForceUpdateCanvases();creditScroll.verticalNormalizedPosition=1;}
        public void ReturnHome()=>Show(home,newGame);
        public void BackFromCredits(){if(creditsFromOptions)Show(options,optionsBack);else ReturnHome();}
        void Update()
        {
            // Credits remain readable without a mouse or scroll wheel.
            var keys=Keyboard.current;
            if(credits.activeSelf && keys!=null){float direction=(keys.upArrowKey.isPressed?1:0)-(keys.downArrowKey.isPressed?1:0);creditScroll.verticalNormalizedPosition=Mathf.Clamp01(creditScroll.verticalNormalizedPosition+direction*Time.unscaledDeltaTime*.35f);}
            if(EventSystem.current!=null && EventSystem.current.currentSelectedGameObject==null)
            {
                Selectable selected=home.activeSelf?newGame:difficulty.activeSelf?normal:options.activeSelf?music:credits.activeSelf?creditsBack:ranksFocus;
                if(selected!=null)EventSystem.current.SetSelectedGameObject(selected.gameObject);
            }
            if((Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)||(Gamepad.current!=null&&Gamepad.current.buttonEast.wasPressedThisFrame))
            {if(credits.activeSelf)BackFromCredits();else if(!home.activeSelf)ReturnHome();}
        }
    }
}
