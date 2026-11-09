using UnityEngine;
using UnityEngine.UI;

namespace CountToZero
{
    /// <summary>A horizontally repeating UI layer; positive speed moves the scenery left.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RawImage))]
    public sealed class TitleParallaxLayer : MonoBehaviour
    {
        [Tooltip("Horizontal speed in source texture pixels per second.")]
        [SerializeField] float speed = 4f;
        RawImage image;
        float phase;

        void OnEnable()
        {
            image = GetComponent<RawImage>();
            image.raycastTarget = false;
            UpdateUV();
        }

        void LateUpdate()
        {
            if (image.texture == null) return;
            // Keep the offset bounded for indefinite playback, including paused menus.
            if (Application.isPlaying)
                phase = Mathf.Repeat(phase + speed * Time.unscaledDeltaTime / image.texture.width, 1f);
            UpdateUV();
        }

        void UpdateUV()
        {
            if (image == null || image.texture == null) return;
            Rect rect = image.rectTransform.rect;
            if (rect.height <= 0f) return;
            // Preserve source pixels' aspect ratio and fill any screen width with repeats.
            float repeats = rect.width * image.texture.height / (rect.height * image.texture.width);
            image.uvRect = new Rect(phase, 0f, repeats, 1f);
        }
    }
}
