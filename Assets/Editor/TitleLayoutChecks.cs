using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
[InitializeOnLoad]
public static class TitleLayoutChecks
{
 static Keyboard keyboard; static Vector3 playerStart; static InputSettings.EditorInputBehaviorInPlayMode oldInput;
 static bool running;static int stage,count,nextFrame,checks;static double next;static string errors="";const string Key="CountToZero.Campaign.v1";
 static TitleLayoutChecks(){Application.logMessageReceived+=(m,s,t)=>{if(running&&(t==LogType.Error||t==LogType.Exception))errors+=m+"\n"+s;};EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Title.Check",false)){SessionState.SetBool("Title.Check",false);running=true;checks=0;stage=-1;errors="";next=EditorApplication.timeSinceStartup+1;}if(m==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("Title.Return",false)){SessionState.SetBool("Title.Return",false);EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");}};}
 static void Check(bool pass,string text){checks++;if(!pass)throw new Exception(text);}
 static void Next(float seconds=.3f){stage++;next=EditorApplication.timeSinceStartup+seconds;nextFrame=Time.frameCount+3;}
 static void Click(string page,string name){var root=GameObject.Find("Screen Canvas/Title Menu/"+page);var b=root.GetComponentsInChildren<Button>().Single(b=>b.name==name);Check(b.IsInteractable(),page+" / "+name+" enabled");b.onClick.Invoke();}
 static void Finish(string result){running=false;if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;InputSystem.settings.editorInputBehaviorInPlayMode=oldInput;}if(errors.Length>0)result="FAIL runtime errors: "+errors+"\n"+result;if(SessionState.GetBool("Title.Had",false))PlayerPrefs.SetString(Key,SessionState.GetString("Title.Save",""));else PlayerPrefs.DeleteKey(Key);foreach(var p in new[]{"CountToZero.Music","CountToZero.SFX"}){if(SessionState.GetBool("Title.Had."+p,false))PlayerPrefs.SetFloat(p,SessionState.GetFloat("Title.Value."+p,0));else PlayerPrefs.DeleteKey(p);}PlayerPrefs.Save();File.WriteAllText("Documentation/title-layout-checks.txt",result);Debug.Log(result);SessionState.SetBool("Title.Return",true);EditorApplication.isPlaying=false;}
 static void Tick(){try{
 if(!running){if(!File.Exists("Temp/ctz-title-check.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-title-check.request");SessionState.SetBool("Title.Had",PlayerPrefs.HasKey(Key));SessionState.SetString("Title.Save",PlayerPrefs.GetString(Key));foreach(var p in new[]{"CountToZero.Music","CountToZero.SFX"}){SessionState.SetBool("Title.Had."+p,PlayerPrefs.HasKey(p));SessionState.SetFloat("Title.Value."+p,PlayerPrefs.GetFloat(p));}PlayerPrefs.DeleteKey(Key);EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");count=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length;SessionState.SetInt("Title.ButtonCount",count);SessionState.SetBool("Title.Check",true);EditorApplication.isPlaying=true;return;}
 if(EditorApplication.timeSinceStartup<next||(stage>=0&&Time.frameCount<nextFrame))return;
 switch(stage){
 case -1:Application.runInBackground=true;Next(1);break;
 case 0:Check(SessionState.GetInt("Title.ButtonCount",0)==11,"11 buttons already authored before Play");Check(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==11,"no runtime duplicate buttons");Check(!GameObject.Find("Continue Button").GetComponent<Button>().interactable,"continue disabled without a save");var title=GameObject.Find("Game Title").GetComponent<TMPro.TMP_Text>();title.ForceMeshUpdate();Check(title.textInfo.lineCount==1,"title stays on one reserved header line");Check(!title.isTextOverflowing,"title fits its frame");var tagline=GameObject.Find("Tagline").GetComponent<TMPro.TMP_Text>();tagline.ForceMeshUpdate();Check(title.transform.TransformPoint(title.textBounds.min).y>tagline.transform.TransformPoint(tagline.textBounds.max).y+8,"rendered title clears tagline");ScreenCapture.CaptureScreenshot("Documentation/title-home.png");Next();break;
 case 1:oldInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Enter));Next();break;
 case 2:InputSystem.QueueStateEvent(keyboard,new KeyboardState());Check(GameObject.Find("Difficulty")!=null&&GameObject.Find("Home")==null,"new game opens authored difficulty page");ScreenCapture.CaptureScreenshot("Documentation/title-difficulty.png");Next();break;
 case 3:Click("Difficulty","Back Button");Click("Home","Options Button");Next();break;
 case 4:var sliders=GameObject.Find("Options").GetComponentsInChildren<Slider>();Check(sliders.Length==2,"both audio sliders authored");sliders.First(s=>s.name=="BGM").value=.27f;sliders.First(s=>s.name=="SFX").value=.43f;Check(Mathf.Abs(GameAudio.MusicVolume-.27f)<.01f&&Mathf.Abs(GameAudio.EffectsVolume-.43f)<.01f,"audio sliders update settings");ScreenCapture.CaptureScreenshot("Documentation/title-options.png");Next();break;
 case 5:Click("Options","Credits Button");Check(GameObject.Find("Credit Content").GetComponent<TMPro.TMP_Text>().text.Contains("Joystick Pack"),"authored credits populated");Click("Credits","Back Button");Next();break;
 case 6:Check(GameObject.Find("Options")!=null,"credits returns to options");Click("Options","Back Button");Next();break;
 case 7:Click("Home","Credits Button");Next();break;
 case 8:Click("Credits","Back Button");Next();break;
 case 9:Check(GameObject.Find("Home")!=null,"home credits returns to home");Check(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==11,"all navigation reuses scene buttons");Click("Home","New Game Button");Next();break;
 case 10:Click("Difficulty","HARD Button");Next(1.5f);break;
 case 11:Check(SceneManager.GetActiveScene().name=="Story"&&Campaign.Data.selectedDifficulty==2,"authored difficulty button starts story");SceneManager.LoadScene("CountToZero");Next(1.5f);break;
 case 12:var g=GameManager.Instance;g.RecordScores=false;g.SkipIntro();g.Tutorial.Skip();g.Controls.PersistBindings=false;g.Controls.RestoreDefaults(false);g.Boss.CrashUntil=Time.time+100;g.Encounters.ResetRun();g.Orbs.Clear();playerStart=g.Player.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.D,UnityEngine.InputSystem.Key.J));Next(.4f);break;
 case 13:g=GameManager.Instance;Check(g.Player.transform.position.x>playerStart.x+.1f,"keyboard movement");Check(g.Player.ShotsFired>0&&!g.Controls.MouseAiming,"keyboard fire uses auto aim without mouse");InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Space));Next(.2f);break;
 case 14:g=GameManager.Instance;Check(g.Player.DashesUsed>0,"keyboard dash");g.Score.Focus=1;InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.E));Next(.2f);break;
 case 15:g=GameManager.Instance;Check(g.SlowUntil>Time.unscaledTime,"keyboard focus");InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Escape));Next(.2f);break;
 case 16:Check(GameManager.Instance.State==GameState.Paused,"keyboard pause");InputSystem.QueueStateEvent(keyboard,new KeyboardState());Next(.2f);break;
 case 17:InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Escape));Next(.2f);break;
 case 18:g=GameManager.Instance;Check(g.State==GameState.Playing,"keyboard resume");InputSystem.QueueStateEvent(keyboard,new KeyboardState());g.View.GetComponent<FollowCamera>().Snap();foreach(float x in new[]{-11f,11f})foreach(float z in new[]{-8f,8f}){var v=g.View.WorldToViewportPoint(new Vector3(x,0,z));Check(v.x>.02f&&v.x<.98f&&v.y>.08f&&v.y<.92f,"arena corners inside safe camera frame");}ScreenCapture.CaptureScreenshot("Documentation/keyboard-camera.png");Next(1);break;
 case 19:Finish("PASS "+checks+" checks: authored title layout, pages/audio/credits, keyboard Enter starts menu, movement, auto-aim fire, dash, focus, pause/resume and camera arena framing. Campaign and audio preferences restored.");break;
 }
 }catch(Exception e){Finish("FAIL "+checks+": "+e);}}
}
