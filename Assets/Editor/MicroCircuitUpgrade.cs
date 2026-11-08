using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MicroCircuitUpgrade
{
    static MicroCircuitUpgrade(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(!File.Exists("Temp/ctz-micro-upgrade.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete("Temp/ctz-micro-upgrade.request");try{Run();}catch(Exception e){Debug.LogException(e);File.WriteAllText("Documentation/micro-upgrade.txt",e.ToString());}
    }
    static void Wire(UnityEngine.Object o,string key,UnityEngine.Object value){var so=new SerializedObject(o);so.FindProperty(key).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static void Array(UnityEngine.Object o,string key,UnityEngine.Object[] values){var so=new SerializedObject(o);var p=so.FindProperty(key);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static T Add<T>(GameObject go) where T:Component{var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
    static void Box(Transform parent,string name,Vector3 p,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;
    }
    [MenuItem("Count To Zero/Upgrade Micro Circuit Sectors")]
    public static void Run()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        AssetDatabase.Refresh();
        var variants=new[]{"8walk","8walk_scout","8walk_heavy"}.SelectMany(n=>PresentationUpgrade.SliceWalkFrames("Assets/GameAssets/Sprites/man/"+n+".png")).ToArray();
        const string idlePath="Assets/GameAssets/Sprites/man/1 idle.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(idlePath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=16;
#pragma warning disable 0618
        importer.spritesheet=Enumerable.Range(0,4).Select(i=>new SpriteMetaData{name=(i<2?"IdleFront":"IdleBack")+(i%2),rect=new Rect(i<2?0:16,(1-i%2)*16,16,16),alignment=0,pivot=new Vector2(.5f,.5f)}).ToArray();
#pragma warning restore 0618
        importer.SaveAndReimport();
        var idle=AssetDatabase.LoadAllAssetsAtPath(idlePath).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");var roots=scene.GetRootGameObjects();var g=UnityEngine.Object.FindAnyObjectByType<GameManager>();
        var hazards=Add<SectorHazards>(g.gameObject);
        Array(hazards,"floorLines",roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Circuit Z").ToArray());
        var beamObject=GameObject.Find("Red Sector Barrier");if(beamObject==null)beamObject=new GameObject("Red Sector Barrier");
        var beam=Add<LineRenderer>(beamObject);beam.positionCount=2;beam.useWorldSpace=true;beam.alignment=LineAlignment.View;beam.numCapVertices=0;
        const string beamPath="Assets/GameAssets/Materials/CircuitRedLaser.mat";
        var beamMaterial=AssetDatabase.LoadAssetAtPath<Material>(beamPath);if(beamMaterial==null){beamMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(beamMaterial,beamPath);}beamMaterial.color=new Color(1,.01f,.025f);EditorUtility.SetDirty(beamMaterial);
        beam.sharedMaterial=beamMaterial;beam.startColor=beam.endColor=Color.red;beam.widthMultiplier=.22f;beam.enabled=false;Wire(hazards,"laser",beam);
        Array(g.Player.GetComponent<HeroAnimator>(),"idleFronts",idle.Where(s=>s.name.StartsWith("IdleFront")).ToArray());Array(g.Player.GetComponent<HeroAnimator>(),"idleBacks",idle.Where(s=>s.name.StartsWith("IdleBack")).ToArray());
        foreach(var minion in roots.SelectMany(r=>r.GetComponentsInChildren<MinionController>(true)))
        {
            var appearance=Add<RobotAppearance>(minion.gameObject);Wire(appearance,"animator",minion.GetComponent<HeroAnimator>());
            Array(appearance,"fronts",variants.Where(s=>s.name.StartsWith("Front")).ToArray());Array(appearance,"backs",variants.Where(s=>s.name.StartsWith("Back")).ToArray());
        }
        var old=GameObject.Find("Micro Circuit Border");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        var border=new GameObject("Micro Circuit Border").transform;
        var colors=new[]{new Color(.05f,.8f,.95f),new Color(.2f,1,.55f),new Color(1,.65f,.12f),new Color(.65f,.35f,1)};
        for(int n=0;n<4;n++)
        {
            string path="Assets/GameAssets/Materials/CircuitTrace"+n+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,path);}mat.color=colors[n];EditorUtility.SetDirty(mat);
            foreach(int side in new[]{-1,1})for(int j=0;j<4;j++)
            {
                float z=-6+j*4+n*.19f,x=side*(11.4f+n*.26f);
                Box(border,"Copper signal trace",new Vector3(x,.2f,z),new Vector3(.045f,.04f,2.4f),mat);
                Box(border,"Right angle trace",new Vector3(x+side*.22f,.2f,z+1.18f),new Vector3(.48f,.04f,.045f),mat);
                Box(border,"Circuit contact",new Vector3(x+side*.44f,.2f,z+1.18f),new Vector3(.16f,.045f,.16f),mat);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Documentation/micro-upgrade.txt","Authored conveyor floor lines, red sector laser, three robot variants, idle frames and multicolor PCB borders.\n");
    }
}
