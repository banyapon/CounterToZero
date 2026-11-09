using System;
using System.IO;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class StoryColumnsUpgrade
{
    static TMPro.TMP_Text[] Labels(CampaignScreen screen)=>new[]{screen.Heading,screen.Body,screen.PrimaryButton.GetComponentInChildren<TMPro.TMP_Text>(),screen.SecondaryButton.GetComponentInChildren<TMPro.TMP_Text>()};
    static string AssetId(UnityEngine.Object asset)=>GlobalObjectId.GetGlobalObjectIdSlow(asset).ToString();
    static bool checking;static int stage;static double next;static string errors="";
    static StoryColumnsUpgrade()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(m,s,t)=>{if(checking&&(t==LogType.Error||t==LogType.Exception))errors+=m+"\n";};
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("StoryColumns.Check",false)){SessionState.SetBool("StoryColumns.Check",false);checking=true;stage=0;errors="";next=EditorApplication.timeSinceStartup+1;Application.runInBackground=true;}
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("StoryColumns.Return",false)){SessionState.SetBool("StoryColumns.Return",false);EditorSceneManager.OpenScene(SessionState.GetString("StoryColumns.Scene","Assets/Scenes/Story.unity"));}
        };
    }
    static void Tick()
    {
        try{
        if(!checking)
        {
            if(!File.Exists("Temp/ctz-story-columns.request")||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            File.Delete("Temp/ctz-story-columns.request");Run();return;
        }
        if(EditorApplication.timeSinceStartup<next)return;
        var screen=UnityEngine.Object.FindAnyObjectByType<CampaignScreen>();
        if(stage==6){Finish(errors.Length==0?"PASS: Story and StoryEnd use 60/40 artwork/text columns; all prologue and ending/Endless text fits, both buttons remain within their column. Screenshots captured.":"FAIL: "+errors);return;}
        Canvas.ForceUpdateCanvases();screen.Body.ForceMeshUpdate();screen.Heading.ForceMeshUpdate();
        var labels=Labels(screen);
        for(int i=0;i<labels.Length;i++)
        {
            string key="StoryColumns.Font."+screen.Page+"."+i;
            if(AssetId(labels[i].font)!=SessionState.GetString(key,""))throw new Exception("Authored Pixel font changed during Play: "+labels[i].name);
            if(AssetId(labels[i].fontSharedMaterial)!=SessionState.GetString(key+".Material",""))throw new Exception("Authored font material changed during Play: "+labels[i].name);
        }
        if(screen.Body.isTextOverflowing||screen.Heading.isTextOverflowing)throw new Exception("Story text overflow at page "+stage);
        var left=GameObject.Find("Artwork 60 Percent").GetComponent<RectTransform>();var right=GameObject.Find("Narrative 40 Percent").GetComponent<RectTransform>();
        if(left.anchorMax.x!=.6f||right.anchorMin.x!=.6f)throw new Exception("Wrong column split");
        foreach(var button in new[]{screen.PrimaryButton,screen.SecondaryButton})
        {
            var r=(RectTransform)button.transform;
            if(r.parent!=right||r.rect.height<50||!button.IsInteractable())throw new Exception("Button not visible/interactable");
            var label=button.GetComponentInChildren<TMPro.TMP_Text>();label.ForceMeshUpdate();if(label.isTextOverflowing)throw new Exception("Button label overflow");
        }
        if(stage==0){ScreenCapture.CaptureScreenshot("Documentation/story-columns.png");stage++;next=EditorApplication.timeSinceStartup+.4f;return;}
        if(stage==1||stage==2)screen.PrimaryButton.onClick.Invoke();
        if(stage==3){SceneManager.LoadScene("StoryEnd");}
        if(stage==4){screen.SecondaryButton.onClick.Invoke();Canvas.ForceUpdateCanvases();screen.Body.ForceMeshUpdate();if(screen.Body.isTextOverflowing)throw new Exception("Endless prompt overflow");}
        if(stage==5)ScreenCapture.CaptureScreenshot("Documentation/story-columns-ending.png");
        stage++;next=EditorApplication.timeSinceStartup+.7f;
        }catch(Exception e){if(checking)Finish("FAIL: "+e);else{Debug.LogException(e);File.WriteAllText("Documentation/story-columns-checks.txt",e.ToString());}}
    }
    [MenuItem("Count To Zero/Arrange Story 60-40 Columns")]
    public static void Run()
    {
        var original=SceneManager.GetActiveScene();if(original.isDirty)EditorSceneManager.SaveScene(original);SessionState.SetString("StoryColumns.Scene",original.path);
        SessionState.SetBool("StoryColumns.HadSave",PlayerPrefs.HasKey("CountToZero.Campaign.v1"));SessionState.SetString("StoryColumns.Save",PlayerPrefs.GetString("CountToZero.Campaign.v1",""));
        foreach(string name in new[]{"Story","StoryEnd"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var screen=UnityEngine.Object.FindAnyObjectByType<CampaignScreen>();
            var labels=Labels(screen);
            // Repair the earlier default-font override while retaining custom authored fonts.
            for(int i=0;i<labels.Length;i++)
            {
                if(AssetDatabase.GetAssetPath(labels[i].font)!="Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset")continue;
                var pixel=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(i<2?"Assets/PixelUISciFiFree/Font/Pixel UI Sci-Fi TMP.asset":"Assets/Fonts/Body SDF.asset");
                labels[i].font=pixel;labels[i].fontSharedMaterial=pixel.material;
            }
            for(int i=0;i<labels.Length;i++){string key="StoryColumns.Font."+screen.Page+"."+i;SessionState.SetString(key,AssetId(labels[i].font));SessionState.SetString(key+".Material",AssetId(labels[i].fontSharedMaterial));}
            var image=StoryPageLayout.Arrange(screen);image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Sprites/cutscene_"+(name=="Story"?1:4)+".png");
            screen.Heading.text=name=="Story"?"PROLOGUE":"CORE LIBERATED";
            var book=JsonUtility.FromJson<StoryBook>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/GameAssets/Dialogues/story_en.json").text);
            screen.Body.text=name=="Story"?book.intro[0]:book.ending[0];
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/Story.unity");SessionState.SetBool("StoryColumns.Check",true);EditorApplication.isPlaying=true;
    }
    static void Finish(string result)
    {
        checking=false;
        if(SessionState.GetBool("StoryColumns.HadSave",false))PlayerPrefs.SetString("CountToZero.Campaign.v1",SessionState.GetString("StoryColumns.Save",""));else PlayerPrefs.DeleteKey("CountToZero.Campaign.v1");PlayerPrefs.Save();
        File.WriteAllText("Documentation/story-columns-checks.txt",result);Debug.Log(result);SessionState.SetBool("StoryColumns.Return",true);EditorApplication.isPlaying=false;
    }
}
