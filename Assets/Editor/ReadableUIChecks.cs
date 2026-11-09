using System;
using System.IO;
using System.Linq;
using CountToZero;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ReadableUIChecks
{
    static bool running; static int stage, checks; static double next;
    static UIButtonFeedback feedback; static Vector3 scale;
    static ReadableUIChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += mode => {
            if(mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("UI.Check",false))
            {SessionState.SetBool("UI.Check",false);running=true;stage=-1;next=EditorApplication.timeSinceStartup+1;checks=SessionState.GetInt("UI.Checks",0);}
        };
    }
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static void Next(){stage++;next=EditorApplication.timeSinceStartup+.35;}
    static void Tick()
    {
        try
        {
            if(!running)
            {
                if(!File.Exists("Temp/ctz-ui-check.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete("Temp/ctz-ui-check.request");checks=0;
                foreach(var name in new[]{"Title","Preload","Story","CountToZero","TurnEnd","StoryEnd"})
                {
                    var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var roots=scene.GetRootGameObjects();
                    Check(roots.SelectMany(r=>r.GetComponentsInChildren<AudioListener>(true)).Count()==1,name+" listener");
                    Check(roots.SelectMany(r=>r.GetComponentsInChildren<Button>(true)).All(b=>b.GetComponent<UIButtonFeedback>()!=null),name+" button feedback");
                    var audio=roots.SelectMany(r=>r.GetComponentsInChildren<GameAudio>(true)).Single();var cues=new SerializedObject(audio).FindProperty("cues");
                    foreach(var pair in new[]{(SoundCue.UIHover,"wobble"),(SoundCue.UIClick,"Choose"),(SoundCue.Explosion,"Explosion"),(SoundCue.Death,"GameOver")})
                    {var entry=cues.GetArrayElementAtIndex((int)pair.Item1);Check(entry.FindPropertyRelative("clip").objectReferenceValue.name==pair.Item2,name+" "+pair.Item1);}
                }
                EditorSceneManager.OpenScene("Assets/Scenes/Story.unity");SessionState.SetInt("UI.Checks",checks);SessionState.SetBool("UI.Check",true);EditorApplication.isPlaying=true;return;
            }
            if(EditorApplication.timeSinceStartup<next)return;
            switch(stage)
            {
                case -1: Application.runInBackground=true;Next();break;
                case 0:
                    feedback=UnityEngine.Object.FindObjectsByType<UIButtonFeedback>(FindObjectsSortMode.None).First(b=>b.GetComponent<Button>().interactable);
                    EventSystem.current.SetSelectedGameObject(null);feedback.OnPointerExit(null);feedback.enabled=false;feedback.enabled=true;scale=feedback.transform.localScale;
                    Time.timeScale=0;feedback.OnPointerEnter(new PointerEventData(EventSystem.current));Next();break;
                case 1:
                    Check(feedback.Highlighted && feedback.transform.localScale.x>scale.x,"hover grows while paused");
                    feedback.OnPointerDown(new PointerEventData(EventSystem.current));Next();break;
                case 2:
                    Check(feedback.transform.localScale.x<scale.x,"press shrinks while paused");feedback.OnPointerUp(null);feedback.OnPointerExit(null);feedback.OnSelect(null);Next();break;
                case 3:
                    Check(feedback.Highlighted && feedback.transform.localScale.x>scale.x,"keyboard selection feedback");
                    var body=UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).First(t=>t.name=="Story and Results");body.ForceMeshUpdate();
                    Check(body.fontSize>=42 && !body.isTextOverflowing,"story body enlarged and fits");
                    GameAudio.Play(SoundCue.UIClick);var effects=(AudioSource)new SerializedObject(GameAudio.Instance).FindProperty("effects").objectReferenceValue;
                    Check(effects.isPlaying,"new click audio plays");ScreenCapture.CaptureScreenshot("Documentation/readable-story.png");Next();break;
                case 4: Finish("PASS "+checks+" UI/audio checks across six scenes; paused hover, press, keyboard focus and click audio verified.");break;
            }
        }
        catch(Exception e){Finish("FAIL "+checks+": "+e);}
    }
    static void Finish(string result){running=false;Time.timeScale=1;File.WriteAllText("Documentation/readable-ui-checks.txt",result);Debug.Log(result);EditorApplication.isPlaying=false;}
}

