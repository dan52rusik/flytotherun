using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Менеджер спавна фигур. Выдаёт игроку по 3 случайные фигуры.
/// Новые фигуры появляются только когда все 3 предыдущие установлены.
/// Теперь использует ShapeFactory для генерации фигур из шаблонов.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Настройки спавна")]
    [Tooltip("Точки на экране внизу, где появляются фигуры (нужно 3 штуки)")]
    public Transform[] spawnPoints;

    // Список текущих активных фигур, которые доступны игроку
    private List<Shape> currentShapes = new List<Shape>();

    // Флаг, чтобы не спавнить фигуры повторно в одном кадре
    private bool isWaitingForSpawn = false;
    private const float spawnShapeScale = 0.6f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Небольшая задержка перед первым спавном, чтобы ShapeFactory успел инициализироваться
        isWaitingForSpawn = true;
        Invoke(nameof(SpawnNewShapes), 0.1f);
    }

    private void Update()
    {
        // Очищаем список от уничтоженных фигур (они удаляются при установке на поле)
        currentShapes.RemoveAll(shape => shape == null);

        // Если все фигуры выставлены — спавним новую тройку
        if (currentShapes.Count == 0 && !isWaitingForSpawn)
        {
            isWaitingForSpawn = true;
            // Маленькая задержка перед появлением новых фигур (фидбек для игрока)
            Invoke(nameof(SpawnNewShapes), 0.3f);
        }
    }

    /// <summary>
    /// Создаёт 3 новые случайные фигуры через ShapeFactory
    /// </summary>
    private void SpawnNewShapes()
    {
        isWaitingForSpawn = false;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("SpawnManager: spawnPoints is not configured.");
            return;
        }

        if (ShapeFactory.Instance == null)
        {
            Debug.LogError("SpawnManager: ShapeFactory.Instance is missing in the scene.");
            isWaitingForSpawn = true;
            Invoke(nameof(SpawnNewShapes), 0.3f);
            return;
        }

        List<GameObject> spawnedShapes = new List<GameObject>();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogError($"SpawnManager: spawnPoints[{i}] is not assigned.");
                continue;
            }

            // Создаём случайную фигуру через фабрику
            GameObject spawnedShape = ShapeFactory.Instance.SpawnRandomShape(spawnPoints[i].position);

            if (spawnedShape == null)
            {
                Debug.LogError($"SpawnManager: failed to spawn shape at point {i}.");
                continue;
            }

            // Масштабируем фигуру, чтобы она выглядела компактнее в области спавна
            spawnedShape.transform.localScale = Vector3.one * spawnShapeScale;
            spawnedShapes.Add(spawnedShape);

            // Сохраняем ссылку
            Shape shapeComp = spawnedShape.GetComponent<Shape>();
            if (shapeComp != null)
            {
                currentShapes.Add(shapeComp);
            }
        }

        LayoutSpawnedShapes(spawnedShapes);

        // Проверяем, есть ли хотя бы один допустимый ход
        CheckCanPlay();
    }

    private void LayoutSpawnedShapes(List<GameObject> spawnedShapes)
    {
        if (spawnedShapes == null || spawnedShapes.Count == 0)
        {
            return;
        }

        for (int i = 0; i < spawnedShapes.Count; i++)
        {
            if (i >= spawnPoints.Length || spawnPoints[i] == null)
            {
                continue;
            }

            GameObject shape = spawnedShapes[i];
            Bounds bounds = GetShapeBounds(shape);
            Vector3 slotPosition = spawnPoints[i].position;
            Vector3 position = shape.transform.position;

            // Центрируем фигуру относительно слота по реальной ширине,
            // а низ фигуры кладём на высоту spawn point.
            position.x += slotPosition.x - bounds.center.x;
            position.y += slotPosition.y - bounds.min.y;
            position.z = 0f;

            shape.transform.position = position;
        }
    }

    private Bounds GetShapeBounds(GameObject shapeObject)
    {
        Renderer[] renderers = shapeObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(shapeObject.transform.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    /// <summary>
    /// Проверяет, можно ли поставить ХОТЯ БЫ ОДНУ из текущих фигур на поле
    /// </summary>
    public void CheckCanPlay()
    {
        // Очищаем null-ы перед проверкой
        currentShapes.RemoveAll(shape => shape == null);

        // Если фигур нет — не проверяем (скоро заспавнятся новые)
        if (currentShapes.Count == 0) return;

        bool canPlaceAny = false;

        foreach (var shape in currentShapes)
        {
            if (shape == null) continue;

            for (int x = 0; x < GridManager.Instance.columns; x++)
            {
                for (int y = 0; y < GridManager.Instance.rows; y++)
                {
                    if (GridManager.Instance.CanPlaceShape(shape, new Vector2Int(x, y)))
                    {
                        canPlaceAny = true;
                        break;
                    }
                }
                if (canPlaceAny) break;
            }
            if (canPlaceAny) break;
        }

        if (!canPlaceAny)
        {
            GridManager.Instance.GameOver();
        }
    }
}
