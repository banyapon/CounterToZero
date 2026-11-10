using System.Collections.Generic;
using UnityEngine;

namespace CountToZero
{
    public enum BossShape { Omega, Serpent, Skull, Flail, Storm, Walker, HeavyWalker }

    /// Cycles the boss through its looks: the classic OMEGA diamond, the virus-cyborg serpent,
    /// the skull diamond, two giants (chain-flail arms / grey-blue whip arms that slam a shock line)
    /// and the minion robots (8walk / 8walk_heavy) blown up x4.
    /// A new shape every phase; each run starts one step further along the cycle.
    [DefaultExecutionOrder(50)]
    public sealed class BossForm : MonoBehaviour
    {
        public const int SnakePattern = 21;
        [SerializeField] BossController boss;
        [SerializeField] SpriteRenderer omega;
        [Header("Serpent")]
        [SerializeField] SpriteRenderer head;
        [SerializeField] Sprite[] heads;
        [SerializeField] Sprite body;
        [SerializeField] int segmentCount = 6;
        [SerializeField] float headScale = 2.5f, bodyScale = 1.7f, tailScale = 1f, spacing = .95f;
        [Header("Skull")]
        [SerializeField] Sprite skull;
        [Header("Giants")]
        [SerializeField] SpriteRenderer giant;
        [SerializeField] Sprite flailBody, stormBody, link, stormLink, mace, prong;
        [SerializeField] Sprite[] shock;
        [SerializeField] float giantScale = .85f; // 16 PPU x .85 = the hero's pixel size
        [Header("Walkers (minion robots x4)")]
        [SerializeField] Sprite[] walkFronts, walkBacks, heavyFronts, heavyBacks, scoutFronts, scoutBacks;
        [SerializeField] float giantRobotScale = 2; // giants = 8walk_heavy (flail) / 8walk_scout (storm) at x2
        [SerializeField] float walkerScale = 3.4f, walkerFps = 6;
        float walkClock, omegaY;
        [Header("Armored Omega")]
        [SerializeField] Material laserMaterial;
        OmegaLaser laser;
        /// Levels (campaign) where Omega starts the fight and changes form each phase.
        public static bool MorphLevel(int level) => level > 1 && level % 5 == 0;

        public Sprite Body => body;
        public BossShape Shape { get; private set; }
        public bool Serpent => Shape == BossShape.Serpent;
        readonly List<SpriteRenderer> segments = new();
        GiantArms arms;
        Sprite omegaSprite;
        int runs, formKey = int.MinValue;
        float headAngle, omegaScale = 1;
        Vector3 lastPosition;

        void Awake()
        {
            for (int i = 0; i < segmentCount; i++)
            {
                var s = new GameObject("Serpent body " + (i + 1)).AddComponent<SpriteRenderer>();
                s.transform.SetParent(transform.parent, false);
                s.sprite = body; s.sortingOrder = 2; s.enabled = false;
                s.transform.localScale = Vector3.one * Mathf.Lerp(bodyScale, tailScale, i / Mathf.Max(1f, segmentCount - 1));
                segments.Add(s);
            }
            omegaScale = Mathf.Max(.01f, omega.transform.localScale.x);
            omegaSprite = omega.sprite; omegaY = omega.transform.localPosition.y;
            head.sortingOrder = 3; head.enabled = false; lastPosition = transform.position;
            if (giant != null)
            {
                giant.sortingOrder = 3; giant.enabled = false;
                arms = new GiantArms(transform.parent, giantScale, shock);
            }
            if (laserMaterial != null) laser = new OmegaLaser(transform.parent, laserMaterial);
        }

        /// Called by BossController.ResetRun.
        public void ResetRun() { runs++; formKey = int.MinValue; headAngle = 0; lastPosition = transform.position; arms?.ResetRun(); laser?.ResetRun(); }

        static int Mod(int a, int n) => ((a % n) + n) % n;

        void LateUpdate()
        {
            var g = GameManager.Instance;
            if (g == null) return;
            bool ending = g.State == GameState.Victory || g.State == GameState.Won;
            int seed = Campaign.Active ? Campaign.Data.level : runs;
            int level = Campaign.Active ? Campaign.Data.level : 1;
            bool snakePattern = !g.Training && boss.PatternIndex == SnakePattern;
            bool armored = !g.Training && boss.ArmoredPattern;   // lucky-armor patterns: Omega alone, laser attack
            bool morph = MorphLevel(level);
            // Level 1 and armored levels: Omega only. Morph levels: Omega first, a new form each phase.
            // Every other level: one random form for the whole fight (stable on retry).
            int key = snakePattern ? -1 : armored || level == 1 || g.Training ? -2 : morph ? seed * 10 + g.Phase : seed * 10;
            if (key != formKey)
            {
                bool first = formKey == int.MinValue;
                formKey = key;
                int count = giant == null ? 3 : walkFronts != null && walkFronts.Length > 0 ? 7 : 5;
                var rng = new System.Random(seed * 7919 + (morph ? g.Phase * 104729 : 0) + (Campaign.Active ? Campaign.Data.hazardSeed : 0));
                var shape = snakePattern ? BossShape.Serpent
                    : key == -2 ? BossShape.Omega
                    : morph ? (g.Phase <= 1 ? BossShape.Omega : (BossShape)(1 + rng.Next(count - 1)))
                    : (BossShape)rng.Next(count);
                head.sprite = heads[Mod(seed + g.Phase, heads.Length)];
                if (shape != Shape && !first) BattleExpansion.Instance?.Burst(omega.transform.position, Color.white, true, 30);
                if (shape == BossShape.Serpent && (Shape != BossShape.Serpent || first)) LayOutChain();
                Shape = shape;
                if (Shape == BossShape.Flail) arms.Configure(false, link, mace);
                if (Shape == BossShape.Storm) arms.Configure(true, stormLink, prong);
            }

            if (laser != null)
            {
                if (!ending && key == -2 && armored && Shape == BossShape.Omega) { var rush = laser.Tick(g, transform.position); if (rush.HasValue) transform.position = rush.Value; }
                else laser.Hide();
            }

            if (ending) { head.enabled = false; foreach (var s in segments) s.enabled = false; if (giant != null) { giant.enabled = false; arms.Hide(); } return; }

            bool serpent = Shape == BossShape.Serpent, isGiant = Shape == BossShape.Flail || Shape == BossShape.Storm;
            bool walker = Shape == BossShape.Walker || Shape == BossShape.HeavyWalker;
            omega.enabled = !serpent && !isGiant && !walker;
            omega.sprite = Shape == BossShape.Skull && skull != null ? skull : omegaSprite;
            // Omega is tinted hard (state colours); drawn bosses keep their palette and only take a hint of it.
            var tint = Color.Lerp(Color.white, omega.color, .35f);
            if (Shape == BossShape.Skull) omega.color = tint;
            head.enabled = serpent;
            // BattleExpansion owns damageable segments during the snake pattern; ours are decorative.
            bool chain = serpent && !(BattleExpansion.Instance != null && BattleExpansion.Instance.SegmentsAlive > 0);
            foreach (var s in segments) s.enabled = chain;
            if (giant != null)
            {
                giant.enabled = isGiant || walker;
                if (isGiant) UpdateGiant(g, tint); else arms.Hide();
                if (walker) UpdateWalker(g, tint);
            }
            if (!serpent) { lastPosition = transform.position; return; }
            UpdateSerpent(g, tint, chain);
        }

        void UpdateGiant(GameManager g, Color tint)
        {
            bool storm = Shape == BossShape.Storm;
            bool robots = scoutFronts != null && scoutFronts.Length > 0;
            float size = robots ? giantScale * giantRobotScale : giantScale;
            if (robots) Robot(g, storm ? scoutFronts : heavyFronts, storm ? scoutBacks : heavyBacks, size, tint);
            else
            {
                giant.sprite = storm ? stormBody : flailBody;
                giant.transform.localPosition = omega.transform.localPosition + Vector3.up * .35f;
                giant.transform.localScale = Vector3.one * size;
                giant.transform.rotation = g.View.transform.rotation;
                giant.color = tint;
            }
            // 16 px robot: hands hang 6.5 px either side of centre, 2 px below it (56 px drawn giant: 20 px / 3 px above).
            float px = size / 16f;
            float shoulderX = robots ? 6.5f * px : 20 * px, shoulderY = giant.transform.position.y + (robots ? -2 : 3) * px;
            arms.Tick(g, transform.position, shoulderY, shoulderX);
        }

        void UpdateWalker(GameManager g, Color tint)
        {
            bool heavy = Shape == BossShape.HeavyWalker;
            Robot(g, heavy ? heavyFronts : walkFronts, heavy ? heavyBacks : walkBacks, walkerScale * (heavy ? 1.1f : 1f), tint);
        }

        /// Animated minion-robot sheet on the giant renderer, feet on the floor, front/back by facing.
        void Robot(GameManager g, Sprite[] fronts, Sprite[] backs, float size, Color tint)
        {
            // Facing like HeroAnimator: above the player on screen (larger Z) we see the robot's front.
            bool front = transform.position.z > g.Player.transform.position.z;
            var frames = front ? fronts : backs;
            bool moving = (transform.position - lastPosition).sqrMagnitude > .00002f;
            if (g.State == GameState.Playing && moving) walkClock += Time.deltaTime; else if (!moving) walkClock = 0;
            if (frames != null && frames.Length > 0) giant.sprite = frames[Mathf.FloorToInt(walkClock * walkerFps) % frames.Length];
            // Pivot is centred: lift by half the height so the feet stay on the floor; keep the boss bob.
            giant.transform.localPosition = new Vector3(0, size * .5f + (omega.transform.localPosition.y - omegaY), 0);
            giant.transform.localScale = Vector3.one * size;
            giant.transform.rotation = g.View.transform.rotation;
            giant.color = tint;
        }

        void UpdateSerpent(GameManager g, Color tint, bool chain)
        {
            var view = g.View.transform.rotation;
            // Head: follows the omega bob offset and turns toward its motion (or the player when idle).
            head.transform.localPosition = omega.transform.localPosition;
            head.transform.localScale = Vector3.one * headScale * (omega.transform.localScale.x / omegaScale); // follows BattleExpansion's snake shrink
            Vector3 motion = transform.position - lastPosition; lastPosition = transform.position;
            Vector3 facing = motion.sqrMagnitude > .0004f ? motion : g.Player.transform.position - transform.position;
            if (facing.sqrMagnitude > .0001f)
                headAngle = Mathf.LerpAngle(headAngle, Mathf.Atan2(facing.x, -facing.z) * Mathf.Rad2Deg, Mathf.Min(1, Time.deltaTime * 8));
            head.transform.rotation = view * Quaternion.Euler(0, 0, headAngle);
            head.color = tint;

            if (!chain) return;
            Vector3 anchor = head.transform.position;
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                Vector3 target = i == 0 ? anchor : segments[i - 1].transform.position;
                float gap = spacing * (i == 0 ? head.transform.localScale.x * .55f : s.transform.localScale.x * .8f);
                var delta = target - s.transform.position;
                if (delta.magnitude > gap) s.transform.position = target - delta.normalized * gap;
                var p = s.transform.position; p.y = Mathf.Lerp(p.y, anchor.y - .15f * i, Time.deltaTime * 4); s.transform.position = p;
                float angle = Mathf.Atan2(-delta.x, delta.z) * Mathf.Rad2Deg;
                s.transform.rotation = view * Quaternion.Euler(0, 0, angle);
                s.color = tint;
            }
        }

        void LayOutChain()
        {
            Vector3 p = omega.transform.position;
            for (int i = 0; i < segments.Count; i++)
                segments[i].transform.position = p + Vector3.forward * (i + 1) * spacing * bodyScale * .8f;
        }
    }
}
