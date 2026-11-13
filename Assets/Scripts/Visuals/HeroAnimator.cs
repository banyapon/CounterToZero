using UnityEngine;

namespace CountToZero
{
    [DefaultExecutionOrder(100)]
    public sealed class HeroAnimator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Transform facingTarget;
        [SerializeField] Sprite[] backFrames, frontFrames;
        [SerializeField] Sprite[] idleFronts, idleBacks;
        [SerializeField] float framesPerSecond = 8;
        public void SetFrames(Sprite[] front,Sprite[] back){frontFrames=front;backFrames=back;clock=0;}
        public bool Celebrating { get; set; }
        float clock;
        Vector3 previous, aimPoint;
        bool hasAim;
        /// <summary>Face the shot aim. A lower aim (smaller Z, down the screen) shows the front.</summary>
        public void LookAt(Vector3 worldPoint) { aimPoint = worldPoint; hasAim = true; }
        void OnEnable() { previous = transform.position; clock = 0; }
        void LateUpdate()
        {
            var g = GameManager.Instance;
            bool moving = (transform.position - previous).sqrMagnitude > .000001f;
            previous = transform.position;
            if (Celebrating || (g != null && g.State == GameState.Playing && moving))
                clock += Time.unscaledDeltaTime;
            else if (g != null && g.State == GameState.Playing) clock = 0;
            // The arena is XZ: increasing Z is upward on screen (screen Y).
            var target = facingTarget != null ? facingTarget : g != null ? g.Boss.transform : null;
            bool facingFront = Celebrating || (hasAim ? transform.position.z > aimPoint.z : target != null && transform.position.z > target.position.z);
            hasAim = false;
            var frames = facingFront ? frontFrames : backFrames;
            var idle = facingFront ? idleFronts : idleBacks;
            if(!moving && !Celebrating && idle!=null && idle.Length>0)frames=idle;
            if (frames != null && frames.Length > 0)
                visual.sprite = frames[Mathf.FloorToInt(clock * framesPerSecond) % frames.Length];
        }
    }
}
