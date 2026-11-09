using System;
using System.IO;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SceneMusicUpgrade
{
    static SceneMusicUpgrade(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        const string request="Temp/ctz-scene-music.request";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete(request);try{Run();}catch(Exception e){Debug.LogException(e);File.WriteAllText("Documentation/scene-music-upgrade.txt",e.ToString());}
    }
    [MenuItem("Count To Zero/Assign Scene Music and Countdown Beeps")]
    public static void Run()
    {
        AssetDatabase.Refresh();
        var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string path=original.path;
        if(original.isDirty)EditorSceneManager.SaveScene(original);
        var title=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameAssets/Audio/BGM/title.mp3");
        var story=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameAssets/Audio/BGM/story.mp3");
        var beep=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameAssets/Audio/Polish/Countdown.wav");
        var ready=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameAssets/Audio/Polish/Ready.wav");
        if(title==null||story==null||beep==null||ready==null)throw new Exception("Missing scene music or countdown audio");
        foreach(string name in new[]{"Title","Preload","Story","StoryEnd","CountToZero","TurnEnd"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
            var audio=UnityEngine.Object.FindAnyObjectByType<GameAudio>();
            var so=new SerializedObject(audio);
            so.FindProperty("titleMusic").objectReferenceValue=title;
            so.FindProperty("storyMusic").objectReferenceValue=story;
            var cues=so.FindProperty("cues");int count=cues.arraySize;
            foreach(var item in new[]{(SoundCue.Countdown,beep),(SoundCue.Ready,ready)})
            {
                int index=-1;for(int i=0;i<cues.arraySize;i++)if(cues.GetArrayElementAtIndex(i).FindPropertyRelative("cue").intValue==(int)item.Item1){index=i;break;}
                if(index<0){index=cues.arraySize;cues.arraySize++;}
                var entry=cues.GetArrayElementAtIndex(index);entry.FindPropertyRelative("cue").intValue=(int)item.Item1;
                entry.FindPropertyRelative("clip").objectReferenceValue=item.Item2;entry.FindPropertyRelative("volume").floatValue=.75f;
            }
            var bed=(AudioSource)so.FindProperty("bed").objectReferenceValue;
            bed.clip=name=="Story"||name=="StoryEnd"?story:title;bed.loop=true;bed.playOnAwake=false;
            foreach(string field in new[]{"pulse","danger"}){var source=(AudioSource)so.FindProperty(field).objectReferenceValue;source.playOnAwake=false;source.volume=0;}
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);
        File.WriteAllText("Documentation/scene-music-upgrade.txt","PASS: title.mp3 for Title/gameplay; story.mp3 for Story/StoryEnd; dedicated Countdown and Ready cues; six scenes wired.\n");
    }
}
