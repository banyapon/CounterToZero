using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CountToZero
{
    /// Right-edge stack of assistant portraits (one card per assistant on the field, with its code).
    /// The stack re-centres on the right edge as cards come and go; a dying card flashes
    /// grey / white / black / white and is then destroyed.
    public sealed class AssistantPortraits : MonoBehaviour
    {
        [SerializeField] Sprite[] portraits;          // by assistant style: 0 green, 1 blond
        [SerializeField] RectTransform root;          // full-screen rect on the HUD canvas
        [SerializeField] TMP_FontAsset font;
        [SerializeField] float portraitHeight = 112, margin = 16, gap = 10, labelHeight = 22;

        public sealed class Card
        {
            public RectTransform rect; public Image portrait, frame; public TMP_Text code;
            public float dyingAge = -1; public float y;
        }
        readonly List<Card> live = new(), dying = new();
        static readonly Color[] DeathFlash = { new Color(.55f, .55f, .55f), Color.white, Color.black, Color.white };

        public Card Add(int style, int number)
        {
            if (root == null || portraits == null || portraits.Length == 0) return null;
            var sprite = portraits[Mathf.Clamp(style, 0, portraits.Length - 1)];
            float width = portraitHeight * sprite.rect.width / sprite.rect.height;
            var card = new Card();
            card.rect = NewRect("Assistant Card " + number, root, new Vector2(width + 8, portraitHeight + labelHeight + 8));
            card.rect.anchorMin = card.rect.anchorMax = card.rect.pivot = new Vector2(1, .5f);
            card.frame = card.rect.gameObject.AddComponent<Image>();
            card.frame.color = new Color(.02f, .04f, .07f, .82f); card.frame.raycastTarget = false;
            var pr = NewRect("Portrait", card.rect, new Vector2(width, portraitHeight));
            pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(.5f, 1); pr.anchoredPosition = new Vector2(0, -4);
            card.portrait = pr.gameObject.AddComponent<Image>(); card.portrait.sprite = sprite; card.portrait.preserveAspect = true; card.portrait.raycastTarget = false;
            var lr = NewRect("Code", card.rect, new Vector2(width + 8, labelHeight));
            lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(.5f, 0); lr.anchoredPosition = new Vector2(0, 3);
            var code = lr.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) code.font = font;
            code.fontSize = 15; code.alignment = TextAlignmentOptions.Center; code.raycastTarget = false;
            code.color = style == 0 ? new Color(.45f, 1f, .55f) : new Color(1f, .85f, .35f);
            code.text = (style == 0 ? "AST-G" : "AST-B") + number.ToString("00");
            card.code = code;
            card.rect.anchoredPosition = new Vector2(card.rect.sizeDelta.x + margin, 0); // slides in from the right
            live.Add(card);
            return card;
        }

        public void SetHealth(Card card, float hp, float max)
        {
            if (card == null || card.dyingAge >= 0) return;
            card.frame.color = hp <= 1 ? new Color(.35f, .03f, .06f, .85f) : new Color(.02f, .04f, .07f, .82f);
        }

        public void Kill(Card card)
        {
            if (card == null || card.dyingAge >= 0) return;
            live.Remove(card); card.dyingAge = 0; dying.Add(card);
        }

        public void Clear()
        {
            foreach (var c in live) Destroy(c.rect.gameObject);
            foreach (var c in dying) Destroy(c.rect.gameObject);
            live.Clear(); dying.Clear();
        }

        void Update()
        {
            // Layout: live cards stacked and centred vertically on the right edge; height adapts to the count.
            if (live.Count > 0)
            {
                float cardH = live[0].rect.sizeDelta.y;
                float available = root.rect.height - 2 * margin - 120; // keep clear of the top HUD / bottom notice
                float scale = Mathf.Min(1, available / (live.Count * cardH + (live.Count - 1) * gap));
                float step = (cardH + gap) * scale;
                for (int i = 0; i < live.Count; i++)
                {
                    var c = live[i];
                    c.rect.localScale = Vector3.one * scale;
                    float targetY = ((live.Count - 1) * .5f - i) * step;
                    var p = c.rect.anchoredPosition;
                    p = Vector2.Lerp(p, new Vector2(-margin, targetY), Mathf.Min(1, Time.unscaledDeltaTime * 10));
                    c.rect.anchoredPosition = p;
                }
            }
            for (int i = dying.Count - 1; i >= 0; i--)
            {
                var c = dying[i];
                c.dyingAge += Time.unscaledDeltaTime;
                int f = Mathf.FloorToInt(c.dyingAge / .09f);
                if (f >= DeathFlash.Length * 2) { Destroy(c.rect.gameObject); dying.RemoveAt(i); continue; }
                var color = DeathFlash[f % DeathFlash.Length];
                c.portrait.color = color; c.code.color = color; c.code.text = "LOST";
                c.frame.color = new Color(color.r * .3f, color.g * .3f, color.b * .3f, .85f);
            }
        }

        static RectTransform NewRect(string name, Transform parent, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.sizeDelta = size; return r;
        }
    }
}
