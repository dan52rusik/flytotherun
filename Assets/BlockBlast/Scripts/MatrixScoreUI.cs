using UnityEngine;
using UnityEngine.UI;

public class MatrixScoreUI : MonoBehaviour
{
    private static MatrixScoreUI instance;

    public Text scoreValueText { get; private set; }

    public static MatrixScoreUI EnsureExists()
    {
        if (instance != null)
            return instance;

        GameObject uiObject = new GameObject("MatrixScoreUI");
        instance = uiObject.AddComponent<MatrixScoreUI>();
        instance.Build();
        return instance;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Build()
    {
        DontDestroyOnLoad(gameObject);

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform rootRect = gameObject.GetComponent<RectTransform>();
        if (rootRect == null)
            rootRect = gameObject.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Font font = ResolveFont();

        Image glow = CreateImage("Glow", transform, new Color(0.18f, 1f, 0.5f, 0.05f));
        RectTransform glowRect = glow.rectTransform;
        glowRect.anchorMin = new Vector2(0f, 1f);
        glowRect.anchorMax = new Vector2(0f, 1f);
        glowRect.pivot = new Vector2(0f, 1f);
        glowRect.anchoredPosition = new Vector2(24f, -24f);
        glowRect.sizeDelta = new Vector2(310f, 164f);

        Image panel = CreateImage("Panel", transform, new Color(0.03f, 0.07f, 0.06f, 0.86f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(32f, -30f);
        panelRect.sizeDelta = new Vector2(286f, 146f);

        Image border = CreateImage("Border", panel.transform, new Color(0.58f, 1f, 0.78f, 0.11f));
        Stretch(border.rectTransform);

        Image inner = CreateImage("Inner", panel.transform, new Color(0.05f, 0.12f, 0.1f, 0.42f));
        RectTransform innerRect = inner.rectTransform;
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(10f, 10f);
        innerRect.offsetMax = new Vector2(-10f, -10f);
        inner.transform.SetAsFirstSibling();

        Image topLine = CreateImage("TopLine", panel.transform, new Color(0.42f, 1f, 0.68f, 0.92f));
        RectTransform topLineRect = topLine.rectTransform;
        topLineRect.anchorMin = new Vector2(0f, 1f);
        topLineRect.anchorMax = new Vector2(1f, 1f);
        topLineRect.offsetMin = new Vector2(12f, -3f);
        topLineRect.offsetMax = new Vector2(-12f, 0f);

        Text tag = CreateText("Tag", panel.transform, font, 10, FontStyle.Bold);
        tag.text = "LIVE SCORE";
        tag.alignment = TextAnchor.MiddleLeft;
        tag.color = new Color(0.58f, 1f, 0.72f, 0.7f);
        RectTransform tagRect = tag.rectTransform;
        tagRect.anchorMin = new Vector2(0f, 1f);
        tagRect.anchorMax = new Vector2(1f, 1f);
        tagRect.offsetMin = new Vector2(18f, -28f);
        tagRect.offsetMax = new Vector2(-18f, -10f);

        Text title = CreateText("Title", panel.transform, font, 22, FontStyle.Bold);
        title.text = "SCORE";
        title.alignment = TextAnchor.MiddleLeft;
        title.color = new Color(0.9f, 1f, 0.94f, 0.98f);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(18f, -54f);
        titleRect.offsetMax = new Vector2(-18f, -26f);

        Text subtitle = CreateText("Subtitle", panel.transform, font, 11, FontStyle.Normal);
        subtitle.text = "current run";
        subtitle.alignment = TextAnchor.MiddleLeft;
        subtitle.color = new Color(0.44f, 0.92f, 0.6f, 0.62f);
        RectTransform subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.offsetMin = new Vector2(18f, -72f);
        subtitleRect.offsetMax = new Vector2(-18f, -54f);

        Image divider = CreateImage("Divider", panel.transform, new Color(0.35f, 1f, 0.58f, 0.1f));
        RectTransform dividerRect = divider.rectTransform;
        dividerRect.anchorMin = new Vector2(0f, 0.5f);
        dividerRect.anchorMax = new Vector2(1f, 0.5f);
        dividerRect.offsetMin = new Vector2(18f, 8f);
        dividerRect.offsetMax = new Vector2(-18f, 10f);

        scoreValueText = CreateText("Value", panel.transform, font, 46, FontStyle.Bold);
        scoreValueText.text = "0";
        scoreValueText.alignment = TextAnchor.MiddleLeft;
        scoreValueText.color = new Color(0.62f, 1f, 0.72f, 1f);
        RectTransform valueRect = scoreValueText.rectTransform;
        valueRect.anchorMin = new Vector2(0f, 0f);
        valueRect.anchorMax = new Vector2(1f, 0f);
        valueRect.offsetMin = new Vector2(18f, 18f);
        valueRect.offsetMax = new Vector2(-18f, 84f);
    }

    private static Font ResolveFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
            return font;

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, Font font, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.supportRichText = false;
        return text;
    }
}
