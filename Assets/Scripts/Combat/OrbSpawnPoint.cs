using UnityEngine;

namespace CountToZero
{
    public sealed class OrbSpawnPoint : MonoBehaviour
    {
        public MathOpType Operation = MathOpType.Add;
        public int Operand = 3;
        void OnDrawGizmos()
        {
            Gizmos.color = Operation == MathOpType.Subtract ? ArenaVisuals.Gold : ArenaVisuals.Cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * .55f, .42f);
        }
    }
}
