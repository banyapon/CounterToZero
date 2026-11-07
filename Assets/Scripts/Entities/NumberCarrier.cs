using System;
using UnityEngine;

namespace CountToZero
{
    public abstract class NumberCarrier : MonoBehaviour
    {
        public int CurrentValue { get; protected set; } = 1;
        public event Action<int,int> OnValueChanged;
        public event Action OnLuckyMatchTriggered;
        public event Action OnOverflowTriggered;
        protected GameManager Game => GameManager.Instance;
        public void SetValue(int value)
        {
            int old = CurrentValue; CurrentValue = value; OnValueChanged?.Invoke(old, value);
        }
        public virtual void ApplyMathOperation(MathOpType opType, int operand)
        {
            if (Game.State != GameState.Playing) return;
            int next = LuckyRuleEngine.Calculate(CurrentValue, opType, operand);
            SetValue(next);
            if (Math.Abs(next) > 99) { OnOverflowTriggered?.Invoke(); Game.Overflow(this); }
            else if (Game.Rules.Evaluate(next, Game.Rule)) { OnLuckyMatchTriggered?.Invoke(); Game.Match(this); }
            else if (next == 0) Game.Zero(this);
        }
    }
}
