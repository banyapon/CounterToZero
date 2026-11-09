using System;
using System.IO;
using System.Linq;
using CountToZero;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Wires the virus-cyborg serpent boss form and the player's control ring into the arena scene.
/// Run from the menu, or drop the request file to run it once the editor is idle.
[InitializeOnLoad]
public static class BossSerpentUpgrade
{
    const string Request = "Temp/ctz-boss-serpent.request";
    const string Report = "Documentation/boss-serpent.txt";
    const string Scene = "Assets/Scenes/CountToZero.unity";
    const string Boss = "Assets/GameAssets/Sprites/boss/";
    const string Ring = "Assets/GameAssets/Sprites/player_ring.png";

    static BossSerpentUpgrade()
    {
        EditorApplication.update += () =>
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying) return;
            File.Delete(Request);
            try { Run(); }
            catch (Exception e) { File.WriteAllText(Report, "FAIL\n" + e); Debug.LogException(e); }
        };
    }

    [MenuItem("Count To Zero/Author Boss Forms + Control Ring + Fireballs")]
    public static void Run()
    {
        foreach (var path in new[] { "boss_head_64", "boss_head_100", "boss_head_150", "boss_body_64", "boss_body_100", "boss_body_150" }.Select(n => Boss + n + ".png").Append(Ring))
            PixelSprite(path);
        PixelSprite(Boss + "boss_skull.png"); // 64 px = 1 unit, a drop-in for Omega.png
        foreach (var n in GiantArt.Concat(Fireballs).Concat(Shocks)) PixelSprite(Boss + n + ".png", 16); // hero pixel size

        Directory.CreateDirectory("Backup/BossSerpent");
        File.Copy(Scene, "Backup/BossSerpent/CountToZero-before-serpent.unity", true);
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) throw new OperationCanceledException("Unsaved scenes kept open; run again.");
        string previous = EditorSceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(Scene);

        var boss = UnityEngine.Object.FindAnyObjectByType<BossController>(FindObjectsInactive.Include);
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (boss == null || player == null) throw new InvalidOperationException("Boss or player missing from " + Scene);

        var omega = new SerializedObject(boss).FindProperty("sprite").objectReferenceValue as SpriteRenderer;
        if (omega == null) throw new InvalidOperationException("Boss sprite not wired");
        var headTransform = boss.transform.Find("Serpent head");
        if (headTransform == null)
        {
            headTransform = new GameObject("Serpent head").transform;
            headTransform.SetParent(boss.transform, false);
            headTransform.SetAsLastSibling();
        }
        headTransform.localPosition = omega.transform.localPosition;
        var head = Ensure<SpriteRenderer>(headTransform.gameObject);
        head.sprite = Load(Boss + "boss_head_100.png"); head.sortingOrder = 3; head.enabled = false;
        if (omega.sharedMaterial != null) head.sharedMaterial = omega.sharedMaterial;

        var form = Ensure<BossForm>(boss.gameObject);
        var so = new SerializedObject(form);
        so.FindProperty("boss").objectReferenceValue = boss;
        so.FindProperty("omega").objectReferenceValue = omega;
        so.FindProperty("head").objectReferenceValue = head;
        var heads = so.FindProperty("heads"); heads.arraySize = 3;
        heads.GetArrayElementAtIndex(0).objectReferenceValue = Load(Boss + "boss_head_100.png");
        heads.GetArrayElementAtIndex(1).objectReferenceValue = Load(Boss + "boss_head_150.png");
        heads.GetArrayElementAtIndex(2).objectReferenceValue = Load(Boss + "boss_head_64.png");
        so.FindProperty("body").objectReferenceValue = Load(Boss + "boss_body_100.png");
        so.FindProperty("skull").objectReferenceValue = Load(Boss + "boss_skull.png");
        var giantTransform = boss.transform.Find("Giant body");
        if (giantTransform == null)
        {
            giantTransform = new GameObject("Giant body").transform;
            giantTransform.SetParent(boss.transform, false);
            giantTransform.SetAsLastSibling();
        }
        giantTransform.localPosition = omega.transform.localPosition + Vector3.up * .35f;
        var giant = Ensure<SpriteRenderer>(giantTransform.gameObject);
        giant.sprite = Load(Boss + "giant_flail_body.png"); giant.sortingOrder = 3; giant.enabled = false;
        if (omega.sharedMaterial != null) giant.sharedMaterial = omega.sharedMaterial;
        so.FindProperty("giant").objectReferenceValue = giant;
        foreach (var (field, file) in new[] { ("flailBody", "giant_flail_body"), ("stormBody", "giant_storm_body"), ("link", "chain_link"), ("stormLink", "chain_link_storm"), ("mace", "chain_mace"), ("prong", "whip_prong") })
            so.FindProperty(field).objectReferenceValue = Load(Boss + file + ".png");
        SetArray(so.FindProperty("shock"), Shocks);
        var laserMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/GameAssets/Materials/CircuitRedLaser.mat");
        if (laserMaterial == null) throw new FileNotFoundException("CircuitRedLaser.mat");
        so.FindProperty("laserMaterial").objectReferenceValue = laserMaterial;
        // Minion robot sheets blown up x4 as boss forms.
        SetFrames(so.FindProperty("walkFronts"), Man + "8walk.png", "Front"); SetFrames(so.FindProperty("walkBacks"), Man + "8walk.png", "Back");
        SetFrames(so.FindProperty("heavyFronts"), Man + "8walk_heavy.png", "Front"); SetFrames(so.FindProperty("heavyBacks"), Man + "8walk_heavy.png", "Back");
        SetFrames(so.FindProperty("scoutFronts"), Man + "8walk_scout.png", "Front"); SetFrames(so.FindProperty("scoutBacks"), Man + "8walk_scout.png", "Back"); // storm giant x2
        so.ApplyModifiedPropertiesWithoutUndo();

        var heroSprite = new SerializedObject(player).FindProperty("sprite").objectReferenceValue as SpriteRenderer;
        var ringTransform = player.transform.Find("Control Ring");
        if (ringTransform == null)
        {
            ringTransform = new GameObject("Control Ring").transform;
            ringTransform.SetParent(player.transform, false);
        }
        ringTransform.SetAsLastSibling(); // keep the hero sprite first for GetComponentInChildren lookups
        ringTransform.localPosition = new Vector3(0, .04f, 0); ringTransform.rotation = Quaternion.Euler(90, 0, 0);
        var ring = Ensure<SpriteRenderer>(ringTransform.gameObject);
        ring.sprite = Load(Ring); ring.color = ArenaVisuals.Cyan; ring.sortingOrder = -1;
        var control = Ensure<ControlRing>(player.gameObject);
        var cso = new SerializedObject(control);
        cso.FindProperty("ring").objectReferenceValue = ring;
        cso.FindProperty("hero").objectReferenceValue = heroSprite;
        cso.ApplyModifiedPropertiesWithoutUndo();

        // Assistant portraits: right-edge card stack on the HUD canvas.
        foreach (var n in new[] { "portrait_twin_green", "portrait_twin_blond" }) UiSprite("Assets/GameAssets/UI/" + n + ".png");
        var game = UnityEngine.Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        var battle = game.GetComponent<BattleExpansion>();
        var notice = new SerializedObject(battle).FindProperty("lossNotice").objectReferenceValue as TMPro.TMP_Text;
        var hudCanvas = notice != null ? notice.canvas.rootCanvas : UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c => c.renderMode != RenderMode.WorldSpace);
        var stack = hudCanvas.transform.Find("Assistant Portraits") as RectTransform;
        if (stack == null) { stack = new GameObject("Assistant Portraits", typeof(RectTransform)).GetComponent<RectTransform>(); stack.SetParent(hudCanvas.transform, false); }
        stack.anchorMin = Vector2.zero; stack.anchorMax = Vector2.one; stack.offsetMin = stack.offsetMax = Vector2.zero; stack.SetAsLastSibling();
        var cards = Ensure<AssistantPortraits>(game.gameObject);
        var pso = new SerializedObject(cards);
        var pa = pso.FindProperty("portraits"); pa.arraySize = 2;
        pa.GetArrayElementAtIndex(0).objectReferenceValue = Load("Assets/GameAssets/UI/portrait_twin_green.png"); // style 0 = assistant_green
        pa.GetArrayElementAtIndex(1).objectReferenceValue = Load("Assets/GameAssets/UI/portrait_twin_blond.png"); // style 1 = assistant_gold
        pso.FindProperty("root").objectReferenceValue = stack;
        if (notice != null) pso.FindProperty("font").objectReferenceValue = notice.font;
        pso.ApplyModifiedPropertiesWithoutUndo();

        int fires = 0;
        foreach (var obstacle in UnityEngine.Object.FindObjectsByType<ArenaObstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var fireTransform = obstacle.transform.Find("Fireball");
            if (fireTransform == null) { fireTransform = new GameObject("Fireball").transform; fireTransform.SetParent(obstacle.transform, false); }
            fireTransform.localScale = Vector3.one * 1.1f; fireTransform.localPosition = Vector3.up * 1f;
            var fire = Ensure<SpriteRenderer>(fireTransform.gameObject);
            fire.sprite = Load(Boss + "fireball_0.png"); fire.sortingOrder = 1;
            fireTransform.gameObject.SetActive(false);
            var oso = new SerializedObject(obstacle);
            oso.FindProperty("fire").objectReferenceValue = fire;
            SetArray(oso.FindProperty("fireFrames"), Fireballs);
            oso.ApplyModifiedPropertiesWithoutUndo();
            fires++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        string summary = "PASS: boss forms (omega, serpent, skull, flail giant, storm giant, walker x4, heavy walker x4; armored Omega 8-way laser, assistant portraits, robot giants x2) on " + boss.name + ", control ring on " + player.name + ", fireball variant on " + fires + " obstacles.\n";
        if (!string.IsNullOrEmpty(previous) && previous != Scene) EditorSceneManager.OpenScene(previous);
        File.WriteAllText(Report, summary);
    }

    static readonly string[] GiantArt = { "giant_flail_body", "giant_storm_body", "chain_link", "chain_link_storm", "chain_mace", "whip_prong" };
    static readonly string[] Fireballs = { "fireball_0", "fireball_1", "fireball_2", "fireball_3" };
    static readonly string[] Shocks = { "shock_0", "shock_1", "shock_2" };
    const string Man = "Assets/GameAssets/Sprites/man/";
    static void SetFrames(SerializedProperty array, string sheet, string prefix)
    {
        var frames = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().Where(s => s.name.StartsWith(prefix)).OrderBy(s => s.name).ToArray();
        if (frames.Length == 0) throw new FileNotFoundException(prefix + " frames in " + sheet);
        array.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
    }
    static void SetArray(SerializedProperty array, string[] files)
    {
        array.arraySize = files.Length;
        for (int i = 0; i < files.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = Load(Boss + files[i] + ".png");
    }
    static T Ensure<T>(GameObject go) where T : Component { var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>(); }
    static Sprite Load(string path) { var s = AssetDatabase.LoadAssetAtPath<Sprite>(path); if (s == null) throw new FileNotFoundException(path); return s; }

    static void UiSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter; if (importer == null) throw new FileNotFoundException(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
    }
    static void PixelSprite(string path, int pixelsPerUnit = 0)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter; if (importer == null) throw new FileNotFoundException(path);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit > 0 ? pixelsPerUnit : texture != null ? texture.width : 100; // default: 1 world unit, scaled in code
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
