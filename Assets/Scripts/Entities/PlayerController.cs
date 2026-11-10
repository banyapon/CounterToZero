using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CountToZero
{
    public sealed class PlayerController : NumberCarrier
    {
        public int ShotsFired {get;private set;}
        public int DashesUsed {get;private set;}
        public string LastDamageCause {get;private set;}="";
        float cutInProtection;
        public void ProtectCutIn(){cutInProtection=.25f;}
        public float Health { get; private set; } = 5;
        public float OverchargeUntil, StunUntil, SilenceUntil;
        public float DashCooldown => Mathf.Max(0, dashReady - Time.time);
        public bool Invulnerable => Time.time < invulnerableUntil;
        float dashReady, dashUntil, invulnerableUntil, shotReady;
        Vector3 direction = Vector3.forward, dashDirection;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] TMP_Text label;
        [SerializeField] Transform spawnPoint;
        Vector3 visualOrigin;
        void Awake() { visualOrigin = sprite.transform.localPosition; ResetRun(); }
        public void ResetRun()
        {
            sprite.enabled=true;ShotsFired=DashesUsed=0;LastDamageCause="";cutInProtection=0;transform.position = spawnPoint.position; Health = 5; SetValue(1);
            dashReady = dashUntil = invulnerableUntil = shotReady = OverchargeUntil = StunUntil = SilenceUntil = 0;
        }
        void Update()
        {
            label.text = CurrentValue.ToString("+0;-0;0");
            if(Game.State==GameState.Victory || Game.State==GameState.Won)return;
            sprite.color = Invulnerable && Mathf.Sin(Time.unscaledTime*40)>0 ? Color.white : OverchargeUntil > Time.time ? ArenaVisuals.Green : Color.white;
            if (Game.State != GameState.Playing) return;
            if(!Game.CutIn.Visible)cutInProtection=Mathf.Max(0,cutInProtection-Time.unscaledDeltaTime);
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
            if(Game.Tutorial!=null&&!Game.Tutorial.CanDash)return;
            if (Time.time < dashReady || Time.time < SilenceUntil || Time.time < StunUntil) return;
            DashesUsed++;GameAudio.Play(SoundCue.Dash);dashDirection = direction; dashReady = Time.time + 3; dashUntil = Time.time + .18f;
            invulnerableUntil = Time.time + .3f; ApplyMathOperation(MathOpType.Multiply,-1);
        }
        public void ModuloShot()
        {
            if(Game.Tutorial!=null&&!Game.Tutorial.CanFire)return;
            if (Time.time < shotReady) return;
            ShotsFired++;GameAudio.Play(SoundCue.Shoot);
            bool charged = Time.time < OverchargeUntil; shotReady = Time.time + (charged ? .095f : .18f);
            Vector3 aim = Game.Encounters.AutoAim(transform.position);
            if (Game.Controls.StickAim.sqrMagnitude > .01f) aim=transform.position+Game.Controls.WorldDirection(Game.Controls.StickAim)*15;
            else if (Game.Controls.MouseAiming && Mouse.current != null)
            {
                var ray = Game.View.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray,out float distance)) aim = ray.GetPoint(distance);
            }
            Game.Bullets.Fire(transform.position,(aim-transform.position).normalized,false,16,charged);
        }
        public void Hurt(bool unavoidable = false,string cause="Enemy projectile")
        {
            if(Game.State!=GameState.Playing)return;
            if(Game.Training){Game.Tutorial.PracticeHit();return;}
            if(Game.CutIn.Visible||cutInProtection>0||(!unavoidable&&Invulnerable))return;
            LastDamageCause=cause;
            Health--; Game.RecordDamage(); invulnerableUntil = Time.time + 1.2f; Game.Score.Hit();
            BattleExpansion.Instance?.Burst(transform.position,Color.red,false,Health<=0?64:22);Game.Feedback.Hit(transform.position);Game.Feedback.Announce("HIT / "+cause.ToUpperInvariant(),ArenaVisuals.Red);
            if (Health <= 0) {sprite.enabled=false;Game.Finish(false);}
        }
        public bool HitLaser(Vector3 pushDirection)
        {
            if(Game.State!=GameState.Playing || Game.Training || Invulnerable || Game.CutIn.Visible || cutInProtection>0)return false;
            LastDamageCause="Red laser / 50% max HP";Health=Mathf.Max(0,Health-2.5f);Game.RecordDamage();Game.Score.Hit();
            invulnerableUntil=Time.time+1.2f;StunUntil=Time.time+.2f;
            transform.position=Game.Encounters.Move(transform.position,pushDirection.normalized*2.2f);
            BattleExpansion.Instance?.Burst(transform.position,Color.red,false,Health<=0?64:22);Game.Feedback.Hit(transform.position);Game.Feedback.Announce("LASER HIT / -50% HP",ArenaVisuals.Red);
            if(Health<=0){sprite.enabled=false;Game.Finish(false);}return true;
        }
        public void Crush()
        {
            if(Game.State!=GameState.Playing || Game.Training || Game.CutIn.Visible)return;
            sprite.enabled=false;LastDamageCause="Crushed at bottom edge";Health=0;Game.RecordDamage();BattleExpansion.Instance?.Burst(transform.position,Color.red,false,Health<=0?64:22);Game.Feedback.Hit(transform.position);Game.Finish(false);
        }
        public void Heal() { if(Health<5)GameAudio.Play(SoundCue.Heal);Health = Mathf.Min(5,Health+1); }
    }
}
