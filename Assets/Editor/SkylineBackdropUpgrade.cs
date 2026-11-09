using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SkylineBackdropUpgrade
{
    static bool running;static int stage,checks;static double next,deadline;static string errors="";static Vector4 previousUV;
    static SkylineBackdropUpgrade()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(m,s,t)=>{if(running&&(t==LogType.Error||t==LogType.Exception))errors+=m+"\n";};
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("Skyline.Check",false)){SessionState.SetBool("Skyline.Check",false);running=true;stage=checks=0;errors="";next=EditorApplication.timeSinceStartup+.7;deadline=next+45;Application.runInBackground=true;}
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("Skyline.Return",false)){SessionState.SetBool("Skyline.Return",false);EditorSceneManager.OpenScene(SessionState.GetString("Skyline.Scene","Assets/Scenes/CountToZero.unity"));}
        };
    }
    static void Check(bool pass,string message){checks++;if(!pass)throw new Exception(message);}
    static Transform Root=>GameManager.Instance.View.transform.Find("Skyline Parallax");
    static Vector4 UV(){var block=new MaterialPropertyBlock();Root.Find("City Billboard Row 6").GetComponent<MeshRenderer>().GetPropertyBlock(block);return block.GetVector("_MainTex_ST");}
    static void Validate()
    {
        var camera=GameManager.Instance.View;Check(Root!=null&&Root.childCount==14,"fixed 12 city rows plus sky and haze");
        foreach(Transform layer in Root)
        {
            Check(Quaternion.Angle(layer.rotation,camera.transform.rotation)<.01f,"billboard faces camera: "+layer.name);
            Vector3 a=camera.WorldToViewportPoint(layer.TransformPoint(new Vector3(-.5f,-.5f,0)));
            Vector3 b=camera.WorldToViewportPoint(layer.TransformPoint(new Vector3(.5f,.5f,0)));
            Check(Mathf.Abs(a.x)<.002f&&Mathf.Abs(b.x-1)<.002f,"layer fills screen width: "+layer.name);
            Check(a.z>55&&a.z<camera.farClipPlane,"background remains behind arena and inside far clip");
            if(layer.name.StartsWith("Sky /"))Check(Mathf.Abs(a.y-.9f)<.002f&&Mathf.Abs(b.y-1)<.002f,"sky occupies exactly top 10 percent");
        }
        Check(!ShaderUtil.ShaderHasError(Root.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial.shader),"backdrop shader compiled");
    }
    static void Next(float delay){stage++;next=EditorApplication.timeSinceStartup+delay;}
    static void Tick()
    {
        try{
        if(!running)
        {
            if(!File.Exists("Temp/ctz-skyline.request")||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            File.Delete("Temp/ctz-skyline.request");Run();return;
        }
        if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timed out at stage "+stage);
        if(EditorApplication.timeSinceStartup<next)return;
        var g=GameManager.Instance;
        switch(stage)
        {
            case 0:g.RecordScores=false;Campaign.Data.tutorialComplete=true;Check(g.View.GetComponent<FollowCamera>().Touring,"backdrop present during intro");Validate();previousUV=UV();ScreenCapture.CaptureScreenshot("Documentation/skyline-intro.png");Next(1.6f);break;
            case 1:Validate();Check(UV()!=previousUV,"parallax changes during orbit");g.SkipIntro();Next(4);break;
            case 2:
                Check(g.State==GameState.Playing,"backdrop present during gameplay");Validate();ScreenCapture.CaptureScreenshot("Documentation/skyline-gameplay.png");Next(.3f);break;
            case 3:
                var camera=g.View;float size=camera.orthographicSize;camera.orthographicSize*=1.5f;camera.GetComponent<SkylineParallaxBackdrop>().RefreshBackdrop();Validate();camera.orthographicSize=size;camera.GetComponent<SkylineParallaxBackdrop>().RefreshBackdrop();
                g.TogglePause();previousUV=UV();Next(.15f);break;
            case 4:Check(UV()==previousUV,"paused backdrop stays still");g.TogglePause();g.Begin();Next(.3f);break;
            case 5:Check(g.View.GetComponent<FollowCamera>().Touring,"retry intro keeps backdrop");Validate();Check(errors.Length==0,"no rendering/runtime errors: "+errors);Finish("PASS: "+checks+" checks; 12 dense city billboard rows, top 10% sky, camera orbit, live parallax, zoom coverage, pause and retry, no accumulating objects or shader errors. Original campaign restored.");break;
        }
        }catch(Exception e){if(running)Finish("FAIL stage "+stage+": "+e);else{Debug.LogException(e);File.WriteAllText("Documentation/skyline-checks.txt",e.ToString());}}
    }
    [MenuItem("Count To Zero/Add Floating City Parallax Backdrop")]
    public static void Run()
    {
        var scene=SceneManager.GetActiveScene();if(scene.isDirty)EditorSceneManager.SaveScene(scene);SessionState.SetString("Skyline.Scene",scene.path);
        SessionState.SetBool("Skyline.HadSave",PlayerPrefs.HasKey("CountToZero.Campaign.v1"));SessionState.SetString("Skyline.Save",PlayerPrefs.GetString("CountToZero.Campaign.v1",""));
        scene=EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");var view=UnityEngine.Object.FindAnyObjectByType<GameManager>().View;
        var component=view.GetComponent<SkylineParallaxBackdrop>();if(component==null)component=view.gameObject.AddComponent<SkylineParallaxBackdrop>();
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/GameAssets/Materials/SkylineBackdrop.shader");
        const string materialPath="Assets/GameAssets/Materials/SkylineBackdrop.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}else material.shader=shader;
        var so=new SerializedObject(component);so.FindProperty("sky").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GameAssets/Backgrounds/Sky.png");so.FindProperty("backdropMaterial").objectReferenceValue=material;
        var textures=so.FindProperty("buildings");textures.arraySize=4;string[] order={"Buildings 1","Buildings 4","Buildings 3","Buildings 2"};
        for(int i=0;i<4;i++)textures.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GameAssets/Backgrounds/"+order[i]+".png");
        so.FindProperty("skyFraction").floatValue=.1f;so.FindProperty("cityRows").intValue=12;so.ApplyModifiedPropertiesWithoutUndo();
        component.RefreshBackdrop();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();SessionState.SetBool("Skyline.Check",true);EditorApplication.isPlaying=true;
    }
    static void Finish(string result)
    {
        running=false;if(SessionState.GetBool("Skyline.HadSave",false))PlayerPrefs.SetString("CountToZero.Campaign.v1",SessionState.GetString("Skyline.Save",""));else PlayerPrefs.DeleteKey("CountToZero.Campaign.v1");PlayerPrefs.Save();
        File.WriteAllText("Documentation/skyline-checks.txt",result);Debug.Log(result);SessionState.SetBool("Skyline.Return",true);EditorApplication.isPlaying=false;
    }
}
