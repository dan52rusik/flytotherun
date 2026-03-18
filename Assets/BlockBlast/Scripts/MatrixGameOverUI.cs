using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class MatrixGameOverUI : MonoBehaviour
{
    private static MatrixGameOverUI instance;

    private CanvasGroup canvasGroup;
    private Text titleText;
    private Text bodyText;
    private Text scoreText;
    private Text statusText;
    private Button continueButton;
    private Button skipButton;
    private Text continueButtonText;
    private Text skipButtonText;

    private Action continueAction;
    private Action skipAction;

    public static MatrixGameOverUI EnsureExists()
    {
        if (instance != null)
            return instance;

        GameObject uiObject = new GameObject("MatrixGameOverUI");
        instance = uiObject.AddComponent<MatrixGameOverUI>();
        instance.Build();
        return instance;
    }

    public void Show(int score, bool canContinue, Action onContinue, Action onSkip)
    {
        continueAction = onContinue;
        skipAction = onSkip;

        titleText.text = canContinue ? "> RUN BLOCKED" : "> RUN TERMINATED";
        bodyText.text = canContinue
            ? "no legal placements remain\ncontinue via reward stream?"
            : "no legal placements remain\nreboot session?";
        scoreText.text = $"score snapshot: {score}";
        statusText.text = canContinue ? "recovery channel available" : "recovery token spent";

        continueButton.gameObject.SetActive(canContinue);
        skipButtonText.text = canContinue ? "SKIP" : "RESTART";
        SetBusy(false, statusText.text);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    public void Hide()
    {
        continueAction = null;
        skipAction = null;

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    public void SetBusy(bool busy, string status)
    {
        if (!string.IsNullOrEmpty(status) && statusText != null)
            statusText.text = status;

        if (continueButton != null)
            continueButton.interactable = !busy;

        if (skipButton != null)
            skipButton.interactable = !busy;

        if (continueButtonText != null)
            continueButtonText.text = busy ? "LOADING..." : "CONTINUE";
    }

    private void Build()
    {
        DontDestroyOnLoad(gameObject);
        EnsureEventSystemExists();

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100;

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

        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        Image overlay = CreateImage("Overlay", transform, new Color(0f, 0.03f, 0.01f, 0.82f));
        RectTransform overlayRect = overlay.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image panel = CreateImage("Panel", transform, new Color(0.04f, 0.11f, 0.06f, 0.95f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 360f);

        Image border = CreateImage("Border", panel.transform, new Color(0.45f, 1f, 0.62f, 0.18f));
        RectTransform borderRect = border.rectTransform;
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        Image innerShade = CreateImage("InnerShade", panel.transform, new Color(0.01f, 0.05f, 0.03f, 0.35f));
        RectTransform innerShadeRect = innerShade.rectTransform;
        innerShadeRect.anchorMin = Vector2.zero;
        innerShadeRect.anchorMax = Vector2.one;
        innerShadeRect.offsetMin = new Vector2(12f, 12f);
        innerShadeRect.offsetMax = new Vector2(-12f, -12f);
        innerShade.transform.SetAsFirstSibling();

        titleText = CreateText("Title", panel.transform, font, 34, FontStyle.Bold);
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = new Color(0.76f, 1f, 0.84f, 0.98f);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(36f, -74f);
        titleRect.offsetMax = new Vector2(-36f, -24f);

        bodyText = CreateText("Body", panel.transform, font, 22, FontStyle.Normal);
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.color = new Color(0.48f, 0.98f, 0.62f, 0.84f);
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(36f, -150f);
        bodyRect.offsetMax = new Vector2(-36f, -88f);

        scoreText = CreateText("Score", panel.transform, font, 20, FontStyle.Bold);
        scoreText.alignment = TextAnchor.MiddleLeft;
        scoreText.color = new Color(0.68f, 1f, 0.76f, 0.92f);
        RectTransform scoreRect = scoreText.rectTransform;
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(1f, 1f);
        scoreRect.offsetMin = new Vector2(36f, -198f);
        scoreRect.offsetMax = new Vector2(-36f, -158f);

        statusText = CreateText("Status", panel.transform, font, 16, FontStyle.Normal);
        statusText.alignment = TextAnchor.MiddleLeft;
        statusText.color = new Color(0.42f, 0.92f, 0.54f, 0.7f);
        RectTransform statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(36f, 30f);
        statusRect.offsetMax = new Vector2(-36f, 58f);

        continueButton = CreateButton("ContinueButton", panel.transform, font, new Color(0.16f, 0.44f, 0.24f, 0.96f), out continueButtonText);
        RectTransform continueRect = continueButton.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(0f, 0f);
        continueRect.anchorMax = new Vector2(0.5f, 0f);
        continueRect.offsetMin = new Vector2(36f, 86f);
        continueRect.offsetMax = new Vector2(-10f, 152f);
        continueButton.onClick.AddListener(() => continueAction?.Invoke());

        skipButton = CreateButton("SkipButton", panel.transform, font, new Color(0.1f, 0.18f, 0.12f, 0.96f), out skipButtonText);
        RectTransform skipRect = skipButton.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0.5f, 0f);
        skipRect.anchorMax = new Vector2(1f, 0f);
        skipRect.offsetMin = new Vector2(10f, 86f);
        skipRect.offsetMax = new Vector2(-36f, 152f);
        skipButton.onClick.AddListener(() => skipAction?.Invoke());

        Hide();
    }

    private static void EnsureEventSystemExists()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, Font font, int fontSize, FontStyle style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.supportRichText = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Font font, Color color, out Text label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = color;

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.1f;
        colors.pressedColor = color * 0.9f;
        colors.selectedColor = color;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
        button.colors = colors;

        label = CreateText("Label", go.transform, font, 22, FontStyle.Bold);
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.78f, 1f, 0.84f, 1f);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }
}
