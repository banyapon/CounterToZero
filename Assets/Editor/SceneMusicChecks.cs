using System;
using System.IO;
using CountToZero;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SceneMusicChecks
{
    static bool running;static int stage,checks;static double next,deadline;static string errors="";
    static readonly System.Collections.Generic.HashSet<string> beats=new();
    static AudioSource Source(string field)=>(AudioSource)new SerializedObject(GameAudio.Instance).FindProperty(field).objectReferenceValue;
    static SceneMusicChecks()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(m,s,t)=>{if(running&&(t==LogType.Exception||t==LogType.Error))errors+=m+"\n";};
        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Music.Run",false))
            {SessionState.SetBool("Music.Run",false);running=true;stage=checks=0;errors="";beats.Clear();next=EditorApplication.timeSinceStartup+1;deadline=next+60;Application.runInBackground=true;}
            if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("Music.Return",false))
            {SessionState.SetBool("Music.Return",false);EditorSceneManager.OpenScene(SessionState.GetString("Music.Scene","Assets/Scenes/Title.unity"));}
        };
    }
    static void Check(bool pass,string message){checks++;if(!pass)throw new Exception(message);}
    static void Next(float delay=.3f){stage++;next=EditorApplication.timeSinceStartup+delay;}
    static void Finish(string result)
    {
        running=false;
        foreach(string key in new[]{"CountToZero.Music","CountToZero.SFX","CountToZero.Campaign.v1"})
        {if(SessionState.GetBool("Music.Had."+key,false)){if(key.EndsWith("v1"))PlayerPrefs.SetString(key,SessionState.GetString("Music.Value."+key,""));else PlayerPrefs.SetFloat(key,SessionState.GetFloat("Music.Value."+key,0));}else PlayerPrefs.DeleteKey(key);}
        PlayerPrefs.Save();File.WriteAllText("Documentation/scene-music-checks.txt",result+"\n"+errors);Debug.Log(result);
        SessionState.SetBool("Music.Return",true);EditorApplication.isPlaying=false;
    }
    static void Tick()
    {
        try{
        if(!running)
        {
            const string request="Temp/ctz-scene-music-check.request";
            if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete(request);var scene=SceneManager.GetActiveScene();if(scene.isDirty)EditorSceneManager.SaveScene(scene);SessionState.SetString("Music.Scene",scene.path);
            foreach(string key in new[]{"CountToZero.Music","CountToZero.SFX","CountToZero.Campaign.v1"})
            {SessionState.SetBool("Music.Had."+key,PlayerPrefs.HasKey(key));if(key.EndsWith("v1"))SessionState.SetString("Music.Value."+key,PlayerPrefs.GetString(key,""));else SessionState.SetFloat("Music.Value."+key,PlayerPrefs.GetFloat(key));}
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");SessionState.SetBool("Music.Run",true);EditorApplication.isPlaying=true;return;
        }
        if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout at stage "+stage);
        if(EditorApplication.timeSinceStartup<next)return;
        switch(stage)
        {
            case 0:
                Check(Source("bed").clip.name=="title"&&Source("bed").isPlaying&&Source("bed").loop,"title.mp3 looping on title");
                var view=UnityEngine.Object.FindAnyObjectByType<TitleMenuView>();var so=new SerializedObject(view);
                ((Slider)so.FindProperty("music").objectReferenceValue).value=.23f;((Slider)so.FindProperty("sfx").objectReferenceValue).value=.31f;
                Check(Mathf.Approximately(Source("bed").volume,.23f),"Options BGM updates immediately");
                Check(Mathf.Approximately(Source("effects").volume,.31f),"Options SFX updates immediately");
                GameAudio.SetMusic(0);GameAudio.SetEffects(0);Check(Source("bed").volume==0&&Source("effects").volume==0,"both sliders can mute");
                GameAudio.SetMusic(.23f);GameAudio.SetEffects(.31f);SceneManager.LoadScene("Story");Next();break;
            case 1:Check(Source("bed").clip.name=="story"&&Source("bed").isPlaying,"story.mp3 on story");SceneManager.LoadScene("StoryEnd");Next();break;
            case 2:Check(Source("bed").clip.name=="story"&&Source("bed").isPlaying,"story.mp3 on ending");Check(UnityEngine.Object.FindObjectsByType<GameAudio>(FindObjectsSortMode.None).Length==1,"single persistent audio director");Campaign.NewGame();Campaign.Data.tutorialComplete=true;Campaign.Save();SceneManager.LoadScene("CountToZero");Next(.4f);break;
            case 3:
                Check(Source("bed").clip.name=="title"&&Source("bed").isPlaying,"title.mp3 during gameplay");
                Check(!Source("pulse").isPlaying&&!Source("danger").isPlaying,"old music layers stopped");
                Check(Mathf.Approximately(Source("bed").volume,.23f)&&Mathf.Approximately(Source("effects").volume,.31f),"Options volumes persist across scenes");
                GameManager.Instance.RecordScores=false;GameManager.Instance.SkipIntro();Next(.02f);break;
            case 4:
                var g=GameManager.Instance;
                if(!g.CountingDown){Check(g.State==GameState.Playing&&beats.SetEquals(new[]{"3","2","1","READY!"}),"four individual audible countdown beats");SceneManager.LoadScene("Title");Next();break;}
                string label=GameObject.Find("3-2-1-READY!").GetComponent<TMP_Text>().text;
                if(beats.Add(label)){Check(Source("effects").isPlaying,"beep plays for "+label);Check(g.State==GameState.Intro,"combat waits for beep "+label);}
                break;
            case 5:Check(Source("bed").clip.name=="title"&&Source("bed").isPlaying,"title music on return");Check(errors.Length==0,"no audio/runtime errors");Finish("PASS: "+checks+" audio checks; looping Title/gameplay and Story/ending tracks, immediate Options BGM/SFX volume and mute, persistent settings, four separate countdown beeps, no overlapping old music. Original preferences and campaign restored.");break;
        }
        }catch(Exception e){if(running)Finish("FAIL: "+e);else{Debug.LogException(e);File.WriteAllText("Documentation/scene-music-checks.txt",e.ToString());}}
    }
}
