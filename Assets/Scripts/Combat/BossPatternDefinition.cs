using UnityEngine;

namespace CountToZero
{
    public enum BossShot { Ring, Fan, Spiral, Cross, Wave, AlternatingRing, TwinSpiral, AimedBurst, Flower, Pinwheel, Rain, SideSweep, Vortex, DoubleFan, Lanes, Pulse, Scissors, RotatingGate, Scatter, Finale }
    public enum BossMotion { Sway, Orbit, Sweep, Pursue, FigureEight }
    [CreateAssetMenu(menuName="Count To Zero/Boss Pattern")]
    public sealed class BossPatternDefinition : ScriptableObject
    {
        public string displayName;
        public BossShot shot;
        public BossMotion motion;
        [Min(.1f)] public float interval=.8f;
        [Min(1)] public int count=12;
        [Min(1)] public float speed=3.5f;
        [Min(2)] public float duration=7;
        public Vector2 movementRange=new Vector2(4,1.2f);
    }
}
