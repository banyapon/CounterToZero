using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DifficultyPolishChecks
{
    static bool running;
    static int stage, checks, nextFrame;
    static double next;
    static MinionController enemy;
    static SpriteRenderer playerSprite;
    const string SaveKey = "CountToZero.Campaign.v1";
    static DifficultyPolishChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += mode => {
            if (mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Difficulty.Check", false))
            { SessionState.SetBool("Difficulty.Check", false); running = true; stage = -1; checks = 0; next = EditorApplication.timeSinceStartup + 1; }
        };
    }
    [MenuItem("Count To Zero/Run Difficulty Polish Checks")]
    public static void Run()
    {
        SessionState.SetBool("Difficulty.HadSave", PlayerPrefs.HasKey(SaveKey)); SessionState.SetString("Difficulty.Save", PlayerPrefs.GetString(SaveKey));
        EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity"); SessionState.SetBool("Difficulty.Check", true); EditorApplication.isPlaying = true;
    }
    static void Check(bool pass, string label) { checks++; if (!pass) throw new Exception(label); }
    static void Next(double seconds = .1) { stage++; next = EditorApplication.timeSinceStartup + seconds; nextFrame = Time.frameCount + 3; }
    static void Finish(string result)
    {
        running = false;
        if (SessionState.GetBool("Difficulty.HadSave", false)) PlayerPrefs.SetString(SaveKey, SessionState.GetString("Difficulty.Save", "")); else PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save(); File.WriteAllText("Documentation/difficulty-polish-playchecks.txt", result); Debug.Log(result); EditorApplication.isPlaying = false;
    }
    static void Tick()
    {
        if (!running)
        {
            if (File.Exists("Temp/ctz-difficulty-check.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            { File.Delete("Temp/ctz-difficulty-check.request"); Run(); }
            return;
        }
        if (EditorApplication.timeSinceStartup < next || (stage > 0 && Time.frameCount < nextFrame)) return;
        try
        {
            var g = GameManager.Instance;
            switch (stage)
            {
                case -1:
                    Application.runInBackground = true; Next(1); break;
                case 0:
                    g.RecordScores=false;g.Begin();g.Tutorial.Skip();g.Player.enabled=false;g.Boss.enabled=false;
                    playerSprite=(SpriteRenderer)new SerializedObject(g.Player).FindProperty("sprite").objectReferenceValue;
                    Check(playerSprite.transform.localScale==Vector3.one*.85f,"player half scale");
                    var minions=UnityEngine.Object.FindObjectsByType<MinionController>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                    Check(minions.All(e=>((SpriteRenderer)new SerializedObject(e).FindProperty("sprite").objectReferenceValue).transform.localScale==Vector3.one*.85f),"all enemies half scale");
                    Campaign.Data.level=1;int easyCount=g.Encounters.BlockWaveSize;float easyInterval=g.Encounters.EventInterval;
                    Campaign.Data.level=91;Check(g.Encounters.BlockWaveSize>easyCount && g.Encounters.EventInterval<easyInterval,"later levels spawn more blocks more often");
                    g.Encounters.ResetRun();g.Encounters.SummonBlockWave(true);
                    var blocks=UnityEngine.Object.FindObjectsByType<ArenaObstacle>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(o=>o.Active).ToArray();
                    Check(blocks.Length==8,"late wave fills eight scattered block sites");Check(blocks.All(o=>!o.Solid),"warning before blocks become solid");
                    Check(blocks.All(o=>blocks.All(other=>o==other||Vector3.Distance(o.Position,other.Position)>=2)),"blocks have spaced random locations");
                    Check(UnityEngine.Object.FindObjectsByType<ElectricBlockPulse>(FindObjectsSortMode.None).Length>0,"purple perimeter pillars also pulse");
                    Check(ArenaObstacle.ElectricColor(0)==ArenaVisuals.Violet && ArenaObstacle.ElectricColor(.18f)==Color.white && ArenaObstacle.ElectricColor(.35f)==Color.black,"purple white black electrical loop");
                    g.Encounters.ResetRun();Campaign.Data.level=1;enemy=minions[0];enemy.Spawn(new Vector3(7,0,5));Next(3);break;
                case 1:
                    Check(enemy.ShotsFired==0,"early minions do not shoot");
                    Campaign.Data.level=61;enemy.Spawn(new Vector3(7,0,5));Next(3);break;
                case 2:
                    Check(enemy.ShotsFired>=5,"late minions fire five-shot volleys");
                    Check(MinionController.ShotCountAtLevel(21)==3 && MinionController.CanShootAtLevel(6),"intermediate shooting progression");
                    Check(MinionController.ShotIntervalAtLevel(91)<MinionController.ShotIntervalAtLevel(6),"later volleys fire faster");
                    g.Encounters.ResetRun();g.Bullets.Clear();g.Player.enabled=true;g.CutIn.Show();Next(.22);break;
                case 3:
                    var cutin=new SerializedObject(g.CutIn);var portrait=(UnityEngine.UI.Image)cutin.FindProperty("portrait").objectReferenceValue;
                    Check(g.CutIn.Visible && portrait.sprite.name=="aim_pixel","Lucky uses gun charge frames");
                    Check(portrait.color==Color.white && portrait.sprite.texture.filterMode==FilterMode.Point,"untinted sharp charge sprite");
                    ScreenCapture.CaptureScreenshot("Documentation/lucky-charge.png");
                    g.Player.OverchargeUntil=Time.time+100;Next(.15);break;
                case 4:
                    g.Finish(true);Next(1.7);break;
                case 5:
                    Check(g.State==GameState.Victory && g.Player.GetComponent<HeroAnimator>().Celebrating,"victory celebration active");
                    Check(playerSprite.color==Color.white,"victory stays untinted even during overcharge");
                    ScreenCapture.CaptureScreenshot("Documentation/small-hero-victory.png");Next(2.3);break;
                case 6:
                    Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="TurnEnd","victory still transitions to result");
                    Finish("PASS / "+checks+" checks: half-size actors, level-scaled random block waves, electric colors, actual early/late minion firing, sharp charge cut-in, untinted overcharged victory and result transition.");break;
            }
        }
        catch (Exception e) { Finish("FAIL / stage " + stage + ": " + e); }
    }
}
