using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CountToZero
{
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] TMP_Text bossTitle, bossHealth, score, rule, ruleClock, currentValue, integrity;
        [SerializeField] TMP_Text dashStatus, focusStatus, announcement, status;
        [SerializeField] Image healthFill, ruleFill, dashFill, focusFill;
        [SerializeField] GameObject overlay;
        [SerializeField] TMP_Text overlayTitle, overlayBody, actionLabel;
        [SerializeField] Button actionButton;
        [SerializeField] Button returnToTitle;
        Image menuCard; Sprite normalCardSprite; Color normalCardColor; Outline cardOutline; Color normalOutline;
        GameState previousState = (GameState)(-1);
        OptionsController options;
        readonly System.Collections.Generic.List<Button> menu = new();
        void Awake() { actionButton.onClick.AddListener(Activate);
            options=FindAnyObjectByType<OptionsController>();
            // Menu buttons are stepped with Up/Down by MenuKeys(), so uGUI's own spatial navigation must not move them twice.
            foreach(var b in MenuButtons())b.navigation=new Navigation{mode=Navigation.Mode.None};
            var backdrop=overlay.GetComponent<Image>();backdrop.sprite=null;backdrop.color=new Color(0,0,0,.4f);
            menuCard=overlay.transform.Find("Menu Card").GetComponent<Image>();normalCardSprite=menuCard.sprite;normalCardColor=menuCard.color;
            cardOutline=menuCard.GetComponent<Outline>();if(cardOutline!=null)normalOutline=cardOutline.effectColor;
        }
        public void SelectAction() { EventSystem.current?.SetSelectedGameObject(actionButton.gameObject); }
        void OnDestroy() { actionButton.onClick.RemoveListener(Activate); }
        void Activate()
        {
            var g = GameManager.Instance;
            if (g.Controls.OptionsOpen || g.Controls.Listening) return;
            if (g.State == GameState.Paused) g.TogglePause(); else g.Begin();
        }
        System.Collections.Generic.IEnumerable<Button> MenuButtons()
        {
            yield return actionButton;
            if(options!=null&&options.OpenButton!=null)yield return options.OpenButton;
            if(returnToTitle!=null)yield return returnToTitle;
        }
        // Pause / result menu: Up and Down (arrows, W/S, d-pad, stick) cycle RESUME / OPTIONS / TITLE; Enter or A presses.
        void MenuKeys(GameManager g)
        {
            menu.Clear();
            bool open=overlay.activeSelf&&!g.Controls.OptionsOpen&&!g.Controls.Listening;
            foreach(var b in MenuButtons()){bool on=open&&b.gameObject.activeInHierarchy&&b.IsInteractable();if(on)menu.Add(b);}
            var events=EventSystem.current;
            int index=-1;
            for(int i=0;i<menu.Count;i++)if(events!=null&&events.currentSelectedGameObject==menu[i].gameObject)index=i;
            var k=Keyboard.current;var pad=Gamepad.current;
            int step=0;
            if(k!=null){if(k.upArrowKey.wasPressedThisFrame||k.wKey.wasPressedThisFrame)step--;if(k.downArrowKey.wasPressedThisFrame||k.sKey.wasPressedThisFrame)step++;}
            if(pad!=null){if(pad.dpad.up.wasPressedThisFrame||pad.leftStick.up.wasPressedThisFrame)step--;if(pad.dpad.down.wasPressedThisFrame||pad.leftStick.down.wasPressedThisFrame)step++;}
            if(menu.Count>0&&events!=null)
            {
                if(index<0&&(step!=0||events.currentSelectedGameObject==null)){index=0;step=0;events.SetSelectedGameObject(menu[0].gameObject);}
                if(step!=0){index=(index+step+menu.Count)%menu.Count;events.SetSelectedGameObject(menu[index].gameObject);GameAudio.Play(SoundCue.UIHover);}
            }
            // Selected entry grows slightly so the choice reads clearly on every skin.
            foreach(var b in MenuButtons()){float target=menu.Contains(b)&&index>=0&&menu[index]==b?1.08f:1f;b.transform.localScale=Vector3.Lerp(b.transform.localScale,Vector3.one*target,Mathf.Min(1,Time.unscaledDeltaTime*14));}
        }
        void Update()
        {
            var g = GameManager.Instance;
            if(returnToTitle!=null)returnToTitle.gameObject.SetActive(!g.Controls.OptionsOpen&&(g.State==GameState.Paused||g.State==GameState.Lost));
            if(EventSystem.current!=null)EventSystem.current.sendNavigationEvents=g.State!=GameState.Playing||g.Controls.OptionsOpen;
            int level=Campaign.Active?Campaign.Data.level:1;
            bossTitle.text = g.Training ? "TRAINING" : "LEVEL "+level.ToString("00")+" / TURN "+((level-1)%20+1).ToString("00");
            score.text = "SCORE / x" + g.Score.Combo.ToString("0.0") + "\n" + g.Score.Score.ToString("000000");
            rule.text = g.Rule.Label; ruleClock.text = "LUCKY RULE / " + Mathf.CeilToInt(g.RuleTime) + "s";
            ruleFill.fillAmount = g.RuleTime / 15;
            currentValue.text = g.Player.CurrentValue.ToString("+0;-0;0");
            integrity.text = "PLAYER HP";
            dashFill.fillAmount = 1 - g.Player.DashCooldown / 3;
            dashStatus.text = g.Player.SilenceUntil > Time.time ? "JINX / DASH LOCKED" : g.Player.DashCooldown > 0 ? g.Player.DashCooldown.ToString("0.0") + "s" : "READY / FLIPS YOUR SIGN";
            focusFill.fillAmount = g.Score.Focus;
            focusStatus.text = g.SlowUntil > Time.unscaledTime ? "BULLET TIME ACTIVE" : "GRAZE BULLETS TO CHARGE";
            announcement.text = Time.unscaledTime < g.Feedback.MessageUntil ? g.Feedback.Message : "";
            announcement.color = g.Feedback.MessageColor;
            status.text = g.Boss.LuckyNumberOnly ? "LUCKY NUMBER ONLY\nSHOTS BLOCKED" : g.Boss.Sucking ? "WARNING / ORB SIPHON" : g.Player.StunUntil > Time.time ? "ZERO OUT / STUNNED" : g.Player.OverchargeUntil > Time.time ? "OVERCHARGED / HOMING SHOTS" : (BattleExpansion.Instance!=null && BattleExpansion.Instance.SegmentsAlive>0?"BREAK VIRUS SEGMENTS / "+BattleExpansion.Instance.SegmentsAlive:g.Hazards!=null?g.Hazards.Label:"");
            overlay.SetActive(g.State != GameState.Playing && g.State != GameState.Intro && g.State != GameState.Victory && !g.Controls.OptionsOpen);
            actionButton.interactable=!g.Controls.OptionsOpen;
            if (g.State != previousState)
            {
                previousState=g.State;
                bool dead=g.State==GameState.Lost;
                // An opaque red result card; the world keeps its original colors.
                menuCard.sprite=dead?null:normalCardSprite;menuCard.color=dead?new Color(.34f,.025f,.055f,1):normalCardColor;
                if(cardOutline!=null)cardOutline.effectColor=dead?new Color(1,.16f,.25f):normalOutline;
                overlayTitle.color=dead?new Color(1,.77f,.79f):Color.white;
                if (g.State!=GameState.Playing && g.State!=GameState.Intro && g.State!=GameState.Victory && !g.Controls.OptionsOpen) SelectAction();
                else EventSystem.current?.SetSelectedGameObject(null);
            }
            if (g.State == GameState.Playing) return;
            MenuKeys(g);
            bool title = g.State == GameState.Title, paused = g.State == GameState.Paused;
            overlayTitle.text = title ? "COUNT TO ZERO" : paused ? "SYSTEM PAUSED" : g.State == GameState.Won ? "CORE LIBERATED" : "SIGNAL LOST";
            overlayBody.text = title ? "Outcalculate OMEGA-99. Survive the bullet storm.\n\n01   Collect orbs to match the golden Lucky Rule.\n\n02   Shoot +/- orbs to flip them. Deny the boss its orbs.\n\n03   Dash flips your sign. Zero stuns. +/-100 explodes.\n\nExact match = laser. Other matches = homing fire for 5s."
                : paused ? "Controls can be changed in OPTIONS.\n\nGamepad: left stick move / right stick aim / RT fire\nA: dash / X: bullet time / Start: pause\n\nTouch: joystick + FIRE / DASH / FOCUS buttons."
                : (g.State==GameState.Lost?"LAST HIT: "+g.Player.LastDamageCause+"\nRetry starts immediately.\n\n":"")+"SCORE  " + g.Score.Score.ToString("N0") + "     RANK  " + g.Score.Grade + "\n\nTIME  " + g.Elapsed.ToString("0.0") + "s    PHASE  " + g.Phase + " OF 3\n\nPERSONAL BEST  " + PlayerPrefs.GetInt(GameManager.BestScoreKey, 0).ToString("N0") + (CloudRanks.PlayerRank > 0 ? "\nCLOUD RANK  #" + CloudRanks.PlayerRank.ToString("00") : "");
            actionLabel.text = title ? "ENTER THE CORE  [ ENTER ]" : paused ? "RESUME  [ ESC ]" : "REBOOT RUN  [ ENTER / R ]";
        }
    }
}
