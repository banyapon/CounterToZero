using System;
using System.IO;
using System.Text;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
// Itch submission audit: plays the live level, captures screenshots and reports
// on-screen overlaps between HUD panels and the boss / player.
// Trigger: create Temp/ctz-itch-audit.request while the editor is open.
[InitializeOnLoad]
public static class ItchAudit
{
    const string Request="Temp/ctz-itch-audit.request",Out="Documentation/itch-audit.txt",Save="CountToZero.Campaign.v1";
    static bool running;static int stage;static double next;static readonly StringBuilder log=new StringBuilder();static string errors="";
    static ItchAudit()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(running&&(t==LogType.Error||t==LogType.Exception))errors+=m+"\n"+s+"\n";};
        EditorApplication.playModeStateChanged+=m=>{
            if(m==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Itch.Run",false)){SessionState.SetBool("Itch.Run",false);running=true;stage=0;log.Clear();errors="";next=EditorApplication.timeSinceStartup+.7;}
            if(m==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("Itch.Return",false)){SessionState.SetBool("Itch.Return",false);EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");}
        };
        EditorApplication.update+=Tick;
    }
    static void Wait(double s){stage++;next=EditorApplication.timeSinceStartup+s;}
    static void Shot(string name){ScreenCapture.CaptureScreenshot("Documentation/"+name+".png");log.AppendLine("shot "+name);}
    static Rect ScreenRect(RectTransform r){var c=new Vector3[4];r.GetWorldCorners(c);var canvas=r.GetComponentInParent<Canvas>();var cam=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;Vector2 a=RectTransformUtility.WorldToScreenPoint(cam,c[0]),b=RectTransformUtility.WorldToScreenPoint(cam,c[2]);return Rect.MinMaxRect(a.x,a.y,b.x,b.y);}
    static void Report(string label)
    {
        var g=GameManager.Instance;var cam=g.View;
        Vector2 boss=cam.WorldToScreenPoint(g.Boss.transform.position),player=cam.WorldToScreenPoint(g.Player.transform.position);
        log.AppendLine("== "+label+" screen "+Screen.width+"x"+Screen.height+" boss "+boss+" player "+player);
        foreach(var r in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(r.GetComponentInParent<Canvas>()==null)continue;
            var g2=r.GetComponent<UnityEngine.UI.Graphic>();if(g2==null||!g2.enabled||g2.color.a<.05f)continue;
            var rect=ScreenRect(r);if(rect.width<4||rect.height<4)continue;
            var tmp=r.GetComponent<TMPro.TMP_Text>();if(tmp!=null&&string.IsNullOrWhiteSpace(tmp.text))continue;
            bool hb=rect.Contains(boss),hp=rect.Contains(player);
            if(hb||hp)log.AppendLine((hb?"COVERS BOSS ":"")+(hp?"COVERS PLAYER ":"")+Path(r)+" "+rect+(tmp!=null?" text='"+tmp.text.Replace("\n"," | ")+"'":""));
        }
    }
    static string Path(Transform t){var s=t.name;while(t.parent!=null){t=t.parent;s=t.name+"/"+s;}return s;}
    static void Finish(string message)
    {
        running=false;
        if(SessionState.GetBool("Itch.Had",false))PlayerPrefs.SetString(Save,SessionState.GetString("Itch.Save",""));else PlayerPrefs.DeleteKey(Save);PlayerPrefs.Save();
        File.WriteAllText(Out,message+"\n"+log+(errors.Length>0?"\nRUNTIME ERRORS:\n"+errors:""));
        SessionState.SetBool("Itch.Return",true);EditorApplication.isPlaying=false;
    }
    static void Tick()
    {
        try
        {
            if(!running)
            {
                if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete(Request);
                SessionState.SetBool("Itch.Had",PlayerPrefs.HasKey(Save));SessionState.SetString("Itch.Save",PlayerPrefs.GetString(Save,""));
                PlayerPrefs.DeleteKey(Save);PlayerPrefs.Save(); // fresh campaign: level 1 with tutorial, like a first-time jam player
                EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");SessionState.SetBool("Itch.Run",true);EditorApplication.isPlaying=true;return;
            }
            if(EditorApplication.timeSinceStartup<next)return;
            var g=GameManager.Instance;Application.runInBackground=true;g.RecordScores=false;
            switch(stage)
            {
                case 0:Report("intro");Shot("itch-1-intro");g.SkipIntro();Wait(4.8);break;
                case 1:log.AppendLine("step "+g.Tutorial.Step);Shot("itch-2-move");g.Player.transform.position+=Vector3.right*1.4f;Wait(.6);break;
                case 2:log.AppendLine("step "+g.Tutorial.Step+" orbs "+g.Orbs.Active.Count+" value "+g.Player.CurrentValue);Report("count");Shot("itch-3-count");if(g.Orbs.Active.Count>0)g.Player.transform.position=g.Orbs.Active[0].transform.position;Wait(.8);break;
                case 3:log.AppendLine("step "+g.Tutorial.Step+" orbs "+g.Orbs.Active.Count+" value "+g.Player.CurrentValue);Shot("itch-4-count2");if(g.Orbs.Active.Count>0)g.Player.transform.position=g.Orbs.Active[0].transform.position;Wait(.5);break;
                case 4:log.AppendLine("step "+g.Tutorial.Step+" value "+g.Player.CurrentValue);Shot("itch-5-lucky7");Wait(.1);break;
                case 5:g.Tutorial.Skip();Wait(5.5);break;
                case 6:Report("live+dialogue");Shot("itch-6-live");log.AppendLine("rule "+g.Rule.Label);Wait(4);break;
                case 7:g.Player.SetValue(g.Rule.Type==RuleType.ExactMatch?g.Rule.TargetValue:g.Player.CurrentValue);if(g.Rule.Type==RuleType.ExactMatch&&g.State==GameState.Playing)g.Match(g.Player);Wait(.35);break;
                case 8:Shot("itch-7-exact");log.AppendLine("after exact rule "+g.Rule.Label);Wait(12);break;
                case 9:Report("later");Shot("itch-8-later");log.AppendLine("rule "+g.Rule.Label+" minions "+g.Encounters.ActiveMinions+" blocks "+g.Encounters.ActiveObstacles);Finish("DONE state="+g.State+" bossHP="+g.Boss.Health+" playerHP="+g.Player.Health);break;
            }
        }
        catch(Exception e){Finish("FAIL stage "+stage+": "+e);}
    }
}
