using System;
using System.IO;
using CountToZero;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class LuckyBeamUpgrade
{
    static bool running;static int stage,checks;static double next,deadline;static Vector3 middle;static string errors="";
    static LineRenderer Beam=>(LineRenderer)new SerializedObject(GameManager.Instance.Feedback).FindProperty("laser").objectReferenceValue;
    static LuckyBeamUpgrade()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(m,s,t)=>{if(running&&(t==LogType.Error||t==LogType.Exception))errors+=m+"\n";};
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Beam.Check",false)){SessionState.SetBool("Beam.Check",false);running=true;stage=checks=0;errors="";next=EditorApplication.timeSinceStartup+.5;deadline=next+40;Application.runInBackground=true;}
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("Beam.Return",false)){SessionState.SetBool("Beam.Return",false);EditorSceneManager.OpenScene(SessionState.GetString("Beam.Scene","Assets/Scenes/CountToZero.unity"));}
        };
    }
    static void Check(bool pass,string message){checks++;if(!pass)throw new Exception(message);}
    static void Next(float delay){stage++;next=EditorApplication.timeSinceStartup+delay;}
    static void Tick()
    {
        try{
        if(!running)
        {
            if(!File.Exists("Temp/ctz-lucky-beam.request")||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            File.Delete("Temp/ctz-lucky-beam.request");Run();return;
        }
        if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Test timeout at "+stage);
        if(EditorApplication.timeSinceStartup<next)return;
        var g=GameManager.Instance;
        switch(stage)
        {
            case 0:Campaign.Data.tutorialComplete=true;g.RecordScores=false;g.Begin();g.SkipIntro();Next(.2f);break;
            case 1:
                var label=GameObject.Find("3-2-1-READY!").GetComponent<TMP_Text>();
                Check(AssetDatabase.GetAssetPath(label.font)=="Assets/PixelUISciFiFree/Font/Pixel UI Sci-Fi TMP.asset","countdown uses configured Pixel font");
                ScreenCapture.CaptureScreenshot("Documentation/pixel-countdown.png");Next(4);break;
            case 2:Check(g.State==GameState.Playing,"countdown completed");g.Match(g.Player);Next(.1f);break;
            case 3:Check(g.CutIn.Visible&&!Beam.enabled,"beam waits for cut-in");Next(.4f);break;
            case 4:
                if(g.CutIn.Visible||!Beam.enabled)return;
                Check(!g.CutIn.Visible&&Beam.enabled,"beam visible after cut-in");Check(Beam.positionCount==25,"segmented beam");
                Check(Beam.sharedMaterial.shader.name=="Sprites/Default"&&Beam.sortingOrder==100,"visible beam material and sorting");
                Check(Beam.transform.Find("Lucky Beam Glow").GetComponent<LineRenderer>().enabled,"glow enabled");
                middle=Beam.GetPosition(12);ScreenCapture.CaptureScreenshot("Documentation/lucky-jitter-beam.png");Next(.08f);break;
            case 5:
                Check((Beam.GetPosition(12)-middle).sqrMagnitude>.000001f,"beam jitters between frames");
                Check(Vector3.Distance(Beam.GetPosition(0),g.Player.transform.position+Vector3.up*.7f)<.02f,"beam attached to hero");
                Check(Vector3.Distance(Beam.GetPosition(24),g.Boss.transform.position+Vector3.up*1.2f)<.02f,"beam attached to boss");Next(.85f);break;
            case 6:
                Check(!Beam.enabled,"beam expires cleanly");for(int i=0;i<20&&g.Rule.Type==RuleType.ExactMatch;i++)g.ShiftRule();
                Check(g.Rule.Type!=RuleType.ExactMatch,"non-exact Lucky rule selected");g.Match(g.Player);Next(.6f);break;
            case 7:
                if(g.CutIn.Visible||!Beam.enabled)return;
                Check(Beam.enabled,"other Lucky matches also show beam");g.Feedback.ResetFeedback();Check(!Beam.enabled&&!Beam.transform.Find("Lucky Beam Glow").GetComponent<LineRenderer>().enabled,"reset clears core and glow");
                Check(errors.Length==0,"no runtime errors: "+errors);Finish("PASS: "+checks+" checks; Pixel countdown, Lucky beam visible after cut-in, 25 jittering points, hero/boss endpoints, glow, clean expiry/reset, exact and non-exact Lucky matches. Original campaign restored.");break;
        }
        }catch(Exception e){if(running)Finish("FAIL stage "+stage+": "+e);else{Debug.LogException(e);File.WriteAllText("Documentation/lucky-beam-checks.txt",e.ToString());}}
    }
    [MenuItem("Count To Zero/Upgrade Lucky Beam and Pixel Countdown")]
    public static void Run()
    {
        var scene=SceneManager.GetActiveScene();if(scene.isDirty)EditorSceneManager.SaveScene(scene);SessionState.SetString("Beam.Scene",scene.path);
        SessionState.SetBool("Beam.HadSave",PlayerPrefs.HasKey("CountToZero.Campaign.v1"));SessionState.SetString("Beam.Save",PlayerPrefs.GetString("CountToZero.Campaign.v1",""));
        scene=EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");var g=UnityEngine.Object.FindAnyObjectByType<GameManager>();
        var countdown=g.GetComponent<RoundCountdown>();if(countdown==null)countdown=g.gameObject.AddComponent<RoundCountdown>();
        var so=new SerializedObject(countdown);so.FindProperty("pixelFont").objectReferenceValue=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/PixelUISciFiFree/Font/Pixel UI Sci-Fi TMP.asset");so.ApplyModifiedPropertiesWithoutUndo();
        const string path="Assets/GameAssets/Materials/LuckyBeam.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,path);}
        material.color=Color.white;EditorUtility.SetDirty(material);
        so=new SerializedObject(g.Feedback);so.FindProperty("luckyBeamMaterial").objectReferenceValue=material;so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();SessionState.SetBool("Beam.Check",true);EditorApplication.isPlaying=true;
    }
    static void Finish(string result)
    {
        running=false;
        if(SessionState.GetBool("Beam.HadSave",false))PlayerPrefs.SetString("CountToZero.Campaign.v1",SessionState.GetString("Beam.Save",""));else PlayerPrefs.DeleteKey("CountToZero.Campaign.v1");PlayerPrefs.Save();
        File.WriteAllText("Documentation/lucky-beam-checks.txt",result);Debug.Log(result);SessionState.SetBool("Beam.Return",true);EditorApplication.isPlaying=false;
    }
}
