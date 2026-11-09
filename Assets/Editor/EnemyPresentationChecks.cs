using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EnemyPresentationChecks
{
    static bool running;
    static int stage, checks, nextFrame;
    static double next;
    static MinionController enemy;
    static PixelDeathRing ring;
    static SpriteRenderer enemySprite, playerSprite;
    static float initialScale;
    static int killScore;
    const string SaveKey = "CountToZero.Campaign.v1";
    static EnemyPresentationChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += mode => {
            if (mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Enemy.Check", false))
            { SessionState.SetBool("Enemy.Check", false); running = true; stage = -1; checks = 0; next = EditorApplication.timeSinceStartup + 1; }
        };
    }
    [MenuItem("Count To Zero/Run Enemy Presentation Checks")]
    public static void Run()
    {
        SessionState.SetBool("Enemy.HadSave", PlayerPrefs.HasKey(SaveKey)); SessionState.SetString("Enemy.Save", PlayerPrefs.GetString(SaveKey));
        EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity"); SessionState.SetBool("Enemy.Check", true); EditorApplication.isPlaying = true;
    }
    static void Check(bool pass, string label) { checks++; if (!pass) throw new Exception(label); }
    static void Next(double seconds = .1) { stage++; next = EditorApplication.timeSinceStartup + seconds; nextFrame = Time.frameCount + 3; }
    static void Finish(string result)
    {
        running = false;
        if (SessionState.GetBool("Enemy.HadSave", false)) PlayerPrefs.SetString(SaveKey, SessionState.GetString("Enemy.Save", "")); else PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save(); File.WriteAllText("Documentation/enemy-presentation-playchecks.txt", result); Debug.Log(result); EditorApplication.isPlaying = false;
    }
    static void Tick()
    {
        if (!running)
        {
            if (File.Exists("Temp/ctz-enemy-check.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            { File.Delete("Temp/ctz-enemy-check.request"); Run(); }
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
                    Application.runInBackground = true; g.RecordScores = false; g.Begin(); g.Tutorial.Skip(); Check(g.State == GameState.Playing, "playing: " + g.State);
                    g.Player.enabled = g.Boss.enabled = false;
                    var enemies = UnityEngine.Object.FindObjectsByType<MinionController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    Check(enemies.Length == 6 && enemies.All(e => e.GetComponent<HeroAnimator>() != null), "all six enemies animated");
                    var frames = AssetDatabase.LoadAllAssetsAtPath("Assets/GameAssets/Sprites/man/8walk.png").OfType<Sprite>().ToArray();
                    Check(frames.Length == 8 && frames.All(s => s.rect.size == new Vector2(16,16)), "eight 16x16 enemy frames");
                    enemy = enemies[0]; enemy.enabled = false; enemy.Spawn(new Vector3(3,0,0));
                    ring = (PixelDeathRing)new SerializedObject(enemy).FindProperty("deathRing").objectReferenceValue;
                    enemySprite = (SpriteRenderer)new SerializedObject(enemy).FindProperty("sprite").objectReferenceValue;
                    playerSprite = (SpriteRenderer)new SerializedObject(g.Player).FindProperty("sprite").objectReferenceValue;
                    g.Boss.transform.position = Vector3.zero; g.Player.transform.position = new Vector3(0,0,3); Next(); break;
                case 1:
                    Check(playerSprite.sprite.name.Contains("Front"), "hero above boss faces front: " + playerSprite.sprite.name + " player=" + g.Player.transform.position + " boss=" + g.Boss.transform.position + " state=" + g.State);
                    Check(enemySprite.sprite.name.Contains("Back"), "enemy faces away toward player above");
                    g.Player.transform.position = new Vector3(0,0,-3); Next(); break;
                case 2:
                    Check(playerSprite.sprite.name.Contains("Back"), "hero below boss faces away");
                    Check(enemySprite.sprite.name.Contains("Front"), "enemy faces front toward player below");
                    enemy.Hit(3); killScore = g.Score.Score;
                    Check(!enemy.Active && ring.gameObject.activeInHierarchy, "death ring survives disabled enemy");
                    Check(ring.GetComponent<SpriteRenderer>().color == Color.white, "white pixel ring");
                    initialScale = ring.transform.localScale.x; enemy.Hit(3); Check(g.Score.Score == killScore, "dead enemies cannot award duplicate score"); Next(.15); break;
                case 3:
                    Check(ring.gameObject.activeInHierarchy && ring.transform.localScale.x > initialScale, "death ring expands");
                    ScreenCapture.CaptureScreenshot("Documentation/enemy-death-ring.png"); Next(.5); break;
                case 4:
                    Check(!ring.gameObject.activeSelf, "ring returns to pool");
                    enemy.Spawn(new Vector3(3,0,0)); Check(enemy.Active && enemy.Health == 3, "enemy reuses pool with full health");
                    enemy.Hit(3); g.Encounters.ResetRun(); Check(!ring.gameObject.activeSelf, "restart clears death effects");
                    g.Player.GetComponent<HeroAnimator>().Celebrating = true; Next(); break;
                case 5:
                    Check(playerSprite.sprite.name.Contains("Front"), "victory still forces front-facing pose");
                    Finish("PASS / " + checks + " checks: hero/enemy facing, eight enemy frames, white expanding death ring, lifetime, pool reuse and duplicate-hit protection."); break;
            }
        }
        catch (Exception e) { Finish("FAIL / stage " + stage + ": " + e); }
    }
}
