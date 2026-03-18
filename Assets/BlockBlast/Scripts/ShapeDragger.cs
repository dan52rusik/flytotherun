using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управление перетаскиванием фигуры (Drag-and-Drop).
/// Использует New Input System (Mouse + Touchscreen).
/// </summary>
[RequireComponent(typeof(Shape))]
[RequireComponent(typeof(BoxCollider))]
public class ShapeDragger : MonoBehaviour
{
    private Shape shape;
    private Vector3 startPos; // Позиция в Spawn Area, куда фигура вернётся
    private Vector3 spawnScale;
    private bool isDragging = false;
    private Camera mainCam;

    // Смещение между точкой клика и центром фигуры (чтобы фигура не прыгала)
    private Vector3 dragOffset;

    [Header("Настройки возврата")]
    public float returnSpeed = 15f;

    [Header("Ghost Preview")]
    public GameObject previewBlockPrefab; // Полупрозрачный куб-призрак
    private GameObject[] ghostBlocks;

    // Статическая ссылка на текущую перетаскиваемую фигуру (чтобы не таскать две одновременно)
    private static ShapeDragger currentlyDragged = null;

    private void Start()
    {
        shape = GetComponent<Shape>();
        mainCam = Camera.main;
        startPos = transform.position;
        spawnScale = transform.localScale;
    }

    private void Update()
    {
        HandleInput();

        if (isDragging)
        {
            DragProcessing();
            UpdateGhostPreview();
        }
        else if (Vector3.Distance(transform.position, startPos) > 0.01f)
        {
            // Плавный возврат на исходную позицию (эффект пружинки)
            transform.position = Vector3.Lerp(transform.position, startPos, Time.deltaTime * returnSpeed);
        }
    }

    /// <summary>
    /// Получаем текущую позицию указателя (мышь или тач) через New Input System
    /// </summary>
    private Vector2 GetPointerPosition()
    {
        // Сначала проверяем тачскрин
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }

        // Иначе — мышь
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        return Vector2.zero;
    }

    /// <summary>
    /// Проверяем, нажал ли пользователь (тач или мышь)
    /// </summary>
    private bool WasPointerPressedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        return false;
    }

    /// <summary>
    /// Проверяем, отпустил ли пользователь (тач или мышь)
    /// </summary>
    private bool WasPointerReleasedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            return true;

        return false;
    }

    /// <summary>
    /// Обработка ввода: поддержка и мыши, и тачскрина через New Input System
    /// </summary>
    private void HandleInput()
    {
        if (mainCam == null)
        {
            return;
        }

        // Нажатие (начало перетаскивания)
        if (WasPointerPressedThisFrame() && !isDragging && currentlyDragged == null)
        {
            Vector2 screenPos = GetPointerPosition();
            RaycastHit hit;
            ShapeDragger target;

            if (TryGetShapeUnderPointer(screenPos, out hit, out target) && target == this)
            {
                StartDragging(hit.point);
            }
        }

        // Отпускание (конец перетаскивания)
        if (WasPointerReleasedThisFrame() && isDragging)
        {
            StopDragging();
        }
    }

    private bool TryGetShapeUnderPointer(Vector2 screenPos, out RaycastHit targetHit, out ShapeDragger targetDragger)
    {
        Ray ray = mainCam.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            ShapeDragger dragger = hits[i].collider.GetComponentInParent<ShapeDragger>();
            if (dragger != null)
            {
                targetHit = hits[i];
                targetDragger = dragger;
                return true;
            }
        }

        targetHit = default;
        targetDragger = null;
        return false;
    }

    /// <summary>
    /// Начать перетаскивание фигуры
    /// </summary>
    private void StartDragging(Vector3 hitPoint)
    {
        isDragging = true;
        currentlyDragged = this;

        AudioManager.Instance?.PlayGrab(); // Звук взятия фигуры

        // Запоминаем смещение, чтобы фигура не прыгала к курсору
        dragOffset = transform.position - hitPoint;
        dragOffset.z = 0;

        // Сбрасываем масштаб к нормальному (фигуры в зоне спавна уменьшены)
        transform.localScale = Vector3.one;

        // Поднимаем фигуру ближе к камере, чтобы она была поверх всего
        Vector3 pos = transform.position;
        pos.z = -1f;
        transform.position = pos;

        CreateGhostPreview();
    }

    /// <summary>
    /// Закончить перетаскивание: или ставим фигуру, или возвращаем на место
    /// </summary>
    private void StopDragging()
    {
        isDragging = false;
        currentlyDragged = null;
        DestroyGhostPreview();

        // Возвращаем z на 0
        Vector3 dropPos = transform.position;
        dropPos.z = 0;
        transform.position = dropPos;

        // Узнаём, над какой ячейкой сетки находится центр фигуры
        Vector2Int gridPos = GridManager.Instance.GetGridPosition(transform.position);

        // Если туда можно встать — ставим!
        if (GridManager.Instance.CanPlaceShape(shape, gridPos))
        {
            AudioManager.Instance?.PlayDrop(); // Звук успешной установки
            transform.localScale = Vector3.one;
            GridManager.Instance.PlaceShape(shape, gridPos);
            Destroy(gameObject); // Фигура теперь часть поля
        }
        else
        {
            AudioManager.Instance?.PlayReturn(); // Звук возврата на базу
            // Если нельзя — возвращаем на место
            transform.position = startPos;
            transform.localScale = spawnScale;
        }
    }

    /// <summary>
    /// Перемещение фигуры за курсором
    /// </summary>
    private void DragProcessing()
    {
        Vector2 screenPos = GetPointerPosition();
        Vector3 screenPos3 = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCam.transform.position.z));
        Vector3 worldPos = mainCam.ScreenToWorldPoint(screenPos3);

        // Применяем смещение и поднимаем фигуру выше пальца
        worldPos += dragOffset;
        worldPos.y += 1.5f; // Чтобы палец не закрывал фигуру
        worldPos.z = -1f;   // Фигура поверх поля

        transform.position = worldPos;
    }

    #region Ghost Preview

    private void CreateGhostPreview()
    {
        if (previewBlockPrefab == null) return;

        ghostBlocks = new GameObject[shape.blockOffsets.Length];
        for (int i = 0; i < ghostBlocks.Length; i++)
        {
            ghostBlocks[i] = Instantiate(previewBlockPrefab);
            ghostBlocks[i].SetActive(false);
        }
    }

    private void UpdateGhostPreview()
    {
        if (ghostBlocks == null) return;

        Vector2Int gridPos = GridManager.Instance.GetGridPosition(transform.position);

        if (GridManager.Instance.CanPlaceShape(shape, gridPos))
        {
            // Место валидно — показываем призрака на сетке
            Vector2Int[] positions = shape.GetGridPositions(gridPos);
            for (int i = 0; i < positions.Length; i++)
            {
                ghostBlocks[i].SetActive(true);
                Vector3 ghostPos = GridManager.Instance.GetWorldPosition(positions[i]);
                ghostPos.z = -0.5f; // Чуть ближе к камере
                ghostBlocks[i].transform.position = ghostPos;
            }
        }
        else
        {
            foreach (var gb in ghostBlocks)
                if (gb != null) gb.SetActive(false);
        }
    }

    private void DestroyGhostPreview()
    {
        if (ghostBlocks == null) return;
        foreach (var gb in ghostBlocks)
            if (gb != null) Destroy(gb);
        ghostBlocks = null;
    }

    #endregion
}
