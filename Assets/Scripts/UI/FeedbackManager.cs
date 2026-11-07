using UnityEngine;

namespace CountToZero
{
    public sealed class FeedbackManager : MonoBehaviour
    {
        public string Message { get; private set; }
        public Color MessageColor { get; private set; }
        public float MessageUntil { get; private set; }
        float hitStopUntil, shakeUntil, laserUntil;
        public Vector3 ShakeOffset => Time.unscaledTime < shakeUntil && GameManager.Instance.State == GameState.Playing ? Random.insideUnitSphere*.09f : Vector3.zero;
        [SerializeField] LineRenderer laser;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip pickup, lucky, hit;
        [SerializeField] TextMesh floating;
        float floatUntil;
        public void ResetFeedback() {hitStopUntil=shakeUntil=laserUntil=0;MessageUntil=0;floatUntil=0;}
        public void Announce(string message,Color color) {Message=message;MessageColor=color;MessageUntil=Time.unscaledTime+2.7f;}
        public void Pickup() {audioSource.PlayOneShot(pickup);}
        public void Lucky(Vector3 position,bool player)
        {
            hitStopUntil=Time.unscaledTime+.15f; shakeUntil=Time.unscaledTime+.4f;
            Time.timeScale=.1f;audioSource.PlayOneShot(lucky);
            Announce(player?"LUCKY MATCH / OVERLOAD":"OMEGA MATCH / COUNTERATTACK",player?ArenaVisuals.Green:ArenaVisuals.Red);
            floating.transform.position=position+Vector3.up*2; floating.text=player?"LUCKY!":"COUNTER!";
            floating.color=player?ArenaVisuals.Green:ArenaVisuals.Red; floatUntil=Time.unscaledTime+1;
        }
        public void Hit(Vector3 position) {shakeUntil=Time.unscaledTime+.22f;audioSource.PlayOneShot(hit);}
        public void Laser(Vector3 from,Vector3 to)
        {laser.SetPosition(0,from+Vector3.up*.6f);laser.SetPosition(1,to+Vector3.up*1.2f);laserUntil=Time.unscaledTime+.4f;}
        void LateUpdate()
        {
            var g=GameManager.Instance;
            Time.timeScale=g.State!=GameState.Playing?0:Time.unscaledTime<hitStopUntil?.1f:Time.unscaledTime<g.SlowUntil?.35f:1;

            laser.enabled=Time.unscaledTime<laserUntil;
            floating.gameObject.SetActive(Time.unscaledTime<floatUntil);
            if(floating.gameObject.activeSelf) floating.transform.position+=Vector3.up*Time.unscaledDeltaTime*.6f;
        }
    }
}
