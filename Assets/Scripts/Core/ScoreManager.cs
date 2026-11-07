using UnityEngine;

namespace CountToZero
{
    public sealed class ScoreManager
    {
        public int Score { get; private set; }
        public float Combo { get; private set; } = 1;
        public float Focus { get; set; }
        int phaseStart;
        bool phaseHit;
        public string Grade => Score >= 80000 ? "S" : Score >= 40000 ? "A" : Score >= 18000 ? "B" : "C";
        public void Award(int points, float difficulty = 1, int bonus = 0) { Score += Mathf.RoundToInt(points * difficulty * Combo) + bonus; }
        public void Graze() { Award(50); Combo = Mathf.Min(10, Combo + .1f); Focus = Mathf.Min(1, Focus + .06f); }
        public void Hit() { Combo = 1; phaseHit = true; }
        public void CompletePhase()
        {
            if (!phaseHit) { Award(10000); Score = phaseStart + Mathf.RoundToInt((Score - phaseStart) * 1.5f); }
            phaseHit = false; phaseStart = Score;
        }
    }
}
