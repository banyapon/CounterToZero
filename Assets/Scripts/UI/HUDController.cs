using UnityEngine;
using UnityEngine.UI;

namespace CountToZero
{
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] Text bossTitle, bossHealth, score, rule, ruleClock, currentValue, integrity;
        [SerializeField] Text dashStatus, focusStatus, announcement, status;
        [SerializeField] Image healthFill, ruleFill, dashFill, focusFill;
        [SerializeField] GameObject overlay;
        [SerializeField] Text overlayTitle, overlayBody, actionLabel;
        [SerializeField] Button actionButton;
        void Awake() { actionButton.onClick.AddListener(Activate); }
        void OnDestroy() { actionButton.onClick.RemoveListener(Activate); }
        void Activate()
        {
            var g = GameManager.Instance;
            if (g.State == GameState.Paused) g.TogglePause(); else g.Begin();
        }
        void Update()
        {
            var g = GameManager.Instance;
            bossTitle.text = "OMEGA-99 / PHASE 0" + g.Phase + " / " + g.Boss.PhaseName;
            bossHealth.text = Mathf.CeilToInt(g.Boss.Health) + " / 99";
            healthFill.fillAmount = g.Boss.Health / 99;
            score.text = "SCORE / x" + g.Score.Combo.ToString("0.0") + "\n" + g.Score.Score.ToString("000000");
            rule.text = g.Rule.Label; ruleClock.text = "LUCKY RULE / " + Mathf.CeilToInt(g.RuleTime) + "s";
            ruleFill.fillAmount = g.RuleTime / 15;
            currentValue.text = g.Player.CurrentValue.ToString("+0;-0;0");
            integrity.text = "INTEGRITY\n" + g.Player.Health + " / 5";
            dashFill.fillAmount = 1 - g.Player.DashCooldown / 3;
            dashStatus.text = g.Player.SilenceUntil > Time.time ? "JINX / DASH LOCKED" : g.Player.DashCooldown > 0 ? g.Player.DashCooldown.ToString("0.0") + "s" : "READY / FLIPS YOUR SIGN";
            focusFill.fillAmount = g.Score.Focus;
            focusStatus.text = g.SlowUntil > Time.unscaledTime ? "BULLET TIME ACTIVE" : "GRAZE BULLETS TO CHARGE";
            announcement.text = Time.unscaledTime < g.Feedback.MessageUntil ? g.Feedback.Message : "";
            announcement.color = g.Feedback.MessageColor;
            status.text = g.Boss.Sucking ? "WARNING / ORB SIPHON" : g.Player.StunUntil > Time.time ? "ZERO OUT / STUNNED" : g.Player.OverchargeUntil > Time.time ? "OVERCHARGED / HOMING SHOTS" : "";
            overlay.SetActive(g.State != GameState.Playing);
            if (g.State == GameState.Playing) return;
            bool title = g.State == GameState.Title, paused = g.State == GameState.Paused;
            overlayTitle.text = title ? "COUNT TO ZERO" : paused ? "SYSTEM PAUSED" : g.State == GameState.Won ? "CORE LIBERATED" : "SIGNAL LOST";
            overlayBody.text = title ? "Outcalculate OMEGA-99. Survive the bullet storm.\n\n01   Collect orbs to match the golden Lucky Rule.\n\n02   Shoot +/- orbs to flip them. Deny the boss its orbs.\n\n03   Dash flips your sign. Zero stuns. +/-100 explodes.\n\nExact match = laser. Other matches = homing fire for 5s."
                : paused ? "WASD / arrows: move    Mouse + LMB: aim and fire\n\nJ: shoot boss    Space: invert dash    E: bullet time\n\nCyan = add    Gold = subtract    Red = danger"
                : "SCORE  " + g.Score.Score.ToString("N0") + "     RANK  " + g.Score.Grade + "\n\nTIME  " + g.Elapsed.ToString("0.0") + "s    PHASE  " + g.Phase + " OF 3\n\nPERSONAL BEST  " + PlayerPrefs.GetInt(GameManager.BestScoreKey, 0).ToString("N0");
            actionLabel.text = title ? "ENTER THE CORE  [ ENTER ]" : paused ? "RESUME  [ ESC ]" : "REBOOT RUN  [ ENTER / R ]";
        }
    }
}
