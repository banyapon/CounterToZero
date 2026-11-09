using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PresentationUpgrade
{
    const string Request = "Temp/ctz-presentation.request";
    const string Skin = "Assets/PixelUISciFiFree/Sprites/themes/cyan/";
    static PresentationUpgrade() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(Request);
        try { Run(); } catch (Exception e) { Debug.LogException(e); File.WriteAllText("Documentation/presentation-validation.txt", e.ToString()); }
    }
    static void Wire(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void WireArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        var so = new SerializedObject(target); var p = so.FindProperty(field); p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Skin + name + ".png");
    static T Add<T>(GameObject go) where T : Component { var component = go.GetComponent<T>(); return component != null ? component : go.AddComponent<T>(); }
    static void Theme(GameObject[] roots)
    {
        foreach (var image in roots.SelectMany(r => r.GetComponentsInChildren<Image>(true)))
        {
            if(image.name=="Glitch Action Pose")continue; // Character artwork keeps its source sprite.
            string name = image.name.ToLowerInvariant();
            string asset = image.type == Image.Type.Filled || name.Contains("fill") || name == "progress" ? "progress_fill"
                : image.GetComponent<Button>() != null ? "button_normal"
                : name.Contains("handle") ? "grabber"
                : name.Contains("background") || name.Contains("track") || name.Contains("hp bar") ? "progress_bg"
                : name.Contains("modal") || name.Contains("card") ? "window" : "panel";
            image.sprite = Sprite(asset);
            if (image.type != Image.Type.Filled) image.type = Image.Type.Sliced;
            image.color = new Color(1, 1, 1, image.color.a);
            if (image.TryGetComponent<Button>(out var button))
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState { highlightedSprite = Sprite("button_hover"), selectedSprite = Sprite("button_hover"), pressedSprite = Sprite("button_pressed"), disabledSprite = Sprite("button_disabled") };
                PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        }
        foreach (var scaler in roots.SelectMany(r => r.GetComponentsInChildren<CanvasScaler>(true)))
        {
            if (scaler.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace) continue;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 1;
        }
        // Retain the authored 800-high layout while using the full 16:9 canvas.
        foreach (var rect in roots.SelectMany(r => r.GetComponentsInChildren<RectTransform>(true)).Where(r => r.name == "Reference Layout"))
        { rect.localScale = Vector3.one * .9f; }
    }
    public static Sprite[] SliceWalkFrames(string path = "Assets/GameAssets/Sprites/man/1 walk.png")
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true; importer.spritePixelsPerUnit = 16;
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        var slices = new System.Collections.Generic.List<SpriteMetaData>();
        foreach (int col in new[] { 0, 1 }) for (int row = 0; row < 4; row++)
            slices.Add(new SpriteMetaData { name = (col == 0 ? "Front" : "Back" + (col + 1)) + "_" + row,
                rect = new Rect(col * width / 3f, (3 - row) * height / 4f, width / 3f, height / 4f), alignment = 0, pivot = new Vector2(.5f, .5f) });
#pragma warning disable 0618
        importer.spritesheet = slices.ToArray();
#pragma warning restore 0618
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }
    [MenuItem("Count To Zero/Upgrade Widescreen Presentation")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string original = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var frames = SliceWalkFrames();
        foreach (string name in new[] { "Title", "Preload", "Story", "CountToZero", "TurnEnd", "StoryEnd" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            var roots = scene.GetRootGameObjects(); Theme(roots);
            if (name == "CountToZero")
            {
                var g = UnityEngine.Object.FindAnyObjectByType<GameManager>();
                var playerSprite = new SerializedObject(g.Player).FindProperty("sprite").objectReferenceValue as SpriteRenderer;
                var bossSprite = new SerializedObject(g.Boss).FindProperty("sprite").objectReferenceValue as SpriteRenderer;
                var animator = Add<HeroAnimator>(g.Player.gameObject);
                Wire(animator, "visual", playerSprite);
                WireArray(animator, "backFrames", frames.Where(s => s.name.StartsWith("Back")).ToArray());
                WireArray(animator, "frontFrames", frames.Where(s => s.name.StartsWith("Front")).ToArray());
                playerSprite.sprite = frames.First(s => s.name.StartsWith("Back")); playerSprite.color = Color.white;
                playerSprite.transform.localScale = Vector3.one * .85f;
                var hud = UnityEngine.Object.FindAnyObjectByType<HUDController>();
                var bottom = hud.transform.Find("Reference Layout/Player HUD");
                foreach (string child in new[] { "HP Bar", "Integrity" }) if (bottom.Find(child) != null) bottom.Find(child).gameObject.SetActive(false);
                // Boss also has its single HP bar attached to the actor.
                var health = new SerializedObject(hud).FindProperty("healthFill").objectReferenceValue as Image;
                health.transform.parent.gameObject.SetActive(false);
                var bossHealth = new SerializedObject(hud).FindProperty("bossHealth").objectReferenceValue as TMPro.TMP_Text;
                if (bossHealth != null) bossHealth.gameObject.SetActive(false);
                var sequence = Add<VictorySequence>(g.gameObject);
                Wire(sequence, "hero", animator); Wire(sequence, "heroVisual", playerSprite); Wire(sequence, "bossVisual", bossSprite);
                WireArray(sequence, "interfaceGroups", roots.Where(r => r.name != "Victory Flash").SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Select(c => Add<CanvasGroup>(c.gameObject)).ToArray());
                var oldFx = GameObject.Find("Victory Flash"); if (oldFx != null) UnityEngine.Object.DestroyImmediate(oldFx);
                var fx = new GameObject("Victory Flash", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                fx.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; fx.GetComponent<Canvas>().sortingOrder = 100;
                var flash = new GameObject("White Impact", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                flash.transform.SetParent(fx.transform, false); flash.rectTransform.anchorMin = Vector2.zero; flash.rectTransform.anchorMax = Vector2.one; flash.rectTransform.sizeDelta = Vector2.zero; flash.raycastTarget = false;
                Wire(sequence, "whiteFlash", flash);
                foreach (string actor in new[] { "heroSilhouette", "bossSilhouette" }) { var silhouette = new GameObject(actor, typeof(RectTransform), typeof(Image)).GetComponent<Image>(); silhouette.transform.SetParent(flash.transform, false); silhouette.color = Color.black; silhouette.raycastTarget = false; Wire(sequence, actor, silhouette); } flash.gameObject.SetActive(false);
                var fragments = new SpriteRenderer[24];
                for (int i = 0; i < fragments.Length; i++) { var f = new GameObject("Boss Debris " + i).AddComponent<SpriteRenderer>(); f.transform.SetParent(fx.transform, false); f.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/Sprites/UIWhite.png"); f.sortingOrder = 20; f.gameObject.SetActive(false); fragments[i] = f; }
                WireArray(sequence, "fragments", fragments);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
        File.WriteAllText("Documentation/presentation-validation.txt", "Authored six 16:9 screens, PixelUISciFiFree images, 8 hero frames, world HP bars and victory sequence.\n");
    }
}
