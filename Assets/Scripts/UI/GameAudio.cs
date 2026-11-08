using System;
using UnityEngine;
namespace CountToZero
{
    public enum SoundCue { Pickup, Heal, Shoot, Dash, Lucky, Hit, Warning, Death, Phase, UIHover, UIClick, Explosion, Blocked }
    [Serializable] public class CueClip {public SoundCue cue;public AudioClip clip;[Range(0,1)]public float volume=.7f;}
    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance {get;private set;}
        [SerializeField] AudioSource effects,bed,pulse,danger;
        [SerializeField] CueClip[] cues;
        float shotAt,warningAt,hoverAt,explosionAt;
        public static float MusicVolume=>PlayerPrefs.GetFloat("CountToZero.Music",.5f);
        public static float EffectsVolume=>PlayerPrefs.GetFloat("CountToZero.SFX",.75f);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatic(){Instance=null;}
        void Awake(){if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);double start=AudioSettings.dspTime+.1;bed.PlayScheduled(start);pulse.PlayScheduled(start);danger.PlayScheduled(start);}
        public static void SetMusic(float volume){PlayerPrefs.SetFloat("CountToZero.Music",Mathf.Clamp01(volume));}
        public static void SetEffects(float volume){PlayerPrefs.SetFloat("CountToZero.SFX",Mathf.Clamp01(volume));}
        public static void Play(SoundCue cue){if(Instance!=null)Instance.PlayCue(cue);}
        void PlayCue(SoundCue cue){if(cue==SoundCue.UIHover){if(Time.unscaledTime<hoverAt)return;hoverAt=Time.unscaledTime+.08f;}if(cue==SoundCue.Explosion){if(Time.unscaledTime<explosionAt)return;explosionAt=Time.unscaledTime+.08f;}if(cue==SoundCue.Shoot){if(Time.unscaledTime<shotAt)return;shotAt=Time.unscaledTime+.075f;}if(cue==SoundCue.Warning){if(Time.unscaledTime<warningAt)return;warningAt=Time.unscaledTime+.6f;}foreach(var item in cues)if(item.cue==cue){effects.PlayOneShot(item.clip,item.volume*EffectsVolume);break;}}
        void Update(){var g=GameManager.Instance;bool playing=g!=null&&g.State==GameState.Playing;bool training=playing&&g.Training;float music=MusicVolume;bed.volume=Mathf.MoveTowards(bed.volume,music*(playing?.32f:.18f),Time.unscaledDeltaTime*.3f);pulse.volume=Mathf.MoveTowards(pulse.volume,playing&&!training?music*.3f:0,Time.unscaledDeltaTime*.4f);danger.volume=Mathf.MoveTowards(danger.volume,playing&&!training?music*(g.Phase-1)*.16f:0,Time.unscaledDeltaTime*.3f);}
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
