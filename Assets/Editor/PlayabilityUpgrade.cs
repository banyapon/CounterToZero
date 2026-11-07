using System;
using System.IO;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class PlayabilityUpgrade
{
    static void Wire(UnityEngine.Object target,string name,UnityEngine.Object value){var so=new SerializedObject(target);var p=so.FindProperty(name)??so.FindProperty("<"+name+">k__BackingField");p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static TMP_Text Label(string name,Transform parent,float x,float y,float w,float h,string value,int size=22){var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=TypographyUpgrade.BodyFont;t.fontStyle=FontStyles.Bold;t.fontSize=size;t.color=Color.white;t.text=value;t.raycastTarget=false;return t;}
    static Image Image(string name,Transform parent,float x,float y,float w,float h,Color color){var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
    static Button Button(string name,Transform parent,float x,float y,float w,float h,string value){var image=Image(name,parent,x,y,w,h,new Color(.05f,.24f,.28f));image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;var t=Label("Label",b.transform,0,0,w,h,value,20);t.alignment=TextAlignmentOptions.Center;return b;}
    static Slider Slider(string name,Transform parent,float x,float y,float w){var root=Rect(name,parent,x,y,w,28);var bg=Image("Track",root,0,7,w,14,new Color(.12f,.2f,.25f));var fill=Image("Fill",root,0,7,w,14,ArenaVisuals.Cyan);var slider=root.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=new Vector2(0,7);fill.rectTransform.offsetMax=new Vector2(0,-7);var handle=Image("Handle",root,0,0,20,28,Color.white);handle.raycastTarget=true;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;slider.value=.5f;bg.raycastTarget=true;return slider;}
    static void Audio()
    {
        if(UnityEngine.Object.FindAnyObjectByType<GameAudio>()!=null)return;
        var root=new GameObject("Game Audio / Persistent Mix");var director=root.AddComponent<GameAudio>();
        foreach(string name in new[]{"effects","bed","pulse","danger"}){
            var source=new GameObject(name);source.transform.SetParent(root.transform,false);var audio=source.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;audio.volume=name=="effects"?1:0;audio.loop=name!="effects";
            if(name!="effects")audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameAssets/Audio/Polish/Music"+(name=="bed"?"Bed":name=="pulse"?"Pulse":"Danger")+".wav");Wire(director,name,audio);
        }
        var so=new SerializedObject(director);var cues=so.FindProperty("cues");cues.arraySize=9;for(int i=0;i<9;i++){var item=cues.GetArrayElementAtIndex(i);item.FindPropertyRelative("cue").enumValueIndex=i;item.FindPropertyRelative("clip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameAssets/Audio/Polish/"+(SoundCue)i+".wav");item.FindPropertyRelative("volume").floatValue=i==(int)SoundCue.Shoot?.32f:.75f;}so.ApplyModifiedPropertiesWithoutUndo();
    }
    [MenuItem("Count To Zero/Upgrade First Minute and Audio")]
    public static void Run()
    {
        try{
            foreach(var path in Directory.GetFiles("Assets/GameAssets/Audio/Polish","*.wav")){var importer=(AudioImporter)AssetImporter.GetAtPath(path);var sample=importer.defaultSampleSettings;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.8f;sample.loadType=AudioClipLoadType.DecompressOnLoad;importer.defaultSampleSettings=sample;importer.SaveAndReimport();}
            foreach(string name in new[]{"Title","Preload","Story","CountToZero","TurnEnd","StoryEnd"}){
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");Audio();
                if(name=="CountToZero"){
                    var g=UnityEngine.Object.FindAnyObjectByType<GameManager>();var hud=UnityEngine.Object.FindAnyObjectByType<HUDController>();var layout=hud.transform.Find("Reference Layout");
                    if(UnityEngine.Object.FindAnyObjectByType<TutorialDirector>()==null){
                        var go=new GameObject("First Minute Tutorial");go.transform.SetParent(g.transform.parent,false);var tutorial=go.AddComponent<TutorialDirector>();Wire(g,"Tutorial",tutorial);
                        var panel=Image("First Minute",layout,310,132,730,140,new Color(.015f,.035f,.065f,.96f));Wire(tutorial,"panel",panel.gameObject);Wire(tutorial,"heading",Label("Step",panel.transform,18,10,490,32,"TRAINING / MAKE YOUR LUCK",24));Wire(tutorial,"instruction",Label("Instruction",panel.transform,18,47,500,75,"Move to begin. Make 1 + 6 = 7.",21));
                        var skip=Button("Skip",panel.transform,535,45,177,48,"SKIP TRAINING");Wire(tutorial,"skip",skip);Wire(tutorial,"skipLabel",skip.GetComponentInChildren<TMP_Text>());var progress=Image("Progress",panel.transform,18,125,694,5,ArenaVisuals.Gold);progress.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Sprites/UIWhite.png");progress.type=UnityEngine.UI.Image.Type.Filled;progress.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;progress.fillOrigin=0;Wire(tutorial,"progress",progress);
                        var point=new GameObject("Training +6 Spawn");point.transform.SetParent(GameObject.Find("Spawn Points").transform,false);point.transform.position=new Vector3(0,0,-1.5f);Wire(tutorial,"orbSpawn",point.transform);
                        var marker=new GameObject("Training Target Marker").AddComponent<SpriteRenderer>();marker.transform.SetParent(GameObject.Find("Effects").transform,false);marker.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Sprites/NumberDisc.png");marker.color=ArenaVisuals.Gold;marker.transform.position=point.transform.position+Vector3.up*.04f;marker.transform.rotation=Quaternion.Euler(90,0,0);marker.transform.localScale=Vector3.one*1.65f;Wire(tutorial,"marker",marker);Wire(tutorial,"englishJson",AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/GameAssets/Dialogues/onboarding_en.json"));marker.gameObject.SetActive(false);panel.gameObject.SetActive(false);
                    }
                    var options=UnityEngine.Object.FindAnyObjectByType<OptionsController>();var modal=layout.Find("Options Modal");if(modal.Find("Music Volume")==null){
                        Label("Music Label",modal,310,708,160,34,"MUSIC",22);Label("SFX Label",modal,670,708,110,34,"SFX",22);Wire(options,"musicVolume",Slider("Music Volume",modal,470,709,170));Wire(options,"sfxVolume",Slider("SFX Volume",modal,790,709,195));Wire(options,"title",Button("Title",modal,1010,630,210,50,"SAVE & TITLE"));
                    }
                    var nav=layout.Find("Navigation");if(nav.Find("Title")==null){var title=Button("Title",nav,1115,245,140,45,"TITLE");UnityEditor.Events.UnityEventTools.AddPersistentListener(title.onClick,g.ReturnToTitle);Wire(hud,"returnToTitle",title);title.gameObject.SetActive(false);}
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");File.WriteAllText("Documentation/playability-upgrade.txt","Authored tutorial, skip controls, nine cues, three synchronized stems, music/SFX sliders, title exit.\n");if(Application.isBatchMode)EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText("Documentation/playability-upgrade.txt",e.ToString());Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
    }
}
