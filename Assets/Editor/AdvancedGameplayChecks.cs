using System;
using System.IO;
using CountToZero;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class AdvancedGameplayChecks
{
    static bool running;
    static int stage,checks;
    static double next;
    static Vector3 cameraStart,playerStart;
    static Gamepad pad;
    static Keyboard keyboard;
    static Key oldUp;
    static float pausedAt;
    static AdvancedGameplayChecks()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=mode=>{if(mode==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("CTZ.Advanced",false)){SessionState.SetBool("CTZ.Advanced",false);Start();}};
    }
    [MenuItem("Count To Zero/Run Advanced Gameplay Checks")]
    public static void Run(){if(EditorApplication.isPlaying)Start();else{SessionState.SetBool("CTZ.Advanced",true);EditorApplication.isPlaying=true;}}
    static void Start(){running=true;stage=checks=0;next=EditorApplication.timeSinceStartup+1;}
    static void Check(bool pass,string name){checks++;if(!pass)throw new Exception(name);}
    static void Advance(float seconds){stage++;next=EditorApplication.timeSinceStartup+seconds;}
    static void Cleanup()
    {
        if(pad!=null){InputSystem.RemoveDevice(pad);pad=null;}
        if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}
        var g=GameManager.Instance;if(g!=null){g.Controls.OptionsOpen=false;g.Controls.CancelListen();g.Controls.Assign(0,oldUp,false);g.Controls.PersistBindings=true;g.RecordScores=true;}
    }
    static void Tick()
    {
        if(!running||EditorApplication.timeSinceStartup<next)return;
        try
        {
            var g=GameManager.Instance;
            switch(stage)
            {
                case 0:
                    oldUp=g.Controls.GetBinding(0);g.Controls.PersistBindings=false;Campaign.Leave();g.RecordScores=false;g.Begin();g.Boss.CrashUntil=Time.time+100;g.Orbs.Clear();
                    Check(g.Dialogue.CurrentId=="custom","world-tour pre-fight line");
                    g.Player.ApplyMathOperation(MathOpType.Add,6);Check(g.CutIn.Visible,"Lucky cut-in triggers");
                    g.Begin();g.Boss.CrashUntil=Time.time+100;g.Orbs.Clear();
                    for(int i=0;i<6;i++){g.Boss.SelectPattern(i);Check(g.Boss.PatternIndex==i&&!string.IsNullOrEmpty(g.Boss.PatternName),"pattern asset "+i);}
                    g.Player.Hurt(true);g.Encounters.DropHealth(g.Player.transform.position,true);
                    cameraStart=g.View.transform.position;g.Player.transform.position+=Vector3.right*3;
                    g.Encounters.SummonObstacle(new Vector3(-4,0,0),false);g.Encounters.SummonObstacle(new Vector3(4,0,0),true);
                    Check(!g.Encounters.Blocked(new Vector3(-4,0,0)),"warning does not block immediately");
                    g.Encounters.SummonMinion();Check(g.Encounters.ActiveMinions==1,"minion spawn");
                    Advance(2);break;
                case 1:
                    Check(g.Encounters.ActiveObstacles==2,"cube pools activate");
                    Check(g.Encounters.Blocked(new Vector3(-4,0,0)),"wall becomes solid");
                    Check(g.Encounters.Blocked(new Vector3(4,0,0)),"falling cube lands");
                    var result=g.Encounters.Move(new Vector3(-7,0,0),new Vector3(6,0,0));Check(result.x<-5,"dash cannot tunnel through cube");
                    Check(g.Encounters.Intercept(new Vector3(-6,0,0),new Vector3(-2,0,0),false,1),"wall blocks projectiles");
                    Check(Vector3.Distance(cameraStart,g.View.transform.position)>.1f && g.View.orthographic,"orthographic camera follows player");
                    g.Encounters.ResetRun();Check(g.Encounters.ActiveMinions==0&&g.Encounters.ActiveObstacles==0,"restart clears encounters");
                    g.Encounters.DropHealth(g.Player.transform.position,true);Advance(.4f);break;
                case 2:
                    Check(g.Player.Health==5,"health item heals on collection");
                    g.Controls.Assign(0,Key.T,false);Check(g.Controls.GetBinding(0)==Key.T,"rebind primary key");
                    g.Controls.OptionsOpen=true;g.Controls.Listen(0);keyboard=InputSystem.AddDevice<Keyboard>();Advance(.2f);break;
                case 3:
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Y));Advance(.3f);break;
                case 4:
                    Check(g.Controls.GetBinding(0)==Key.Y&&!g.Controls.Listening,"live key listening captures keyboard event");
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());g.Controls.OptionsOpen=false;
                    pad=InputSystem.AddDevice<Gamepad>();playerStart=g.Player.transform.position;
                    InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=new Vector2(1,0),rightStick=new Vector2(0,1),rightTrigger=1});Advance(.4f);break;
                case 5:
                    Check(g.Controls.Move.x>.5f,"gamepad left stick");Check(g.Controls.StickAim.y>.5f,"gamepad right stick");Check(g.Controls.FireHeld,"gamepad trigger fire");
                    Check(Vector3.Distance(playerStart,g.Player.transform.position)>.2f,"gamepad moves character");
                    InputSystem.QueueStateEvent(pad,new GamepadState());g.TogglePause();pausedAt=g.Elapsed;Advance(.5f);break;
                case 6:
                    Check(Mathf.Approximately(g.Elapsed,pausedAt)&&g.State==GameState.Paused,"pause freezes combat clock");
                    g.TogglePause();g.Controls.TouchDash();Advance(.2f);break;
                case 7:
                    Check(g.Player.DashCooldown>0,"touch dash routes to player");
                    Cleanup();g.Begin();g.TogglePause();
                    Directory.CreateDirectory("Documentation");File.WriteAllText("Documentation/advanced-gameplay-validation.txt","PASS: "+checks+" Play mode assertions.\nEnglish JSON, cut-in, six pattern assets, warning/solid/falling cubes, swept obstruction, minions, health pickup, camera follow, key assignment/listening, virtual gamepad move/aim/fire, pause, touch dash, encounter reset.\nPhysical mobile browser and physical gamepad not tested.\n");
                    Debug.Log("COUNT TO ZERO: PASS "+checks+" advanced gameplay assertions.");running=false;break;
            }
        }
        catch(Exception error)
        {
            Cleanup();running=false;Directory.CreateDirectory("Documentation");File.WriteAllText("Documentation/advanced-gameplay-validation.txt","FAIL stage "+stage+": "+error);Debug.LogException(error);
        }
    }
}
