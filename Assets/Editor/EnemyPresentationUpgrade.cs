using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EnemyPresentationUpgrade
{
    static EnemyPresentationUpgrade() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists("Temp/ctz-enemy-presentation.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete("Temp/ctz-enemy-presentation.request");
        try { Run(); } catch (Exception e) { File.WriteAllText("Documentation/enemy-presentation.txt", e.ToString()); Debug.LogException(e); }
    }
    static void Wire(UnityEngine.Object obj, string field, UnityEngine.Object value)
    { var so = new SerializedObject(obj); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Frames(HeroAnimator animator, string field, Sprite[] sprites)
    {
        var so = new SerializedObject(animator); var p = so.FindProperty(field); p.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Sprite Ring()
    {
        const string path = "Assets/GameAssets/Sprites/EnemyDeathRing.png";
        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixels = new Color32[32 * 32];
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            float radius = new Vector2(x - 15.5f, y - 15.5f).magnitude;
            pixels[y * 32 + x] = radius >= 11.5f && radius <= 15.5f ? new Color32(255,255,255,255) : new Color32(0,0,0,0);
        }
        texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.spritePixelsPerUnit = 32;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    [MenuItem("Count To Zero/Upgrade Enemy Facing and Death FX")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.Refresh();
        var frames = PresentationUpgrade.SliceWalkFrames("Assets/GameAssets/Sprites/man/8walk.png");
        var ring = Ring();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CountToZero.unity");
        var g = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        var minions = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MinionController>(true)).ToArray();
        foreach (var minion in minions)
        {
            var visual = (SpriteRenderer)new SerializedObject(minion).FindProperty("sprite").objectReferenceValue;
            var animator = minion.GetComponent<HeroAnimator>(); if (animator == null) animator = minion.gameObject.AddComponent<HeroAnimator>();
            Wire(animator, "visual", visual); Wire(animator, "facingTarget", g.Player.transform);
            Frames(animator, "frontFrames", frames.Where(f => f.name.StartsWith("Front")).ToArray());
            Frames(animator, "backFrames", frames.Where(f => f.name.StartsWith("Back")).ToArray());
            visual.sprite = frames.First(f => f.name.StartsWith("Front")); visual.color = Color.white; visual.transform.localScale = Vector3.one * .85f;
            string fxName = minion.name + " Death Ring";
            var existing = minion.transform.parent.Find(fxName);
            var fx = existing != null ? existing.GetComponent<PixelDeathRing>() : new GameObject(fxName, typeof(SpriteRenderer), typeof(PixelDeathRing)).GetComponent<PixelDeathRing>();
            fx.transform.SetParent(minion.transform.parent, false);
            var renderer = fx.GetComponent<SpriteRenderer>(); renderer.sprite = ring; renderer.color = Color.white; renderer.sortingOrder = 30;
            Wire(fx, "visual", renderer); Wire(minion, "deathRing", fx); fx.gameObject.SetActive(false);
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        File.WriteAllText("Documentation/enemy-presentation.txt", "Authored " + minions.Length + " enemies with 8walk column 1/2 animations and independent pooled pixel death rings.\n");
    }
}
