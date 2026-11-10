using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CountToZero;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class LeaderboardUpgrade
{
    const string Skin = "Assets/PixelUISciFiFree/Sprites/themes/cyan/";
    const string Request = "Temp/ctz-ranks-rebuild.request";
    static readonly Color Cyan = new Color(.25f, .94f, .95f);
    static readonly Color Gold = new Color(1f, .72f, .3f);
    static readonly Color Muted = new Color(.52f, .69f, .76f);

    static LeaderboardUpgrade()
    {
        EditorApplication.update += () =>
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try { Run(); }
            catch (Exception exception)
            {
                File.WriteAllText("Documentation/leaderboard.txt", exception.ToString());
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("Count To Zero/Author Leaderboard")]
    public static void Run()
    {
        Directory.CreateDirectory("Backup/Leaderboard");
        const string backup = "Backup/Leaderboard/Title-before-ranks.unity";
        if (!File.Exists(backup)) File.Copy("Assets/Scenes/Title.unity", backup, false);
        File.Copy("Assets/Scenes/Title.unity", "Backup/Leaderboard/Title-before-rank-rebuild.unity", true);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        var view = UnityEngine.Object.FindAnyObjectByType<TitleMenuView>();
        var menu = GameObject.Find("Title Menu");
        if (view == null || menu == null) throw new InvalidOperationException("Title menu is missing.");
        Author(menu.transform, view);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        int buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Length;
        File.WriteAllText("Documentation/leaderboard.txt", "PASS: ranks page rebuilt as a 10-row table. Buttons in the scene: " + buttons + ".\nLeaderboard config: Assets/GameAssets/Cloud/count_to_zero_best.lb\nThe cloud returned LeaderboardNotFound until this board is deployed.\nDeploy: running\n");
        DeployBoard();
    }

    public static void Author(Transform menu, TitleMenuView view)
    {
        var actions = menu.Find("Home/Main Actions");
        if (actions == null) throw new InvalidOperationException("Main Actions is missing.");
        Place(actions.Find("New Game Button"), 120f);
        Place(actions.Find("Continue Button"), 54f);
        Place(actions.Find("Options Button"), -78f);
        Place(actions.Find("Credits Button"), -144f);
        if (actions.Find("Ranks Button") == null)
        {
            var created = Button("Ranks Button", actions, "RANKS", 0f, -12f, 208f, 56f, Cyan, view.ShowRanks);
            created.GetComponentInChildren<TMP_Text>().fontSize = 28f;
        }
        else Place(actions.Find("Ranks Button"), -12f);

        var previous = menu.Find("Ranks");
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

        var page = Page("Ranks", menu, "CLOUD RANKS", "TOP 10 BEST SCORES. UNITY ADDS #0000 TO A CALLSIGN.");
        var columns = Rect("Columns", page, 0f, 184f, 1000f, 26f);
        Label("Rank Header", columns, "RANK", -400f, 0f, 140f, 26f, 18f, Cyan, TextAlignmentOptions.MidlineLeft);
        Label("Name Header", columns, "CALLSIGN", -40f, 0f, 520f, 26f, 18f, Cyan, TextAlignmentOptions.MidlineLeft);
        Label("Score Header", columns, "SCORE", 390f, 0f, 180f, 26f, 18f, Cyan, TextAlignmentOptions.MidlineRight);
        var list = MakeImage("List", page, 0f, 16f, 1040f, 280f, Color.white, "panel_inset");
        var rowRank = new TMP_Text[10];
        var rowName = new TMP_Text[10];
        var rowScore = new TMP_Text[10];
        for (int i = 0; i < 10; i++) Row(list.transform, i, 117f - i * 26f, out rowRank[i], out rowName[i], out rowScore[i]);
        var empty = Text("Empty", page, "WAITING FOR UNITY CLOUD", 0f, 16f, 920f, 140f, 30f, Cyan);
        empty.overflowMode = TextOverflowModes.Overflow;
        MakeImage("You Strip", page, 0f, -162f, 1040f, 56f, Color.white, "panel_inset");
        Text("You Label", page, "YOU", -450f, -162f, 90f, 40f, 22f, Cyan);
        var youRank = Label("You Rank", page, "#--", -330f, -162f, 130f, 44f, 28f, Color.white, TextAlignmentOptions.MidlineLeft);
        var youScore = Label("You Score", page, "BEST  0", -90f, -162f, 250f, 44f, 26f, Gold, TextAlignmentOptions.MidlineLeft);
        Text("Callsign Label", page, "CALLSIGN", 175f, -162f, 160f, 40f, 18f, Muted);
        var field = CallsignField(page, 400f, -162f, 220f, 44f);
        var status = Text("Status", page, "CLOUD RANK OFFLINE", 0f, -276f, 1000f, 28f, 18f, Muted);
        var refresh = Button("Refresh Button", page, "REFRESH", -180f, -228f, 240f, 52f, Gold, null);
        Button("Back Button", page, "BACK", 180f, -228f, 240f, 52f, Color.white, view.ReturnHome);
        var panel = page.gameObject.AddComponent<LeaderboardPanel>();
        Wire(panel, "youRank", youRank);
        Wire(panel, "youScore", youScore);
        Wire(panel, "status", status);
        Wire(panel, "empty", empty);
        WireArray(panel, "rowRank", rowRank);
        WireArray(panel, "rowName", rowName);
        WireArray(panel, "rowScore", rowScore);
        Wire(panel, "callsign", field);
        Wire(panel, "refresh", refresh);
        Wire(view, "ranks", page.gameObject);
        Wire(view, "ranksFocus", refresh);
        page.SetAsLastSibling();
        page.gameObject.SetActive(false);
    }

    static void Row(Transform list, int index, float y, out TMP_Text rank, out TMP_Text name, out TMP_Text score)
    {
        var root = Rect("Row " + index.ToString("00"), list, 0f, y, 1000f, 26f);
        if ((index & 1) == 0)
        {
            var stripe = root.gameObject.AddComponent<Image>();
            stripe.color = new Color(.12f, .28f, .34f, .45f);
            stripe.raycastTarget = false;
        }
        rank = Label("Rank", root, "", -400f, 0f, 140f, 26f, 22f, Cyan, TextAlignmentOptions.MidlineLeft);
        name = Label("Name", root, "", -40f, 0f, 520f, 26f, 22f, Color.white, TextAlignmentOptions.MidlineLeft);
        score = Label("Score", root, "", 390f, 0f, 180f, 26f, 22f, Gold, TextAlignmentOptions.MidlineRight);
        root.gameObject.SetActive(false);
    }

    static void Place(Transform target, float y)
    {
        if (target == null) throw new InvalidOperationException("A title button is missing.");
        var rect = (RectTransform)target;
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(208f, 56f);
    }

    static void Wire(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null) throw new MissingMemberException(target.GetType().Name, field);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void WireArray(UnityEngine.Object target, string field, TMP_Text[] values)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null || !property.isArray) throw new MissingMemberException(target.GetType().Name, field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h) => CampaignExpansionUI.Rect(name, parent, new Vector2(x, y), new Vector2(w, h));

    static Image MakeImage(string name, Transform parent, float x, float y, float w, float h, Color color, string sprite = "panel")
    {
        var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Skin + sprite + ".png");
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text Text(string name, Transform parent, string text, float x, float y, float w, float h, float size, Color color, bool title = false)
    {
        var label = Rect(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = title ? TypographyUpgrade.TitleFont : TypographyUpgrade.BodyFont;
        label.fontStyle = FontStyles.Bold;
        label.text = text;
        label.fontSize = size;
        label.enableAutoSizing = false;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    static TMP_Text Label(string name, Transform parent, string text, float x, float y, float w, float h, float size, Color color, TextAlignmentOptions alignment)
    {
        var label = Text(name, parent, text, x, y, w, h, size, color);
        label.alignment = alignment;
        return label;
    }

    static void Style(Button button, string label, Color accent)
    {
        var image = (Image)button.targetGraphic;
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Skin + "button_normal.png");
        image.type = Image.Type.Sliced;
        image.color = Color.Lerp(Color.white, accent, .4f);
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Skin + "button_hover.png"),
            selectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Skin + "button_hover.png"),
            pressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Skin + "button_pressed.png"),
            disabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Skin + "button_disabled.png")
        };
        button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        var text = button.GetComponentInChildren<TMP_Text>();
        if (text == null) text = (TMP_Text)Text("Label", button.transform, label, 0f, 0f, 300f, 60f, 28f, Color.white);
        text.font = TypographyUpgrade.BodyFont;
        text.fontStyle = FontStyles.Bold;
        text.text = label;
        text.fontSize = 28f;
        text.enableAutoSizing = false;
        text.color = Color.Lerp(Color.white, accent, .25f);
        text.alignment = TextAlignmentOptions.Center;
        var rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12f, 4f);
        rect.offsetMax = new Vector2(-12f, -4f);
        text.raycastTarget = false;
        var outline = button.GetComponent<Outline>();
        if (outline == null) outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = accent;
        outline.effectDistance = Vector2.one;
        var feedback = button.GetComponent<UIButtonFeedback>();
        if (feedback == null) feedback = button.gameObject.AddComponent<UIButtonFeedback>();
        var serialized = new SerializedObject(feedback);
        serialized.FindProperty("accent").colorValue = accent;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static Button Button(string name, Transform parent, string label, float x, float y, float w, float h, Color color, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(name, parent, x, y, w, h);
        var image = rect.gameObject.AddComponent<Image>();
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Style(button, label, color);
        if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }

    static RectTransform Page(string name, Transform parent, string title, string subtitle)
    {
        var rect = Rect(name, parent, 0f, 0f, 1160f, 650f);
        MakeImage("Panel", rect, 0f, 0f, 1160f, 650f, Color.white, "window");
        MakeImage("Dark Backing", rect, 0f, 0f, 1134f, 624f, new Color(.015f, .028f, .05f), "panel");
        Text("Heading", rect, title, 0f, 278f, 1000f, 60f, 44f, Color.white, true);
        Text("Subtitle", rect, subtitle, 0f, 222f, 1000f, 30f, 20f, Muted);
        return rect;
    }

    static TMP_InputField CallsignField(Transform parent, float x, float y, float w, float h)
    {
        var background = MakeImage("Callsign Input", parent, x, y, w, h, Color.white, "panel_inset");
        background.raycastTarget = true;
        var text = StretchText("Text", background.transform, "", 22f, Color.white);
        var placeholder = StretchText("Placeholder", background.transform, "PILOT", 22f, Muted);
        var field = background.gameObject.AddComponent<TMP_InputField>();
        field.textViewport = background.rectTransform;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.characterLimit = 16;
        field.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        field.pointSize = 22f;
        field.caretColor = Cyan;
        field.selectionColor = new Color(.25f, .94f, .95f, .35f);
        return field;
    }

    static TMP_Text StretchText(string name, Transform parent, string value, float size, Color color)
    {
        var label = Text(name, parent, value, 0f, 0f, 420f, 46f, size, color);
        var rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 4f);
        rect.offsetMax = new Vector2(-16f, -4f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return label;
    }

    static async void DeployBoard()
    {
        try
        {
            File.AppendAllText("Documentation/leaderboard.txt", await DeployBoardAsync() + "\n");
        }
        catch (Exception exception)
        {
            File.AppendAllText("Documentation/leaderboard.txt", "Deploy failed: " + exception.GetType().Name + " " + exception.Message + "\n");
            Debug.LogWarning("Leaderboard deploy: " + exception.Message);
        }
    }

    static async Task<string> DeployBoardAsync()
    {
        var deployments = Type.GetType("Unity.Services.DeploymentApi.Editor.Deployments, Unity.Services.DeploymentApi");
        if (deployments == null) return "Deploy skipped: deployment assembly is not loaded. Use Services > Deployment.";
        var instance = deployments.GetProperty("Instance").GetValue(null);
        var providers = deployments.GetProperty("DeploymentProviders").GetValue(instance) as IEnumerable;
        if (providers == null) return "Deploy skipped: no deployment providers.";
        object item = null;
        object owner = null;
        foreach (var provider in providers)
        {
            var items = provider.GetType().GetProperty("DeploymentItems")?.GetValue(provider) as IEnumerable;
            if (items == null) continue;
            foreach (var candidate in items)
            {
                var path = (candidate.GetType().GetProperty("Path")?.GetValue(candidate) as string ?? "").Replace('\\', '/');
                if (!path.EndsWith("count_to_zero_best.lb", StringComparison.OrdinalIgnoreCase)) continue;
                item = candidate;
                owner = provider;
                break;
            }
            if (item != null) break;
        }
        if (item == null || owner == null) return "Deploy skipped: count_to_zero_best.lb is not in Services > Deployment yet.";
        if (string.IsNullOrEmpty(CloudProjectSettings.accessToken)) return "Deploy skipped: sign in through Unity Hub, then use Services > Deployment.";
        var command = owner.GetType().GetProperty("DeployCommand")?.GetValue(owner);
        if (command == null) return "Deploy skipped: Leaderboards has no deploy command.";
        var array = Array.CreateInstance(item.GetType(), 1);
        array.SetValue(item, 0);
        MethodInfo execute = null;
        foreach (var method in command.GetType().GetMethods())
        {
            if (method.Name != "ExecuteAsync") continue;
            var parameters = method.GetParameters();
            if (parameters.Length == 2 && parameters[0].ParameterType.IsAssignableFrom(array.GetType())) execute = method;
        }
        if (execute == null) return "Deploy skipped: ExecuteAsync was not found.";
        var task = execute.Invoke(command, new object[] { array, CancellationToken.None }) as Task;
        if (task == null) return "Deploy failed: ExecuteAsync did not return a task.";
        await task;
        var status = item.GetType().GetProperty("Status")?.GetValue(item);
        var severity = status?.GetType().GetProperty("MessageSeverity")?.GetValue(status);
        var message = status?.GetType().GetProperty("Message")?.GetValue(status) as string;
        return "Deploy result: " + severity + (string.IsNullOrEmpty(message) ? "" : " " + message);
    }
}
