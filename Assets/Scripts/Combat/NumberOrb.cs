using TMPro;
using UnityEngine;

namespace CountToZero
{
    public sealed class NumberOrb : MonoBehaviour
    {
        public MathOpType Operation;
        public int Operand;
        public int Reward;
        public bool Training;
        public bool Pulled { get; private set; }
        float age;
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] TMP_Text label;
        OrbSpawner owner;
        public void Initialize(OrbSpawner spawner)
        {
            owner = spawner;
        }
        public void Spawn(Vector3 position, MathOpType op, int operand, int reward = 0)
        {
            Training=false;transform.position = position; age = 0; Pulled = false; Reward = reward;
            SetOperation(op,operand); gameObject.SetActive(true);
        }
        public void SetOperation(MathOpType op, int operand)
        {
            Operation = op; Operand = operand;
            label.text = Reward == 1 ? "+HP" : Reward == 2 ? "xS" : op switch
            {
                MathOpType.Add => "+"+operand, MathOpType.Subtract => "-"+operand,
                MathOpType.Multiply => "x"+operand, MathOpType.Divide => "/"+operand,
                MathOpType.Modulo => "%"+operand, MathOpType.Set => "="+operand, _ => "?"
            };
            sprite.color = Reward > 0 ? ArenaVisuals.Green : op == MathOpType.Add ? ArenaVisuals.Cyan : op == MathOpType.Subtract ? ArenaVisuals.Gold : ArenaVisuals.Violet;
        }
        public void Shot()
        {
            if (Reward > 0) return;
            if (Pulled || Operation == MathOpType.Jinx) { owner.Release(this); return; }
            if (Operation == MathOpType.Add) SetOperation(MathOpType.Subtract,Operand);
            else if (Operation == MathOpType.Subtract) SetOperation(MathOpType.Add,Operand);
        }
        void Update()
        {
            var g = GameManager.Instance; if (g.State != GameState.Playing) return;
            age += Time.deltaTime;
            float lifetime = Operation == MathOpType.Multiply || Operation == MathOpType.Divide ? 5 : 18;
            if (!Training && age > lifetime) { owner.Release(this); return; }
            float bossDistance = Vector3.Distance(transform.position,g.Boss.transform.position);
            Pulled = !Training && Reward == 0 && g.Boss.Sucking && bossDistance < 5.5f;
            Vector3 target = Pulled ? g.Boss.transform.position : Operation == MathOpType.Jinx ? g.Player.transform.position : Vector3.zero;
            if (Reward == 0 && !Training)
            {
                Vector3 velocity = (target-transform.position).normalized * (Pulled ? 1.9f : Operation == MathOpType.Jinx ? 1.2f : .35f);
                if (!Pulled && (Operation == MathOpType.Multiply || Operation == MathOpType.Divide)) velocity.x += Mathf.Sin(age*5)*1.5f;
                transform.position += velocity*Time.deltaTime;
            }
            if (Vector3.Distance(transform.position,g.Player.transform.position) < .67f)
            {
                if (Reward == 1) g.Player.Heal();
                else if (Reward == 2) g.Score.Award(500);
                else
                {
                    if (Pulled && g.Rules.Evaluate(LuckyRuleEngine.Calculate(g.Boss.CurrentValue,Operation,Operand),g.Rule)) g.Score.Award(500,g.Rule.DifficultyMultiplier);
                    MathOpType op = Operation; int operand = Operand;
                    if (op == MathOpType.Jinx) { operand = Random.Range(-10,11); g.Player.SilenceUntil = Time.time+2; }
                    owner.Release(this); g.Player.ApplyMathOperation(op,operand); g.Feedback.Pickup(); return;
                }
                owner.Release(this); g.Feedback.Pickup(); return;
            }
            if (Pulled && bossDistance < .85f)
            {
                MathOpType op = Operation; int operand = Operand;
                owner.Release(this); g.Boss.ApplyMathOperation(op,operand);
            }
        }
    }
}
