using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[InitializeOnLoad]
public static class MobileSkinChecks
{
 static bool running;static int stage,checks,nextFrame,shots,dashes;static double next;static string errors="";static Joystick joy;static TouchHoldButton fire;static GameObject root;const string SaveKey="CountToZero.Campaign.v1";
 static MobileSkinChecks(){Application.logMessageReceived+=(m,s,t)=>{if(running&&(t==LogType.Exception||t==LogType.Error))errors+=m+"\n"+s;};EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Mobile.Check",false)){SessionState.SetBool("Mobile.Check",false);running=true;stage=-1;checks=0;errors="";next=EditorApplication.timeSinceStartup+1;}if(m==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("Mobile.Return",false)){SessionState.SetBool("Mobile.Return",false);EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");}};}
 static void Check(bool pass,string label){checks++;if(!pass)throw new Exception(label);}
 static void Next(float seconds=.35f){stage++;next=EditorApplication.timeSinceStartup+seconds;nextFrame=Time.frameCount+3;}
 static void Finish(string result){running=false;if(errors.Length>0)result="FAIL runtime errors: "+errors+"\n"+result;if(SessionState.GetBool("Mobile.Had",false))PlayerPrefs.SetString(SaveKey,SessionState.GetString("Mobile.Save",""));else PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();File.WriteAllText("Documentation/mobile-skin-checks.txt",result);Debug.Log(result);SessionState.SetBool("Mobile.Return",true);EditorApplication.isPlaying=false;}
 static void Tick(){try{
 if(!running){if(!File.Exists("Temp/ctz-mobile-check.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-mobile-check.request");SessionState.SetBool("Mobile.Had",PlayerPrefs.HasKey(SaveKey));SessionState.SetString("Mobile.Save",PlayerPrefs.GetString(SaveKey));EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");SessionState.SetBool("Mobile.Check",true);EditorApplication.isPlaying=true;return;}
 if(EditorApplication.timeSinceStartup<next||(stage>=0&&Time.frameCount<nextFrame))return;var g=GameManager.Instance;
 switch(stage){
 case -1:Application.runInBackground=true;Next(1);break;
 case 0:g.RecordScores=false;g.Begin();g.Tutorial.Skip();g.Boss.enabled=false;g.Encounters.enabled=false;g.Hazards.Configure(SectorMode.Standard);g.Dialogue.Clear();typeof(PlayerInputRouter).GetField("<TouchVisible>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g.Controls,true);var so=new SerializedObject(g.Controls);joy=(Joystick)so.FindProperty("joystick").objectReferenceValue;fire=(TouchHoldButton)so.FindProperty("fireButton").objectReferenceValue;root=(GameObject)so.FindProperty("touchRoot").objectReferenceValue;Next();break;
 case 1:
  Check(root.activeSelf,"touch surface visible");Check(root.GetComponent<MobileControlLayout>()!=null,"safe area layout attached");var js=new SerializedObject(joy);var bg=(RectTransform)js.FindProperty("background").objectReferenceValue;var h=(RectTransform)js.FindProperty("handle").objectReferenceValue;
  Check(bg.GetComponent<Image>().sprite.name=="Analog_White"&&h.GetComponent<Image>().sprite.name=="Analog_White","analog uses sliced sprite");Check(fire.GetComponent<Image>().sprite.name=="Fire_Orange_B","orange fire slice");Check(root.GetComponentsInChildren<Button>().Where(b=>b.name!="Fire").All(b=>((Image)b.targetGraphic).sprite.name.Contains("White")),"other buttons white");
  var center=RectTransformUtility.WorldToScreenPoint(null,bg.position);joy.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=1,position=center+new Vector2(70,0)});fire.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=2});shots=g.Player.ShotsFired;dashes=g.Player.DashesUsed;Next();break;
 case 2:
  Check(g.Controls.Move.x>.1f&&g.Controls.FireHeld&&g.Player.ShotsFired>shots,"analog movement and held fire simultaneously");fire.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=3});Check(fire.Held,"unrelated touch cannot release fire");ScreenCapture.CaptureScreenshot("Documentation/mobile-controls.png");root.GetComponentsInChildren<Button>().First(b=>b.name=="Dash").onClick.Invoke();Next();break;
 case 3:
  Check(g.Player.DashesUsed>dashes,"slide button triggers dash");g.Score.Focus=1;root.GetComponentsInChildren<Button>().First(b=>b.name=="Focus").onClick.Invoke();Next();break;
 case 4:
  Check(g.SlowUntil>Time.unscaledTime,"focus button works");fire.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=2});joy.OnPointerUp(null);Next();break;
 case 5:Check(!fire.Held&&!g.Controls.FireHeld&&joy.Direction==Vector2.zero,"release stops movement and fire");fire.OnPointerDown(new PointerEventData(EventSystem.current){pointerId=2});g.TogglePause();Next();break;
 case 6:Check(!root.activeSelf&&!fire.Held&&joy.Direction==Vector2.zero,"pause hides and resets touch controls");Finish("PASS "+checks+" mobile control checks: slicing, white analog, orange fire, simultaneous movement/fire, touch ownership, slide/focus, release and pause reset. Editor simulated touches; physical device not tested.");break;
 }
 }catch(Exception e){Finish("FAIL "+checks+": "+e);}}
}
