using TMPro;
using UnityEngine;

namespace CountToZero
{
    public static class SceneArtBake
    {
        public static readonly Color Cyan = new Color(0, .941f, 1);
        public static readonly Color Red = new Color(1, .165f, .329f);
        public static readonly Color Gold = new Color(1, .722f, 0);
        public static readonly Color Green = new Color(0, 1, .4f);
        public static readonly Color Violet = new Color(.65f, .35f, 1);
        static Sprite disc, player, boss;
        public static Sprite Disc => disc != null ? disc : disc = MakeSprite(0);
        public static Sprite Player => player != null ? player : player = MakeSprite(1);
        public static Sprite Boss => boss != null ? boss : boss = MakeSprite(2);
        static Sprite MakeSprite(int kind)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Generated neon sprite " + kind };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float px = (x - 31.5f) / 31.5f, py = (y - 31.5f) / 31.5f;
                float radius = Mathf.Sqrt(px * px + py * py);
                Color c = Color.clear;
                if (kind == 0 && radius < .94f) c = radius > .76f ? Color.white : new Color(.14f, .2f, .25f, .94f);
                if (kind == 1)
                {
                    if (Mathf.Abs(px) < .52f && py > -.65f && py < .15f) c = new Color(.22f,.45f,.5f);
                    if (Mathf.Abs(px) < .68f && py > -.05f && py < .7f) c = Color.white;
                    if (Mathf.Abs(px) < .48f && py > .2f && py < .42f) c = new Color(.04f,.08f,.14f);
                    if (Mathf.Abs(px) > .22f && Mathf.Abs(px) < .5f && py < -.45f && py > -.92f) c = Color.white;
                    if (Mathf.Abs(px) > .65f && Mathf.Abs(px) < .84f && py > -.45f && py < .05f) c = Color.white;
                }
                if (kind == 2)
                {
                    float diamond = Mathf.Abs(px) + Mathf.Abs(py);
                    if (diamond < 1.15f) c = diamond > .87f ? Color.white : new Color(.25f,.12f,.22f);
                    if (Mathf.Abs(py) < .16f && Mathf.Abs(px) < .63f) c = Color.white;
                    if (Mathf.Abs(px) < .13f && Mathf.Abs(py) < .11f) c = new Color(.08f,.04f,.1f);
                    if (Mathf.Abs(px) > .8f && Mathf.Abs(py) < .36f) c = Color.white;
                }
                pixels[y * size + x] = c;
            }
            texture.SetPixels(pixels); texture.Apply();
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f,.5f), size);
        }
        public static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Color color, float scale, Vector3 offset)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localPosition = offset; go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
            go.AddComponent<Billboard>(); return renderer;
        }
        public static TextMeshPro Label(Transform parent, float height, Color color, float size = .22f)
        {
            var go = new GameObject("World label"); go.transform.SetParent(parent, false); go.transform.localPosition = Vector3.up * height;
            go.AddComponent<Billboard>(); var text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center; text.fontSize = 5; text.font=TypographyUpgrade.BodyFont; text.fontStyle=FontStyles.Bold;
            text.fontSize = size*32; text.rectTransform.sizeDelta=new Vector2(5,1); text.color = color; return text;
        }
        public static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
            go.transform.position = position; go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); material.color = color;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
        public static void Build(Transform parent)
        {
            Box(parent, "Obsidian arena", new Vector3(0,-.35f,0), new Vector3(22,.6f,16), new Color(.025f,.045f,.075f));
            for (int i = -10; i <= 10; i += 2) Box(parent, "Circuit X", new Vector3(i,-.035f,0), new Vector3(.022f,.02f,16), new Color(.055f,.14f,.19f));
            for (int i = -8; i <= 8; i += 2) Box(parent, "Circuit Z", new Vector3(0,-.03f,i), new Vector3(22,.02f,.022f), new Color(.055f,.14f,.19f));
            for (int side = -1; side <= 1; side += 2)
            {
                Box(parent, "Cyan boundary", new Vector3(side*11,.1f,0), new Vector3(.12f,.3f,16), Cyan * .65f);
                Box(parent, "Cyan boundary", new Vector3(0,.1f,side*8), new Vector3(22,.3f,.12f), Cyan * .65f);
                for (int z = -6; z <= 6; z += 4)
                {
                    Box(parent, "Core tower", new Vector3(side*12,1,z), new Vector3(.8f,2,1.1f), new Color(.07f,.1f,.16f));
                    Box(parent, "Tower light", new Vector3(side*12,2.05f,z), new Vector3(.85f,.12f,1.15f), Violet * .7f);
                }
            }
        }
    }
}
