using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CountToZero
{
    public sealed class RoundCountdown : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset pixelFont;
        Canvas canvas;
        TMP_Text label;

        void Create()
        {
            var root = new GameObject("Round Countdown", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            var rect = CampaignExpansionUI.Rect("3-2-1-READY!", root.transform, Vector2.zero, new Vector2(900, 220));
            label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = pixelFont!=null?pixelFont:FindAnyObjectByType<HUDController>()?.GetComponentInChildren<TMP_Text>(true)?.font;
            label.fontStyle = FontStyles.Normal;
            label.fontSize = 112;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.outlineColor = new Color32(5, 12, 24, 255);
            label.outlineWidth = .22f;
        }

        public IEnumerator Play()
        {
            if (canvas == null) Create();
            canvas.gameObject.SetActive(true);
            foreach (string text in new[] { "3", "2", "1", "READY!" })
            {
                label.text = text;
                label.color = text == "READY!" ? ArenaVisuals.Cyan : Color.white;
                GameAudio.Play(text == "READY!" ? SoundCue.Ready : SoundCue.Countdown);
                float duration = text == "READY!" ? .65f : 1f;
                for (float age = 0; age < duration; age += Time.unscaledDeltaTime)
                {
                    label.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, Mathf.Clamp01(age / .18f));
                    yield return null;
                }
            }
            Hide();
        }

        public void Hide(){if(canvas!=null)canvas.gameObject.SetActive(false);}
    }
}
