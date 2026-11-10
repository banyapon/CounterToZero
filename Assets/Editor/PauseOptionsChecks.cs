using System;
using System.IO;
using System.Linq;
using System.Text;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PauseOptionsChecks
{
    const string Save = "CountToZero.Campaign.v1";
    static bool running, baseline;
    static int stage, frame;
    static double next;
    static string folder;
    static readonly StringBuilder log = new StringBuilder();
    static PauseOptionsChecks()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (m,s,t) => { if(running && (t == LogType.Error || t == LogType.Exception)) log.AppendLine("RUNTIME ERROR: " + m); };
        EditorApplication.playModeStateChanged += state =>
        {
            if(state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("PauseCheck.Run",false))
            {
                SessionState.SetBool("PauseCheck.Run",false); baseline=SessionState.GetBool("PauseCheck.Baseline",false);
                folder=SessionState.GetString("PauseCheck.Folder",""); running=true; stage=0; frame=0; log.Clear();
                Campaign.NewGame(0); Campaign.Data.level=15; Campaign.Data.tutorialComplete=true; Campaign.Save();
                next=EditorApplication.timeSinceStartup+7;
            }
            if(state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("PauseCheck.Restore",false))
            {
                SessionState.SetBool("PauseCheck.Restore",false);
                if(SessionState.GetBool("PauseCheck.Had",false)) PlayerPrefs.SetString(Save,SessionState.GetString("PauseCheck.Save",""));
                else PlayerPrefs.DeleteKey(Save);
                PlayerPrefs.Save(); EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            }
        };
    }
    [MenuItem("Count To Zero/Checks/Pause Options Baseline")]
    public static void Baseline() => Start(true);
    [MenuItem("Count To Zero/Checks/Pause Options and Gameplay GIF")]
    public static void Final() => Start(false);
    static void Start(bool before)
    {
        folder=before?"Documentation/PauseOptions-Before":"Snapshot screen/Gameplay-GIF";
        Directory.CreateDirectory(folder); if(!before) Directory.CreateDirectory(folder+"/Frames");
        SessionState.SetString("PauseCheck.Folder",folder); SessionState.SetBool("PauseCheck.Baseline",before);
        SessionState.SetBool("PauseCheck.Had",PlayerPrefs.HasKey(Save)); SessionState.SetString("PauseCheck.Save",PlayerPrefs.GetString(Save,""));
        SessionState.SetBool("PauseCheck.Restore",true); SessionState.SetBool("PauseCheck.Run",true);
        EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        EditorApplication.isPlaying=true;
    }
    static void Wait(double seconds) { stage++; next=EditorApplication.timeSinceStartup+seconds; }
    static void Check(bool pass,string message) { if(!pass) throw new Exception(message); log.AppendLine("PASS: "+message); }
    static Rect Bounds(Selectable selectable)
    {
        var r=(RectTransform)selectable.transform;var corners=new Vector3[4];r.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
    }
    static void Inspect(OptionsController options)
    {
        var buttons=UnityEngine.Object.FindObjectsByType<Selectable>(FindObjectsInactive.Exclude).Where(b=>b.IsActive() && b.IsInteractable()).ToArray();
        foreach(var b in buttons) log.AppendLine(b.name+" "+Bounds(b)+" inOptions="+b.transform.IsChildOf(options.PanelTransform));
        foreach(var a in buttons) foreach(var b in buttons)
        {
            if(a==b || a.transform.IsChildOf(b.transform) || b.transform.IsChildOf(a.transform))continue;
            var x=Bounds(a);var y=Bounds(b);
            bool overlaps=Mathf.Min(x.xMax,y.xMax)-Mathf.Max(x.xMin,y.xMin)>2 && Mathf.Min(x.yMax,y.yMax)-Mathf.Max(x.yMin,y.yMin)>2;
            if(overlaps) { log.AppendLine("OVERLAP: "+a.name+" / "+b.name); if(!baseline)throw new Exception("Options controls overlap: "+a.name+" / "+b.name); }
        }
    }
    static void Finish(string result)
    {
        running=false;File.WriteAllText(folder+"/validation.txt",result+"\n"+log);
        EditorApplication.isPlaying=false;
    }
    static void Tick()
    {
        if(!running || !EditorApplication.isPlaying)return;
        try
        {
            var g=GameManager.Instance;if(g==null)return;
            g.RecordScores=false;Application.runInBackground=true;
            if(g.State==GameState.Intro){g.SkipIntro();return;}
            if(g.Training)g.Tutorial.Skip();
            var options=UnityEngine.Object.FindAnyObjectByType<OptionsController>();
            if(stage>=5 && g.State==GameState.Playing)
            {
                g.Player.Heal();g.Player.transform.position=new Vector3(Mathf.Sin(Time.time*.6f)*7,0,-3+Mathf.Cos(Time.time*.4f)*2);g.Player.ModuloShot();
            }
            if(EditorApplication.timeSinceStartup<next)return;
            switch(stage)
            {
                case 0: Check(g.State==GameState.Playing,"gameplay started");g.TogglePause();Wait(.6);break;
                case 1: ScreenCapture.CaptureScreenshot(folder+"/pause.png");options.OpenButton.onClick.Invoke();Wait(.6);break;
                case 2:
                    ScreenCapture.CaptureScreenshot(folder+"/options.png");Inspect(options);
                    if(!baseline) { Check(g.Controls.OptionsOpen,"options modal opened");Check(!options.OpenButton.gameObject.activeInHierarchy,"underlying Options button hidden");Check(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(options.PanelTransform),"focus inside options modal"); }
                    Wait(.5);break;
                case 3:
                    if(baseline){Finish("BASELINE");break;}
                    options.Close();Wait(.5);break;
                case 4:
                    Check(g.State==GameState.Paused&&!g.Controls.OptionsOpen,"Back restores Pause");Check(options.OpenButton.gameObject.activeInHierarchy,"Options button restored");
                    Check(EventSystem.current.currentSelectedGameObject==options.OpenButton.gameObject,"focus restored to Options");
                    g.TogglePause();Wait(1);break;
                case 5:
                    Check(g.State==GameState.Playing,"resume gameplay after Options");
                    ScreenCapture.CaptureScreenshot(folder+"/Frames/"+frame.ToString("D4")+".png");frame++;
                    if(frame==120){Wait(1);break;}next=EditorApplication.timeSinceStartup+.1;break;
                case 6:Finish("PASS: Pause -> Options -> Back -> Resume; recorded 120 gameplay frames at nominal 10 fps.");break;
            }
        }
        catch(Exception ex){Finish("FAIL: "+ex);}
    }
}
