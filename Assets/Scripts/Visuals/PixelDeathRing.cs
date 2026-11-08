using UnityEngine;

namespace CountToZero
{
    public sealed class PixelDeathRing : MonoBehaviour
    {
        [SerializeField] SpriteRenderer visual;
        [SerializeField] float duration = .45f, diameter = 3.2f;
        float age;
        public void Play(Vector3 position)
        {
            transform.position = position;
            age = 0;
            transform.localScale = Vector3.one * .15f;
            visual.color = Color.white;
            if (GameManager.Instance != null) transform.rotation = GameManager.Instance.View.transform.rotation;
            gameObject.SetActive(true);
        }
        public void Clear() { gameObject.SetActive(false); }
        void Update()
        {
            var g = GameManager.Instance;
            if (g == null || g.State == GameState.Won || g.State == GameState.Lost || g.State == GameState.Victory) { Clear(); return; }
            if (g.State != GameState.Playing) return;
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(.15f, diameter, 1 - (1 - t) * (1 - t));
            transform.rotation = g.View.transform.rotation;
            visual.color = new Color(1, 1, 1, 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.6f, 1, t)));
            if (t >= 1) Clear();
        }
    }
}
