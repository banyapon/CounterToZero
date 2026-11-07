using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using CountToZero;
public static class TypographyUpgrade
{
    public static TMP_FontAsset BodyFont=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Body SDF.asset");
    public static TMP_FontAsset TitleFont=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Title SDF.asset");
    static TMP_FontAsset Font(string source,string path)
    {
        var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(existing!=null)return existing;
        var font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(source),90,9,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
        font.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(font,path);
        AssetDatabase.AddObjectToAsset(font.material,font);foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
        string chars="";for(int c=32;c<127;c++)chars+=(char)c;for(int c=0xE01;c<=0xE5B;c++)chars+=(char)c;
        font.TryAddCharacters(chars,out string missing);font.material.SetFloat(ShaderUtilities.ID_OutlineWidth,.12f);font.material.SetColor(ShaderUtilities.ID_OutlineColor,new Color(.015f,.025f,.045f,1));font.material.SetFloat(ShaderUtilities.ID_FaceDilate,.04f);font.material.EnableKeyword("OUTLINE_ON");EditorUtility.SetDirty(font);return font;
    }
    static ulong Id(UnityEngine.Object obj)=>GlobalObjectId.GetGlobalObjectIdSlow(obj).targetObjectId;
    static void Wire(UnityEngine.Object obj,string field,UnityEngine.Object value){var so=new SerializedObject(obj);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static void Style(TMP_Text t,bool title,float size)
    {
        t.font=title?TitleFont:BodyFont;t.fontStyle=FontStyles.Bold;t.fontSize=size;t.color=Color.white;t.raycastTarget=false;
        t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Overflow;
    }
    static void Convert(GameObject[] roots,string raw)
    {
        var components=roots.SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c!=null&&c.GetType().Namespace=="CountToZero").ToArray();
        var originals=new Dictionary<MonoBehaviour,string>();
        foreach(var c in components){var m=Regex.Match(raw,@"(?ms)^--- !u!\d+ &"+Id(c)+@"\r?\n(.*?)(?=^--- !u!|\z)");originals[c]=m.Groups[1].Value;}
        var replacement=new Dictionary<ulong,TMP_Text>();
        foreach(var old in roots.SelectMany(r=>r.GetComponentsInChildren<Text>(true)).ToArray())
        {
            ulong id=Id(old);var go=old.gameObject;string text=old.text;float size=old.fontSize;Color color=old.color;var align=old.alignment;
            bool title=go.name=="Heading"||go.name=="Title"||go.name=="Action Text";
            UnityEngine.Object.DestroyImmediate(old);var tmp=go.AddComponent<TextMeshProUGUI>();Style(tmp,title,Mathf.Max(title?32:20,size*1.18f));tmp.text=text;tmp.color=color;
            tmp.alignment=align==TextAnchor.MiddleCenter?TextAlignmentOptions.Center:TextAlignmentOptions.TopLeft;
            tmp.enableAutoSizing=true;tmp.fontSizeMax=tmp.fontSize;tmp.fontSizeMin=Mathf.Min(tmp.fontSize,18);
            if(go.name.Contains("HP")||go.name=="Integrity"){tmp.fontSizeMin=24;tmp.fontSizeMax=30;tmp.color=Color.white;}
            if(go.name=="Controls")tmp.text=tmp.text.Replace("\n\n","\n");
            replacement[id]=tmp;
        }
        foreach(var old in roots.SelectMany(r=>r.GetComponentsInChildren<TextMesh>(true)).ToArray())
        {
            ulong id=Id(old);var go=old.gameObject;string text=old.text;Color color=old.color;float size=old.characterSize;var local=go.transform.localPosition;var scale=go.transform.localScale;
            UnityEngine.Object.DestroyImmediate(old);var tmp=go.AddComponent<TextMeshPro>();Style(tmp,false,Mathf.Max(3.5f,size*32));tmp.text=text;tmp.color=color;tmp.alignment=TextAlignmentOptions.Center;tmp.rectTransform.sizeDelta=new Vector2(text.Length>8?6:3,1);tmp.transform.localPosition=local;tmp.transform.localScale=scale;replacement[id]=tmp;
        }
        foreach(var c in components)
        {
            var so=new SerializedObject(c);string block=originals[c];
            foreach(var field in c.GetType().GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public))
            {
                if(field.FieldType==typeof(TMP_Text)){
                    var match=Regex.Match(block,@"(?m)^  "+field.Name+@": \{fileID: (\d+)");if(match.Success&&replacement.TryGetValue(ulong.Parse(match.Groups[1].Value),out var value))so.FindProperty(field.Name).objectReferenceValue=value;
                }
                else if(field.FieldType==typeof(TMP_Text[])){
                    var match=Regex.Match(block,@"(?ms)^  "+field.Name+@":\r?\n((?:  - [^\n]*\n)*)");var ids=Regex.Matches(match.Groups[1].Value,@"fileID: (\d+)");var p=so.FindProperty(field.Name);p.arraySize=ids.Count;for(int i=0;i<ids.Count;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=replacement[ulong.Parse(ids[i].Groups[1].Value)];
                }
            }so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position){var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=position;return r;}
    static ActorHealthBar Bar(Transform parent,MonoBehaviour actor,bool world,float height=0)
    {
        var r=Rect("HP Bar",parent,new Vector2(180,28),Vector2.zero);
        if(world){var canvas=r.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;r.localPosition=new Vector3(0,height,0);r.localScale=Vector3.one*.014f;r.gameObject.AddComponent<Billboard>();}
        var bg=r.gameObject.AddComponent<Image>();bg.color=new Color(.015f,.025f,.045f,.96f);bg.raycastTarget=false;
        var fillRect=Rect("Health Fill",r,new Vector2(174,22),Vector2.zero);var fill=fillRect.gameObject.AddComponent<Image>();fill.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Sprites/UIWhite.png");fill.color=actor is PlayerController?ArenaVisuals.Green:actor is BossController?ArenaVisuals.Red:ArenaVisuals.Violet;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;fill.raycastTarget=false;
        var label=Rect("HP Value",r,new Vector2(178,28),Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();Style(label,false,22);label.text=actor is PlayerController?"5 / 5 HP":actor is BossController?"99 / 99 HP":"3 / 3 HP";label.alignment=TextAlignmentOptions.Center;
        var hp=r.gameObject.AddComponent<ActorHealthBar>();Wire(hp,"fill",fill);Wire(hp,"value",label);Wire(hp,actor is PlayerController?"player":actor is BossController?"boss":"minion",actor);return hp;
    }
    [MenuItem("Count To Zero/Upgrade TMP and HP Bars")]
    public static void Run()
    {
        try{
            if(TMP_Settings.instance==null)throw new InvalidOperationException("Import the included Assets/TextMesh Pro/Resources/TMP Settings.asset before running the upgrade.");
            Font("Assets/Fonts/2005_iannnnnAMD.ttf","Assets/Fonts/Body SDF.asset");var title=Font("Assets/Fonts/ZF#2ndPixelus.ttf","Assets/Fonts/Title SDF.asset");title.fallbackFontAssetTable=new List<TMP_FontAsset>{BodyFont};EditorUtility.SetDirty(title);AssetDatabase.SaveAssets();
            foreach(string name in new[]{"Title","Preload","Story","CountToZero","TurnEnd","StoryEnd"}){
                string path="Assets/Scenes/"+name+".unity";string raw=File.ReadAllText(path);var scene=EditorSceneManager.OpenScene(path);var roots=scene.GetRootGameObjects();Convert(roots,raw);
                if(name=="CountToZero"){
                    var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();var boss=UnityEngine.Object.FindAnyObjectByType<BossController>();
                    if(player.transform.Find("HP Bar")==null)Bar(player.transform,player,true,2.35f);
                    if(boss.transform.Find("HP Bar")==null)Bar(boss.transform,boss,true,3.55f);
                    foreach(var minion in roots.SelectMany(r=>r.GetComponentsInChildren<MinionController>(true)))if(minion.transform.Find("HP Bar")==null)Bar(minion.transform,minion,true,1.85f);
                    var hud=UnityEngine.Object.FindAnyObjectByType<HUDController>();var bottom=hud.transform.Find("Reference Layout/Player HUD");
                    if(bottom.Find("HP Bar")==null){var hp=Bar(bottom,player,false);var rt=(RectTransform)hp.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(176,-66);}
                    var integrity=bottom.Find("Integrity").GetComponent<TMP_Text>();integrity.rectTransform.sizeDelta=new Vector2(180,32);integrity.rectTransform.anchoredPosition=new Vector2(176,-30);
                    var health=new SerializedObject(hud).FindProperty("healthFill").objectReferenceValue as Image;health.rectTransform.sizeDelta=new Vector2(health.rectTransform.sizeDelta.x,20);if(health.transform.parent.GetComponent<Image>()!=null)((RectTransform)health.transform.parent).sizeDelta=new Vector2(((RectTransform)health.transform.parent).sizeDelta.x,24);
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            string prefab="Assets/GameAssets/Prefabs/NumberOrb.prefab";string prefabRaw=File.ReadAllText(prefab);var root=PrefabUtility.LoadPrefabContents(prefab);Convert(new[]{root},prefabRaw);var orb=root.GetComponent<NumberOrb>();Wire(orb,"label",root.GetComponentInChildren<TMP_Text>(true));PrefabUtility.SaveAsPrefabAsset(root,prefab);PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");File.WriteAllText("Documentation/typography-upgrade.txt","TMP conversion and authored HP bars complete.\n");if(Application.isBatchMode)EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText("Documentation/typography-upgrade.txt",e.ToString());Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
    }
}
