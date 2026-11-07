using UnityEngine;
using UnityEngine.InputSystem;

namespace CountToZero
{
    public enum GameState { Title, Playing, Paused, Won, Lost }
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        public const string BestScoreKey = "CountToZero.Best.v1";
        public static GameManager Instance { get; private set; }
        [field: SerializeField] public Camera View { get; private set; }
        [field: SerializeField] public PlayerController Player { get; private set; }
        [field: SerializeField] public BossController Boss { get; private set; }
        [field: SerializeField] public OrbSpawner Orbs { get; private set; }
        [field: SerializeField] public BulletPattern Bullets { get; private set; }
        [field: SerializeField] public FeedbackManager Feedback { get; private set; }
        public LuckyRuleEngine Rules { get; } = new LuckyRuleEngine();
        public ScoreManager Score { get; private set; } = new ScoreManager();
        public LuckyRule Rule { get; private set; }
        public GameState State { get; private set; }
        public float RuleTime { get; private set; }
        public float Elapsed { get; private set; }
        public int Phase { get; private set; } = 1;
        public float SlowUntil { get; private set; }
        public float HazardUntil { get; private set; }
        public bool RecordScores { get; set; } = true;
        public Vector3 HazardPosition { get; private set; }
        [SerializeField] SpriteRenderer hazard;
        void Awake()
        {
            Instance = this; Time.timeScale = 1;
            Rule = LuckyRuleEngine.Create(RuleType.ExactMatch,7); RuleTime = 15; State = GameState.Title;
        }
        public void Begin()
        {
            Score = new ScoreManager(); Phase = 1; Elapsed = 0; SlowUntil = HazardUntil = 0;
            Bullets.Clear(); Orbs.Clear(); Player.ResetRun(); Boss.ResetRun();
            Feedback.ResetFeedback(); Rule = LuckyRuleEngine.Create(RuleType.ExactMatch,7); RuleTime = 15;
            State = GameState.Playing; Time.timeScale = 1;
            Orbs.SeedOpening(); Feedback.Announce("BREACH THE DETERMINISTIC CORE", ArenaVisuals.Cyan);
        }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame && (State == GameState.Title || State == GameState.Won || State == GameState.Lost)) Begin();
                if (keyboard.escapeKey.wasPressedThisFrame) TogglePause();
                if (keyboard.rKey.wasPressedThisFrame && (State == GameState.Won || State == GameState.Lost)) Begin();
            }
            if (State != GameState.Playing) return;
            Elapsed += Time.deltaTime; RuleTime -= Time.deltaTime;
            if (RuleTime <= 0) ShiftRule();
            bool activeHazard = Time.time < HazardUntil;
            hazard.gameObject.SetActive(activeHazard);
            if (activeHazard && Vector3.Distance(Player.transform.position, HazardPosition) < 2.3f) Player.Hurt();

        }
        public void TogglePause()
        {
            if (State == GameState.Playing) State = GameState.Paused;
            else if (State == GameState.Paused) State = GameState.Playing;
        }
        public void ShiftRule()
        {
            Rule = Rules.GenerateNewRule(Phase); RuleTime = 15;
            Feedback.Announce("RULE SHIFT  /  " + Rule.Label, ArenaVisuals.Gold);
        }
        public void Match(NumberCarrier carrier)
        {
            if (State != GameState.Playing) return;
            bool player = carrier == Player;
            bool exact = Rule.Type == RuleType.ExactMatch;
            Feedback.Lucky(carrier.transform.position, player);
            if (player)
            {
                Score.Award(1000, Rule.DifficultyMultiplier, RuleTime >= 12 ? 2500 : 0);
                if (exact) { Bullets.ClearEnemy(); Feedback.Laser(Player.transform.position, Boss.transform.position); DamageBoss(19, true); }
                else Player.OverchargeUntil = Time.time + 5;
            }
            else if (exact) Player.Hurt(true);
            else Boss.ShieldUntil = Time.time + 5;
            carrier.SetValue(player ? 1 : 50);
            if (State != GameState.Playing) return;
            if (exact) ShiftRule(); else Orbs.Reroll();
        }
        public void Overflow(NumberCarrier carrier)
        {
            bool player = carrier == Player;
            carrier.SetValue(player ? 1 : 50);
            if (player) Player.Hurt(true); else DamageBoss(99 * .05f, true);
            HazardPosition = carrier.transform.position; HazardPosition = new Vector3(HazardPosition.x,.05f,HazardPosition.z);
            hazard.transform.position = HazardPosition; HazardUntil = Time.time + 4;
            Feedback.Announce("OVERFLOW / UNSTABLE SECTOR", ArenaVisuals.Red);
        }
        public void Zero(NumberCarrier carrier)
        {
            if (carrier == Player) { Player.StunUntil = Time.time + 1.5f; Feedback.Announce("ZERO OUT / STUNNED", ArenaVisuals.Gold); }
            else { Boss.CrashUntil = Time.time + 5; Feedback.Announce("SYSTEM CRASH / DOUBLE DAMAGE", ArenaVisuals.Green); }
            Orbs.DropRewards(carrier.transform.position);
        }
        public void DamageBoss(float amount, bool bypass = false)
        {
            if (State != GameState.Playing) return;
            if (!bypass && Boss.ShieldUntil > Time.time && Boss.CrashUntil <= Time.time)
            {
                Bullets.Fire(Boss.transform.position, (Player.transform.position - Boss.transform.position).normalized, true, 7);
                return;
            }
            Boss.Health = Mathf.Max(0, Boss.Health - amount * (Boss.CrashUntil > Time.time ? 2 : 1));
            if (Boss.Health > 99 - Phase * 33) return;
            Score.CompletePhase();
            if (Phase == 3) { Finish(true); return; }
            Phase++; Bullets.ClearEnemy(); Boss.CrashUntil = Time.time + 2; Player.Heal();
            ShiftRule(); Feedback.Announce("PHASE 0" + Phase + " / " + Boss.PhaseName, ArenaVisuals.Violet);
        }
        public void Focus()
        {
            if (State == GameState.Playing && Score.Focus >= 1) { Score.Focus = 0; SlowUntil = Time.unscaledTime + 3; Feedback.Announce("BULLET TIME", ArenaVisuals.Cyan); }
        }
        public void Finish(bool won)
        {
            State = won ? GameState.Won : GameState.Lost;
            if (won) Score.Award(Mathf.Max(0, 15000 - Mathf.RoundToInt(Elapsed * 50)));
            int best = PlayerPrefs.GetInt(BestScoreKey, 0);
            if (RecordScores && Score.Score > best) { PlayerPrefs.SetInt(BestScoreKey, Score.Score); PlayerPrefs.Save(); }
        }
        void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; }
    }
}
