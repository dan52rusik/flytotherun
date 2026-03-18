using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatrixConsoleUI : MonoBehaviour
{
    private const float panelWidth = 304f;
    private const float panelPadding = 18f;
    private const int maxLines = 18;
    private const int maxFlyBitsPerCell = 3;
    private const int chunksPerCycle = 24;

    private static MatrixConsoleUI instance;
    public static System.Action<int> onConsoleCycleCompleted;

    private RectTransform canvasRect;
    private RectTransform streamAnchor;
    private Text streamText;
    private Text statusText;
    private Text metricText;
    private Text progressLabel;
    private Image progressFill;
    private Image panelTint;
    private Image glowImage;
    private Font consoleFont;
    private readonly Queue<string> logLines = new Queue<string>();
    private readonly List<FlyingBit> flyingBits = new List<FlyingBit>();
    private int receivedChunks;
    private float currentFill;
    private float targetFill;
    private int cycleChunks;
    private int cycleIndex;
    private float pulseTimer;

    public static void EnsureExists()
    {
        if (instance != null)
            return;

        GameObject consoleObject = new GameObject("MatrixConsoleUI");
        instance = consoleObject.AddComponent<MatrixConsoleUI>();
        instance.Build();
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

    public static void EmitFromWorld(Vector3 worldPosition, int glyphCount = maxFlyBitsPerCell)
    {
        EnsureExists();
        instance.CreateFlyingBits(worldPosition, glyphCount);
    }

    private void Build()
    {
        DontDestroyOnLoad(gameObject);

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>();

        canvasRect = gameObject.GetComponent<RectTransform>();
        if (canvasRect == null)
            canvasRect = gameObject.AddComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        consoleFont = ResolveFont();
        if (consoleFont == null)
        {
            Debug.LogWarning("MatrixConsoleUI: no UI font found, console UI disabled.");
            return;
        }

        glowImage = CreateImage("Glow", transform, new Color(0.18f, 1f, 0.48f, 0.05f));
        RectTransform glowRect = glowImage.rectTransform;
        glowRect.anchorMin = new Vector2(1f, 0.08f);
        glowRect.anchorMax = new Vector2(1f, 0.92f);
        glowRect.pivot = new Vector2(1f, 0.5f);
        glowRect.sizeDelta = new Vector2(panelWidth + 18f, 0f);
        glowRect.anchoredPosition = new Vector2(-26f, 0f);

        panelTint = CreateImage("Panel", transform, new Color(0.03f, 0.07f, 0.06f, 0.88f));
        RectTransform panelRect = panelTint.rectTransform;
        panelRect.anchorMin = new Vector2(1f, 0.08f);
        panelRect.anchorMax = new Vector2(1f, 0.92f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = new Vector2(panelWidth, 0f);
        panelRect.anchoredPosition = new Vector2(-28f, 0f);

        Image border = CreateImage("Border", panelTint.transform, new Color(0.58f, 1f, 0.78f, 0.1f));
        Stretch(border.rectTransform);

        Image inner = CreateImage("Inner", panelTint.transform, new Color(0.05f, 0.11f, 0.09f, 0.42f));
        RectTransform innerRect = inner.rectTransform;
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(10f, 10f);
        innerRect.offsetMax = new Vector2(-10f, -10f);
        inner.transform.SetAsFirstSibling();

        Image topLine = CreateImage("TopLine", panelTint.transform, new Color(0.42f, 1f, 0.68f, 0.92f));
        RectTransform topLineRect = topLine.rectTransform;
        topLineRect.anchorMin = new Vector2(0f, 1f);
        topLineRect.anchorMax = new Vector2(1f, 1f);
        topLineRect.offsetMin = new Vector2(12f, -3f);
        topLineRect.offsetMax = new Vector2(-12f, 0f);

        Text tag = CreateText("Tag", panelTint.transform, consoleFont, 10, FontStyle.Bold);
        tag.text = "NEURAL STREAM";
        tag.alignment = TextAnchor.MiddleLeft;
        tag.color = new Color(0.56f, 1f, 0.7f, 0.72f);
        RectTransform tagRect = tag.rectTransform;
        tagRect.anchorMin = new Vector2(0f, 1f);
        tagRect.anchorMax = new Vector2(1f, 1f);
        tagRect.offsetMin = new Vector2(panelPadding, -28f);
        tagRect.offsetMax = new Vector2(-panelPadding, -10f);

        Text title = CreateText("Title", panelTint.transform, consoleFont, 22, FontStyle.Bold);
        title.text = "SYSLOG";
        title.alignment = TextAnchor.MiddleLeft;
        title.color = new Color(0.9f, 1f, 0.94f, 0.98f);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(panelPadding, -58f);
        titleRect.offsetMax = new Vector2(-panelPadding, -28f);

        Text subtitle = CreateText("Subtitle", panelTint.transform, consoleFont, 10, FontStyle.Normal);
        subtitle.text = "capturing cleared fragments";
        subtitle.alignment = TextAnchor.MiddleLeft;
        subtitle.color = new Color(0.44f, 0.92f, 0.6f, 0.62f);
        RectTransform subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.offsetMin = new Vector2(panelPadding, -76f);
        subtitleRect.offsetMax = new Vector2(-panelPadding, -56f);

        progressLabel = CreateText("ProgressLabel", panelTint.transform, consoleFont, 10, FontStyle.Bold);
        progressLabel.text = "BUFFER 00%";
        progressLabel.alignment = TextAnchor.MiddleRight;
        progressLabel.color = new Color(0.7f, 1f, 0.8f, 0.78f);
        RectTransform progressLabelRect = progressLabel.rectTransform;
        progressLabelRect.anchorMin = new Vector2(0f, 1f);
        progressLabelRect.anchorMax = new Vector2(1f, 1f);
        progressLabelRect.offsetMin = new Vector2(panelPadding, -104f);
        progressLabelRect.offsetMax = new Vector2(-panelPadding, -86f);

        Image barBack = CreateImage("BarBack", panelTint.transform, new Color(0.08f, 0.14f, 0.12f, 0.94f));
        RectTransform barBackRect = barBack.rectTransform;
        barBackRect.anchorMin = new Vector2(0f, 1f);
        barBackRect.anchorMax = new Vector2(1f, 1f);
        barBackRect.offsetMin = new Vector2(panelPadding, -124f);
        barBackRect.offsetMax = new Vector2(-panelPadding, -112f);

        progressFill = CreateImage("BarFill", barBack.transform, new Color(0.48f, 1f, 0.66f, 0.94f));
        RectTransform barFillRect = progressFill.rectTransform;
        barFillRect.anchorMin = new Vector2(0f, 0f);
        barFillRect.anchorMax = new Vector2(0f, 1f);
        barFillRect.pivot = new Vector2(0f, 0.5f);
        barFillRect.sizeDelta = Vector2.zero;

        metricText = CreateText("Metric", panelTint.transform, consoleFont, 9, FontStyle.Normal);
        metricText.text = "cycle:000  ingress:000";
        metricText.alignment = TextAnchor.MiddleLeft;
        metricText.color = new Color(0.38f, 0.9f, 0.56f, 0.58f);
        RectTransform metricRect = metricText.rectTransform;
        metricRect.anchorMin = new Vector2(0f, 1f);
        metricRect.anchorMax = new Vector2(1f, 1f);
        metricRect.offsetMin = new Vector2(panelPadding, -148f);
        metricRect.offsetMax = new Vector2(-panelPadding, -130f);

        Image divider = CreateImage("Divider", panelTint.transform, new Color(0.35f, 1f, 0.58f, 0.1f));
        RectTransform dividerRect = divider.rectTransform;
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = new Vector2(1f, 1f);
        dividerRect.offsetMin = new Vector2(panelPadding, -168f);
        dividerRect.offsetMax = new Vector2(-panelPadding, -166f);

        streamText = CreateText("Stream", panelTint.transform, consoleFont, 15, FontStyle.Normal);
        streamText.alignment = TextAnchor.UpperLeft;
        streamText.horizontalOverflow = HorizontalWrapMode.Wrap;
        streamText.verticalOverflow = VerticalWrapMode.Truncate;
        streamText.text = "> awaiting fragments...";
        streamText.color = new Color(0.7f, 1f, 0.76f, 0.94f);
        RectTransform streamRect = streamText.rectTransform;
        streamRect.anchorMin = new Vector2(0f, 0f);
        streamRect.anchorMax = new Vector2(1f, 1f);
        streamRect.offsetMin = new Vector2(panelPadding, 58f);
        streamRect.offsetMax = new Vector2(-panelPadding, -182f);

        Image footerDivider = CreateImage("FooterDivider", panelTint.transform, new Color(0.35f, 1f, 0.58f, 0.1f));
        RectTransform footerRect = footerDivider.rectTransform;
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.offsetMin = new Vector2(panelPadding, 50f);
        footerRect.offsetMax = new Vector2(-panelPadding, 52f);

        statusText = CreateText("Status", panelTint.transform, consoleFont, 10, FontStyle.Normal);
        statusText.text = "channel idle";
        statusText.alignment = TextAnchor.MiddleLeft;
        statusText.color = new Color(0.42f, 0.92f, 0.58f, 0.6f);
        RectTransform statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.offsetMin = new Vector2(panelPadding, 16f);
        statusRect.offsetMax = new Vector2(-panelPadding, 34f);

        streamAnchor = new GameObject("StreamAnchor", typeof(RectTransform)).GetComponent<RectTransform>();
        streamAnchor.SetParent(panelTint.transform, false);
        streamAnchor.anchorMin = new Vector2(0f, 1f);
        streamAnchor.anchorMax = new Vector2(1f, 1f);
        streamAnchor.offsetMin = new Vector2(panelPadding, -168f);
        streamAnchor.offsetMax = new Vector2(-panelPadding, -150f);
    }

    private void Update()
    {
        if (panelTint != null)
        {
            pulseTimer = Mathf.Max(0f, pulseTimer - Time.deltaTime * 1.8f);
            panelTint.color = Color.Lerp(new Color(0.03f, 0.07f, 0.06f, 0.88f), new Color(0.08f, 0.16f, 0.12f, 0.94f), pulseTimer);
        }

        if (glowImage != null)
        {
            Color glowColor = glowImage.color;
            glowColor.a = Mathf.Lerp(0.05f, 0.12f, pulseTimer);
            glowImage.color = glowColor;
        }

        if (progressFill != null)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * 3.2f);
            progressFill.rectTransform.sizeDelta = new Vector2((panelWidth - panelPadding * 2f) * Mathf.Max(0.06f, currentFill), 0f);
            if (progressLabel != null)
                progressLabel.text = $"BUFFER {Mathf.RoundToInt(currentFill * 100f):00}%";
        }

        if (metricText != null)
            metricText.text = $"cycle:{cycleIndex:000}  ingress:{receivedChunks:000}";

        for (int i = flyingBits.Count - 1; i >= 0; i--)
        {
            FlyingBit bit = flyingBits[i];
            bit.progress += Time.deltaTime * bit.speed;

            if (bit.label == null)
            {
                flyingBits.RemoveAt(i);
                continue;
            }

            float eased = Mathf.SmoothStep(0f, 1f, bit.progress);
            Vector2 position = Vector2.Lerp(bit.start, bit.target, eased);
            position.y += Mathf.Sin(bit.progress * Mathf.PI) * bit.arcHeight;
            bit.label.rectTransform.anchoredPosition = position;
            bit.label.fontSize = Mathf.RoundToInt(Mathf.Lerp(18f, 11f, eased));

            Color color = bit.label.color;
            color.a = Mathf.Lerp(0.95f, 0.24f, eased);
            bit.label.color = color;

            if (bit.progress >= 1f)
            {
                AppendChunk(bit.chunk);
                Destroy(bit.label.gameObject);
                flyingBits.RemoveAt(i);
            }
            else
            {
                flyingBits[i] = bit;
            }
        }
    }

    private void CreateFlyingBits(Vector3 worldPosition, int glyphCount)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || canvasRect == null || streamAnchor == null || consoleFont == null)
            return;

        glyphCount = Mathf.Clamp(glyphCount, 1, maxFlyBitsPerCell);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCam, worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);

        for (int i = 0; i < glyphCount; i++)
        {
            string chunk = RandomBinary(Random.Range(4, 10));
            Text bitLabel = CreateText("FlyingBit", transform, consoleFont, 18, FontStyle.Bold);
            bitLabel.alignment = TextAnchor.MiddleCenter;
            bitLabel.text = chunk;
            bitLabel.color = new Color(0.8f, 1f, 0.86f, 0.98f);

            RectTransform bitRect = bitLabel.rectTransform;
            bitRect.sizeDelta = new Vector2(92f, 28f);
            bitRect.anchoredPosition = localPoint + new Vector2(Random.Range(-14f, 14f), Random.Range(-16f, 16f));

            flyingBits.Add(new FlyingBit
            {
                label = bitLabel,
                start = bitRect.anchoredPosition,
                target = GetRandomTargetInsideConsole(),
                speed = Random.Range(1.55f, 2.15f),
                progress = 0f,
                chunk = chunk,
                arcHeight = Random.Range(18f, 40f)
            });
        }
    }

    private Vector2 GetRandomTargetInsideConsole()
    {
        Vector3 world = streamAnchor.TransformPoint(new Vector3(Random.Range(18f, panelWidth - 70f), Random.Range(-10f, -250f), 0f));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(null, world), null, out Vector2 localTarget);
        return localTarget;
    }

    private void AppendChunk(string chunk)
    {
        receivedChunks++;
        cycleChunks++;
        string prefix = receivedChunks % 4 == 0 ? "sync" : "row";
        string line = $"> {prefix}_{receivedChunks:000}: {chunk} {RandomBinary(8)}";

        logLines.Enqueue(line);
        while (logLines.Count > maxLines)
            logLines.Dequeue();

        streamText.text = string.Join("\n", logLines.ToArray());
        targetFill = Mathf.Clamp01(cycleChunks / (float)chunksPerCycle);

        if (statusText != null)
            statusText.text = $"channel synced  fragments:{receivedChunks:000}";

        if (cycleChunks >= chunksPerCycle)
            CompleteCycle();
    }

    private void CompleteCycle()
    {
        cycleIndex++;
        pulseTimer = 1f;
        onConsoleCycleCompleted?.Invoke(cycleIndex);

        logLines.Clear();
        logLines.Enqueue($"> archive_{cycleIndex:000}: buffer committed");
        logLines.Enqueue("> score uplink: +console bonus");
        logLines.Enqueue("> next buffer opened");
        streamText.text = string.Join("\n", logLines.ToArray());

        cycleChunks = 0;
        currentFill = 1f;
        targetFill = 0f;

        if (statusText != null)
            statusText.text = $"buffer archived  cycle:{cycleIndex:000}";
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
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.supportRichText = false;
        return text;
    }

    private static string RandomBinary(int length)
    {
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = Random.value > 0.5f ? '1' : '0';
        return new string(chars);
    }

    private struct FlyingBit
    {
        public Text label;
        public Vector2 start;
        public Vector2 target;
        public float speed;
        public float progress;
        public float arcHeight;
        public string chunk;
    }
}
