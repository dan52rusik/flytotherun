using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawn")]
    [Tooltip("Bottom scene points where playable shapes appear.")]
    public Transform[] spawnPoints;

    private readonly List<Shape> currentShapes = new List<Shape>();
    private bool isWaitingForSpawn;

    private const float spawnShapeScale = 0.6f;
    private const float spawnFieldGap = 0.35f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        isWaitingForSpawn = true;
        Invoke(nameof(SpawnNewShapes), 0.1f);
    }

    private void Update()
    {
        currentShapes.RemoveAll(shape => shape == null);

        if (currentShapes.Count == 0 && !isWaitingForSpawn)
        {
            isWaitingForSpawn = true;
            Invoke(nameof(SpawnNewShapes), 0.3f);
        }
    }

    private void SpawnNewShapes()
    {
        isWaitingForSpawn = false;
        SpawnShapeSet(preferPlayableOnly: false, ensurePlayableOption: true);
    }

    public bool RespawnPlayableShapesForContinue()
    {
        CancelInvoke(nameof(SpawnNewShapes));
        isWaitingForSpawn = false;
        ClearCurrentShapes();
        return SpawnShapeSet(preferPlayableOnly: true, ensurePlayableOption: true);
    }

    public void UnregisterShape(Shape shape)
    {
        if (shape == null)
            return;

        currentShapes.Remove(shape);
    }

    public void CheckCanPlay()
    {
        currentShapes.RemoveAll(shape => shape == null);

        if (currentShapes.Count == 0)
            return;

        if (GridManager.Instance == null)
            return;

        foreach (Shape shape in currentShapes)
        {
            if (shape == null || shape.blockOffsets == null)
                continue;

            if (GridManager.Instance.HasAnyPlacement(shape.blockOffsets))
                return;
        }

        GridManager.Instance.GameOver();
    }

    private bool SpawnShapeSet(bool preferPlayableOnly, bool ensurePlayableOption)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("SpawnManager: spawnPoints is not configured.");
            return false;
        }

        if (ShapeFactory.Instance == null)
        {
            Debug.LogError("SpawnManager: ShapeFactory.Instance is missing in the scene.");
            isWaitingForSpawn = true;
            Invoke(nameof(SpawnNewShapes), 0.3f);
            return false;
        }

        List<ShapeTemplate> selectedTemplates = BuildTemplateSet(preferPlayableOnly, ensurePlayableOption, spawnPoints.Length);
        List<GameObject> spawnedShapes = new List<GameObject>();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogError($"SpawnManager: spawnPoints[{i}] is not assigned.");
                continue;
            }

            GameObject spawnedShape = ShapeFactory.Instance.SpawnShape(selectedTemplates[i], spawnPoints[i].position);
            if (spawnedShape == null)
            {
                Debug.LogError($"SpawnManager: failed to spawn shape at point {i}.");
                continue;
            }

            spawnedShape.transform.localScale = Vector3.one * spawnShapeScale;
            spawnedShapes.Add(spawnedShape);

            Shape shapeComp = spawnedShape.GetComponent<Shape>();
            if (shapeComp != null)
                currentShapes.Add(shapeComp);
        }

        LayoutSpawnedShapes(spawnedShapes);
        CheckCanPlay();
        return currentShapes.Count > 0;
    }

    private List<ShapeTemplate> BuildTemplateSet(bool preferPlayableOnly, bool ensurePlayableOption, int count)
    {
        List<ShapeTemplate> templates = new List<ShapeTemplate>(count);
        List<ShapeTemplate> playableTemplates = GetPlayableTemplates();
        bool shouldInjectPlayable = ensurePlayableOption && playableTemplates.Count > 0;

        for (int i = 0; i < count; i++)
        {
            ShapeTemplate selectedTemplate;

            if (preferPlayableOnly && playableTemplates.Count > 0)
            {
                selectedTemplate = playableTemplates[Random.Range(0, playableTemplates.Count)];
            }
            else if (shouldInjectPlayable)
            {
                selectedTemplate = playableTemplates[Random.Range(0, playableTemplates.Count)];
                shouldInjectPlayable = false;
            }
            else
            {
                selectedTemplate = ShapeFactory.Instance.GetRandomTemplate();
            }

            templates.Add(selectedTemplate);
        }

        return templates;
    }

    private List<ShapeTemplate> GetPlayableTemplates()
    {
        List<ShapeTemplate> playableTemplates = new List<ShapeTemplate>();
        if (GridManager.Instance == null || ShapeFactory.Instance == null)
            return playableTemplates;

        IReadOnlyList<ShapeTemplate> allTemplates = ShapeFactory.Instance.GetTemplates();
        for (int i = 0; i < allTemplates.Count; i++)
        {
            if (GridManager.Instance.HasAnyPlacement(allTemplates[i].offsets))
                playableTemplates.Add(allTemplates[i]);
        }

        return playableTemplates;
    }

    private void ClearCurrentShapes()
    {
        currentShapes.RemoveAll(shape => shape == null);

        for (int i = 0; i < currentShapes.Count; i++)
        {
            if (currentShapes[i] != null)
                Destroy(currentShapes[i].gameObject);
        }

        currentShapes.Clear();
    }

    private void LayoutSpawnedShapes(List<GameObject> spawnedShapes)
    {
        if (spawnedShapes == null || spawnedShapes.Count == 0)
            return;

        for (int i = 0; i < spawnedShapes.Count; i++)
        {
            if (i >= spawnPoints.Length || spawnPoints[i] == null)
                continue;

            GameObject shape = spawnedShapes[i];
            Bounds bounds = GetShapeBounds(shape);
            Vector3 slotPosition = spawnPoints[i].position;
            Vector3 position = shape.transform.position;

            position.x += slotPosition.x - bounds.center.x;
            position.y += slotPosition.y - bounds.min.y;
            position.z = 0f;

            if (GridManager.Instance != null)
            {
                float maxAllowedTop = GridManager.Instance.startPosition.y - spawnFieldGap;
                float shiftedTop = bounds.max.y + (position.y - shape.transform.position.y);
                if (shiftedTop > maxAllowedTop)
                    position.y -= shiftedTop - maxAllowedTop;
            }

            shape.transform.position = position;
        }
    }

    private Bounds GetShapeBounds(GameObject shapeObject)
    {
        Renderer[] renderers = shapeObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(shapeObject.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }
}
