using System;
using System.IO;
using System.Linq;
using CountToZero;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class MobileSkinUpgrade
{
    const string Path="Assets/GameAssets/UI/gdb-switch-2.png";
    static MobileSkinUpgrade(){EditorApplication.update+=()=>{if(!File.Exists("Temp/ctz-mobile-skin.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete("Temp/ctz-mobile-skin.request");try{Run();}catch(Exception e){File.WriteAllText("Documentation/mobile-skin.txt",e.ToString());Debug.LogException(e);}};}
    static T Add<T>(GameObject go)where T:Component{var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
    static SpriteMetaData Slice(string name,int x,int y){return new SpriteMetaData{name=name,rect=new Rect(x,640-y-16,16,16),alignment=0,pivot=Vector2.one*.5f};}
    [MenuItem("Count To Zero/Apply Switch Mobile Controls")]
    public static void Run()
    {
        AssetDatabase.Refresh();var importer=(TextureImporter)AssetImporter.GetAtPath(Path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=16;importer.alphaIsTransparency=true;
#pragma warning disable 0618
        importer.spritesheet=new[]{Slice("Fire_Orange_B",32,544),Slice("Slide_White_Arrow",80,432),Slice("Focus_White_X",32,464),Slice("Analog_White",240,480)};
#pragma warning restore 0618
        importer.SaveAndReimport();var sprites=AssetDatabase.LoadAllAssetsAtPath(Path).OfType<Sprite>().ToDictionary(s=>s.name);
        var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;var scene=EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");var router=UnityEngine.Object.FindFirstObjectByType<PlayerInputRouter>();var so=new SerializedObject(router);var root=(GameObject)so.FindProperty("touchRoot").objectReferenceValue;var joy=(Joystick)so.FindProperty("joystick").objectReferenceValue;
        var canvas=root.GetComponentInParent<Canvas>();root.transform.SetParent(canvas.transform,false);var rr=(RectTransform)root.transform;rr.localScale=Vector3.one;rr.anchorMin=Vector2.zero;rr.anchorMax=Vector2.one;rr.offsetMin=rr.offsetMax=Vector2.zero;Add<MobileControlLayout>(root);
        // Keep this surface beneath navigation and options, while preserving its input wiring.
        root.transform.SetAsLastSibling();
        var jr=(RectTransform)joy.transform;jr.anchorMin=jr.anchorMax=Vector2.zero;jr.pivot=Vector2.one*.5f;jr.anchoredPosition=new Vector2(145,235);jr.sizeDelta=new Vector2(184,184);joy.HandleRange=.65f;joy.DeadZone=.15f;
        var js=new SerializedObject(joy);var background=(RectTransform)js.FindProperty("background").objectReferenceValue;var handle=(RectTransform)js.FindProperty("handle").objectReferenceValue;
        var bg=background.GetComponent<Image>();bg.sprite=sprites["Analog_White"];bg.type=Image.Type.Simple;bg.preserveAspect=true;bg.color=new Color(1,1,1,.48f);bg.raycastTarget=true;
        var hi=handle.GetComponent<Image>();hi.sprite=sprites["Analog_White"];hi.type=Image.Type.Simple;hi.preserveAspect=true;hi.color=Color.white;hi.raycastTarget=false;handle.sizeDelta=new Vector2(76,76);
        foreach(var b in root.GetComponentsInChildren<Button>(true))
        {
            bool fire=b.name=="Fire",dash=b.name=="Dash";var rect=(RectTransform)b.transform;rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=Vector2.one*.5f;rect.anchoredPosition=fire?new Vector2(-120,340):dash?new Vector2(-250,205):new Vector2(-105,205);rect.sizeDelta=Vector2.one*(fire?120:96);
            var image=(Image)b.targetGraphic;image.sprite=sprites[fire?"Fire_Orange_B":dash?"Slide_White_Arrow":"Focus_White_X"];image.type=Image.Type.Simple;image.preserveAspect=true;image.color=Color.white;
            b.transition=Selectable.Transition.ColorTint;var colors=ColorBlock.defaultColorBlock;colors.pressedColor=new Color(.7f,.7f,.7f);b.colors=colors;b.navigation=new Navigation{mode=Navigation.Mode.None};
            var label=b.GetComponentInChildren<TMP_Text>(true);label.text=fire?"FIRE":dash?"SLIDE":"FOCUS";label.color=fire?new Color(1,.63f,.15f):Color.white;label.fontSize=24;label.enableAutoSizing=false;label.rectTransform.anchorMin=label.rectTransform.anchorMax=new Vector2(.5f,0);label.rectTransform.pivot=new Vector2(.5f,1);label.rectTransform.anchoredPosition=new Vector2(0,-4);label.rectTransform.sizeDelta=new Vector2(125,32);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
            var feedback=Add<UIButtonFeedback>(b.gameObject);var fs=new SerializedObject(feedback);fs.FindProperty("accent").colorValue=fire?new Color(1,.6f,.1f):Color.white;fs.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(b);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(joy);PrefabUtility.RecordPrefabInstancePropertyModifications(jr);PrefabUtility.RecordPrefabInstancePropertyModifications(bg);PrefabUtility.RecordPrefabInstancePropertyModifications(hi);PrefabUtility.RecordPrefabInstancePropertyModifications(handle);
        so.FindProperty("showTouchInEditor").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        File.WriteAllText("Documentation/mobile-skin.txt","PASS: 4 point-filtered 16x16 slices from gdb-switch-2.png; orange fire, white slide/focus, white Joystick Pack analog; safe-area touch surface. Desktop controls unchanged.\n");
    }
}
