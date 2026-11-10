using System;
using System.IO;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class CombatClarityChecks
{
 static MinionController enemy;static float hp;static bool running;static int stage,frame,count;static double next;static string errors="";const string Save="CountToZero.Campaign.v1";
 static CombatClarityChecks(){Application.logMessageReceived+=(m,s,t)=>{if(running&&(t==LogType.Error||t==LogType.Exception))errors+=m;};EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Clarity.Check",false)){SessionState.SetBool("Clarity.Check",false);running=true;stage=-1;count=0;errors="";next=EditorApplication.timeSinceStartup+1;}if(m==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("Clarity.Return",false)){SessionState.SetBool("Clarity.Return",false);EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");}};EditorApplication.update+=Tick;}
 static void Check(bool p,string why){count++;if(!p)throw new Exception(why);}
 static void Next(){stage++;frame=Time.frameCount+3;next=EditorApplication.timeSinceStartup+1;}
 static void Finish(string message){running=false;if(SessionState.GetBool("Clarity.Had",false))PlayerPrefs.SetString(Save,SessionState.GetString("Clarity.Save",""));else PlayerPrefs.DeleteKey(Save);PlayerPrefs.Save();File.WriteAllText("Documentation/combat-clarity-checks.txt",errors.Length>0?"FAIL runtime: "+errors:message);SessionState.SetBool("Clarity.Return",true);EditorApplication.isPlaying=false;}
 static void Tick(){try{if(!running){if(!File.Exists("Temp/ctz-clarity-check.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-clarity-check.request");SessionState.SetBool("Clarity.Had",PlayerPrefs.HasKey(Save));SessionState.SetString("Clarity.Save",PlayerPrefs.GetString(Save,""));EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");SessionState.SetBool("Clarity.Check",true);EditorApplication.isPlaying=true;return;}if(GameManager.Instance!=null&&GameManager.Instance.State==GameState.Paused)GameManager.Instance.TogglePause();if(EditorApplication.timeSinceStartup<next||(stage>=0&&Time.frameCount<frame))return;
 var g=GameManager.Instance;if(g.State==GameState.Paused)g.TogglePause();var hud=UnityEngine.Object.FindAnyObjectByType<HUDController>();
 switch(stage){case -1:Application.runInBackground=true;g.RecordScores=false;g.SkipIntro();g.Tutorial.Skip();g.Boss.enabled=false;g.Encounters.enabled=false;g.Dialogue.Clear();var setup=new SerializedObject(g.CutIn);setup.FindProperty("duration").floatValue=4;setup.ApplyModifiedPropertiesWithoutUndo();Next();break;
 case 0:
 if(g.State==GameState.Intro){g.SkipIntro();return;}if(g.Training){g.Tutorial.Skip();return;}
 g.Encounters.ResetRun();g.Orbs.Clear();g.Bullets.Clear();BattleExpansion.Instance.ResetBattle();BattleExpansion.Instance.enabled=false;
 enemy=(MinionController)new SerializedObject(g.Encounters).FindProperty("minions").GetArrayElementAtIndex(0).objectReferenceValue;enemy.Spawn(new Vector3(2,0,0));enemy.enabled=false;
 g.Player.transform.position=Vector3.zero;g.Boss.transform.position=new Vector3(0,0,1);
 Check(g.Encounters.AutoAim(Vector3.zero)==g.Boss.transform.position,"closer boss wins over nearby minion");
 g.Boss.transform.position=new Vector3(0,0,7);Check(g.Encounters.AutoAim(Vector3.zero)==enemy.transform.position,"closer minion wins");
 enemy.transform.position=new Vector3(7,0,0);g.Boss.transform.position=new Vector3(0,0,9);Check(g.Encounters.AutoAim(Vector3.zero)==enemy.transform.position,"nearest enemy beyond old six-unit limit");
 enemy.Clear();Check(g.Encounters.AutoAim(Vector3.zero)==g.Boss.transform.position,"inactive enemy ignored");
 enemy.Spawn(new Vector3(2,0,0));enemy.enabled=false;hp=enemy.Health;g.Player.ModuloShot();Next();break;
 case 1:Check(enemy.Health<hp,"actual auto-aim shot damages nearer minion: shots="+g.Player.ShotsFired+" hp="+enemy.Health+" training="+g.Training+" state="+g.State);enemy.Clear();g.CutIn.Show();Next();break;
 case 2:var so=new SerializedObject(g.CutIn);var portrait=(Image)so.FindProperty("portrait").objectReferenceValue;var panel=(RectTransform)so.FindProperty("panel").objectReferenceValue;
 Check(portrait.rectTransform.anchorMin==new Vector2(0,1)&&portrait.transform.localScale==Vector3.one,"Lucky image visible inside panel");Check(g.CutIn.Visible,"Lucky remains visible");Check(portrait.rectTransform.rect.height==110,"Lucky portrait half original height");Check(panel.rect.width==320&&panel.rect.height==126,"compact Lucky panel");Check(AssetDatabase.GetAssetPath(portrait.sprite).EndsWith("aim_pixel.png"),"supplied artwork kept");
 ScreenCapture.CaptureScreenshot("Documentation/compact-lucky.png");Next();break;
 case 3:g.CutIn.Clear();g.Player.Crush();Next();break;
 case 4:Check(g.State==GameState.Lost,"crush produces real defeat");var body=(TMPro.TMP_Text)new SerializedObject(hud).FindProperty("overlayBody").objectReferenceValue;body.ForceMeshUpdate();Check(body.text.Contains("NEXT TRY")&&body.text.Contains("Move sideways"),"death advice matches actual crush");Check(!body.isTextOverflowing,"death advice fits panel");
 foreach(var cause in new[]{"Enemy projectile","Falling cube","Falling fireball","Red laser / 50% max HP","Tracking lightning","Virus segment","Sentinel contact","Unstable floor","Boss Lucky counter","VALUE overflow (+/-100)","Whip arm","Chain flail","Whip shock","Omega 8-way laser","Omega rush"}){Check(DefeatAdvice.For(cause)!=DefeatAdvice.For("unknown"),"specific advice: "+cause);body.text="LAST HIT  "+cause+"\n\nNEXT TRY\n"+DefeatAdvice.For(cause)+"\n\nSCORE  999,999   RANK  S   TIME  999.9s";body.ForceMeshUpdate();Check(!body.isTextOverflowing,"advice fits: "+cause);}
 Next();break;
 case 5:ScreenCapture.CaptureScreenshot("Documentation/defeat-advice.png");Next();break;
 case 6:((Button)new SerializedObject(hud).FindProperty("actionButton").objectReferenceValue).onClick.Invoke();Next();break;
 case 7:if(g.State==GameState.Intro){g.SkipIntro();return;}Check(g.State==GameState.Playing&&g.Player.Health==5,"retry remains functional");Finish("PASS "+count+" checks: nearest target selection, real shot hits nearby enemy, compact Lucky image/panel, cause-specific defeat advice and text fit, retry. Campaign restored.");break;}

 }catch(Exception e){Finish("FAIL stage "+stage+": "+e);}}
}
