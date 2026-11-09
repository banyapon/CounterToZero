using System;
using System.IO;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class CompactHudChecks
{
 static bool running;static int stage,frame,count;static double next;static string errors="";const string Save="CountToZero.Campaign.v1";
 static CompactHudChecks(){Application.logMessageReceived+=(m,s,t)=>{if(running&&(t==LogType.Error||t==LogType.Exception))errors+=m;};EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Compact.Check",false)){SessionState.SetBool("Compact.Check",false);running=true;stage=-1;count=0;errors="";next=EditorApplication.timeSinceStartup+1;}if(m==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("Compact.Return",false)){SessionState.SetBool("Compact.Return",false);EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");}};EditorApplication.update+=Tick;}
 static void Check(bool p,string why){count++;if(!p)throw new Exception(why);}
 static void Next(){stage++;frame=Time.frameCount+3;next=EditorApplication.timeSinceStartup+1;}
 static void Finish(string message){running=false;if(SessionState.GetBool("Compact.Had",false))PlayerPrefs.SetString(Save,SessionState.GetString("Compact.Save",""));else PlayerPrefs.DeleteKey(Save);PlayerPrefs.Save();File.WriteAllText("Documentation/compact-hud-checks.txt",errors.Length>0?"FAIL runtime: "+errors:message);SessionState.SetBool("Compact.Return",true);EditorApplication.isPlaying=false;}
 static void Tick(){try{if(!running){if(!File.Exists("Temp/ctz-compact-check.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-compact-check.request");SessionState.SetBool("Compact.Had",PlayerPrefs.HasKey(Save));SessionState.SetString("Compact.Save",PlayerPrefs.GetString(Save,""));EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");SessionState.SetBool("Compact.Check",true);EditorApplication.isPlaying=true;return;}if(EditorApplication.timeSinceStartup<next||(stage>=0&&Time.frameCount<frame))return;
 var g=GameManager.Instance;if(g.State==GameState.Paused)g.TogglePause();var hud=UnityEngine.Object.FindAnyObjectByType<HUDController>();
 switch(stage){case -1:Application.runInBackground=true;g.RecordScores=false;g.SkipIntro();g.Tutorial.Skip();g.Boss.enabled=false;g.Encounters.enabled=false;g.Dialogue.Clear();Next();break;
 case 0:var bottom=(RectTransform)hud.transform.Find("Reference Layout/Player HUD");Check(bottom.rect.height==74,"74-unit compact bar");foreach(var t in bottom.GetComponentsInChildren<TMPro.TMP_Text>()){t.ForceMeshUpdate();Check(!t.isTextOverflowing,"text fits: "+t.name);}ScreenCapture.CaptureScreenshot("Documentation/compact-hud.png");Next();break;
 case 1:g.Player.Crush();Next();break;
 case 2:var card=hud.transform.Find("Reference Layout/Menu Overlay/Menu Card").GetComponent<Image>();Check(g.State==GameState.Lost&&card.gameObject.activeInHierarchy,"death result shown");Check(card.sprite==null&&card.color.r>card.color.g*4&&card.color.a==1,"opaque red card");var overlay=card.transform.parent.GetComponent<Image>();Check(overlay.color.r<.1f,"no fullscreen red tint");ScreenCapture.CaptureScreenshot("Documentation/red-defeat-panel.png");Next();break;
 case 3:var so=new SerializedObject(hud);((Button)so.FindProperty("actionButton").objectReferenceValue).onClick.Invoke();Next();break;
 case 4:Check(g.State==GameState.Playing&&g.Player.Health==5,"retry restores gameplay");var normal=hud.transform.Find("Reference Layout/Menu Overlay/Menu Card").GetComponent<Image>();Check(normal.sprite!=null,"normal card skin restored");foreach(string scene in new[]{"Story","StoryEnd"}){var path="Assets/Scenes/"+scene+".unity";Check(File.ReadAllText(path).Contains("comic:"),scene+" comic references retained");}for(int i=1;i<=3;i++){var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Sprites/cutscene_"+i+".png");Check(sprite!=null&&sprite.texture.width==480&&sprite.texture.height==360,"4:3 panel imported "+i);Check(File.Exists("ArtSource/cutscene_"+i+".aseprite"),"native Aseprite source "+i);}Finish("PASS "+count+" checks: compact HUD text fit, opaque red death panel, neutral screen backdrop, retry restores skin and health, Story/StoryEnd references and three 480x360 Aseprite panels. Campaign save restored.");break;}
 }catch(Exception e){Finish("FAIL stage "+stage+": "+e);}}
}
