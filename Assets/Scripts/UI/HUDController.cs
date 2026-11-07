using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
        GameState previousState = (GameState)(-1);
        void Awake() { actionButton.onClick.AddListener(Activate); }
        public void SelectAction() { EventSystem.current?.SetSelectedGameObject(actionButton.gameObject); }
        void OnDestroy() { actionButton.onClick.RemoveListener(Activate); }
        void Activate()
        {
            var g = GameManager.Instance;
            if (g.Controls.OptionsOpen || g.Controls.Listening) return;
            if (g.State == GameState.Paused) g.TogglePause(); else g.Begin();
        }
        void Update()
        {
            var g = GameManager.Instance;
            if(returnToTitle!=null)returnToTitle.gameObject.SetActive(g.State==GameState.Paused||g.State==GameState.Lost);
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
            status.text = g.Boss.Sucking ? "WARNING / ORB SIPHON" : g.Player.StunUntil > Time.time ? "ZERO OUT / STUNNED" : g.Player.OverchargeUntil > Time.time ? "OVERCHARGED / HOMING SHOTS" : "";
            overlay.SetActive(g.State != GameState.Playing && g.State != GameState.Intro && !g.Controls.OptionsOpen);
            actionButton.interactable=!g.Controls.OptionsOpen;
            if (g.State != previousState)
            {
                previousState=g.State;
                if (g.State!=GameState.Playing && g.State!=GameState.Intro && !g.Controls.OptionsOpen) SelectAction();
                else EventSystem.current?.SetSelectedGameObject(null);
            }
            if (g.State == GameState.Playing) return;
            bool title = g.State == GameState.Title, paused = g.State == GameState.Paused;
            overlayTitle.text = title ? "COUNT TO ZERO" : paused ? "SYSTEM PAUSED" : g.State == GameState.Won ? "CORE LIBERATED" : "SIGNAL LOST";
            overlayBody.text = title ? "Outcalculate OMEGA-99. Survive the bullet storm.\n\n01   Collect orbs to match the golden Lucky Rule.\n\n02   Shoot +/- orbs to flip them. Deny the boss its orbs.\n\n03   Dash flips your sign. Zero stuns. +/-100 explodes.\n\nExact match = laser. Other matches = homing fire for 5s."
                : paused ? "Controls can be changed in OPTIONS.\n\nGamepad: left stick move / right stick aim / RT fire\nA: dash / X: bullet time / Start: pause\n\nTouch: joystick + FIRE / DASH / FOCUS buttons."
                : (g.State==GameState.Lost?"LAST HIT: "+g.Player.LastDamageCause+"\nRetry starts immediately.\n\n":"")+"SCORE  " + g.Score.Score.ToString("N0") + "     RANK  " + g.Score.Grade + "\n\nTIME  " + g.Elapsed.ToString("0.0") + "s    PHASE  " + g.Phase + " OF 3\n\nPERSONAL BEST  " + PlayerPrefs.GetInt(GameManager.BestScoreKey, 0).ToString("N0");
            actionLabel.text = title ? "ENTER THE CORE  [ ENTER ]" : paused ? "RESUME  [ ESC ]" : "REBOOT RUN  [ ENTER / R ]";
        }
    }
}
