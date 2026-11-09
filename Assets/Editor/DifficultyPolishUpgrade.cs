using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DifficultyPolishUpgrade
{
    static DifficultyPolishUpgrade(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(!File.Exists("Temp/ctz-difficulty-polish.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("Temp/ctz-difficulty-polish.request");
        try{Run();}catch(Exception e){Debug.LogException(e);File.WriteAllText("Documentation/difficulty-polish.txt",e.ToString());}
    }
    [MenuItem("Count To Zero/Upgrade Difficulty and Lucky Punch")]
    public static void Run()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        AssetDatabase.Refresh();
        const string path="Assets/GameAssets/Sprites/man/1 punch.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency=true;importer.spritePixelsPerUnit=16;
        // Fixed crop inside column 1 removes transparent padding without shifting the pose.
        var slices=Enumerable.Range(0,4).Select(row=>new SpriteMetaData{name="PunchFront_"+row,rect=new Rect(8,(3-row)*32+4,16,20),alignment=0,pivot=new Vector2(.5f,.5f)}).ToArray();
#pragma warning disable 0618
        importer.spritesheet=slices;
#pragma warning restore 0618
        importer.SaveAndReimport();
        var frames=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");
        var g=UnityEngine.Object.FindAnyObjectByType<GameManager>();
        var actors=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MinionController>(true)).Cast<MonoBehaviour>().Append(g.Player);
        foreach(var actor in actors)
        {
            var visual=(SpriteRenderer)new SerializedObject(actor).FindProperty("sprite").objectReferenceValue;
            visual.transform.localScale=Vector3.one*.85f;
            var bar=actor.transform.Find("HP Bar");if(bar!=null)bar.localPosition=new Vector3(0,actor is PlayerController?1.55f:1.35f,0);
        }
        foreach(var renderer in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>(true)))
            if(renderer.sharedMaterial!=null && renderer.sharedMaterial.name=="Arena_743EB2B2" && renderer.GetComponent<ElectricBlockPulse>()==null)
                renderer.gameObject.AddComponent<ElectricBlockPulse>();
        var cutin=new SerializedObject(g.CutIn);
        var panel=(RectTransform)cutin.FindProperty("panel").objectReferenceValue;
        var portrait=panel.Find("Glitch Action Pose").GetComponent<Image>();
        portrait.sprite=frames[0];portrait.type=Image.Type.Simple;portrait.preserveAspect=true;portrait.color=Color.white;
        portrait.transform.localRotation=Quaternion.identity;portrait.rectTransform.sizeDelta=new Vector2(176,220);
        cutin.FindProperty("portrait").objectReferenceValue=portrait;
        var array=cutin.FindProperty("punchFrames");array.arraySize=frames.Length;
        for(int i=0;i<frames.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=frames[i];
        cutin.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Documentation/difficulty-polish.txt","Applied 50% actor size (1.7 -> 0.85), point-filtered four-frame Lucky punch at the original lower-left position.\n");
    }
}
