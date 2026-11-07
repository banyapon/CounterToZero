using UnityEngine;

namespace CountToZero
{
    public sealed class BossController : NumberCarrier
    {
        public float Health = 99, ShieldUntil, CrashUntil;
        public string PhaseName => Game.Phase == 1 ? "THE SENTINEL" : Game.Phase == 2 ? "SPIRAL PROTOCOL" : "ENTROPY ENGINE";
        public bool Sucking => Game.Elapsed % 9 > 5 && Time.time >= CrashUntil;
        float fireAt, angle;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] TextMesh label;
        [SerializeField] Transform spawnPoint;
        Vector3 visualOrigin;
        void Awake() { visualOrigin = sprite.transform.localPosition; ResetRun(); }
        public void ResetRun()
        {
            Health = 99; ShieldUntil = CrashUntil = fireAt = angle = 0;
            SetValue(50); transform.position = spawnPoint.position;
        }
        void Update()
        {
            label.text = "OMEGA-99  [" + CurrentValue + "]";
            sprite.color = CrashUntil > Time.time ? ArenaVisuals.Green : ShieldUntil > Time.time ? ArenaVisuals.Violet : ArenaVisuals.Red;
            if (Game.State != GameState.Playing || Time.time < CrashUntil) return;
            transform.position = spawnPoint.position + new Vector3(Mathf.Sin(Game.Elapsed*.37f)*3.8f,0,Mathf.Cos(Game.Elapsed*.5f)*.6f);
            sprite.transform.localPosition = visualOrigin + Vector3.up * (Mathf.Sin(Game.Elapsed*2)*.12f);
            if (Time.time < fireAt) return;
            int phase = Game.Phase;
            fireAt = Time.time + (phase == 1 ? .95f : phase == 2 ? .6f : .4f);
            float speed = (phase == 1 ? 3.2f : 4) * (ShieldUntil > Time.time ? 1.5f : 1);
            angle += phase == 1 ? 11 : 23;
            Game.Bullets.Ring(transform.position,phase == 1 ? 12 : 16,angle,speed);
            if (phase >= 2)
            {
                Vector3 aim = (Game.Player.transform.position-transform.position).normalized;
                for (int i=-1;i<=1;i++) Game.Bullets.Fire(transform.position,Quaternion.Euler(0,i*13,0)*aim,true,speed+1);
            }
        }
    }
}
