using System;
using System.IO;
using CountToZero;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CountToZeroPlayChecks
{
    static double next;
    static int stage, assertions;
    static bool running;
    static CountToZeroPlayChecks()
    {
        EditorApplication.playModeStateChanged += mode =>
        {
            if(mode==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CTZ.Check",false))
            {SessionState.SetBool("CTZ.Check",false);Start();}
        };
        EditorApplication.update += Tick;
    }
    [MenuItem("Count To Zero/Run Play Checks")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)Start();
        else {SessionState.SetBool("CTZ.Check",true);EditorApplication.isPlaying=true;}
    }
    static void Start() {running=true;stage=0;assertions=0;next=EditorApplication.timeSinceStartup+1;}
    static void Check(bool value,string message) {assertions++;if(!value)throw new Exception(message);}
    static void Tick()
    {
        if(!running || EditorApplication.timeSinceStartup<next)return;
        try
        {
            var g=GameManager.Instance;
            if(g==null)throw new Exception("Game did not bootstrap");
            if(stage==0)
            {
                Campaign.Leave();g.RecordScores=false;
                g.Begin();Check(g.Player.Health==5 && g.Boss.Health==99,"initial state");
                g.Player.ApplyMathOperation(MathOpType.Add,6);
                Check(g.Player.CurrentValue==1 && g.Boss.Health==80,"exact laser and reset");
                Check(g.Score.Score>=5500,"match and speed score");
                g.Player.SetValue(13);g.Player.DashInversion();Check(g.Player.DashCooldown>2.9f,"dash cooldown");
                g.Begin();g.Player.ApplyMathOperation(MathOpType.Subtract,1);
                Check(g.Player.StunUntil>Time.time,"zero stun");Check(g.Orbs.Active.Count>=11,"zero reward drops");
                g.Player.SetValue(99);g.Player.ApplyMathOperation(MathOpType.Add,1);
                Check(g.Player.CurrentValue==1 && g.Player.Health==4,"overflow reset and damage");
                Check(g.HazardUntil>Time.time,"overflow hazard");
                g.Boss.SetValue(1);g.Boss.ApplyMathOperation(MathOpType.Subtract,1);
                Check(g.Boss.CrashUntil>Time.time,"boss crash");
                float hp=g.Boss.Health;g.DamageBoss(2,true);Check(g.Boss.Health==hp-4,"crash double damage");
                g.Begin();int tries=0;while(g.Rule.Type!=RuleType.EvenNumber && tries++<200)g.ShiftRule();
                Check(g.Rule.Type==RuleType.EvenNumber,"conditional rule generated");
                g.Player.ApplyMathOperation(MathOpType.Add,1);Check(g.Player.OverchargeUntil>Time.time,"conditional overcharge");
                g.Boss.ApplyMathOperation(MathOpType.Add,2);Check(g.Boss.ShieldUntil>Time.time,"boss shield");
                hp=g.Boss.Health;g.DamageBoss(1);Check(g.Boss.Health==hp,"shield reflects ordinary shots");
                g.Begin();g.DamageBoss(33,true);Check(g.Phase==2,"phase two");
                g.Boss.CrashUntil=0;g.DamageBoss(33,true);Check(g.Phase==3,"phase three");
                g.Boss.CrashUntil=0;g.DamageBoss(33,true);Check(g.State==GameState.Won,"victory");
                g.Begin();for(int i=0;i<5;i++)g.Player.Hurt(true);Check(g.State==GameState.Lost,"defeat");
                g.Begin();g.Score.Focus=1;g.Focus();Check(g.Score.Focus==0 && g.SlowUntil>Time.unscaledTime,"bullet time");
                g.TogglePause();Check(g.State==GameState.Paused,"pause");g.TogglePause();
                g.Begin();stage=1;next=EditorApplication.timeSinceStartup+4;
            }
            else if(stage==1)
            {
                Check(g.Elapsed>1,"simulation advances");
                Check(g.Orbs.Active.Count>5,"spawner runs");
                Check(Quaternion.Angle(g.Player.transform.Find("Glitch sprite").rotation,g.View.transform.rotation)<.01f,"sprite faces camera");
                Check(BulletPattern.SegmentDistance(Vector3.zero,new Vector3(-2,0,0),new Vector3(2,0,0))<.001f,"swept collision");
                g.Begin();Check(g.Player.Health==5 && g.Orbs.Active.Count==5,"clean restart and pooled release");
                Directory.CreateDirectory("Documentation");
                File.WriteAllText("Documentation/play-validation.txt","PASS: "+assertions+" assertions in Unity Play mode.\nBootstrap, exact/conditional match, zero, overflow, shield, damage, phases, win/loss, focus, pause, spawning, billboards, swept collision and restart.\n");
                Debug.Log("COUNT TO ZERO: PASS "+assertions+" Play mode assertions.");
                g.RecordScores=true;g.TogglePause();running=false;
            }
        }
        catch(Exception exception)
        {
            running=false;Directory.CreateDirectory("Documentation");
            if(GameManager.Instance!=null)GameManager.Instance.RecordScores=true;
            File.WriteAllText("Documentation/play-validation.txt","FAIL: "+exception);
            Debug.LogException(exception);
        }
    }
}
