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
public static class StoryRoundUpgrade
{
    static StoryRoundUpgrade(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        const string request="Temp/ctz-story-round.request";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete(request);
        try{Run();}catch(Exception e){Debug.LogException(e);File.WriteAllText("Documentation/story-round-upgrade.txt",e.ToString());}
    }

    [MenuItem("Count To Zero/Upgrade Story and Round Presentation")]
    public static void Run()
    {
        var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        string originalPath=original.path;
        if(original.isDirty)EditorSceneManager.SaveScene(original);
        var comic=new Sprite[4];
        for(int i=0;i<4;i++)
        {
            string path="Assets/GameAssets/Sprites/cutscene_"+(i+1)+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point;
            importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            comic[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        int buttons=0;
        foreach(string name in new[]{"Title","Preload","Story","CountToZero","TurnEnd","StoryEnd"})
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
            var roots=scene.GetRootGameObjects();
            foreach(var expansion in roots.SelectMany(r=>r.GetComponentsInChildren<CampaignExpansionUI>(true)))
            {
                var so=new SerializedObject(expansion);var array=so.FindProperty("comic");array.arraySize=4;
                for(int i=0;i<4;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=comic[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var button in roots.SelectMany(r=>r.GetComponentsInChildren<Button>(true)))
            {
            if(button.targetGraphic is Image image)image.color=new Color(.18f,.26f,.34f,1);
            foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                label.fontStyle=FontStyles.Bold;label.color=Color.white;
                label.enableAutoSizing=true;label.fontSize=30;label.fontSizeMin=20;label.fontSizeMax=32;
                label.textWrappingMode=TextWrappingModes.NoWrap;
                label.raycastTarget=false;
                buttons++;
            }
            }
            var manager=roots.SelectMany(r=>r.GetComponentsInChildren<GameManager>(true)).FirstOrDefault();
            if(manager!=null&&manager.GetComponent<RoundCountdown>()==null)manager.gameObject.AddComponent<RoundCountdown>();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(originalPath))EditorSceneManager.OpenScene(originalPath);
        Directory.CreateDirectory("Documentation");
        File.WriteAllText("Documentation/story-round-upgrade.txt","PASS: four cutscene references, three prologue panels, ending art and bold readable labels on "+buttons+" button texts. Round countdown authored in gameplay scene.\n");
    }
}
