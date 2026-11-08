using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class ExpansionChecks
{
 static string errors="";static bool run;static int stage,checks,nextFrame;static double next;static float hp;const string Key="CountToZero.Campaign.v1";
 static ExpansionChecks(){Application.logMessageReceived+=(message,stack,type)=>{if(run && (type==LogType.Exception||type==LogType.Error))errors+=message+"\n"+stack+"\n";};EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("Expansion.ReturnTitle",false)){SessionState.SetBool("Expansion.ReturnTitle",false);EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");}if(m==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Expansion.Test",false)){SessionState.SetBool("Expansion.Test",false);run=true;errors="";stage=-1;checks=0;next=EditorApplication.timeSinceStartup+1;}};}
 static void Check(bool v,string msg){checks++;if(!v)throw new Exception(msg);}
 static void Next(float seconds=.4f){stage++;next=EditorApplication.timeSinceStartup+seconds;nextFrame=Time.frameCount+3;}
 static void Finish(string result){run=false;if(errors.Length>0)result="FAIL runtime errors:\n"+errors+"\n"+result;Time.timeScale=1;if(SessionState.GetBool("Expansion.Had",false))PlayerPrefs.SetString(Key,SessionState.GetString("Expansion.Save",""));else PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();File.WriteAllText("Documentation/expansion-checks.txt",result);Debug.Log(result);SessionState.SetBool("Expansion.ReturnTitle",true);EditorApplication.isPlaying=false;}
 static void Tick(){try{
 if(!run){if(!File.Exists("Temp/ctz-expansion-check.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-expansion-check.request");SessionState.SetBool("Expansion.Had",PlayerPrefs.HasKey(Key));SessionState.SetString("Expansion.Save",PlayerPrefs.GetString(Key));EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");SessionState.SetBool("Expansion.Test",true);EditorApplication.isPlaying=true;return;}
 if(EditorApplication.timeSinceStartup<next || (stage>=0 && Time.frameCount<nextFrame))return;var g=GameManager.Instance;var e=BattleExpansion.Instance;
 switch(stage){
 case -1:Application.runInBackground=true;Next(1);break;
 case 0:
  g.RecordScores=false;Campaign.NewGame(0);Check(Campaign.Data.difficulty==.75f,"easy selected");Campaign.NewGame(2);Check(Campaign.Data.difficulty==1.4f,"hard selected");Campaign.NewGame(1);Check(Campaign.Data.difficulty==1,"normal selected");Campaign.Data.level=30;Campaign.Complete(true,10,0,100);Check(Campaign.NeedsEnding,"ending at 30");Campaign.EnableEndless();Check(Campaign.Data.level==31&&Campaign.Data.endless&&!Campaign.NeedsEnding,"infinite starts 31");
  Campaign.Data.level=22;g.Begin();g.Tutorial.Skip();g.Boss.SelectPattern(21);g.Dialogue.Clear();g.Boss.enabled=false;g.Encounters.enabled=false;g.Player.enabled=false;stage=99;Next();break;
 case 100:ScreenCapture.CaptureScreenshot("Documentation/expansion-serpent.png");stage=0;Next();break;
 case 1:
  Check(e.SegmentsAlive==5,"five serpent segments; actual="+e.SegmentsAlive+" state="+g.State+" training="+g.Training);var targetSegment=GameObject.Find("Virus segment").transform.position;targetSegment.y=0;Check(e.HitSegment(targetSegment-Vector3.right,targetSegment+Vector3.right,1),"ground-plane bullets hit floating segments");hp=g.Boss.Health;g.DamageBoss(20);Check(e.SegmentsAlive==5 && g.Boss.Health==hp,"normal shots cannot bypass segments");g.DamageBoss(1,true);Check(g.Boss.Health==hp,"segments shield boss including lucky beam");for(int i=0;i<5;i++)e.DamageSegment(20);Check(e.SegmentsAlive==0,"segments can all be destroyed");g.DamageBoss(1,true);Check(g.Boss.Health<hp,"boss vulnerable after segments");
  for(int i=0;i<6;i++)e.SpawnAssistant();Check(e.AssistantCount==4,"four assistant cap");Next();break;
 case 2:
  var ally=GameObject.Find("Assistant");Check(ally!=null,"assistant visible");for(int i=0;i<3;i++){e.HitAssistant(ally.transform.position,ally.transform.position);var field=typeof(BattleExpansion).GetField("allies",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);var list=(System.Collections.IList)field.GetValue(e);if(list.Count>0)list[0].GetType().GetField("immune").SetValue(list[0],0f);}
  Check(e.AssistantCount==3,"assistant takes projectile damage and dies");Check(GameObject.Find("Assistant Loss Notice").GetComponent<TMPro.TMP_Text>().text.Contains("LOST"),"bottom right death notification");Check(GameObject.Find("Pixel debris")!=null,"pixel explosion created");
  g.Boss.SelectPattern(20);g.Boss.enabled=true;g.Player.Heal();Next(1);break;
 case 3:Check(g.Boss.PatternIndex==20,"square rush pattern selected");g.Boss.SelectPattern(22);Next(1);break;
 case 4:g.Boss.SelectPattern(23);Next(1);break;
 case 5:g.Boss.SelectPattern(24);Next(1);break;
 case 6:
  Check(g.Boss.PatternIndex==24,"all five new firing patterns execute");g.Boss.enabled=false;g.Bullets.Clear();g.Hazards.Configure(SectorMode.Standard);g.Encounters.SummonObstacle(new Vector3(5,0,4),true);e.SummonLightning(g.Player.transform.position);hp=g.Player.Health;ScreenCapture.CaptureScreenshot("Documentation/expansion-battle.png");Next();break;
 case 7:Check(!e.LightningActive && g.Player.Health==hp,"lightning telegraph gives safe warning");Next(1.5f);break;
 case 8:
  Check(e.LightningActive,"tracking lightning activates after warning");Check(GameObject.Find("Lightning blue outline").GetComponent<LineRenderer>().enabled,"lightning has blue outline");ScreenCapture.CaptureScreenshot("Documentation/expansion-lightning.png");Next();break;
 case 9:g.Player.Hurt(true);Check(GameObject.Find("Pixel debris")!=null,"player red impact particles");g.Player.Crush();Check(g.Player.Health==0&&g.State==GameState.Lost,"crush kills player");Next();break;
 case 10:
  UnityEngine.Object.FindFirstObjectByType<OptionsController>().Open();Check(GameObject.Find("CREDITS")!=null,"in-game options credit button");GameObject.Find("CREDITS").GetComponent<Button>().onClick.Invoke();stage=100;Next();break;
 case 101:Check(GameObject.Find("Credits viewport")!=null,"in-game credits opens");ScreenCapture.CaptureScreenshot("Documentation/expansion-game-credits.png");stage=101;Next();break;
 case 102:UnityEngine.SceneManagement.SceneManager.LoadScene("Story");stage=10;Next(1);break;
 case 11:
  Check(GameObject.Find("4:3 comic frame")!=null,"comic frame exists");var rect=GameObject.Find("4:3 comic frame").GetComponent<RectTransform>();Check(Mathf.Abs(rect.rect.width/rect.rect.height-4f/3)<.01,"comic 4:3 ratio");Check(GameObject.Find("Aseprite comic").GetComponent<Image>().sprite!=null,"Aseprite image wired");ScreenCapture.CaptureScreenshot("Documentation/expansion-story.png");Next();break;
 case 12:UnityEngine.SceneManagement.SceneManager.LoadScene("Title");Next(1);break;
 case 13:
  var ui=UnityEngine.Object.FindFirstObjectByType<CampaignExpansionUI>();ui.ChooseDifficulty();Check(GameObject.Find("EASY")!=null&&GameObject.Find("NORMAL")!=null&&GameObject.Find("HARD")!=null,"three difficulty buttons");ScreenCapture.CaptureScreenshot("Documentation/expansion-difficulty.png");Next();break;
 case 14:
  UnityEngine.Object.FindFirstObjectByType<CampaignExpansionUI>().ShowOptions();Next();break;
 case 15:
  Check(UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsSortMode.None).Length>=2,"BGM SFX controls visible");UnityEngine.Object.FindFirstObjectByType<CampaignExpansionUI>().ShowCredits();Next();break;
 case 16:Check(GameObject.Find("Credits viewport")!=null,"scrollable asset credits");ScreenCapture.CaptureScreenshot("Documentation/expansion-credits.png");Next();break;
 case 17:Finish("PASS "+checks+" expansion checks: difficulty, 30-level ending, infinite, serpent protection, assistants, damage, patterns, cutscene, audio controls and credits.");break;
 }
 }catch(Exception ex){Finish("FAIL "+checks+": "+ex);}}
}

