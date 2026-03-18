using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatrixConsoleUI : MonoBehaviour
{
    private const float panelWidth = 260f;
    private const float panelPadding = 18f;
    private const int maxLines = 18;
    private const int maxFlyBitsPerCell = 3;
    private const float panelGlowWidth = 6f;
    private const int chunksPerCycle = 24;

    private static MatrixConsoleUI instance;
    public static System.Action<int> onConsoleCycleCompleted;

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform panelRect;
    private RectTransform streamAnchor;
    private Text streamText;
    private Text statusText;
    private Image progressFill;
    private Image scanLine;
    private Image panelTint;
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

    public static void EmitFromWorld(Vector3 worldPosition, int glyphCount = maxFlyBitsPerCell)
    {
        EnsureExists();
        instance.CreateFlyingBits(worldPosition, glyphCount);
    }

    private void Build()
    {
        DontDestroyOnLoad(gameObject);

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
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

        GameObject panel = new GameObject("ConsolePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0.08f);
        panelRect.anchorMax = new Vector2(1f, 0.92f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.sizeDelta = new Vector2(panelWidth, 0f);
        panelRect.anchoredPosition = new Vector2(-16f, 0f);
        panelTint = panel.GetComponent<Image>();
        panelTint.color = new Color(0.03f, 0.09f, 0.05f, 0.48f);

        GameObject glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
        glow.transform.SetParent(panel.transform, false);
        RectTransform glowRect = glow.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-panelGlowWidth, -panelGlowWidth);
        glowRect.offsetMax = new Vector2(panelGlowWidth, panelGlowWidth);
        Image glowImage = glow.GetComponent<Image>();
        glowImage.color = new Color(0.28f, 1f, 0.42f, 0.06f);
        glow.transform.SetAsFirstSibling();

        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(panel.transform, false);
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        Image borderImage = border.GetComponent<Image>();
        borderImage.color = new Color(0.4f, 1f, 0.56f, 0.14f);

        GameObject innerShade = new GameObject("InnerShade", typeof(RectTransform), typeof(Image));
        innerShade.transform.SetParent(panel.transform, false);
        RectTransform innerShadeRect = innerShade.GetComponent<RectTransform>();
        innerShadeRect.anchorMin = Vector2.zero;
        innerShadeRect.anchorMax = Vector2.one;
        innerShadeRect.offsetMin = new Vector2(10f, 10f);
        innerShadeRect.offsetMax = new Vector2(-10f, -10f);
        Image innerShadeImage = innerShade.GetComponent<Image>();
        innerShadeImage.color = new Color(0.01f, 0.04f, 0.02f, 0.18f);
        innerShade.transform.SetAsFirstSibling();

        Text header = CreateText("Header", panel.transform, consoleFont, 24, FontStyle.Bold);
        RectTransform headerRect = header.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(panelPadding, -48f);
        headerRect.offsetMax = new Vector2(-panelPadding, -12f);
        header.alignment = TextAnchor.MiddleLeft;
        header.text = "> MATRIX SYSLOG";
        header.color = new Color(0.74f, 1f, 0.8f, 0.98f);

        Text subHeader = CreateText("SubHeader", panel.transform, consoleFont, 12, FontStyle.Normal);
        RectTransform subRect = subHeader.rectTransform;
        subRect.anchorMin = new Vector2(0f, 1f);
        subRect.anchorMax = new Vector2(1f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.offsetMin = new Vector2(panelPadding, -70f);
        subRect.offsetMax = new Vector2(-panelPadding, -42f);
        subHeader.alignment = TextAnchor.MiddleLeft;
        subHeader.text = "capturing cleared row fragments...";
        subHeader.color = new Color(0.38f, 0.95f, 0.54f, 0.72f);

        GameObject barBack = new GameObject("ProgressBack", typeof(RectTransform), typeof(Image));
        barBack.transform.SetParent(panel.transform, false);
        RectTransform barBackRect = barBack.GetComponent<RectTransform>();
        barBackRect.anchorMin = new Vector2(0f, 1f);
        barBackRect.anchorMax = new Vector2(1f, 1f);
        barBackRect.pivot = new Vector2(0.5f, 1f);
        barBackRect.offsetMin = new Vector2(panelPadding, -92f);
        barBackRect.offsetMax = new Vector2(-panelPadding, -82f);
        barBack.GetComponent<Image>().color = new Color(0.07f, 0.14f, 0.09f, 0.85f);

        GameObject barFill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        barFill.transform.SetParent(barBack.transform, false);
        progressFill = barFill.GetComponent<Image>();
        RectTransform barFillRect = barFill.GetComponent<RectTransform>();
        barFillRect.anchorMin = new Vector2(0f, 0f);
        barFillRect.anchorMax = new Vector2(0f, 1f);
        barFillRect.pivot = new Vector2(0f, 0.5f);
        barFillRect.sizeDelta = new Vector2(0f, 0f);
        progressFill.color = new Color(0.45f, 1f, 0.56f, 0.85f);

        GameObject scan = new GameObject("ScanLine", typeof(RectTransform), typeof(Image));
        scan.transform.SetParent(panel.transform, false);
        scanLine = scan.GetComponent<Image>();
        RectTransform scanRect = scan.GetComponent<RectTransform>();
        scanRect.anchorMin = new Vector2(0f, 1f);
        scanRect.anchorMax = new Vector2(1f, 1f);
        scanRect.pivot = new Vector2(0.5f, 1f);
        scanRect.offsetMin = new Vector2(panelPadding, -128f);
        scanRect.offsetMax = new Vector2(-panelPadding, -126f);
        scanLine.color = new Color(0.62f, 1f, 0.72f, 0.2f);

        streamText = CreateText("Stream", panel.transform, consoleFont, 16, FontStyle.Normal);
        RectTransform streamRect = streamText.rectTransform;
        streamRect.anchorMin = new Vector2(0f, 0f);
        streamRect.anchorMax = new Vector2(1f, 1f);
        streamRect.offsetMin = new Vector2(panelPadding, panelPadding);
        streamRect.offsetMax = new Vector2(-panelPadding, -106f);
        streamText.alignment = TextAnchor.UpperLeft;
        streamText.horizontalOverflow = HorizontalWrapMode.Wrap;
        streamText.verticalOverflow = VerticalWrapMode.Truncate;
        streamText.text = "> awaiting fragments...";
        streamText.color = new Color(0.54f, 1f, 0.64f, 0.9f);

        statusText = CreateText("Status", panel.transform, consoleFont, 11, FontStyle.Normal);
        RectTransform statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.offsetMin = new Vector2(panelPadding, 8f);
        statusRect.offsetMax = new Vector2(-panelPadding, 28f);
        statusText.alignment = TextAnchor.LowerLeft;
        statusText.text = "channel idle";
        statusText.color = new Color(0.42f, 0.92f, 0.54f, 0.55f);

        streamAnchor = new GameObject("StreamAnchor", typeof(RectTransform)).GetComponent<RectTransform>();
        streamAnchor.SetParent(panel.transform, false);
        streamAnchor.anchorMin = new Vector2(0f, 1f);
        streamAnchor.anchorMax = new Vector2(1f, 1f);
        streamAnchor.pivot = new Vector2(0.5f, 1f);
        streamAnchor.offsetMin = new Vector2(panelPadding, -126f);
        streamAnchor.offsetMax = new Vector2(-panelPadding, -106f);
    }

    private void Update()
    {
        if (panelTint != null)
        {
            pulseTimer = Mathf.Max(0f, pulseTimer - Time.deltaTime * 1.8f);
            panelTint.color = Color.Lerp(
                new Color(0.03f, 0.09f, 0.05f, 0.48f),
                new Color(0.12f, 0.34f, 0.18f, 0.78f),
                pulseTimer);
        }

        if (progressFill != null)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * 3.5f);
            progressFill.rectTransform.sizeDelta = new Vector2((panelWidth - panelPadding * 2f) * Mathf.Max(0.08f, currentFill), 0f);
        }

        if (scanLine != null)
        {
            float scanY = Mathf.Lerp(-128f, -340f, (Mathf.Sin(Time.time * 0.9f) + 1f) * 0.5f);
            scanLine.rectTransform.offsetMin = new Vector2(panelPadding, scanY);
            scanLine.rectTransform.offsetMax = new Vector2(-panelPadding, scanY + 2f);
            Color scanColor = scanLine.color;
            scanColor.a = 0.08f + ((Mathf.Sin(Time.time * 2f) + 1f) * 0.05f);
            scanLine.color = scanColor;
        }

        for (int i = flyingBits.Count - 1; i >= 0; i--)
        {
            FlyingBit bit = flyingBits[i];
            bit.progress += Time.deltaTime * bit.speed;

            if (bit.label == null)
            {
                flyingBits.RemoveAt(i);
                continue;
            }

            Vector2 position = Vector2.Lerp(bit.start, bit.target, Mathf.SmoothStep(0f, 1f, bit.progress));
            bit.label.rectTransform.anchoredPosition = position;
            bit.label.fontSize = Mathf.RoundToInt(Mathf.Lerp(18f, 11f, bit.progress));

            Color color = bit.label.color;
            color.a = Mathf.Lerp(0.95f, 0.3f, bit.progress);
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
            bitLabel.color = new Color(0.72f, 1f, 0.8f, 0.96f);

            RectTransform bitRect = bitLabel.rectTransform;
            bitRect.sizeDelta = new Vector2(80f, 24f);
            bitRect.anchoredPosition = localPoint + new Vector2(Random.Range(-18f, 18f), Random.Range(-12f, 12f));

            Vector2 target = GetRandomTargetInsideConsole();
            flyingBits.Add(new FlyingBit
            {
                label = bitLabel,
                start = bitRect.anchoredPosition,
                target = target,
                speed = Random.Range(1.8f, 2.6f),
                progress = 0f,
                chunk = chunk
            });
        }
    }

    private Vector2 GetRandomTargetInsideConsole()
    {
        Vector3 world = streamAnchor.TransformPoint(new Vector3(Random.Range(20f, panelWidth - 70f), Random.Range(-10f, -220f), 0f));
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
        {
            statusText.text = $"channel synced  fragments:{receivedChunks:000}";
        }

        if (cycleChunks >= chunksPerCycle)
        {
            CompleteCycle();
        }
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

    private static Font ResolveFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
            return font;

        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        if (fonts != null && fonts.Length > 0)
            return fonts[0];

        return null;
    }

    private static string RandomBinary(int length)
    {
        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = Random.value > 0.5f ? '1' : '0';
        return new string(chars);
    }

    private void CompleteCycle()
    {
        cycleIndex++;
        pulseTimer = 1f;
        onConsoleCycleCompleted?.Invoke(cycleIndex);

        logLines.Clear();
        logLines.Enqueue($"> archive_{cycleIndex:000}: buffer committed");
        logLines.Enqueue($"> score uplink: +console bonus");
        logLines.Enqueue($"> next buffer opened");
        streamText.text = string.Join("\n", logLines.ToArray());

        cycleChunks = 0;
        currentFill = 1f;
        targetFill = 0f;

        if (statusText != null)
        {
            statusText.text = $"buffer archived  cycle:{cycleIndex:000}";
        }
    }

    private struct FlyingBit
    {
        public Text label;
        public Vector2 start;
        public Vector2 target;
        public float speed;
        public float progress;
        public string chunk;
    }
}
