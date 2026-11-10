using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
[InitializeOnLoad]
public static class PlayabilityChecks
{
    static bool running;static int stage,checks;static double next,deadline;static Keyboard keyboard;static Vector3 orbPosition;static GameManager gameRef;
    static InputSettings.EditorInputBehaviorInPlayMode oldEditor;static InputSettings.BackgroundBehavior oldBackground;
    const string KeyName="CountToZero.Campaign.v1";
    static PlayabilityChecks(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("CTZ.Playability",false)){SessionState.SetBool("CTZ.Playability",false);running=true;stage=0;checks=SessionState.GetInt("CTZ.PlayabilityCount",0);next=EditorApplication.timeSinceStartup+1;deadline=next+100;}};}
    static void Check(bool pass,string name){checks++;if(!pass)throw new Exception(name);}
    [MenuItem("Count To Zero/Validate First Minute and Retry")]
    public static void Run(){
        SessionState.SetBool("CTZ.PlayHadSave",PlayerPrefs.HasKey(KeyName));SessionState.SetString("CTZ.PlaySave",PlayerPrefs.GetString(KeyName,""));SessionState.SetFloat("CTZ.PlayMusic",GameAudio.MusicVolume);SessionState.SetFloat("CTZ.PlaySFX",GameAudio.EffectsVolume);
        try{checks=0;foreach(string name in new[]{"Title","Preload","Story","CountToZero","TurnEnd","StoryEnd"}){
            EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var audio=UnityEngine.Object.FindAnyObjectByType<GameAudio>();Check(audio!=null,name+" authored audio");var so=new SerializedObject(audio);var cues=so.FindProperty("cues");Check(cues.arraySize>=10,"nine base cues + jackpot");for(int i=0;i<9;i++)Check(cues.GetArrayElementAtIndex(i).FindPropertyRelative("clip").objectReferenceValue!=null,"cue clip "+i);
            int samples=0;foreach(string stem in new[]{"bed","pulse","danger"}){var source=(AudioSource)so.FindProperty(stem).objectReferenceValue;Check(source.loop&&source.clip!=null,"looping stem");if(samples==0)samples=source.clip.samples;Check(samples==source.clip.samples,"synchronized loop length");}
        }
        Check(Mathf.Abs(Campaign.Adapt(1,400,0,0)-1)<=.1201f,"adaptive upward step bounded");Check(Mathf.Abs(Campaign.Adapt(1,2400,150,20)-1)<=.1201f,"adaptive downward step bounded");
        EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");SessionState.SetInt("CTZ.PlayabilityCount",checks);SessionState.SetBool("CTZ.Playability",true);EditorApplication.isPlaying=true;
        }catch(Exception e){Finish("FAIL: "+e);}}
    static void Next(float seconds=.2f){stage++;next=EditorApplication.timeSinceStartup+seconds;}
    static void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
    static void Tick(){if(!running||EditorApplication.timeSinceStartup<next)return;try{
        if(EditorApplication.timeSinceStartup>deadline)throw new Exception("timeout stage "+stage);var g=GameManager.Instance;
        switch(stage){
            case 0:
                oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;oldBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                Campaign.NewGame();g.RecordScores=false;g.BeginIntro();Check(g.State==GameState.Intro,"first intro plays");g.SkipIntro();Check(g.State==GameState.Playing&&!g.View.GetComponent<FollowCamera>().Touring&&Campaign.Data.introSeen,"intro skip settles camera and persists");
                Check(g.Training&&g.Tutorial.Step==TutorialStep.Move,"new campaign starts movement training");g.Player.ModuloShot();g.Player.DashInversion();Check(g.Player.ShotsFired==0&&g.Player.DashesUsed==0,"early actions cannot break arithmetic lesson");keyboard=InputSystem.AddDevice<Keyboard>();Keys(Key.W);Next(.22f);break;
            case 1:
                Keys();Check(g.Tutorial.Step==TutorialStep.Lucky,"actual movement advances lesson");Check(g.Orbs.Active.Count==1&&g.Orbs.Active[0].Operand==2,"counting lesson starts with one +2 orb");orbPosition=g.Orbs.Active[0].transform.position;Check(g.Elapsed==0&&g.RuleTime==15,"training holds combat clock and rule");Next(.4f);break;
            case 2:
                Check(Vector3.Distance(orbPosition,g.Orbs.Active[0].transform.position)<.01f,"lesson orb stays still");Check(g.Encounters.ActiveMinions==0&&g.Encounters.ActiveObstacles==0,"encounters held during training");g.Player.transform.position=orbPosition;stage=20;Next(.5f);break;
            case 21:
                Check(g.Player.CurrentValue==3&&g.Orbs.Active.Count==1&&g.Orbs.Active[0].Operand==4,"1 + 2 = 3, then the missing +4 appears");g.Player.transform.position=g.Orbs.Active[0].transform.position;stage=2;Next(.3f);break;
            case 3:
                Check(g.Tutorial.Step==TutorialStep.Celebrate,"counting 1 + 2 + 4 = 7 makes Lucky");Check(g.Player.CurrentValue==1&&g.Boss.Health==80,"Lucky resets value and visibly damages boss");Next(1.7f);break;
            case 4:
                Check(g.Tutorial.Step==TutorialStep.Fire,"fire lesson follows reward");Keys(Key.J);Next(.65f);break;
            case 5:
                Keys();Check(g.Player.ShotsFired>=3&&g.Tutorial.Step==TutorialStep.Dash,"actual fire input advances lesson");Keys(Key.W,Key.Space);Next(.2f);break;
            case 6:
                Keys();Check(g.Tutorial.Step==TutorialStep.Dodge&&g.Player.DashesUsed==1&&g.Player.CurrentValue==-1,"dash flips value and starts dodge practice");g.Player.Hurt(true);Check(g.Player.Health==5,"practice hits cannot kill");g.TogglePause();Next(.4f);break;
            case 7:
                Check(g.State==GameState.Paused&&g.Training,"pause retains lesson");g.TogglePause();Next(12.3f);break;
            case 8:
                Check(!g.Training&&Campaign.Data.tutorialComplete,"dodge practice completes and saves");Check(g.Player.Health==5&&g.Player.CurrentValue==1&&g.Boss.Health==99&&g.Elapsed<2,"live round starts clean");g.Boss.CrashUntil=Time.time+100;g.Orbs.Clear();g.Player.ApplyMathOperation(MathOpType.Add,6);Check(g.CutIn.Visible,"live Lucky cut-in");g.Player.Hurt(true,"Test hazard");Check(g.Player.Health==5,"cut-in blocks even unavoidable damage");g.TogglePause();Next(1);break;
            case 9:
                Check(g.CutIn.Visible&&g.Player.Health==5,"pause does not consume cut-in protection");g.TogglePause();Next(1.3f);break;
            case 10:
                g.Player.Hurt(true,"Test hazard");Check(g.Player.Health==4,"damage resumes after cut-in grace");gameRef=g;for(int i=0;i<4;i++)g.Player.Hurt(true,"Test hazard");Check(g.State==GameState.Lost&&Campaign.Data.level==1&&Campaign.Data.failures==1,"loss records one failure without changing scene");Next();break;
            case 11:
                var hud=UnityEngine.Object.FindAnyObjectByType<HUDController>();var so=new SerializedObject(hud);Check(((TMPro.TMP_Text)so.FindProperty("overlayBody").objectReferenceValue).text.Contains("Test hazard"),"death screen explains cause");var button=(Button)so.FindProperty("actionButton").objectReferenceValue;Check(button.gameObject.activeInHierarchy,"retry button visible");button.onClick.Invoke();Check(ReferenceEquals(GameManager.Instance,gameRef)&&g.State==GameState.Playing&&!g.View.GetComponent<FollowCamera>().Touring,"retry begins immediately in same scene");Check(!g.Training&&g.Player.Health==5,"retry restores health without repeating lesson");Next();break;
            case 12:
                Check(!EventSystem.current.sendNavigationEvents,"gameplay input cannot navigate pause buttons");g.BeginIntro();Check(g.State==GameState.Playing,"later entries skip intro");Campaign.Data.tutorialComplete=false;g.Begin();g.Tutorial.Skip();Check(!g.Training&&Campaign.Data.tutorialComplete,"training skip persists");
                GameAudio.SetMusic(.2f);GameAudio.SetEffects(.3f);Check(Mathf.Approximately(GameAudio.MusicVolume,.2f)&&Mathf.Approximately(GameAudio.EffectsVolume,.3f),"audio sliders settings independent");Check(GameAudio.Instance!=null,"audio director alive");Finish("PASS: "+checks+" checks. Authored audio in six scenes, 9 cues and 3 synchronized stems; input-driven tutorial movement/fire/dash, real +6 pickup, safe practice, pause, saved completion/skip, clean live start, cut-in protection, death explanation, immediate same-scene retry, first-only intro, UI navigation isolation, bounded adaptation and volume settings.\nHuman fun/readability testing and mobile WebGL testing remain external checks.\n");break;
        }
    }catch(Exception e){Finish("FAIL stage "+stage+": "+e);}}
    static void Finish(string result){running=false;if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}if(EditorApplication.isPlaying){InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;InputSystem.settings.backgroundBehavior=oldBackground;}
        if(SessionState.GetBool("CTZ.PlayHadSave",false))PlayerPrefs.SetString(KeyName,SessionState.GetString("CTZ.PlaySave",""));else PlayerPrefs.DeleteKey(KeyName);GameAudio.SetMusic(SessionState.GetFloat("CTZ.PlayMusic",.5f));GameAudio.SetEffects(SessionState.GetFloat("CTZ.PlaySFX",.75f));PlayerPrefs.Save();
        File.WriteAllText("Documentation/playability-validation.txt",result);Debug.Log(result);EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.Exit(result.StartsWith("PASS")?0:1);
    }
}
