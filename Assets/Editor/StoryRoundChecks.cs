using System;
using System.IO;
using System.Linq;
using CountToZero;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StoryRoundChecks
{
    const string SaveKey="CountToZero.Campaign.v1";
    static bool running;
    static int stage,checks;
    static double next,deadline;
    static string errors="";
    static readonly System.Collections.Generic.HashSet<string> countdown=new();
    static StoryRoundChecks()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(m,s,t)=>{if(running&&(t==LogType.Error||t==LogType.Exception))errors+=m+"\n";};
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("StoryRound.Run",false))
            {SessionState.SetBool("StoryRound.Run",false);running=true;stage=0;checks=0;errors="";countdown.Clear();next=EditorApplication.timeSinceStartup+1;deadline=next+90;}
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("StoryRound.Return",false))
            {SessionState.SetBool("StoryRound.Return",false);EditorSceneManager.OpenScene(SessionState.GetString("StoryRound.Scene","Assets/Scenes/Title.unity"));}
        };
    }
    static void Check(bool pass,string message){checks++;if(!pass)throw new Exception(message);}
    static void Next(float delay=.2f){stage++;next=EditorApplication.timeSinceStartup+delay;}
    static void Click(string name)=>(name=="Continue"?UnityEngine.Object.FindAnyObjectByType<CampaignScreen>().PrimaryButton:UnityEngine.Object.FindAnyObjectByType<CampaignScreen>().SecondaryButton).onClick.Invoke();
    static Image Comic()=>GameObject.Find("Aseprite comic").GetComponent<Image>();
    static void Finish(string result)
    {
        running=false;
        if(SessionState.GetBool("StoryRound.HadSave",false))PlayerPrefs.SetString(SaveKey,SessionState.GetString("StoryRound.Save",""));else PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        File.WriteAllText("Documentation/story-round-checks.txt",result+"\n"+errors);
        Debug.Log(result);SessionState.SetBool("StoryRound.Return",true);EditorApplication.isPlaying=false;
    }
    static void Tick()
    {
        try{
        if(!running)
        {
            const string request="Temp/ctz-story-round-check.request";
            if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(request);
            var scene=SceneManager.GetActiveScene();if(scene.isDirty)EditorSceneManager.SaveScene(scene);
            SessionState.SetString("StoryRound.Scene",scene.path);
            SessionState.SetBool("StoryRound.HadSave",PlayerPrefs.HasKey(SaveKey));SessionState.SetString("StoryRound.Save",PlayerPrefs.GetString(SaveKey,""));
            EditorSceneManager.OpenScene("Assets/Scenes/Story.unity");SessionState.SetBool("StoryRound.Run",true);EditorApplication.isPlaying=true;return;
        }
        if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out at stage "+stage);
        if(EditorApplication.timeSinceStartup<next)return;
        var g=GameManager.Instance;
        switch(stage)
        {
            case 0:Check(Comic().sprite.name=="cutscene_1","first story artwork");Check(!GameObject.Find("Story and Results").GetComponent<TMP_Text>().isTextOverflowing,"prologue fits");Click("Continue");Next();break;
            case 1:Check(Comic().sprite.name=="cutscene_2","second story artwork");Click("Continue");Next();break;
            case 2:Check(Comic().sprite.name=="cutscene_3","third story artwork");SceneManager.LoadScene("StoryEnd");Next(.7f);break;
            case 3:Check(Comic().sprite.name=="cutscene_4","ending artwork");Click("Back");Next();break;
            case 4:Check(Comic().sprite.name=="cutscene_4","endless choice keeps ending artwork");Check(UnityEngine.Object.FindAnyObjectByType<CampaignScreen>().PrimaryButton.GetComponentInChildren<TMP_Text>().text.Contains("ENDLESS"),"endless prompt");Campaign.NewGame();Campaign.Data.introSeen=true;Campaign.Data.tutorialComplete=true;Campaign.Save();SceneManager.LoadScene("CountToZero");Next(.5f);break;
            case 5:Check(g.State==GameState.Intro&&g.View.GetComponent<FollowCamera>().Touring,"saved introSeen still tours");g.RecordScores=false;Next(0);break;
            case 6:
                if(!g.CountingDown)return;
                var text=GameObject.Find("3-2-1-READY!").GetComponent<TMP_Text>().text;countdown.Add(text);
                Check(g.State==GameState.Intro&&g.Elapsed==0,"combat held during countdown");
                if(text=="READY!"){ScreenCapture.CaptureScreenshot("Documentation/round-ready.png");Next(1);}break;
            case 7:Check(countdown.SetEquals(new[]{"3","2","1","READY!"}),"all four countdown beats in order");Check(g.State==GameState.Playing,"combat starts after READY");g.Begin();Next(.1f);break;
            case 8:Check(g.State==GameState.Intro&&g.View.GetComponent<FollowCamera>().Touring,"retry tours again");g.SkipIntro();Next(.1f);break;
            case 9:Check(g.CountingDown&&g.State==GameState.Intro,"skip camera retains countdown");g.SkipIntro();Check(g.CountingDown,"countdown cannot be skipped");Next(4f);break;
            case 10:Check(g.State==GameState.Playing,"retry starts after countdown");Campaign.Leave();var battle=g.GetComponent<BattleExpansion>();battle.SpawnAssistant();battle.SpawnAssistant();g.Finish(true);Next(1.7f);break;
            case 11:
                Check(g.State==GameState.Victory,"result waits for celebration");
                var visuals=g.GetComponent<BattleExpansion>().GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name=="Assistant Visual").ToArray();
                Check(visuals.Length>=2,"two assistant styles present");
                Check(visuals.All(s=>s.sprite.name.StartsWith("Front")),"assistants face front before result");
                Check(visuals.Any(s=>AssetDatabase.GetAssetPath(s.sprite).Contains("assistant_green"))&&visuals.Any(s=>AssetDatabase.GetAssetPath(s.sprite).Contains("assistant_gold")),"green and gold styles celebrate");
                ScreenCapture.CaptureScreenshot("Documentation/squad-victory.png");Next(2.3f);break;
            case 12:Check(g.State==GameState.Won,"result follows squad celebration");Check(errors.Length==0,"no runtime errors: "+errors);Finish("PASS: "+checks+" checks; three story images, fourth ending/choice image, repeat tours, complete countdown with combat gated, retry and both assistants facing front before victory panel. Original campaign save restored.");break;
        }
        }catch(Exception e){if(running)Finish("FAIL: "+e);else{File.WriteAllText("Documentation/story-round-checks.txt",e.ToString());Debug.LogException(e);}}
    }
}
