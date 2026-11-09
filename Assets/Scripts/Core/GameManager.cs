using UnityEngine;
using UnityEngine.InputSystem;

namespace CountToZero
{
    public enum BossDamageSource { Shot, LuckyNumber, Overflow }
    public enum GameState { Title, Playing, Paused, Won, Lost, Intro, Victory }
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
        [field: SerializeField] public PlayerInputRouter Controls { get; private set; }
        [field: SerializeField] public EncounterDirector Encounters { get; private set; }
        [field: SerializeField] public DialogueController Dialogue { get; private set; }
        [field: SerializeField] public TutorialDirector Tutorial { get; private set; }
        public SectorHazards Hazards => GetComponent<SectorHazards>();
        public bool Training=>Tutorial!=null&&Tutorial.Active;
        [field: SerializeField] public LuckyCutIn CutIn { get; private set; }
        public LuckyRuleEngine Rules { get; } = new LuckyRuleEngine();
        public ScoreManager Score { get; private set; } = new ScoreManager();
        public LuckyRule Rule { get; private set; }
        public GameState State { get; private set; }
        public float RuleTime { get; private set; }
        public float Elapsed { get; private set; }
        public int DamageTaken { get; private set; }
        public void RecordDamage(){DamageTaken++;}
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
        void Start(){if(!Campaign.Active&&!Campaign.Resume())Campaign.NewGame();BeginIntro();}
        public bool CountingDown { get; private set; }
        public void BeginIntro()=>Begin();
        public void SkipIntro(){if(State!=GameState.Intro||CountingDown)return;View.GetComponent<FollowCamera>().Snap();CompleteIntro();}
        public void CompleteIntro(){if(State==GameState.Intro&&!CountingDown)StartCoroutine(StartRound());}
        System.Collections.IEnumerator StartRound()
        {
            CountingDown=true;
            var countdown=GetComponent<RoundCountdown>();
            if(countdown==null)countdown=gameObject.AddComponent<RoundCountdown>();
            yield return countdown.Play();
            CountingDown=false;
            State=GameState.Playing;
            if(Campaign.Active){Campaign.Data.introSeen=true;Campaign.Save();}
            Dialogue.Say("intro");
        }
        public void Begin()
        {
            StopAllCoroutines(); CountingDown=false; GetComponent<RoundCountdown>()?.Hide(); GetComponent<VictorySequence>()?.ResetPresentation();
            DamageTaken=0; Score = new ScoreManager(); Phase = 1; Elapsed = 0; SlowUntil = HazardUntil = 0;
            GetComponent<BattleExpansion>()?.ResetBattle();
            Bullets.Clear(); Orbs.Clear(); Player.ResetRun(); Boss.ResetRun(); if(Campaign.Active)Boss.SelectPattern(Campaign.PatternIndex); Encounters.ResetRun(); CutIn.Clear();
            Feedback.ResetFeedback(); Rule = LuckyRuleEngine.Create(RuleType.ExactMatch,7); RuleTime = 15;
            State = GameState.Intro; Time.timeScale = 1;
            View.GetComponent<FollowCamera>().Snap();
            Orbs.SeedOpening(); Tutorial?.ResetForRun(); Hazards?.ResetRun(); hazard.gameObject.SetActive(false); Feedback.Announce("BREACH THE DETERMINISTIC CORE", ArenaVisuals.Cyan);
            View.GetComponent<FollowCamera>().StartIntro();
        }
        void Update()
        {
            if(State==GameState.Paused && SlowUntil>Time.unscaledTime)SlowUntil+=Time.unscaledDeltaTime;
            if (!Controls.OptionsOpen && !Controls.Listening)
            {
                if (Controls.PausePressed) TogglePause();
                if(State==GameState.Lost&&Keyboard.current!=null&&Keyboard.current.rKey.wasPressedThisFrame)Begin();
                // uGUI owns Enter / gamepad submit, preventing a single input from starting twice.
            }
            if (State != GameState.Playing || Training) return;
            Elapsed += Time.deltaTime; RuleTime -= Time.deltaTime;
            if (RuleTime <= 0) ShiftRule();
            bool activeHazard = Time.time < HazardUntil;
            hazard.gameObject.SetActive(activeHazard);
            if (activeHazard && Vector3.Distance(Player.transform.position, HazardPosition) < 2.3f) Player.Hurt(false,"Unstable floor");

        }
        public void TogglePause()
        {
            if (Controls.OptionsOpen || Controls.Listening) return;
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
                Player.ProtectCutIn(); CutIn.Show(); Tutorial?.LuckyMatched(); Dialogue.Say("lucky");
                Score.Award(1000, Rule.DifficultyMultiplier, RuleTime >= 12 ? 2500 : 0);
                Feedback.Laser(Player.transform.position, Boss.transform.position);
                if (exact) { Bullets.ClearEnemy(); DamageBoss(19, true, BossDamageSource.LuckyNumber); }
                else
                {
                    Player.OverchargeUntil = Time.time + 5;
                    if(Boss.LuckyNumberOnly)DamageBoss(10,true,BossDamageSource.LuckyNumber);
                }
            }
            else if (exact) Player.Hurt(true,"Boss Lucky counter");
            else Boss.ShieldUntil = Time.time + 5;
            carrier.SetValue(player ? 1 : 50);
            if (State != GameState.Playing) return;
            if (Training) return;
            if (exact) ShiftRule(); else Orbs.Reroll();
        }
        public void Overflow(NumberCarrier carrier)
        {
            bool player = carrier == Player;
            carrier.SetValue(player ? 1 : 50);
            if (player) Player.Hurt(true,"VALUE overflow (+/-100)"); else DamageBoss(99 * .05f, true, BossDamageSource.Overflow);
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
        public void DamageBoss(float amount, bool bypass = false, BossDamageSource source = BossDamageSource.Shot)
        {
            if (State != GameState.Playing) return;
            if(amount<=0)return;
            if(Boss.LuckyNumberOnly && source!=BossDamageSource.LuckyNumber){if(source==BossDamageSource.Shot)Boss.RejectShot();return;}
            if(GetComponent<BattleExpansion>() is BattleExpansion expansion && expansion.SegmentsAlive>0){if(bypass)expansion.DamageSegment(amount);return;}
            if (!bypass && Boss.ShieldUntil > Time.time && Boss.CrashUntil <= Time.time)
            {
                Bullets.Fire(Boss.transform.position, (Player.transform.position - Boss.transform.position).normalized, true, 7);
                return;
            }
            Boss.Health = Mathf.Max(Training?70:0, Boss.Health - amount * (Boss.CrashUntil > Time.time ? 2 : 1));
            if (Boss.Health > 99 - Phase * 33) return;
            Score.CompletePhase();
            if (Phase == 3) { Finish(true); return; }
            Phase++; GameAudio.Play(SoundCue.Phase); Dialogue.Say("phase" + Phase); Bullets.ClearEnemy(); Boss.CrashUntil = Time.time + 2; Player.Heal();
            ShiftRule(); Feedback.Announce("PHASE 0" + Phase + " / " + Boss.PhaseName, ArenaVisuals.Violet);
        }
        public void Focus()
        {
            if (State == GameState.Playing && Score.Focus >= 1) { Score.Focus = 0; SlowUntil = Time.unscaledTime + 3; Feedback.Announce("BULLET TIME", ArenaVisuals.Cyan); }
        }
        public void Finish(bool won)
        {
            if(State!=GameState.Playing)return;
            State = won ? GameState.Victory : GameState.Lost;
            hazard.gameObject.SetActive(false);
            if (won) Score.Award(Mathf.Max(0, 15000 - Mathf.RoundToInt(Elapsed * 50)));
            int best = PlayerPrefs.GetInt(BestScoreKey, 0);
            if(!won)GameAudio.Play(SoundCue.Death);
            if(Campaign.Active){Campaign.LastCause=Player.LastDamageCause;Campaign.Complete(won,Elapsed,DamageTaken,Score.Score);}
            if(won)StartCoroutine(ShowVictory());
            if (RecordScores && Score.Score > best) { PlayerPrefs.SetInt(BestScoreKey, Score.Score); PlayerPrefs.Save(); }
        }
        System.Collections.IEnumerator ShowVictory()
        {
            var presentation=GetComponent<VictorySequence>();
            if(presentation!=null)yield return presentation.Play();
            else yield return new WaitForSecondsRealtime(3);
            State=GameState.Won;
            if(Campaign.Active)ContinueCampaign();
        }
        public void ReturnToTitle(){PlayerPrefs.Save();Campaign.Leave();UnityEngine.SceneManagement.SceneManager.LoadScene("Title");}
        public void ContinueCampaign(){UnityEngine.SceneManagement.SceneManager.LoadScene("TurnEnd");}
        void OnApplicationFocus(bool focused) { if (!focused && State == GameState.Playing) TogglePause(); }
        void OnApplicationPause(bool paused) { if (paused && State == GameState.Playing) TogglePause(); }
        void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; }
    }
}
