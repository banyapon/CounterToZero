using System.IO;
using System.Text;
using CountToZero;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
// Itch polish pass for the live HUD. Request files (editor open):
//   Temp/ctz-itch-ui-dump.request  -> Documentation/itch-ui-dump.txt
[InitializeOnLoad]
public static class ItchUiUpgrade
{
    const string Scene="Assets/Scenes/CountToZero.unity";
    static ItchUiUpgrade(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        // Lets an external tool pick up script edits while the editor window is in the background.
        if(File.Exists("Temp/ctz-refresh.request")){File.Delete("Temp/ctz-refresh.request");AssetDatabase.Refresh();return;}
        if(File.Exists("Temp/ctz-menu.request")){var item=File.ReadAllText("Temp/ctz-menu.request").Trim();File.Delete("Temp/ctz-menu.request");EditorApplication.ExecuteMenuItem(item);return;}
        if(File.Exists("Temp/ctz-itch-ui-dump.request")){File.Delete("Temp/ctz-itch-ui-dump.request");Dump();}
        if(File.Exists("Temp/ctz-itch-ui-apply.request")){File.Delete("Temp/ctz-itch-ui-apply.request");Apply();}
    }
    static RectTransform Find(string path){var go=GameObject.Find(path);return go!=null?(RectTransform)go.transform:null;}
    static RectTransform FindAny(string path)
    {
        foreach(var r in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(P(r)==path)return r;
        throw new System.Exception("missing "+path);
    }
    static void Place(string path,float x,float y,float w,float h){var r=FindAny(path);Undo.RecordObject(r,"Itch UI");r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    static void Font(string path,float max,float min,bool auto){var t=FindAny(path).GetComponent<TMP_Text>();Undo.RecordObject(t,"Itch UI");t.enableAutoSizing=auto;t.fontSize=max;t.fontSizeMax=max;t.fontSizeMin=min;t.textWrappingMode=TextWrappingModes.Normal;}
    // Moves every message panel into the free strip between the arena and the bottom HUD,
    // so the boss and player stay visible during the first minute.
    static void Apply()
    {
        EditorSceneManager.OpenScene(Scene);var log=new StringBuilder();
        const string L="HUD Canvas/Reference Layout/";
        // Dialogue: one slim strip.
        Place(L+"Dialogue",260,-626,760,74);
        Place(L+"Dialogue/Speaker",16,-5,728,26);Font(L+"Dialogue/Speaker",22,14,true);
        Place(L+"Dialogue/Line",16,-31,728,40);Font(L+"Dialogue/Line",24,15,true);
        // Training card: compact, bottom of the arena.
        Place(L+"First Minute",260,-592,760,108);
        Place(L+"First Minute/Step",16,-8,560,30);Font(L+"First Minute/Step",24,16,true);
        Place(L+"First Minute/Instruction",16,-38,560,62);Font(L+"First Minute/Instruction",26,15,true);
        Place(L+"First Minute/Skip",572,-30,176,46);
        Place(L+"First Minute/Progress",16,-100,728,4);
        // Lucky Rule: the rule itself is the headline, the clock is the caption.
        Place(L+"Lucky Rule/Rule Clock",14,-4,270,28);Font(L+"Lucky Rule/Rule Clock",27,27,false);
        Place(L+"Lucky Rule/Rule",14,-31,270,50);Font(L+"Lucky Rule/Rule",48,22,true);
        var rule=FindAny(L+"Lucky Rule/Rule").GetComponent<TMP_Text>();Undo.RecordObject(rule,"Itch UI");rule.color=new Color(1f,.82f,.22f,1f);rule.textWrappingMode=TextWrappingModes.NoWrap;
        // Boss VALUE: beside the HP bar instead of under it, on top of sprites.
        var bossValue=FindAny("Actors/OMEGA-99/Current Value");Undo.RecordObject(bossValue,"Itch UI");
        bossValue.anchoredPosition=new Vector2(4.6f,3.75f);bossValue.sizeDelta=new Vector2(3.8f,1);
        var bv=bossValue.GetComponent<TMP_Text>();Undo.RecordObject(bv,"Itch UI");bv.fontSize=5.4f;bv.enableAutoSizing=false;bv.textWrappingMode=TextWrappingModes.NoWrap;bv.alignment=TextAlignmentOptions.Left;
        var br=bossValue.GetComponent<MeshRenderer>();if(br!=null){Undo.RecordObject(br,"Itch UI");br.sortingOrder=60;}
        // Player readouts: larger HP bar and value tag.
        var hp=FindAny("Actors/GLITCH-0/HP Bar");Undo.RecordObject(hp,"Itch UI");hp.localScale=Vector3.one*.014f;
        var pv=FindAny("Actors/GLITCH-0/Current Value").GetComponent<TMP_Text>();Undo.RecordObject(pv,"Itch UI");pv.fontSize=6.2f;
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        log.AppendLine("APPLIED itch HUD layout: dialogue + training strip, rule headline, boss value tag, player HP scale");
        // Exact-match jackpot cue in every scene that owns a GameAudio (whichever loads first persists).
        AssetDatabase.ImportAsset("Assets/GameAssets/Audio/Polish/LuckyJackpot.wav");
        var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GameAssets/Audio/Polish/LuckyJackpot.wav");
        foreach(var path in new[]{"Assets/Scenes/Title.unity","Assets/Scenes/Preload.unity","Assets/Scenes/Story.unity","Assets/Scenes/StoryEnd.unity","Assets/Scenes/TurnEnd.unity",Scene})
        {
            var scene=EditorSceneManager.OpenScene(path);
            foreach(var audio in Object.FindObjectsByType<GameAudio>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var so=new SerializedObject(audio);var cues=so.FindProperty("cues");bool has=false;
                for(int i=0;i<cues.arraySize;i++)if(cues.GetArrayElementAtIndex(i).FindPropertyRelative("cue").intValue==(int)SoundCue.Jackpot){has=true;cues.GetArrayElementAtIndex(i).FindPropertyRelative("clip").objectReferenceValue=clip;}
                if(!has){cues.arraySize++;var e=cues.GetArrayElementAtIndex(cues.arraySize-1);e.FindPropertyRelative("cue").intValue=(int)SoundCue.Jackpot;e.FindPropertyRelative("clip").objectReferenceValue=clip;e.FindPropertyRelative("volume").floatValue=.9f;}
                so.ApplyModifiedPropertiesWithoutUndo();log.AppendLine("jackpot cue -> "+path+" clip="+(clip!=null));
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        EditorSceneManager.OpenScene(Scene);
        File.WriteAllText("Documentation/itch-ui-apply.txt",log.ToString());
    }
    static string P(Transform t){var s=t.name;while(t.parent!=null){t=t.parent;s=t.name+"/"+s;}return s;}
    static void Dump()
    {
        EditorSceneManager.OpenScene(Scene);var sb=new StringBuilder();
        string[] roots={"Dialogue","First Minute","Rule","Rule Clock","Rule Panel"};
        foreach(var r in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            bool want=false;foreach(var n in roots)if(r.name==n||(r.parent!=null&&(r.parent.name==n)))want=true;
            if(r.GetComponentInParent<BossController>(true)!=null||r.GetComponentInParent<PlayerController>(true)!=null||r.GetComponentInParent<ActorHealthBar>(true)!=null)want=true;
            if(!want)continue;
            var t=r.GetComponent<TMP_Text>();
            sb.AppendLine(P(r)+" active="+r.gameObject.activeSelf+" amin="+r.anchorMin+" amax="+r.anchorMax+" piv="+r.pivot+" pos="+r.anchoredPosition+" size="+r.sizeDelta+" lpos="+r.localPosition+" scale="+r.localScale+(t!=null?" font="+t.fontSize+" auto="+t.enableAutoSizing+" fontAsset="+(t.font?t.font.name:"null")+" color="+t.color+" text='"+t.text.Replace("\n","|")+"'":""));
        }
        File.WriteAllText("Documentation/itch-ui-dump.txt",sb.ToString());
    }
}
