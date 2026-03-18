using System.Collections.Generic;
using UnityEngine;

public class ShapeFactory : MonoBehaviour
{
    public static ShapeFactory Instance { get; private set; }

    [Header("Resources")]
    public Material blockMaterial;
    public GameObject previewBlockPrefab;

    [Header("Palette")]
    public Color[] shapeColors = new Color[]
    {
        new Color(0.32f, 1f, 0.45f, 1f),
        new Color(0.18f, 0.9f, 0.34f, 1f),
        new Color(0.5f, 1f, 0.65f, 1f),
        new Color(0.1f, 0.78f, 0.25f, 1f),
    };

    private static readonly List<ShapeTemplate> allTemplates = new List<ShapeTemplate>()
    {
        new ShapeTemplate("Dot", new Vector2Int[] { new Vector2Int(0, 0) }),

        new ShapeTemplate("Line2H", new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }),
        new ShapeTemplate("Line3H", new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) }),
        new ShapeTemplate("Line4H", new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }),
        new ShapeTemplate("Line5H", new Vector2Int[] { new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }),

        new ShapeTemplate("Line2V", new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1) }),
        new ShapeTemplate("Line3V", new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1) }),
        new ShapeTemplate("Line4V", new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }),
        new ShapeTemplate("Line5V", new Vector2Int[] { new Vector2Int(0, -2), new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }),

        new ShapeTemplate("Square2x2", new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(0, 1), new Vector2Int(1, 1)
        }),
        new ShapeTemplate("Square3x3", new Vector2Int[] {
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
            new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1)
        }),

        new ShapeTemplate("L_1", new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0) }),
        new ShapeTemplate("L_2", new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) }),
        new ShapeTemplate("L_3", new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(1, 0) }),
        new ShapeTemplate("L_4", new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) }),

        new ShapeTemplate("BigL_1", new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2),
            new Vector2Int(1, 0), new Vector2Int(2, 0)
        }),
        new ShapeTemplate("BigL_2", new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2),
            new Vector2Int(-1, 0), new Vector2Int(-2, 0)
        }),
        new ShapeTemplate("BigL_3", new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(0, -2),
            new Vector2Int(1, 0), new Vector2Int(2, 0)
        }),
        new ShapeTemplate("BigL_4", new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(0, -2),
            new Vector2Int(-1, 0), new Vector2Int(-2, 0)
        }),

        new ShapeTemplate("T_Down", new Vector2Int[] {
            new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1)
        }),
        new ShapeTemplate("T_Up", new Vector2Int[] {
            new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, -1)
        }),
        new ShapeTemplate("T_Right", new Vector2Int[] {
            new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0)
        }),
        new ShapeTemplate("T_Left", new Vector2Int[] {
            new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0)
        }),

        new ShapeTemplate("S_Shape", new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1)
        }),
        new ShapeTemplate("Z_Shape", new Vector2Int[] {
            new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)
        }),
        new ShapeTemplate("S_Vertical", new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, -1)
        }),
        new ShapeTemplate("Z_Vertical", new Vector2Int[] {
            new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(1, 1)
        }),
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        shapeColors = MatrixTheme.GetShapePalette();
    }

    public IReadOnlyList<ShapeTemplate> GetTemplates()
    {
        return allTemplates;
    }

    public ShapeTemplate GetRandomTemplate()
    {
        return allTemplates[Random.Range(0, allTemplates.Count)];
    }

    public GameObject SpawnRandomShape(Vector3 position)
    {
        return SpawnShape(GetRandomTemplate(), position);
    }

    public GameObject SpawnShape(ShapeTemplate template, Vector3 position)
    {
        if (blockMaterial == null)
        {
            Debug.LogError("ShapeFactory: blockMaterial is not assigned.");
            return null;
        }

        if (shapeColors == null || shapeColors.Length == 0)
        {
            Debug.LogError("ShapeFactory: shapeColors is empty.");
            return null;
        }

        Color color = shapeColors[Random.Range(0, shapeColors.Length)];
        return BuildShape(template, position, color);
    }

    private GameObject BuildShape(ShapeTemplate template, Vector3 position, Color color)
    {
        GameObject shapeObj = new GameObject(template.name);
        shapeObj.transform.position = position;

        Shape shapeComp = shapeObj.AddComponent<Shape>();
        shapeComp.blockOffsets = template.offsets;

        ShapeDragger dragger = shapeObj.AddComponent<ShapeDragger>();
        if (dragger == null)
        {
            Debug.LogError("ShapeFactory: failed to add ShapeDragger component.");
            Destroy(shapeObj);
            return null;
        }

        dragger.previewBlockPrefab = previewBlockPrefab;
        dragger.returnSpeed = 15f;

        Material shapeMat = new Material(blockMaterial);
        shapeMat.SetColor("_BaseColor", color);
        shapeMat.SetColor("_Color", color);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < template.offsets.Length; i++)
        {
            Vector2Int offset = template.offsets[i];

            if (offset.x < min.x) min.x = offset.x;
            if (offset.y < min.y) min.y = offset.y;
            if (offset.x > max.x) max.x = offset.x;
            if (offset.y > max.y) max.y = offset.y;

            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Quad);
            block.name = $"Block_{i}";
            block.transform.SetParent(shapeObj.transform);
            block.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            block.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            MeshCollider meshCollider = block.GetComponent<MeshCollider>();
            if (meshCollider != null)
                Destroy(meshCollider);

            MeshRenderer renderer = block.GetComponent<MeshRenderer>();
            renderer.material = shapeMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            MatrixTheme.ApplyToObject(block, MatrixSurfaceType.Block);
        }

        BoxCollider collider = shapeObj.GetComponent<BoxCollider>();
        if (collider == null)
            collider = shapeObj.AddComponent<BoxCollider>();

        float width = max.x - min.x + 1f;
        float height = max.y - min.y + 1f;
        collider.size = new Vector3(width, height, 1f);
        collider.center = new Vector3((min.x + max.x) / 2f, (min.y + max.y) / 2f, 0f);

        return shapeObj;
    }
}

[System.Serializable]
public struct ShapeTemplate
{
    public string name;
    public Vector2Int[] offsets;

    public ShapeTemplate(string name, Vector2Int[] offsets)
    {
        this.name = name;
        this.offsets = offsets;
    }
}
