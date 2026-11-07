using UnityEngine;
using UnityEngine.InputSystem;

namespace CountToZero
{
    public sealed class PlayerController : NumberCarrier
    {
        public int Health { get; private set; } = 5;
        public float OverchargeUntil, StunUntil, SilenceUntil;
        public float DashCooldown => Mathf.Max(0, dashReady - Time.time);
        public bool Invulnerable => Time.time < invulnerableUntil;
        float dashReady, dashUntil, invulnerableUntil, shotReady;
        Vector3 direction = Vector3.forward, dashDirection;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] TextMesh label;
        [SerializeField] Transform spawnPoint;
        Vector3 visualOrigin;
        void Awake() { visualOrigin = sprite.transform.localPosition; ResetRun(); }
        public void ResetRun()
        {
            transform.position = spawnPoint.position; Health = 5; SetValue(1);
            dashReady = dashUntil = invulnerableUntil = shotReady = OverchargeUntil = StunUntil = SilenceUntil = 0;
        }
        void Update()
        {
            label.text = CurrentValue.ToString("+0;-0;0");
            sprite.color = Invulnerable && Mathf.Sin(Time.unscaledTime*40)>0 ? Color.white : OverchargeUntil > Time.time ? ArenaVisuals.Green : ArenaVisuals.Cyan;
            if (Game.State != GameState.Playing) return;
            var controls = Game.Controls;
            Vector3 movement = controls.WorldDirection(controls.Move);
            if (movement.sqrMagnitude > .01f) direction = movement.normalized;
            if (controls.DashPressed) DashInversion();
            if (controls.FocusPressed) Game.Focus();
            if (Time.time >= StunUntil)
            {
                transform.position = Game.Encounters.Move(transform.position, (Time.time < dashUntil ? dashDirection * 24 : movement * 6) * Time.deltaTime);
                var p = transform.position; p.x = Mathf.Clamp(p.x,-10.3f,10.3f); p.z = Mathf.Clamp(p.z,-7.3f,7.3f); transform.position = p;
                if (controls.FireHeld) ModuloShot();
            }
        }
        public void DashInversion()
        {
            if (Time.time < dashReady || Time.time < SilenceUntil || Time.time < StunUntil) return;
            dashDirection = direction; dashReady = Time.time + 3; dashUntil = Time.time + .18f;
            invulnerableUntil = Time.time + .3f; ApplyMathOperation(MathOpType.Multiply,-1);
        }
        public void ModuloShot()
        {
            if (Time.time < shotReady) return;
            bool charged = Time.time < OverchargeUntil; shotReady = Time.time + (charged ? .095f : .18f);
            Vector3 aim = Game.Boss.transform.position;
            if(Game.Controls.TouchVisible)aim=Game.Encounters.AutoAim(transform.position);
            if (Game.Controls.StickAim.sqrMagnitude > .01f) aim=transform.position+Game.Controls.WorldDirection(Game.Controls.StickAim)*15;
            else if (Game.Controls.MouseAiming && Mouse.current != null)
            {
                var ray = Game.View.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray,out float distance)) aim = ray.GetPoint(distance);
            }
            Game.Bullets.Fire(transform.position,(aim-transform.position).normalized,false,16,charged);
        }
        public void Hurt(bool unavoidable = false)
        {
            if (Game.State != GameState.Playing || (!unavoidable && Invulnerable)) return;
            Health--; Game.RecordDamage(); invulnerableUntil = Time.time + 1.2f; Game.Score.Hit();
            Game.Feedback.Hit(transform.position);
            if (Health <= 0) Game.Finish(false);
        }
        public void Heal() { Health = Mathf.Min(5,Health+1); }
    }
}
