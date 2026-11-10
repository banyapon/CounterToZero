using System;
using System.IO;
using System.Linq;
using TMPro;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PixelFontMigration
{
    static string oldPath;
    static float sizeScale;
    const string PixelPath = "Assets/PixelUISciFiFree/Font/Pixel UI Sci-Fi TMP.asset";
    static TMP_FontAsset oldFont, pixel;
    static int changed;

    [MenuItem("Count To Zero/Replace Iannnnn with Pixel Font")]
    public static void Run() => Migrate("Assets/Fonts/Body SDF.asset", PixelTypography.BodyScale);

    [MenuItem("Count To Zero/Replace ZF Title with Pixel Font")]
    public static void RunTitle() => Migrate("Assets/Fonts/Title SDF.asset", PixelTypography.TitleScale);

    static void Migrate(string pathToReplace, float scale)
    {
        oldPath = pathToReplace; sizeScale = scale;
        oldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(oldPath);
        pixel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelPath);
        if (pixel == null) throw new InvalidOperationException("Pixel font missing.");
        if (oldFont == null) { Debug.Log("Font already retired: " + oldPath); return; }
        changed = 0;
        foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            var scene = EditorSceneManager.OpenScene(entry.path);
            foreach (var root in scene.GetRootGameObjects()) Replace(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/GameAssets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try { Replace(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (font == null || font.fallbackFontAssetTable == null) continue;
            for (int i = 0; i < font.fallbackFontAssetTable.Count; i++)
                if (font.fallbackFontAssetTable[i] == oldFont) { font.fallbackFontAssetTable[i] = pixel; EditorUtility.SetDirty(font); }
        }
        AssetDatabase.SaveAssets();
        var paths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var dependencies = AssetDatabase.GetDependencies(paths, true);
        if (dependencies.Contains(oldPath) || dependencies.Any(p => p.Contains("2005_iannnnnAMD") || p.Contains("ZF#2ndPixelus")))
            throw new InvalidOperationException("Old font is still a build dependency.");
        File.AppendAllText("Documentation/pixel-font-migration.txt", "PASS: replaced " + changed + " text components from " + oldPath + "; size ratio=" + sizeScale + "; no retired font scene dependencies.\n");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
    }

    static void Replace(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.font != oldFont) continue;
            t.font = pixel; t.fontSharedMaterial = pixel.material;
            t.fontSize *= sizeScale;
            t.fontSizeMin *= sizeScale;
            t.fontSizeMax *= sizeScale;
            EditorUtility.SetDirty(t); changed++;
        }
        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null) continue;
            var so = new SerializedObject(component);
            var property = so.GetIterator();
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == oldFont)
                    property.objectReferenceValue = pixel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
