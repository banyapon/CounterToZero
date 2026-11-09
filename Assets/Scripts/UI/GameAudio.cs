using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CountToZero
{
    public enum SoundCue { Pickup, Heal, Shoot, Dash, Lucky, Hit, Warning, Death, Phase, UIHover, UIClick, Explosion, Blocked, Countdown, Ready }
    [Serializable] public class CueClip {public SoundCue cue;public AudioClip clip;[Range(0,1)]public float volume=.7f;}
    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance {get;private set;}
        [SerializeField] AudioSource effects,bed,pulse,danger;
        [SerializeField] CueClip[] cues;
        [SerializeField] AudioClip titleMusic,storyMusic;
        float shotAt,warningAt,hoverAt,explosionAt;
        public static float MusicVolume=>PlayerPrefs.GetFloat("CountToZero.Music",.5f);
        public static float EffectsVolume=>PlayerPrefs.GetFloat("CountToZero.SFX",.75f);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatic(){Instance=null;}
        void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);
            pulse.Stop();danger.Stop();pulse.playOnAwake=danger.playOnAwake=false;
            bed.loop=true;bed.spatialBlend=effects.spatialBlend=0;
            ApplyVolumes();SelectMusic(SceneManager.GetActiveScene().name);
            SceneManager.sceneLoaded+=SceneLoaded;
        }
        void SceneLoaded(Scene scene,LoadSceneMode mode)=>SelectMusic(scene.name);
        void SelectMusic(string scene)
        {
            if(scene=="Preload")scene=Campaign.NextScene;
            AudioClip clip=scene=="Story"||scene=="StoryEnd"?storyMusic:scene=="Title"||scene=="CountToZero"?titleMusic:null;
            // Result pages retain the current track; preload never restarts the same song.
            if(clip==null||bed.clip==clip&&bed.isPlaying)return;
            bed.Stop();bed.clip=clip;bed.Play();
        }
        void ApplyVolumes(){bed.volume=MusicVolume;effects.volume=EffectsVolume;pulse.volume=danger.volume=0;}
        public static void SetMusic(float volume){PlayerPrefs.SetFloat("CountToZero.Music",Mathf.Clamp01(volume));if(Instance!=null)Instance.ApplyVolumes();}
        public static void SetEffects(float volume){PlayerPrefs.SetFloat("CountToZero.SFX",Mathf.Clamp01(volume));if(Instance!=null)Instance.ApplyVolumes();}
        public static void Play(SoundCue cue){if(Instance!=null)Instance.PlayCue(cue);}
        void PlayCue(SoundCue cue){if(cue==SoundCue.UIHover){if(Time.unscaledTime<hoverAt)return;hoverAt=Time.unscaledTime+.08f;}if(cue==SoundCue.Explosion){if(Time.unscaledTime<explosionAt)return;explosionAt=Time.unscaledTime+.08f;}if(cue==SoundCue.Shoot){if(Time.unscaledTime<shotAt)return;shotAt=Time.unscaledTime+.075f;}if(cue==SoundCue.Warning){if(Time.unscaledTime<warningAt)return;warningAt=Time.unscaledTime+.6f;}foreach(var item in cues)if(item.cue==cue&&item.clip!=null){effects.PlayOneShot(item.clip,item.volume);break;}}
        void OnDestroy(){SceneManager.sceneLoaded-=SceneLoaded;if(Instance==this)Instance=null;}
    }
}
