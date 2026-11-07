using System;

namespace CountToZero
{
    public enum RuleType { ExactMatch, EvenNumber, OddNumber, MultipleOf, PrimeNumber, GreaterThan, LessThan }
    public enum MathOpType { Add, Subtract, Multiply, Divide, Modulo, Set, Jinx }

    [Serializable]
    public struct LuckyRule
    {
        public RuleType Type;
        public int TargetValue;
        public string DescriptionTH;
        public float DifficultyMultiplier;
        public string Label => Type switch
        {
            RuleType.ExactMatch => "EXACTLY " + TargetValue,
            RuleType.EvenNumber => "EVEN NUMBER",
            RuleType.OddNumber => "ODD NUMBER",
            RuleType.MultipleOf => "MULTIPLE OF " + TargetValue,
            RuleType.PrimeNumber => "PRIME NUMBER",
            RuleType.GreaterThan => "GREATER THAN " + TargetValue,
            _ => "LESS THAN " + TargetValue
        };
    }

    // Intentionally independent of Unity: arithmetic and fairness can be tested headlessly.
    public sealed class LuckyRuleEngine
    {
        readonly Random random;
        public LuckyRuleEngine(int? seed = null) { random = seed.HasValue ? new Random(seed.Value) : new Random(); }
        public LuckyRule GenerateNewRule(int currentBossPhase)
        {
            var type = (RuleType)random.Next(currentBossPhase <= 1 ? 4 : 7);
            int target = type == RuleType.MultipleOf ? random.Next(3, 6) : random.Next(0, 11);
            return Create(type, target);
        }
        public static LuckyRule Create(RuleType type, int target = 0) => new LuckyRule
        {
            Type = type, TargetValue = target,
            DifficultyMultiplier = type == RuleType.ExactMatch ? 3 : type == RuleType.PrimeNumber ? 2.5f : type == RuleType.MultipleOf ? 1.5f : 1,
            DescriptionTH = type switch
            {
                RuleType.ExactMatch => "ทำค่าให้เท่ากับ " + target,
                RuleType.EvenNumber => "จำนวนคู่", RuleType.OddNumber => "จำนวนคี่",
                RuleType.MultipleOf => "พหุคูณของ " + target, RuleType.PrimeNumber => "จำนวนเฉพาะ",
                RuleType.GreaterThan => "มากกว่า " + target, _ => "น้อยกว่า " + target
            }
        };
        public bool Evaluate(int value, LuckyRule rule)
        {
            // Zero is a crash, except when it is explicitly the exact target.
            if (value == 0) return rule.Type == RuleType.ExactMatch && rule.TargetValue == 0;
            return rule.Type switch
            {
                RuleType.ExactMatch => value == rule.TargetValue,
                RuleType.EvenNumber => value % 2 == 0, RuleType.OddNumber => value % 2 != 0,
                RuleType.MultipleOf => rule.TargetValue != 0 && value % rule.TargetValue == 0,
                RuleType.PrimeNumber => IsPrime(value), RuleType.GreaterThan => value > rule.TargetValue,
                RuleType.LessThan => value < rule.TargetValue, _ => false
            };
        }
        public bool IsPrime(int number)
        {
            if (number < 2) return false;
            for (int i = 2; i <= number / i; i++) if (number % i == 0) return false;
            return true;
        }
        public static int Calculate(int value, MathOpType op, int operand) => op switch
        {
            MathOpType.Add => value + operand, MathOpType.Subtract => value - operand,
            MathOpType.Multiply => value * operand,
            MathOpType.Divide => operand == 0 ? value : value / operand,
            MathOpType.Modulo => operand == 0 ? value : value % operand,
            MathOpType.Set => operand, _ => value + operand
        };
        public int Distance(int value, LuckyRule rule)
        {
            int best = 1000;
            for (int i = -99; i <= 99; i++)
                if (Evaluate(i, rule)) best = Math.Min(best, Math.Abs(value - i));
            return best;
        }
        public void HelpfulOperation(int value, LuckyRule rule, out MathOpType op, out int operand)
        {
            op = MathOpType.Add; operand = 1;
            int best = int.MaxValue;
            var operations = new[] { MathOpType.Add, MathOpType.Subtract, MathOpType.Divide, MathOpType.Modulo, MathOpType.Set };
            foreach (var candidate in operations)
            {
                for (int n = 1; n <= 10; n++)
                {
                    int next = Calculate(value, candidate, n);
                    if (Math.Abs(next) > 99 || (next == 0 && !Evaluate(next, rule))) continue;
                    int distance = Distance(next, rule);
                    if (distance < best) { best = distance; op = candidate; operand = n; }
                }
            }
        }
    }
}
