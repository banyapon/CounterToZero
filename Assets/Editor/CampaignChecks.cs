using System;
using System.IO;
using System.Collections.Generic;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
[InitializeOnLoad]
public static class CampaignChecks
{
    static int stage,checks;static bool running;static double next,deadline;static string saved;static bool hadSave;static Mouse mouse;static Keyboard keyboard;static float zoom;static Vector3 position;
    static InputSettings.EditorInputBehaviorInPlayMode oldEditorInput; static InputSettings.BackgroundBehavior oldBackground;
    const string SaveKey="CountToZero.Campaign.v1";
    static CampaignChecks(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("CTZ.CampaignCheck",false)){SessionState.SetBool("CTZ.CampaignCheck",false);running=true;stage=checks=0;next=EditorApplication.timeSinceStartup+1;deadline=next+100;}};}
    [MenuItem("Count To Zero/Run Campaign Checks")]
    public static void Run(){hadSave=PlayerPrefs.HasKey(SaveKey);saved=PlayerPrefs.GetString(SaveKey,"");SessionState.SetBool("CTZ.CampaignHadSave",hadSave);SessionState.SetString("CTZ.CampaignSaved",saved);EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("CTZ.CampaignCheck",true);EditorApplication.isPlaying=true;}
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static void Next(float wait=.5f){stage++;next=EditorApplication.timeSinceStartup+wait;}
    static void Click(string name){var go=GameObject.Find("Screen Canvas/"+name);Check(go!=null,"button exists "+name);var b=go.GetComponent<Button>();Check(b.interactable,"button enabled "+name);var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,b.transform.position)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&hits[0].gameObject==go,"button raycast "+name);b.onClick.Invoke();}
    static void Finish(string message){running=false;if(mouse!=null){InputSystem.RemoveDevice(mouse);mouse=null;}InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;InputSystem.settings.backgroundBehavior=oldBackground;if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}if(SessionState.GetBool("CTZ.CampaignHadSave",false))PlayerPrefs.SetString(SaveKey,SessionState.GetString("CTZ.CampaignSaved",""));else PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();File.WriteAllText("Documentation/campaign-validation.txt",message);Debug.Log(message);EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.Exit(message.StartsWith("PASS")?0:1);}
    static void Tick()
    {
        if(!running){if(File.Exists("Temp/ctz-campaign-check.request")&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlaying){File.Delete("Temp/ctz-campaign-check.request");Run();}return;}
        if(EditorApplication.timeSinceStartup<next)return;
        try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout at stage "+stage);
            string scene=SceneManager.GetActiveScene().name;var g=GameManager.Instance;
            switch(stage){
                case 0:
                    oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;oldBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Check(scene=="Title","title scene");Campaign.NewGame();
                    for(int i=1;i<=30;i++){Check(Campaign.Data.level==i,"level advances "+i);Check(Campaign.PatternIndex==(i-1)%25,"pattern cycle "+i);Campaign.Complete(true,60,1,100);Check(Campaign.BlockComplete==(i%20==0),"block boundary "+i);if(i<30)Campaign.Advance();}
                    Check(Campaign.Data.blocks==1&&Campaign.NeedsEnding,"30 levels reaches ending");Check(Campaign.Resume()&&Campaign.Data.cleared==30,"save reload");Campaign.EnableEndless();Check(Campaign.Data.level==31&&Campaign.Data.endless&&!Campaign.NeedsEnding,"endless starts 31");
                    Check(Campaign.Adapt(1,2400,100,10)<Campaign.Adapt(1,800,5,0),"performance adaptive difficulty");
                    Click("Continue");GameObject.Find("NORMAL").GetComponent<Button>().onClick.Invoke();Next();break;
                case 1:if(scene!="Story")return;Check(Campaign.Data.level==1,"new game resets progress");Click("Back");Next(1);break;
                case 2:if(scene!="CountToZero")return;Check(g.State==GameState.Intro&&g.View.GetComponent<FollowCamera>().Touring,"camera intro blocks combat");Check(g.Elapsed==0,"no combat clock during intro");Next(6.2f);break;
                case 3:
                    Check(g.State==GameState.Playing&&!g.View.GetComponent<FollowCamera>().Touring,"intro returns to gameplay");if(g.Training)g.Tutorial.Skip();Check(Mathf.Abs(Mathf.DeltaAngle(g.View.transform.eulerAngles.y,0))<.01f&&Mathf.Abs(Mathf.DeltaAngle(g.View.transform.eulerAngles.z,0))<.01f,"camera has no sideways tilt");
                    g.RecordScores=false;g.Boss.CrashUntil=Time.time+100;g.Encounters.ResetRun();g.Orbs.Clear();zoom=g.View.orthographicSize;position=g.Player.transform.position;keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl,Key.UpArrow));Next(.4f);break;
                case 4:
                    Check(g.View.orthographicSize<zoom,"Ctrl Up zoom in: before="+zoom+" after="+g.View.orthographicSize+" state="+g.State+" key="+Keyboard.current.upArrowKey.isPressed+" ctrl="+Keyboard.current.leftCtrlKey.isPressed+" dt="+Time.unscaledDeltaTime);Check(Vector3.Distance(position,g.Player.transform.position)<.01f,"Ctrl Up does not move player");zoom=g.View.orthographicSize;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftCtrl,Key.DownArrow));Next(.4f);break;
                case 5:
                    Check(g.View.orthographicSize>zoom,"Ctrl Down zoom out");InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.RemoveDevice(keyboard);keyboard=null;zoom=g.View.orthographicSize;mouse=InputSystem.AddDevice<Mouse>();InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(Screen.width*.5f,Screen.height*.5f),scroll=new Vector2(0,120)});stage=50;next=EditorApplication.timeSinceStartup+.4;break;
                case 50:
                    Check(g.View.orthographicSize<zoom,"mouse wheel zoom in");InputSystem.RemoveDevice(mouse);mouse=null;stage=5;
                    var patterns=new HashSet<string>();for(int i=0;i<25;i++){g.Boss.SelectPattern(i);patterns.Add(g.Boss.PatternName);g.Bullets.Clear();g.Boss.CrashUntil=0;typeof(BossController).GetField("fireAt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(g.Boss,-1f);g.Boss.SendMessage("Update");var bullets=(System.Collections.ICollection)typeof(BulletPattern).GetField("active",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(g.Bullets);Check(bullets.Count>0,"pattern fires "+i);}Check(patterns.Count==25,"25 distinct boss pattern assets");
                    g.Finish(false);Next();break;
                case 6:Check(scene=="CountToZero"&&g.State==GameState.Lost&&!Campaign.LastWon&&Campaign.Data.level==1,"loss keeps level and scene");var retryHud=UnityEngine.Object.FindAnyObjectByType<HUDController>();((Button)new SerializedObject(retryHud).FindProperty("actionButton").objectReferenceValue).onClick.Invoke();Next(.5f);break;
                case 7:if(scene!="CountToZero"||g.State!=GameState.Playing)return;g.RecordScores=false;g.DamageBoss(33,true);g.Boss.CrashUntil=0;g.RecordScores=false;g.DamageBoss(33,true);g.Boss.CrashUntil=0;g.DamageBoss(33,true);Next();break;
                case 8:if(scene!="TurnEnd")return;Check(Campaign.LastWon&&Campaign.Data.cleared==1,"boss defeat records completed level");Campaign.Data.level=30;Campaign.Data.cleared=29;Campaign.Complete(true,60,0,100);Click("Continue");Next();break;
                case 9:if(scene!="StoryEnd")return;Click("Back");Check(GameObject.Find("Screen Canvas/Continue/Label").GetComponent<TMPro.TMP_Text>().text.Contains("ENDLESS"),"ending skip presents endless choice");Click("Back");Next();break;
                case 10:if(scene!="Title")return;Check(!Campaign.Active,"declining endless returns to title");Click("Back");Next();break;
                case 11:if(scene!="StoryEnd")return;Click("Back");Click("Continue");Next(1);break;
                case 12:if(scene!="CountToZero")return;Check(Campaign.Data.endless&&Campaign.Data.level==31,"choice loads endless level");Check(UnityEngine.Object.FindObjectsByType<GameAudio>(FindObjectsInactive.Include).Length==1,"single persistent audio director across scenes");Finish("PASS: "+checks+" campaign checks. 30-level progression, save/resume, adaptive difficulty, actual scene transitions and UI raycasts, story skip, camera intro, Ctrl and mouse-wheel zoom, all 25 patterns emitting bullets, boss defeat/retry, ending, declining and accepting Endless.\nPhysical mobile/browser not tested.\n");break;
            }
        }catch(Exception e){Finish("FAIL stage "+stage+": "+e);}
    }
}
