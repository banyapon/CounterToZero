using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CountToZero
{
    // Unscaled time is intentional: combat is frozen for the entire presentation.
    public sealed class VictorySequence : MonoBehaviour
    {
        [SerializeField] CanvasGroup[] interfaceGroups;
        [SerializeField] SpriteRenderer bossVisual, heroVisual;
        [SerializeField] HeroAnimator hero;
        [SerializeField] Image whiteFlash, heroSilhouette, bossSilhouette;
        [SerializeField] SpriteRenderer[] fragments;
        [SerializeField] float blastDuration = 1.4f, celebrationDuration = 2.2f;
        bool active;
        TMP_Text[] actorLabels;
        bool[] labelStates;
        public void ResetPresentation()
        {
            active = false;
            if (actorLabels != null) for (int i = 0; i < actorLabels.Length; i++) if (actorLabels[i] != null) actorLabels[i].enabled = labelStates[i];
            if (hero != null) hero.Celebrating = false;
            if (bossVisual != null) { bossVisual.enabled = true; bossVisual.color = Color.white; }
            if (heroVisual != null) heroVisual.color = Color.white;
            if (whiteFlash != null) whiteFlash.gameObject.SetActive(false);
            foreach (var fragment in fragments) fragment.gameObject.SetActive(false);
            foreach (var group in interfaceGroups) { if (group == null) continue; group.alpha = 1; group.blocksRaycasts = true; group.interactable = true; }
        }
        public IEnumerator Play()
        {
            active = true;
            var g = GameManager.Instance;
            actorLabels = g.Player.GetComponentsInChildren<TMP_Text>(true).Concat(g.Boss.GetComponentsInChildren<TMP_Text>(true)).ToArray();
            labelStates = actorLabels.Select(label => label.enabled).ToArray();
            foreach (var label in actorLabels) label.enabled = false;
            g.Bullets.Clear(); g.Orbs.Clear(); g.CutIn.Clear(); g.Feedback.ResetFeedback();
            foreach (var group in interfaceGroups) { if (group == null) continue; group.alpha = 0; group.blocksRaycasts = false; group.interactable = false; }
            GameAudio.Play(SoundCue.Explosion);
            Vector3 origin = bossVisual.transform.position;
            whiteFlash.gameObject.SetActive(true);
            // Two deliberate white impact pulses; actors read as black silhouettes.
            for (float t = 0; t < blastDuration; t += Time.unscaledDeltaTime)
            {
                float flash = Mathf.Max(t < .16f ? 1 : 1 - (t - .16f) / .14f, t >= .55f ? (t < .71f ? 1 : 1 - (t - .71f) / .19f) : 0);
                whiteFlash.color = new Color(1, 1, 1, Mathf.Clamp01(flash));
                Silhouette(heroSilhouette, heroVisual, flash);
                Silhouette(bossSilhouette, bossVisual, t < .8f ? flash : 0);
                bossVisual.enabled = t < .8f;
                bossVisual.color = heroVisual.color = flash > .1f ? Color.black : Color.white;
                for (int i = 0; i < fragments.Length; i++)
                {
                    var f = fragments[i]; f.gameObject.SetActive(true);
                    float angle = i * 2.399963f;
                    Vector3 direction = g.View.transform.right * Mathf.Cos(angle) + g.View.transform.up * Mathf.Sin(angle);
                    f.transform.position = origin + direction * t * (2 + i % 4);
                    f.transform.rotation = g.View.transform.rotation;
                    f.transform.localScale = Vector3.one * Mathf.Lerp(.35f, .02f, t / blastDuration);
                    f.color = flash > .1f ? Color.black : new Color(1, .65f, .15f, 1 - t / blastDuration);
                }
                yield return null;
            }
            whiteFlash.gameObject.SetActive(false);
            foreach (var f in fragments) f.gameObject.SetActive(false);
            bossVisual.enabled = false; heroVisual.color = Color.white;
            hero.Celebrating = true;
            yield return new WaitForSecondsRealtime(celebrationDuration);
            active = false;
            // A campaign loads its result screen next; standalone play uses the HUD.
            if (!Campaign.Active) foreach (var group in interfaceGroups) { if (group == null) continue; group.alpha = 1; group.blocksRaycasts = true; group.interactable = true; }
        }
        void Silhouette(Image image, SpriteRenderer actor, float opacity)
        {
            var view = GameManager.Instance.View;
            Vector3 center = view.WorldToViewportPoint(actor.bounds.center);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(center.x, center.y);
            image.rectTransform.anchoredPosition = Vector2.zero;
            image.sprite = actor.sprite;
            Vector3 scale = actor.transform.lossyScale;
            float pixelsPerUnit = Screen.height / (2 * view.orthographicSize);
            image.rectTransform.sizeDelta = new Vector2(actor.sprite.bounds.size.x * Mathf.Abs(scale.x), actor.sprite.bounds.size.y * Mathf.Abs(scale.y)) * pixelsPerUnit;
            image.color = new Color(0, 0, 0, opacity > .1f ? 1 : 0);
        }
        void LateUpdate()
        {
            if (!active) return;
            foreach (var group in interfaceGroups) if (group != null) group.alpha = 0;
        }
    }
}
