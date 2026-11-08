using System;
using System.IO;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class LuckyChargeChecks
{
 static bool running;static int stage,frame,count;static double next;static string errors="";const string Save="CountToZero.Campaign.v1";
 static LuckyChargeChecks(){Application.logMessageReceived+=(m,s,t)=>{if(running&&(t==LogType.Error||t==LogType.Exception))errors+=m;};EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Charge.Check",false)){SessionState.SetBool("Charge.Check",false);running=true;stage=-1;count=0;errors="";next=EditorApplication.timeSinceStartup+1;}if(m==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("Charge.Return",false)){SessionState.SetBool("Charge.Return",false);EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");}};EditorApplication.update+=Tick;}
 static void Check(bool p,string why){count++;if(!p)throw new Exception(why);}
 static void Next(){stage++;frame=Time.frameCount+3;next=EditorApplication.timeSinceStartup+1;}
 static void Finish(string message){running=false;if(SessionState.GetBool("Charge.Had",false))PlayerPrefs.SetString(Save,SessionState.GetString("Charge.Save",""));else PlayerPrefs.DeleteKey(Save);PlayerPrefs.Save();File.WriteAllText("Documentation/lucky-charge-checks.txt",errors.Length>0?"FAIL runtime: "+errors:message);SessionState.SetBool("Charge.Return",true);EditorApplication.isPlaying=false;}
 static void Tick(){try{if(!running){if(!File.Exists("Temp/ctz-charge-check.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-charge-check.request");SessionState.SetBool("Charge.Had",PlayerPrefs.HasKey(Save));SessionState.SetString("Charge.Save",PlayerPrefs.GetString(Save,""));EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");SessionState.SetBool("Charge.Check",true);EditorApplication.isPlaying=true;return;}if(EditorApplication.timeSinceStartup<next||(stage>=0&&Time.frameCount<frame))return;
 var g=GameManager.Instance;if(g.State==GameState.Paused)g.TogglePause();var hud=UnityEngine.Object.FindAnyObjectByType<HUDController>();
 switch(stage){case -1:Application.runInBackground=true;g.RecordScores=false;g.SkipIntro();g.Tutorial.Skip();g.Boss.enabled=false;g.Encounters.enabled=false;g.Dialogue.Clear();var setup=new SerializedObject(g.CutIn);setup.FindProperty("duration").floatValue=4;setup.ApplyModifiedPropertiesWithoutUndo();Next();break;
 case 0:g.CutIn.Show();Next();break;
 case 1:var so=new SerializedObject(g.CutIn);var portrait=(Image)so.FindProperty("portrait").objectReferenceValue;Check(g.CutIn.Visible,"cut-in visible during play");Check(AssetDatabase.GetAssetPath(portrait.sprite)=="Assets/GameAssets/UI/aim_pixel.png","supplied aim_pixel sprite used");Check(portrait.sprite.texture.filterMode==FilterMode.Point&&portrait.color==Color.white,"sharp untinted artwork");Check(so.FindProperty("punchFrames").arraySize==1,"single supplied image");SessionState.SetString("Charge.Frame",portrait.sprite.name);var caption=portrait.transform.parent.Find("Action Text").GetComponent<TMPro.TMP_Text>();caption.ForceMeshUpdate();Check(!caption.isTextOverflowing,"caption fits");ScreenCapture.CaptureScreenshot("Documentation/lucky-charge.png");Next();break;
 case 2:var image=(Image)new SerializedObject(g.CutIn).FindProperty("portrait").objectReferenceValue;Check(image.sprite.name==SessionState.GetString("Charge.Frame",""),"supplied portrait remains throughout cut-in");g.CutIn.Clear();Next();break;
 case 3:Check(!g.CutIn.Visible,"clear hides charge cut-in");Check(File.Exists("Assets/GameAssets/UI/aim_pixel.png"),"supplied PNG retained");Finish("PASS "+count+" checks: supplied aim_pixel portrait, no old animation frames, point filtering, readable caption and clean dismissal. Campaign save restored.");break;}

 }catch(Exception e){Finish("FAIL stage "+stage+": "+e);}}
}
