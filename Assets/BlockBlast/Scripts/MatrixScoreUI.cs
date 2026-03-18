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

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject panel = new GameObject("ScorePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(28f, -28f);
        panelRect.sizeDelta = new Vector2(300f, 150f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.03f, 0.09f, 0.05f, 0.52f);

        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(panel.transform, false);
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        border.GetComponent<Image>().color = new Color(0.36f, 1f, 0.56f, 0.16f);

        Text title = CreateText("Title", panel.transform, font, 24, FontStyle.Bold);
        title.alignment = TextAnchor.MiddleLeft;
        title.text = "> SCORE";
        title.color = new Color(0.72f, 1f, 0.8f, 0.96f);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(18f, -44f);
        titleRect.offsetMax = new Vector2(-18f, -8f);

        Text sub = CreateText("Sub", panel.transform, font, 12, FontStyle.Normal);
        sub.alignment = TextAnchor.MiddleLeft;
        sub.text = "matrix score accumulator";
        sub.color = new Color(0.38f, 0.95f, 0.54f, 0.72f);
        RectTransform subRect = sub.rectTransform;
        subRect.anchorMin = new Vector2(0f, 1f);
        subRect.anchorMax = new Vector2(1f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.offsetMin = new Vector2(18f, -70f);
        subRect.offsetMax = new Vector2(-18f, -42f);

        scoreValueText = CreateText("Value", panel.transform, font, 54, FontStyle.Bold);
        scoreValueText.alignment = TextAnchor.MiddleLeft;
        scoreValueText.text = "0";
        scoreValueText.color = new Color(0.56f, 1f, 0.66f, 1f);
        RectTransform valueRect = scoreValueText.rectTransform;
        valueRect.anchorMin = new Vector2(0f, 0f);
        valueRect.anchorMax = new Vector2(1f, 0f);
        valueRect.pivot = new Vector2(0.5f, 0f);
        valueRect.offsetMin = new Vector2(18f, 14f);
        valueRect.offsetMax = new Vector2(-18f, 86f);
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
