using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChiselPeak
{
    /// Portrait HUD: freed bar on top, ice count, tool, craft prompt, banners, win card.
    public class Hud : MonoBehaviour
    {
        TMP_FontAsset font; Material outline;
        RectTransform barFill, bannerRt, promptRt, winRt;
        TextMeshProUGUI pct, ice, tool, next, prompt, banner, winTitle, winStats, winHint;
        float bannerT = 99f, iceBump;
        int shownIce = -1;

        static readonly Color Navy = new Color(0.07f, 0.12f, 0.3f, 0.6f);
        static readonly Color Ember = new Color(1f, 0.55f, 0.16f);

        public void Build(TMP_FontAsset f, Material outlineMat)
        {
            font = f; outline = outlineMat;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)transform;

            // freed bar
            var title = Text(root, "FREE KINDLE", 74, new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(900, 100), TextAlignmentOptions.Center);
            title.color = Color.white;
            var bar = Box(root, Navy, new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(840, 76));
            var fillGo = Box(bar, Ember, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            barFill = fillGo;
            barFill.anchorMin = new Vector2(0, 0); barFill.anchorMax = new Vector2(0, 1);
            barFill.pivot = new Vector2(0, 0.5f);
            barFill.offsetMin = new Vector2(6, 6); barFill.offsetMax = new Vector2(6, -6);
            pct = Text(bar, "0%", 54, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(820, 76), TextAlignmentOptions.Center);

            // ice count
            var iconRt = Box(root, new Color(0.7f, 0.92f, 1f), new Vector2(0, 1), new Vector2(100, -300), new Vector2(48, 48));
            iconRt.localEulerAngles = new Vector3(0, 0, 45);
            ice = Text(root, "ICE 0", 70, new Vector2(0, 1), new Vector2(345, -300), new Vector2(380, 90), TextAlignmentOptions.Left);

            // tool
            tool = Text(root, "STONE HAMMER", 60, new Vector2(0, 0), new Vector2(370, 290), new Vector2(640, 90), TextAlignmentOptions.Left);
            next = Text(root, "NEXT: PICKAXE 3 ICE", 46, new Vector2(0, 0), new Vector2(370, 225), new Vector2(640, 70), TextAlignmentOptions.Left);
            next.color = new Color(0.85f, 0.93f, 1f);

            // crosshair
            Box(root, new Color(1, 1, 1, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12, 12));

            // craft prompt
            promptRt = Box(root, new Color(1f, 0.55f, 0.16f, 0.92f), new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(720, 116));
            prompt = Text(promptRt, "E  CRAFT PICKAXE", 62, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(700, 116), TextAlignmentOptions.Center);
            promptRt.gameObject.SetActive(false);

            // banner
            banner = Text(root, "PICKAXE!", 140, new Vector2(0.5f, 0.5f), new Vector2(0, 360), new Vector2(1000, 160), TextAlignmentOptions.Center);
            banner.color = new Color(1f, 0.86f, 0.3f);
            bannerRt = banner.rectTransform;
            banner.gameObject.SetActive(false);

            // win card
            winRt = Box(root, new Color(0.07f, 0.12f, 0.3f, 0.0f), new Vector2(0.5f, 0.5f), new Vector2(0, -430), new Vector2(1000, 420));
            winTitle = Text(winRt, "KINDLE IS FREE!", 118, new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(1000, 160), TextAlignmentOptions.Center);
            winTitle.color = new Color(1f, 0.86f, 0.3f);
            winStats = Text(winRt, "TIME 0:00   SWINGS 0", 54, new Vector2(0.5f, 1), new Vector2(0, -210), new Vector2(1000, 80), TextAlignmentOptions.Center);
            winHint = Text(winRt, "R  DIG AGAIN", 52, new Vector2(0.5f, 1), new Vector2(0, -290), new Vector2(1000, 70), TextAlignmentOptions.Center);
            winHint.color = new Color(0.85f, 0.93f, 1f);
            winRt.gameObject.SetActive(false);
        }

        RectTransform Box(RectTransform parent, Color c, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Box", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>(); img.color = c; img.raycastTarget = false;
            return rt;
        }

        TextMeshProUGUI Text(RectTransform parent, string s, float size, Vector2 anchor, Vector2 pos, Vector2 box, TextAlignmentOptions align)
        {
            var go = new GameObject("Text " + s, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = box;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = font;
            if (outline != null) t.fontSharedMaterial = outline;
            t.fontSize = size; t.alignment = align; t.text = s;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.color = Color.white;
            return t;
        }

        public void SetFreed(float f)
        {
            float w = ((RectTransform)barFill.parent).rect.width - 12f;
            barFill.sizeDelta = new Vector2(Mathf.Max(0, w * Mathf.Clamp01(f)), barFill.sizeDelta.y);
            pct.text = Mathf.FloorToInt(f * 100f) + "%";
        }

        public void SetIce(int n)
        {
            if (n > shownIce && shownIce >= 0) iceBump = 1f;
            shownIce = n;
            ice.text = "ICE " + n;
        }

        public void SetTool(ToolDef t, ToolDef nextTool)
        {
            tool.text = t.Name.ToUpperInvariant();
            next.text = nextTool != null ? "NEXT: " + nextTool.Name.ToUpperInvariant() + "  " + nextTool.Cost + " ICE" : "BEST TOOL";
        }

        public void SetPrompt(string s)
        {
            bool on = !string.IsNullOrEmpty(s);
            promptRt.gameObject.SetActive(on);
            if (on) prompt.text = s;
        }

        public RectTransform PromptRect => promptRt;
        public bool WinShown => winRt.gameObject.activeSelf;
        public void SetWinHint(string s) => winHint.text = s;

        public void Banner(string s)
        {
            banner.text = s; banner.gameObject.SetActive(true); bannerT = 0f;
        }

        public void ShowWin(float seconds, int swings)
        {
            int m = Mathf.FloorToInt(seconds / 60f), s = Mathf.FloorToInt(seconds % 60f);
            winStats.text = $"TIME {m}:{s:00}   SWINGS {swings}";
            winRt.gameObject.SetActive(true);
            winRt.localScale = Vector3.one * 0.2f;
            promptRt.gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (banner.gameObject.activeSelf)
            {
                bannerT += dt;
                float pop = bannerT < 0.18f ? Mathf.Lerp(0.3f, 1.15f, bannerT / 0.18f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((bannerT - 0.18f) / 0.15f));
                bannerRt.localScale = Vector3.one * pop;
                var c = banner.color; c.a = 1f - Mathf.Clamp01((bannerT - 1.3f) / 0.3f); banner.color = c;
                if (bannerT > 1.6f) banner.gameObject.SetActive(false);
            }
            if (promptRt.gameObject.activeSelf)
                promptRt.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 7f) * 0.04f);
            if (winRt.gameObject.activeSelf)
                winRt.localScale = Vector3.one * Mathf.MoveTowards(winRt.localScale.x, 1f, dt * 5f);
            iceBump = Mathf.Max(0, iceBump - dt * 6f);
            ice.rectTransform.localScale = Vector3.one * (1f + iceBump * 0.15f);
        }
    }
}
