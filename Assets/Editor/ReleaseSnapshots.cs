using System;
using System.IO;
using System.Text;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ReleaseSnapshots
{
    const string Save = "CountToZero.Campaign.v1";
    static readonly int[] Levels = { 1, 5, 10, 15, 20, 25, 30 };
    static bool running;
    static int index;
    static double next;
    static string folder;
    static readonly StringBuilder log = new StringBuilder();
    static ReleaseSnapshots()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (m,s,t) => { if(running && (t == LogType.Error || t == LogType.Exception)) log.AppendLine("ERROR: " + m); };
        EditorApplication.playModeStateChanged += state =>
        {
            if(state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Snapshots.Run",false))
            {
                SessionState.SetBool("Snapshots.Run",false);
                folder = SessionState.GetString("Snapshots.Folder", "Snapshot screen");
                running = true; index = 0; log.Clear();
                Campaign.NewGame(0); Campaign.Data.tutorialComplete = true; Campaign.Save();
                next = EditorApplication.timeSinceStartup + 9;
            }
            if(state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("Snapshots.Restore",false))
            {
                SessionState.SetBool("Snapshots.Restore",false);
                if(SessionState.GetBool("Snapshots.HadSave",false)) PlayerPrefs.SetString(Save,SessionState.GetString("Snapshots.Save",""));
                else PlayerPrefs.DeleteKey(Save);
                PlayerPrefs.Save();
                EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            }
        };
    }
    [MenuItem("Count To Zero/Capture 14 Release Snapshots")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play mode first.");
        folder = "Snapshot screen/" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(folder);
        SessionState.SetString("Snapshots.Folder",folder);
        SessionState.SetBool("Snapshots.HadSave",PlayerPrefs.HasKey(Save));
        SessionState.SetString("Snapshots.Save",PlayerPrefs.GetString(Save,""));
        SessionState.SetBool("Snapshots.Restore",true);
        EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        SessionState.SetBool("Snapshots.Run",true);
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if(!running || !EditorApplication.isPlaying) return;
        try
        {
            Application.runInBackground = true;
            var g = GameManager.Instance;
            if(g == null) return;
            g.RecordScores = false;
            if(g.State == GameState.Intro) { g.SkipIntro(); return; }
            if(g.Training) g.Tutorial.Skip();
            if(g.State == GameState.Playing)
            {
                g.Player.Heal();
                float t = Time.time;
                g.Player.transform.position = new Vector3(Mathf.Sin(t*.45f)*7,0,-3+Mathf.Cos(t*.6f)*2);
                g.Player.ModuloShot();
            }
            if(EditorApplication.timeSinceStartup < next) return;
            if(index == 14)
            {
                running = false;
                File.WriteAllText(folder + "/capture-log.txt", log.ToString());
                File.WriteAllText("Documentation/release-snapshots.txt", "Captured 14 live Play mode screenshots in " + folder + "\n" + log);
                EditorApplication.isPlaying = false;
                return;
            }
            // Start a new encounter after the previous screenshot has reached disk.
            if(index > 0 && index % 2 == 0 && Campaign.Data.level != Levels[index/2])
            {
                Campaign.Data.level = Levels[index/2]; Campaign.Save();
                SceneManager.LoadScene("CountToZero"); next = EditorApplication.timeSinceStartup + 9; return;
            }
            string name = (index+1).ToString("00") + "-level-" + Campaign.Data.level.ToString("00") + ".png";
            ScreenCapture.CaptureScreenshot(folder + "/" + name, 2);
            log.AppendLine(name + " state=" + g.State + " rule=" + g.Rule.Label + " screen=" + Screen.width + "x" + Screen.height);
            index++; next = EditorApplication.timeSinceStartup + (index == 14 ? 3 : 5);
        }
        catch(Exception e)
        {
            running = false; File.WriteAllText(folder + "/capture-log.txt", "FAILED: " + e + "\n" + log);
            EditorApplication.isPlaying = false;
        }
    }
}
