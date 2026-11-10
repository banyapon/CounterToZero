using UnityEngine;

namespace CountToZero
{
    /// Flat pulsing ring under the hero so the player always sees which character they control.
    public sealed class ControlRing : MonoBehaviour
    {
        [SerializeField] SpriteRenderer ring;
        [SerializeField] SpriteRenderer hero;
        [SerializeField] float size = 1.5f, spin = 40;
        float angle;
        void LateUpdate()
        {
            var g = GameManager.Instance;
            ring.enabled = hero.enabled && (g == null || (g.State != GameState.Victory && g.State != GameState.Won));
            if (!ring.enabled) return;
            angle += Time.unscaledDeltaTime * spin;
            float pulse = 1 + Mathf.Sin(Time.unscaledTime * 4) * .05f;
            ring.transform.localPosition = new Vector3(0, .04f, 0);
            ring.transform.rotation = Quaternion.Euler(90, angle, 0);
            ring.transform.localScale = Vector3.one * size * pulse;
            var c = ArenaVisuals.Cyan; c.a = .65f + Mathf.Sin(Time.unscaledTime * 4) * .2f;
            ring.color = c;
        }
    }
}
