using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MicroCircuitChecks
{
    static bool running;
    static int stage, checks, nextFrame;
    static double next;
    static ArenaObstacle block;
    static Vector3 originalFloor;
    static Transform floor;
    static Vector3 beamAxis,beamCenter;
    static SpriteRenderer playerSprite;
    const string SaveKey = "CountToZero.Campaign.v1";
    static MicroCircuitChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += mode => {
            if (mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Micro.Check", false))
            { SessionState.SetBool("Micro.Check", false); running = true; stage = -1; checks = 0; next = EditorApplication.timeSinceStartup + 1; }
        };
    }
    [MenuItem("Count To Zero/Run Micro Circuit Checks")]
    public static void Run()
    {
        SessionState.SetBool("Micro.HadSave", PlayerPrefs.HasKey(SaveKey)); SessionState.SetString("Micro.Save", PlayerPrefs.GetString(SaveKey));
        EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity"); SessionState.SetBool("Micro.Check", true); EditorApplication.isPlaying = true;
    }
    static void Check(bool pass, string label) { checks++; if (!pass) throw new Exception(label); }
    static void Next(double seconds = .1) { stage++; next = EditorApplication.timeSinceStartup + seconds; nextFrame = Time.frameCount + 3; }
    static void Finish(string result)
    {
        running = false;
        if (SessionState.GetBool("Micro.HadSave", false)) PlayerPrefs.SetString(SaveKey, SessionState.GetString("Micro.Save", "")); else PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save(); File.WriteAllText("Documentation/micro-circuit-playchecks.txt", result); Debug.Log(result); EditorApplication.isPlaying = false;
    }
    static void Tick()
    {
        if (!running)
        {
            if (File.Exists("Temp/ctz-micro-check.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            { File.Delete("Temp/ctz-micro-check.request"); Run(); }
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
                    Check(g.Hazards!=null,"sector hazards authored");
                    Check(SectorHazards.SelectMode(1,45)==SectorMode.Standard && SectorHazards.SelectMode(2,12)==SectorMode.Standard,"opening levels safe");
                    var modes=Enumerable.Range(3,30).Select(level=>SectorHazards.SelectMode(level,123)).ToArray();
                    Check(modes.Contains(SectorMode.Conveyor) && modes.Contains(SectorMode.Laser) && modes.Contains(SectorMode.ConveyorLaser),"seeded sector variety");
                    Check(SectorHazards.SelectMode(20,123)==SectorHazards.SelectMode(20,123),"retry mode is stable");
                    var robots=UnityEngine.Object.FindObjectsByType<RobotAppearance>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                    Check(robots.Length==6,"all enemies have robot variants");var picks=new System.Collections.Generic.HashSet<int>();
                    for(int i=0;i<80;i++){robots[0].Choose();picks.Add(robots[0].Variant);}Check(picks.Count==3,"three robot appearances available");
                    g.Hazards.Configure(SectorMode.Laser);
                    var beam=(LineRenderer)new SerializedObject(g.Hazards).FindProperty("laser").objectReferenceValue;
                    Check(beam.sharedMaterial.color.r>.8f && beam.sharedMaterial.color.g<.1f,"laser uses dedicated red material");
                    beamCenter=(beam.GetPosition(0)+beam.GetPosition(1))*.5f;beamCenter.y=0;
                    beamAxis=Mathf.Abs(beam.GetPosition(0).x-beam.GetPosition(1).x)<.01f?Vector3.right:Vector3.forward;
                    g.Player.transform.position=beamCenter+beamAxis*.8f;
                    Check(!g.Hazards.BeamActive,"laser telegraph does not damage");Next(1.4);break;
                case 1:
                    Check(g.Hazards.BeamActive && g.Player.Health==5,"laser active after warning");
                    g.Player.transform.position=beamCenter-beamAxis*.8f;Next(.12);break;
                case 2:
                    Check(g.Player.Health==2.5f,"beam crossing removes exactly half max HP");
                    Check(Vector3.Dot(g.Player.transform.position-beamCenter,beamAxis)>0,"laser knocks back toward original side");
                    Check(!g.Player.HitLaser(beamAxis) && g.Player.Health==2.5f,"laser hit cooldown prevents repeat damage");
                    g.Hazards.Configure(SectorMode.Conveyor);floor=(Transform)new SerializedObject(g.Hazards).FindProperty("floorLines").GetArrayElementAtIndex(3).objectReferenceValue;
                    originalFloor=floor.position;g.TogglePause();Next(.3);break;
                case 3:
                    Check(floor.position==originalFloor,"conveyor pauses with game");g.TogglePause();Next(.3);break;
                case 4:
                    Check(floor.position.z<originalFloor.z,"floor slides down toward player");
                    g.Encounters.ResetRun();g.Player.transform.position=new Vector3(5,0,-6);g.Encounters.SummonObstacle(new Vector3(0,0,4),false);
                    block=UnityEngine.Object.FindObjectsByType<ArenaObstacle>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(o=>o.Active);Next(8.5);break;
                case 5:
                    Check(block.Active && block.Solid && block.Position.z < 4,"conveyor blocks remain alive beyond normal lifetime and travel with floor");
                    block.transform.position=new Vector3(0,0,-6.4f);g.Player.transform.position=new Vector3(0,0,-7.25f);Next(.1);break;
                case 6:
                    Check(g.State==GameState.Lost && g.Player.Health==0 && g.Player.LastDamageCause.Contains("Crushed"),"block crush at bottom is lethal");
                    g.Begin();Check(g.Player.Health==5,"retry resets health");Check(GameObject.Find("Micro Circuit Border")!=null,"PCB border authored");
                    g.Hazards.Configure(SectorMode.ConveyorLaser);g.Encounters.SummonBlockWave(true);for(int i=0;i<3;i++)g.Encounters.SummonMinion();Next(1.4);break;
                case 7:
                    ScreenCapture.CaptureScreenshot("Documentation/micro-circuit-arena.png");Next(.2);break;
                case 8:
                    Finish("PASS / "+checks+" checks: deterministic sector variety, 3 robot variants, telegraph, real laser crossing/50% damage/knockback, immunity, pause, downward floor and blocks, bottom crush and retry.");break;
            }
        }
        catch (Exception e) { Finish("FAIL / stage " + stage + ": " + e); }
    }
}
