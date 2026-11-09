using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class LuckyArmorChecks
{
 static bool running;static int stage,checks,nextFrame;static double next;static string errors="";const string Key="CountToZero.Campaign.v1";
 static LuckyArmorChecks(){Application.logMessageReceived+=(m,s,t)=>{if(running&&(t==LogType.Exception||t==LogType.Error))errors+=m+"\n"+s;};EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Armor.Check",false)){SessionState.SetBool("Armor.Check",false);running=true;stage=-1;checks=0;errors="";next=EditorApplication.timeSinceStartup+1;}if(m==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("Armor.Return",false)){SessionState.SetBool("Armor.Return",false);EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");}};}
 static void Check(bool pass,string label){checks++;if(!pass)throw new Exception(label);}
 static void Next(float delay=.35f){stage++;next=EditorApplication.timeSinceStartup+delay;nextFrame=Time.frameCount+3;}
 static void Finish(string result){running=false;if(errors.Length>0)result="FAIL runtime: "+errors+"\n"+result;if(SessionState.GetBool("Armor.Had",false))PlayerPrefs.SetString(Key,SessionState.GetString("Armor.Save",""));else PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();File.WriteAllText("Documentation/lucky-armor-checks.txt",result);Debug.Log(result);SessionState.SetBool("Armor.Return",true);EditorApplication.isPlaying=false;}
 static void Tick(){try{
 if(!running){if(!File.Exists("Temp/ctz-armor-check.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-armor-check.request");SessionState.SetBool("Armor.Had",PlayerPrefs.HasKey(Key));SessionState.SetString("Armor.Save",PlayerPrefs.GetString(Key));EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");SessionState.SetBool("Armor.Check",true);EditorApplication.isPlaying=true;return;}
 if(EditorApplication.timeSinceStartup<next||(stage>=0&&Time.frameCount<nextFrame))return;var g=GameManager.Instance;
 switch(stage){
 case -1:Application.runInBackground=true;Next(1);break;
 case 0:
  g.RecordScores=false;Campaign.NewGame();Campaign.Data.level=6;g.Begin();g.Tutorial.Skip();g.Orbs.Clear();g.Boss.enabled=g.Player.enabled=g.Encounters.enabled=false;BattleExpansion.Instance.enabled=false;g.Hazards.Configure(SectorMode.Standard);
  var patterns=(BossPatternDefinition[])typeof(BossController).GetField("patterns",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(g.Boss);Check(patterns.Count(p=>p.luckyNumberOnly)==4,"four selected patterns use armor");Check(g.Boss.LuckyNumberOnly,"level six armor active");
  g.DamageBoss(5);Check(g.Boss.Health==99,"normal damage blocked");var audio=new SerializedObject(GameAudio.Instance);var source=(AudioSource)audio.FindProperty("effects").objectReferenceValue;Check(source.isPlaying,"blocked sound plays");Check(g.Feedback.Message.Contains("SHOT BLOCKED"),"blocked feedback message");var clips=audio.FindProperty("cues");Check(clips.GetArrayElementAtIndex((int)SoundCue.Blocked).FindPropertyRelative("clip").objectReferenceValue.name=="Blocked","dedicated rejection cue assigned");
  var timer=typeof(BossController).GetField("blockedUntil",BindingFlags.NonPublic|BindingFlags.Instance);float deadline=(float)timer.GetValue(g.Boss);for(int i=0;i<20;i++)g.DamageBoss(1);Check((float)timer.GetValue(g.Boss)==deadline,"rapid hits share feedback cooldown");
  g.DamageBoss(10,true);Check(g.Boss.Health==99,"bypass flag cannot bypass lucky restriction");g.Boss.CrashUntil=Time.time+5;g.DamageBoss(1);Check(g.Boss.Health==99,"crash does not bypass lucky restriction");g.Boss.CrashUntil=0;g.Overflow(g.Boss);Check(g.Boss.Health==99,"boss overflow is not lucky damage");
  g.Bullets.Fire(g.Boss.transform.position+Vector3.back*1.3f,Vector3.forward,false,16);Next();break;
 case 1:Check(g.Boss.Health==99,"actual ordinary projectile blocked");g.Bullets.Fire(g.Boss.transform.position+Vector3.back*1.3f,Vector3.forward,false,16,true);Next();break;
 case 2:
  Check(g.Boss.Health==99,"charged homing projectile also blocked");g.Boss.ShieldUntil=Time.time+10;g.Player.ApplyMathOperation(MathOpType.Set,7);Check(g.Boss.Health==80,"exact lucky match deals 19 despite shield");
  var types=new[]{RuleType.EvenNumber,RuleType.OddNumber,RuleType.MultipleOf,RuleType.PrimeNumber,RuleType.GreaterThan,RuleType.LessThan};int[] values={2,3,6,5,5,2};
  for(int i=0;i<types.Length;i++){g.Begin();g.Tutorial.Skip();g.Orbs.Clear();typeof(GameManager).GetField("<Rule>k__BackingField",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,LuckyRuleEngine.Create(types[i],3));g.Player.ApplyMathOperation(MathOpType.Set,values[i]);Check(g.Boss.Health==89,"non-exact lucky rule deals direct damage: "+types[i]);}
  g.Begin();g.Tutorial.Skip();g.Boss.SelectPattern(0);g.DamageBoss(1);Check(g.Boss.Health==98,"ordinary pattern still takes shots");g.Overflow(g.Boss);Check(g.Boss.Health<98,"ordinary pattern still takes overflow");
  g.Begin();g.Tutorial.Skip();g.Orbs.Clear();g.Dialogue.Clear();g.Boss.enabled=true;g.Boss.CrashUntil=Time.time+100;g.DamageBoss(.22f);Next();break;
 case 3:Check(UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("LUCKY NUMBER ONLY")),"persistent HUD instruction visible");ScreenCapture.CaptureScreenshot("Documentation/lucky-armor.png");Next(1);break;
 case 4:Finish("PASS "+checks+" lucky armor checks: chosen patterns, ordinary/charged shots, rejection cue and cooldown, crash/overflow protection, all seven lucky rule types, normal-boss regressions and HUD instruction. Campaign save restored.");break;
 }
 }catch(Exception e){Finish("FAIL "+checks+": "+e);}}
}
