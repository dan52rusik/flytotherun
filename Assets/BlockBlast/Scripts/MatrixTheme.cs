using UnityEngine;

public static class MatrixTheme
{
    private static Texture2D blockTexture;
    private static Texture2D cellTexture;
    private static Texture2D previewTexture;

    private static readonly Color backgroundColor = new Color(0.01f, 0.06f, 0.04f, 1f);
    private static readonly Color blockBase = new Color(0.1f, 0.42f, 0.16f, 1f);
    private static readonly Color blockGlow = new Color(0.45f, 1f, 0.55f, 1f);
    private static readonly Color cellBase = new Color(0.16f, 0.16f, 0.16f, 1f);
    private static readonly Color cellGlow = new Color(0.12f, 0.45f, 0.2f, 1f);
    private static readonly Color previewBase = new Color(0.2f, 1f, 0.45f, 0.32f);
    private static readonly Color previewGlow = new Color(0.65f, 1f, 0.72f, 0.85f);

    public static void ApplyCameraTheme()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = backgroundColor;
        }
    }

    public static void ApplyToObject(GameObject target, MatrixSurfaceType surfaceType)
    {
        if (target == null) return;

        MatrixSurface[] surfaces = target.GetComponentsInChildren<MatrixSurface>(true);
        if (surfaces.Length > 0)
        {
            for (int i = 0; i < surfaces.Length; i++)
            {
                surfaces[i].surfaceType = surfaceType;
                surfaces[i].Refresh();
            }
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MatrixSurface surface = renderers[i].gameObject.GetComponent<MatrixSurface>();
            if (surface == null)
            {
                surface = renderers[i].gameObject.AddComponent<MatrixSurface>();
            }

            surface.surfaceType = surfaceType;
            surface.Refresh();
        }
    }

    public static void ConfigureMaterial(Material material, MatrixSurfaceType surfaceType, float scrollOffset)
    {
        if (material == null) return;

        Texture2D texture = GetTexture(surfaceType);
        Color tint = GetBaseColor(surfaceType);
        Color glow = GetGlowColor(surfaceType);

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", tint);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", tint);
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", glow * (surfaceType == MatrixSurfaceType.Cell ? 0.08f : 0.8f));

        if (material.HasProperty("_Surface") && surfaceType == MatrixSurfaceType.Preview)
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_ZWrite") && surfaceType == MatrixSurfaceType.Preview)
            material.SetFloat("_ZWrite", 0f);

        Vector2 scale = surfaceType == MatrixSurfaceType.Cell ? new Vector2(0.8f, 0.8f) : new Vector2(1f, 1.1f);
        Vector2 offset = new Vector2(0f, scrollOffset);

        if (material.HasProperty("_BaseMap"))
            material.SetTextureScale("_BaseMap", scale);
        if (material.HasProperty("_MainTex"))
            material.SetTextureScale("_MainTex", scale);
        if (material.HasProperty("_BaseMap"))
            material.SetTextureOffset("_BaseMap", offset);
        if (material.HasProperty("_MainTex"))
            material.SetTextureOffset("_MainTex", offset);
    }

    public static Color[] GetShapePalette()
    {
        return new[]
        {
            new Color(0.4f, 1f, 0.5f, 1f),
            new Color(0.28f, 0.92f, 0.4f, 1f),
            new Color(0.62f, 1f, 0.72f, 1f),
            new Color(0.2f, 0.8f, 0.3f, 1f)
        };
    }

    private static Texture2D GetTexture(MatrixSurfaceType surfaceType)
    {
        switch (surfaceType)
        {
            case MatrixSurfaceType.Cell:
                if (cellTexture == null) cellTexture = CreateBinaryTexture(cellBase, cellGlow, 1234);
                return cellTexture;
            case MatrixSurfaceType.Preview:
                if (previewTexture == null) previewTexture = CreateBinaryTexture(previewBase, previewGlow, 3456);
                return previewTexture;
            default:
                if (blockTexture == null) blockTexture = CreateBinaryTexture(blockBase, blockGlow, 2345);
                return blockTexture;
        }
    }

    private static Color GetBaseColor(MatrixSurfaceType surfaceType)
    {
        switch (surfaceType)
        {
            case MatrixSurfaceType.Cell: return cellBase;
            case MatrixSurfaceType.Preview: return previewBase;
            default: return blockBase;
        }
    }

    private static Color GetGlowColor(MatrixSurfaceType surfaceType)
    {
        switch (surfaceType)
        {
            case MatrixSurfaceType.Cell: return cellGlow;
            case MatrixSurfaceType.Preview: return previewGlow;
            default: return blockGlow;
        }
    }

    private static Texture2D CreateBinaryTexture(Color baseColor, Color digitColor, int seed)
    {
        const int width = 64;
        const int height = 64;
        const int columns = 4;
        const int rows = 4;
        int cellWidth = width / columns;
        int cellHeight = height / rows;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Point;

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = baseColor;

        System.Random random = new System.Random(seed);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                bool one = random.NextDouble() > 0.5;
                DrawDigit(pixels, width, height, column * cellWidth, row * cellHeight, cellWidth, cellHeight, one, digitColor);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static void DrawDigit(Color[] pixels, int texWidth, int texHeight, int startX, int startY, int width, int height, bool one, Color color)
    {
        int paddingX = Mathf.Max(1, width / 4);
        int paddingY = Mathf.Max(1, height / 6);
        int innerWidth = width - paddingX * 2;
        int innerHeight = height - paddingY * 2;

        if (one)
        {
            int lineWidth = Mathf.Max(1, innerWidth / 3);
            int centerX = startX + width / 2 - lineWidth / 2;
            FillRect(pixels, texWidth, texHeight, centerX, startY + paddingY, lineWidth, innerHeight, color);
            FillRect(pixels, texWidth, texHeight, centerX - 1, startY + paddingY, lineWidth + 2, Mathf.Max(1, innerHeight / 8), color);
        }
        else
        {
            int stroke = Mathf.Max(1, innerWidth / 5);
            FillRect(pixels, texWidth, texHeight, startX + paddingX, startY + paddingY, innerWidth, stroke, color);
            FillRect(pixels, texWidth, texHeight, startX + paddingX, startY + paddingY + innerHeight - stroke, innerWidth, stroke, color);
            FillRect(pixels, texWidth, texHeight, startX + paddingX, startY + paddingY, stroke, innerHeight, color);
            FillRect(pixels, texWidth, texHeight, startX + paddingX + innerWidth - stroke, startY + paddingY, stroke, innerHeight, color);
        }
    }

    private static void FillRect(Color[] pixels, int texWidth, int texHeight, int x, int y, int width, int height, Color color)
    {
        for (int py = Mathf.Max(0, y); py < Mathf.Min(texHeight, y + height); py++)
        {
            for (int px = Mathf.Max(0, x); px < Mathf.Min(texWidth, x + width); px++)
            {
                pixels[py * texWidth + px] = color;
            }
        }
    }
}

public enum MatrixSurfaceType
{
    Cell,
    Block,
    Preview
}
