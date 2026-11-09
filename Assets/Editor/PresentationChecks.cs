using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PresentationChecks
{
    const string SaveKey = "CountToZero.Campaign.v1";
    static bool running;
    static int stage, checks;
    static double next;
    static float farSize;
    static Sprite front;
    static PresentationChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += mode => {
            if (mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Presentation.Check", false))
            { SessionState.SetBool("Presentation.Check", false); running = true; stage = checks = 0; next = EditorApplication.timeSinceStartup + 1; }
        };
    }
    [MenuItem("Count To Zero/Run Presentation Checks")]
    public static void Run()
    {
        SessionState.SetBool("Presentation.HadSave", PlayerPrefs.HasKey(SaveKey));
        SessionState.SetString("Presentation.Save", PlayerPrefs.GetString(SaveKey));
        SessionState.SetString("Presentation.Scene", SceneManager.GetActiveScene().path);
        EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");
        SessionState.SetBool("Presentation.Check", true); EditorApplication.isPlaying = true;
    }
    static void Check(bool pass, string label) { checks++; if (!pass) throw new Exception(label); }
    static void Next(double delay) { stage++; next = EditorApplication.timeSinceStartup + delay; }
    static void Finish(string result)
    {
        running = false;
        if (SessionState.GetBool("Presentation.HadSave", false)) PlayerPrefs.SetString(SaveKey, SessionState.GetString("Presentation.Save", "")); else PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        File.WriteAllText("Documentation/presentation-playchecks.txt", result); Debug.Log(result);
        EditorApplication.isPlaying = false;
    }
    static void Tick()
    {
        if (!running)
        {
            if (File.Exists("Temp/ctz-presentation-check.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            { File.Delete("Temp/ctz-presentation-check.request"); Run(); }
            return;
        }
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            var g = GameManager.Instance;
            switch (stage)
            {
                case 0:
                    g.RecordScores = false; g.SkipIntro(); g.Tutorial.Skip();
                    Check(g.Player.GetComponent<HeroAnimator>() != null, "hero animator");
                    var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/GameAssets/Sprites/man/1 walk.png").OfType<Sprite>().ToArray();
                    Check(sprites.Length == 8 && sprites.All(s => s.rect.size == new Vector2(16,16)), "eight correct 16x16 frames");
                    Check(g.Player.GetComponentsInChildren<ActorHealthBar>().Length == 1, "one world player HP bar");
                    Check(!UnityEngine.Object.FindAnyObjectByType<HUDController>().transform.Find("Reference Layout/Player HUD/HP Bar").gameObject.activeSelf, "duplicate player HUD HP hidden");
                    foreach (var im in UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (im.GetComponentInParent<VictorySequence>() == null && !im.transform.root.name.Contains("Victory") && im.sprite != null)
                            Check(AssetDatabase.GetAssetPath(im.sprite).Contains("PixelUISciFiFree") || (im.name=="Glitch Action Pose" && AssetDatabase.GetAssetPath(im.sprite).EndsWith("aim_pixel.png")), "UI skin: " + im.name + " / " + AssetDatabase.GetAssetPath(im.sprite));
                    g.Boss.enabled = false; g.Player.enabled = false;
                    g.Boss.transform.position = new Vector3(0,0,6); g.Player.transform.position = new Vector3(0,0,-7);
                    g.View.GetComponent<FollowCamera>().Snap(); Next(1); break;
                case 1:
                    farSize = g.View.orthographicSize;
                    g.Player.transform.position = new Vector3(0,0,3); Next(2); break;
                case 2:
                    Check(g.View.orthographicSize < farSize * .9f && g.View.orthographicSize > farSize * .8f, "gentle near-boss zoom: far=" + farSize + " near=" + g.View.orthographicSize + " state=" + g.State + " distance=" + Vector3.Distance(g.Player.transform.position,g.Boss.transform.position));
                    ScreenCapture.CaptureScreenshot("Documentation/presentation-gameplay.png");
                    g.Player.transform.position = new Vector3(0,0,-7); Next(2); break;
                case 3:
                    Check(Mathf.Abs(g.View.orthographicSize - farSize) < .1f, "far distance restores normal zoom");
                    g.Finish(true); Check(g.State == GameState.Victory, "victory holds gameplay scene");
                    Check(SceneManager.GetActiveScene().name == "CountToZero", "no early results"); Next(.12); break;
                case 4:
                    Check(g.State == GameState.Victory, "impact sequence active");
                    Check(UnityEngine.Object.FindAnyObjectByType<HUDController>().GetComponent<CanvasGroup>().alpha == 0, "HUD hidden during impact");
                    ScreenCapture.CaptureScreenshot("Documentation/presentation-impact.png"); Next(1.5); break;
                case 5:
                    Check(g.Player.GetComponent<HeroAnimator>().Celebrating, "front animation before result");
                    front = g.Player.GetComponentInChildren<SpriteRenderer>().sprite;
                    Check(front.name.StartsWith("Front"), "front-facing sprite");
                    ScreenCapture.CaptureScreenshot("Documentation/presentation-celebration.png"); Next(.2); break;
                case 6:
                    Check(g.Player.GetComponentInChildren<SpriteRenderer>().sprite != front, "front frames loop while timeScale is zero"); Next(2); break;
                case 7:
                    Check(SceneManager.GetActiveScene().name == "TurnEnd", "result loads after celebration");
                    Check(Campaign.LastWon, "campaign win recorded"); Finish("PASS / " + checks + " presentation checks. Far/near/far zoom, theme, HP, impact, animation and delayed campaign transition."); break;
            }
        }
        catch (Exception e) { Finish("FAIL / stage " + stage + ": " + e); }
    }
}
