using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

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
    private Image panelImage;
    private Image glowImage;

    private Action continueAction;
    private Action skipAction;
    private float pulse;

    public static MatrixGameOverUI EnsureExists()
    {
        if (instance != null)
            return instance;

        GameObject uiObject = new GameObject("MatrixGameOverUI");
        instance = uiObject.AddComponent<MatrixGameOverUI>();
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

    public void Show(int score, bool canContinue, Action onContinue, Action onSkip)
    {
        continueAction = onContinue;
        skipAction = onSkip;
        pulse = 1f;

        titleText.text = canContinue ? "> RUN BLOCKED" : "> RUN TERMINATED";
        bodyText.text = canContinue
            ? "No legal placements remain.\nInject a recovery sequence and keep the run alive."
            : "No legal placements remain.\nThe sequence is over. Reboot and start a new run.";
        scoreText.text = $"score snapshot  {score}";
        statusText.text = canContinue ? "reward recovery channel available" : "recovery token spent";

        continueButton.gameObject.SetActive(canContinue);
        skipButtonText.text = canContinue ? "END RUN" : "RESTART";
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
            continueButtonText.text = busy ? "OPENING..." : "CONTINUE";
    }

    private void Update()
    {
        if (canvasGroup == null || canvasGroup.alpha <= 0f)
            return;

        pulse = Mathf.Max(0f, pulse - Time.deltaTime * 1.6f);

        if (panelImage != null)
        {
            panelImage.color = Color.Lerp(new Color(0.04f, 0.1f, 0.07f, 0.94f), new Color(0.1f, 0.22f, 0.15f, 0.98f), pulse);
        }

        if (glowImage != null)
        {
            Color glow = glowImage.color;
            glow.a = Mathf.Lerp(0.08f, 0.2f, pulse);
            glowImage.color = glow;
        }
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

        Image overlay = CreateImage("Overlay", transform, new Color(0f, 0.025f, 0.015f, 0.82f));
        RectTransform overlayRect = overlay.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        glowImage = CreateImage("Glow", transform, new Color(0.2f, 1f, 0.48f, 0.08f));
        RectTransform glowRect = glowImage.rectTransform;
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(760f, 460f);
        glowRect.anchoredPosition = Vector2.zero;

        panelImage = CreateImage("Panel", transform, new Color(0.04f, 0.1f, 0.07f, 0.94f));
        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(680f, 410f);
        panelRect.anchoredPosition = Vector2.zero;

        Image topBar = CreateImage("TopBar", panelImage.transform, new Color(0.48f, 1f, 0.68f, 0.96f));
        RectTransform topBarRect = topBar.rectTransform;
        topBarRect.anchorMin = new Vector2(0f, 1f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.offsetMin = new Vector2(0f, -4f);
        topBarRect.offsetMax = Vector2.zero;

        Image border = CreateImage("Border", panelImage.transform, new Color(0.54f, 1f, 0.74f, 0.16f));
        RectTransform borderRect = border.rectTransform;
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        Image inner = CreateImage("Inner", panelImage.transform, new Color(0.01f, 0.05f, 0.03f, 0.4f));
        RectTransform innerRect = inner.rectTransform;
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(14f, 14f);
        innerRect.offsetMax = new Vector2(-14f, -14f);
        inner.transform.SetAsFirstSibling();

        Image accent = CreateImage("Accent", panelImage.transform, new Color(0.32f, 1f, 0.56f, 0.12f));
        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(72f, 0f);
        accent.transform.SetAsFirstSibling();

        Text tag = CreateText("Tag", panelImage.transform, font, 14, FontStyle.Bold);
        tag.text = "SESSION STATUS";
        tag.alignment = TextAnchor.MiddleLeft;
        tag.color = new Color(0.72f, 1f, 0.82f, 0.76f);
        RectTransform tagRect = tag.rectTransform;
        tagRect.anchorMin = new Vector2(0f, 1f);
        tagRect.anchorMax = new Vector2(1f, 1f);
        tagRect.offsetMin = new Vector2(42f, -44f);
        tagRect.offsetMax = new Vector2(-42f, -18f);

        titleText = CreateText("Title", panelImage.transform, font, 38, FontStyle.Bold);
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = new Color(0.88f, 1f, 0.92f, 0.98f);
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(42f, -98f);
        titleRect.offsetMax = new Vector2(-42f, -44f);

        bodyText = CreateText("Body", panelImage.transform, font, 22, FontStyle.Normal);
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.color = new Color(0.56f, 1f, 0.68f, 0.82f);
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(42f, -184f);
        bodyRect.offsetMax = new Vector2(-42f, -108f);

        Image statCard = CreateImage("StatCard", panelImage.transform, new Color(0.06f, 0.15f, 0.1f, 0.72f));
        RectTransform statCardRect = statCard.rectTransform;
        statCardRect.anchorMin = new Vector2(0f, 1f);
        statCardRect.anchorMax = new Vector2(1f, 1f);
        statCardRect.offsetMin = new Vector2(42f, -248f);
        statCardRect.offsetMax = new Vector2(-42f, -196f);

        scoreText = CreateText("Score", statCard.transform, font, 22, FontStyle.Bold);
        scoreText.alignment = TextAnchor.MiddleLeft;
        scoreText.color = new Color(0.78f, 1f, 0.84f, 0.94f);
        RectTransform scoreRect = scoreText.rectTransform;
        scoreRect.anchorMin = Vector2.zero;
        scoreRect.anchorMax = Vector2.one;
        scoreRect.offsetMin = new Vector2(18f, 0f);
        scoreRect.offsetMax = new Vector2(-18f, 0f);

        continueButton = CreateButton("ContinueButton", panelImage.transform, font, new Color(0.18f, 0.44f, 0.24f, 0.98f), out continueButtonText);
        RectTransform continueRect = continueButton.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(0f, 0f);
        continueRect.anchorMax = new Vector2(0.5f, 0f);
        continueRect.offsetMin = new Vector2(42f, 88f);
        continueRect.offsetMax = new Vector2(-12f, 160f);
        continueButton.onClick.AddListener(() => continueAction?.Invoke());

        skipButton = CreateButton("SkipButton", panelImage.transform, font, new Color(0.08f, 0.16f, 0.11f, 0.98f), out skipButtonText);
        RectTransform skipRect = skipButton.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0.5f, 0f);
        skipRect.anchorMax = new Vector2(1f, 0f);
        skipRect.offsetMin = new Vector2(12f, 88f);
        skipRect.offsetMax = new Vector2(-42f, 160f);
        skipButton.onClick.AddListener(() => skipAction?.Invoke());

        statusText = CreateText("Status", panelImage.transform, font, 15, FontStyle.Normal);
        statusText.alignment = TextAnchor.MiddleLeft;
        statusText.color = new Color(0.46f, 0.95f, 0.58f, 0.7f);
        RectTransform statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(42f, 34f);
        statusRect.offsetMax = new Vector2(-42f, 62f);

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

        Image topBar = CreateImage("TopBar", go.transform, new Color(0.58f, 1f, 0.74f, 0.22f));
        RectTransform topBarRect = topBar.rectTransform;
        topBarRect.anchorMin = new Vector2(0f, 1f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.offsetMin = new Vector2(0f, -3f);
        topBarRect.offsetMax = Vector2.zero;

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.08f;
        colors.pressedColor = color * 0.9f;
        colors.selectedColor = color;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.45f);
        button.colors = colors;

        label = CreateText("Label", go.transform, font, 22, FontStyle.Bold);
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.84f, 1f, 0.9f, 1f);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }
}
