using TMPro;
using System;
using System.IO;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
[InitializeOnLoad]
public static class CampaignBuilder
{
    const string Request="Temp/ctz-campaign-build.request";
    static CampaignBuilder(){EditorApplication.update+=Poll;}
    static void Poll(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;File.Delete(Request);try{Build();}catch(Exception e){File.WriteAllText("Temp/ctz-campaign-build.error",e.ToString());Debug.LogException(e);}}
    static void Wire(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;}
    static TMP_Text Text(string name,Transform parent,float y,float w,float h,int size,string content){var t=Rect(name,parent,0,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=TypographyUpgrade.BodyFont;t.text=content;t.fontSize=size;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
    static Button Button(string name,Transform parent,float x,float y){var r=Rect(name,parent,x,y,300,65);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.04f,.26f,.3f);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;Text("Label",r,0,290,60,22,name);return button;}
    [MenuItem("Count To Zero/Build Campaign Scenes")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)return;
        var active=EditorSceneManager.GetActiveScene();if(active.isDirty)EditorSceneManager.SaveScene(active);
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");
        var camera=UnityEngine.Object.FindFirstObjectByType<FollowCamera>();var so=new SerializedObject(camera);so.FindProperty("offset").vector3Value=new Vector3(0,21,-18);so.ApplyModifiedPropertiesWithoutUndo();camera.transform.position=new Vector3(0,21,-18);camera.transform.LookAt(Vector3.zero);
        string[] names={"RADIAL SWAY","HUNTER FAN","ORBIT SPIRAL","CROSS SWEEP","SINE WAVE","BROKEN HALO","TWIN HELIX","NEEDLE BURST","PETAL ENGINE","PINWHEEL","CODE RAIN","SIDEWINDER","VORTEX","TWIN FANG","SAFE LANE","DOUBLE PULSE","SCISSORS","ROTATING GATE","GOLDEN SCATTER","FINAL EQUATION","SQUARE RUSH","SEGMENTED VIRUS","HEARTBEAT","ORBIT MINES","ZIGZAG"};
        var boss=UnityEngine.Object.FindFirstObjectByType<BossController>();so=new SerializedObject(boss);var array=so.FindProperty("patterns");array.arraySize=25;
        for(int i=0;i<25;i++){
            string path="Assets/GameAssets/Patterns/"+i+"_"+names[i].Replace(' ','_')+".asset";var pattern=AssetDatabase.LoadAssetAtPath<BossPatternDefinition>(path);
            if(pattern==null){pattern=ScriptableObject.CreateInstance<BossPatternDefinition>();pattern.displayName=names[i];pattern.shot=(BossShot)i;pattern.motion=(BossMotion)(i%5);pattern.interval=i==6?.45f:i==10?1.3f:.9f;pattern.speed=3.2f+(i%4)*.2f;pattern.count=12;AssetDatabase.CreateAsset(pattern,path);}array.GetArrayElementAtIndex(i).objectReferenceValue=pattern;
        }so.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        string[] scenes={"Title","Preload","Story","TurnEnd","StoryEnd"};
        for(int i=0;i<scenes.Length;i++){
            string path="Assets/Scenes/"+scenes[i]+".unity";if(File.Exists(path))continue;
            var pageScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cam=new GameObject("Menu Camera").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.012f,.022f,.045f);
            var canvas=new GameObject("Screen Canvas").AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,800);scaler.matchWidthOrHeight=.5f;canvas.gameObject.AddComponent<GraphicRaycaster>();
            var screen=new GameObject("Campaign Screen").AddComponent<CampaignScreen>();so=new SerializedObject(screen);so.FindProperty("page").enumValueIndex=i;so.ApplyModifiedPropertiesWithoutUndo();
            var border=Rect("Accent",canvas.transform,0,245,900,4).gameObject.AddComponent<Image>();border.color=new Color(.1f,.9f,.85f);border.raycastTarget=false;
            Wire(screen,"heading",Text("Heading",canvas.transform,175,1050,100,48,scenes[i].ToUpperInvariant()));Wire(screen,"body",Text("Story and Results",canvas.transform,15,950,220,26,""));
            var primary=Button("Continue",canvas.transform,-175,-220);var secondary=Button("Back",canvas.transform,175,-220);Wire(screen,"primary",primary);Wire(screen,"secondary",secondary);Wire(screen,"primaryLabel",primary.GetComponentInChildren<TMP_Text>());Wire(screen,"secondaryLabel",secondary.GetComponentInChildren<TMP_Text>());
            var bar=Rect("Loading Progress",canvas.transform,0,-140,800,24);var bg=bar.gameObject.AddComponent<Image>();bg.color=new Color(.05f,.12f,.18f);bg.raycastTarget=false;var slider=bar.gameObject.AddComponent<Slider>();slider.interactable=false;
            var fill=Rect("Fill",bar,0,0,800,24);var fillImage=fill.gameObject.AddComponent<Image>();fillImage.color=new Color(.1f,.9f,.85f);fillImage.raycastTarget=false;fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;slider.fillRect=fill;slider.minValue=0;slider.maxValue=1;slider.value=0;Wire(screen,"progress",slider);Wire(screen,"progressLabel",Text("Loading Percent",canvas.transform,-185,700,40,20,"0%"));Wire(screen,"storyJson",AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/GameAssets/Dialogues/story_en.json"));
            var events=new GameObject("EventSystem").AddComponent<EventSystem>();events.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorSceneManager.SaveScene(pageScene,path);
        }
        string[] ordered={"Title","Preload","Story","CountToZero","TurnEnd","StoryEnd"};var settings=new EditorBuildSettingsScene[ordered.Length];for(int i=0;i<ordered.Length;i++)settings[i]=new EditorBuildSettingsScene("Assets/Scenes/"+ordered[i]+".unity",true);EditorBuildSettings.scenes=settings;AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");File.WriteAllText("Temp/ctz-campaign-build.done","Campaign scenes authored. 25 patterns. Title first in build settings.");Debug.Log("COUNT TO ZERO: campaign scenes ready.");
    }
}
