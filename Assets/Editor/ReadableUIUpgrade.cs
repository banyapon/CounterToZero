using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class ReadableUIUpgrade
{
    static ReadableUIUpgrade(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(!File.Exists("Temp/ctz-readable-ui.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("Temp/ctz-readable-ui.request");try{Run();}catch(Exception e){Debug.LogException(e);File.WriteAllText("Documentation/readable-ui.txt",e.ToString());}
    }
    static T Add<T>(GameObject go) where T:Component{var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
    static Color[] Palette={new Color(.3f,1,.76f),new Color(.83f,.55f,1),new Color(1,.78f,.3f)};
    static void Audio(GameAudio audio)
    {
        var so=new SerializedObject(audio);var cues=so.FindProperty("cues");cues.arraySize=Enum.GetValues(typeof(SoundCue)).Length;
        for(int i=0;i<cues.arraySize;i++)
        {
            var cue=(SoundCue)i;string file=cue==SoundCue.Blocked?"Polish/Blocked":cue==SoundCue.UIHover?"wobble":cue==SoundCue.UIClick?"Choose":cue==SoundCue.Explosion?"Explosion":cue==SoundCue.Death?"GameOver":null;
            var p=cues.GetArrayElementAtIndex(i);p.FindPropertyRelative("cue").enumValueIndex=i;
            if(file!=null)p.FindPropertyRelative("clip").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameAssets/Audio/"+file+".wav");
            p.FindPropertyRelative("volume").floatValue=cue==SoundCue.UIHover?.23f:cue==SoundCue.UIClick?.5f:cue==SoundCue.Explosion?.55f:cue==SoundCue.Shoot?.32f:.75f;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Size(TMP_Text text,float size,float width=0,float height=0)
    {
        text.fontSize=size;text.enableAutoSizing=false;text.lineSpacing=4;
        if(width>0)text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
        if(height>0)text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
    }
    [MenuItem("Count To Zero/Upgrade Readable UI and New Audio")]
    public static void Run()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        AssetDatabase.Refresh();string original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        int buttons=0,texts=0;var fit=new List<string>();
        foreach(string name in new[]{"Title","Preload","Story","CountToZero","TurnEnd","StoryEnd"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var roots=scene.GetRootGameObjects();
            foreach(var audio in roots.SelectMany(r=>r.GetComponentsInChildren<GameAudio>(true)))Audio(audio);
            if(!roots.SelectMany(r=>r.GetComponentsInChildren<AudioListener>(true)).Any())
            {var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).FirstOrDefault();if(camera!=null)camera.gameObject.AddComponent<AudioListener>();}
            int index=0;
            foreach(var button in roots.SelectMany(r=>r.GetComponentsInChildren<Button>(true)))
            {
                var accent=Palette[index++%3];var feedback=Add<UIButtonFeedback>(button.gameObject);var so=new SerializedObject(feedback);so.FindProperty("accent").colorValue=accent;so.ApplyModifiedPropertiesWithoutUndo();
                var outline=Add<Outline>(button.gameObject);outline.effectColor=new Color(accent.r,accent.g,accent.b,.45f);outline.effectDistance=Vector2.one;
                var image=button.targetGraphic as Image;if(image!=null)image.color=Color.Lerp(Color.white,accent,.35f);
                var label=button.GetComponentInChildren<TMP_Text>(true);if(label!=null){label.color=Color.Lerp(Color.white,accent,.2f);label.fontSizeMin=24;label.fontSizeMax=Mathf.Max(32,label.fontSizeMax);label.enableAutoSizing=true;}
                PrefabUtility.RecordPrefabInstancePropertyModifications(button);buttons++;
            }
            foreach(var t in roots.SelectMany(r=>r.GetComponentsInChildren<TextMeshProUGUI>(true)))
            {
                var canvas=t.GetComponentInParent<Canvas>();if(canvas!=null && canvas.renderMode==RenderMode.WorldSpace)continue;
                if(t.GetComponentInParent<Button>()!=null)continue;
                if(t.name=="Heading"||t.name=="Title"||t.name=="Action Text")continue;
                if(t.name=="Story and Results")
                {t.fontSize=72;t.fontSizeMin=42;t.fontSizeMax=72;t.enableAutoSizing=true;t.lineSpacing=7;t.rectTransform.sizeDelta=new Vector2(1040,280);}
                else if(t.fontSize<32)
                {Size(t,26,0,Mathf.Max(t.rectTransform.rect.height,36));texts++;}
                if(t.name=="Dash Status"||t.name=="Focus Status")Size(t,24,250,52);
                if(t.name=="Controls")Size(t,24,300,105);
                if(t.name=="Rule Clock")Size(t,24,240,32);
                if(t.name=="Score")Size(t,26,230,65);
                if(t.name=="Status")Size(t,26,340,100);
                if(t.name=="Instruction")Size(t,27,500,90);
                if(t.name=="Step")Size(t,28,500,36);
            }
            foreach(var tutorial in roots.SelectMany(r=>r.GetComponentsInChildren<TutorialDirector>(true)))
            {
                var so=new SerializedObject(tutorial);var panel=(GameObject)so.FindProperty("panel").objectReferenceValue;((RectTransform)panel.transform).sizeDelta=new Vector2(730,170);
                var progress=(Image)so.FindProperty("progress").objectReferenceValue;progress.rectTransform.anchoredPosition=new Vector2(18,-155);
            }
            foreach(var dialog in roots.SelectMany(r=>r.GetComponentsInChildren<DialogueController>(true)))
            {
                var so=new SerializedObject(dialog);var panel=(GameObject)so.FindProperty("panel").objectReferenceValue;
                ((RectTransform)panel.transform).sizeDelta=new Vector2(630,140);
                var speaker=(TMP_Text)so.FindProperty("speaker").objectReferenceValue;var body=(TMP_Text)so.FindProperty("body").objectReferenceValue;
                Size(speaker,26,600,34);Size(body,30,600,94);body.rectTransform.anchoredPosition=new Vector2(15,-40);
                panel.GetComponent<Image>().color=new Color(.85f,.8f,1,.98f);
            }
            foreach(var image in roots.SelectMany(r=>r.GetComponentsInChildren<Image>(true)))
            {
                if(image.name=="Rule Card" || image.name=="Player HUD" || image.name=="Menu Card" || image.name=="First Minute")
                {var outline=Add<Outline>(image.gameObject);var color=image.name=="Rule Card"?Palette[2]:image.name=="Player HUD"?Palette[0]:Palette[1];outline.effectColor=new Color(color.r,color.g,color.b,.7f);outline.effectDistance=Vector2.one;}
            }
            Canvas.ForceUpdateCanvases();
            foreach(var t in roots.SelectMany(r=>r.GetComponentsInChildren<TextMeshProUGUI>(true)))
            {
                if(t.name=="Line"||t.name=="Story and Results"||t.name=="Instruction")
                {t.ForceMeshUpdate(true);fit.Add(name+" / "+t.name+" : size="+t.fontSize+" rect="+t.rectTransform.rect.size+" preferred="+t.GetPreferredValues(t.text,t.rectTransform.rect.width,0));}
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        File.WriteAllText("Documentation/readable-ui.txt","Updated "+texts+" body labels and "+buttons+" buttons across six scenes; 4 new sound mappings; audio listeners on menu cameras.\n"+string.Join("\n",fit));
    }
}
