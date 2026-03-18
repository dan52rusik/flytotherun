using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Shape))]
[RequireComponent(typeof(BoxCollider))]
public class ShapeDragger : MonoBehaviour
{
    private Shape shape;
    private Vector3 startPos;
    private Vector3 spawnScale;
    private bool isDragging;
    private Camera mainCam;
    private Vector3 dragOffset;

    [Header("Return")]
    public float returnSpeed = 15f;

    [Header("Ghost Preview")]
    public GameObject previewBlockPrefab;
    private GameObject[] ghostBlocks;

    private static ShapeDragger currentlyDragged;

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
            transform.position = Vector3.Lerp(transform.position, startPos, Time.deltaTime * returnSpeed);
        }
    }

    private Vector2 GetPointerPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Vector2.zero;
    }

    private bool WasPointerPressedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        return false;
    }

    private bool WasPointerReleasedThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            return true;

        return false;
    }

    private void HandleInput()
    {
        if (GridManager.Instance != null && GridManager.Instance.IsInteractionLocked && !isDragging)
            return;

        if (mainCam == null)
            return;

        if (WasPointerPressedThisFrame() && !isDragging && currentlyDragged == null)
        {
            Vector2 screenPos = GetPointerPosition();
            if (TryGetShapeUnderPointer(screenPos, out RaycastHit hit, out ShapeDragger target) && target == this)
                StartDragging(hit.point);
        }

        if (WasPointerReleasedThisFrame() && isDragging)
            StopDragging();
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

    private void StartDragging(Vector3 hitPoint)
    {
        isDragging = true;
        currentlyDragged = this;

        AudioManager.Instance?.PlayGrab();

        dragOffset = transform.position - hitPoint;
        dragOffset.z = 0f;
        transform.localScale = Vector3.one;

        Vector3 pos = transform.position;
        pos.z = -1f;
        transform.position = pos;

        CreateGhostPreview();
    }

    private void StopDragging()
    {
        isDragging = false;
        currentlyDragged = null;
        DestroyGhostPreview();

        Vector3 dropPos = transform.position;
        dropPos.z = 0f;
        transform.position = dropPos;

        Vector2Int gridPos = GridManager.Instance.GetGridPosition(transform.position);

        if (GridManager.Instance.CanPlaceShape(shape, gridPos))
        {
            AudioManager.Instance?.PlayDrop();
            transform.localScale = Vector3.one;
            SpawnManager.Instance?.UnregisterShape(shape);
            GridManager.Instance.PlaceShape(shape, gridPos);
            Destroy(gameObject);
        }
        else
        {
            AudioManager.Instance?.PlayReturn();
            transform.position = startPos;
            transform.localScale = spawnScale;
        }
    }

    private void DragProcessing()
    {
        Vector2 screenPos = GetPointerPosition();
        Vector3 screenPos3 = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCam.transform.position.z));
        Vector3 worldPos = mainCam.ScreenToWorldPoint(screenPos3);

        worldPos += dragOffset;
        worldPos.y += 1.5f;
        worldPos.z = -1f;

        transform.position = worldPos;
    }

    private void CreateGhostPreview()
    {
        if (previewBlockPrefab == null)
            return;

        ghostBlocks = new GameObject[shape.blockOffsets.Length];
        for (int i = 0; i < ghostBlocks.Length; i++)
        {
            ghostBlocks[i] = Instantiate(previewBlockPrefab);
            MatrixTheme.ApplyToObject(ghostBlocks[i], MatrixSurfaceType.Preview);
            ghostBlocks[i].SetActive(false);
        }
    }

    private void UpdateGhostPreview()
    {
        if (ghostBlocks == null)
            return;

        Vector2Int gridPos = GridManager.Instance.GetGridPosition(transform.position);

        if (GridManager.Instance.CanPlaceShape(shape, gridPos))
        {
            Vector2Int[] positions = shape.GetGridPositions(gridPos);
            for (int i = 0; i < positions.Length; i++)
            {
                ghostBlocks[i].SetActive(true);
                Vector3 ghostPos = GridManager.Instance.GetWorldPosition(positions[i]);
                ghostPos.z = -0.5f;
                ghostBlocks[i].transform.position = ghostPos;
            }
        }
        else
        {
            for (int i = 0; i < ghostBlocks.Length; i++)
            {
                if (ghostBlocks[i] != null)
                    ghostBlocks[i].SetActive(false);
            }
        }
    }

    private void DestroyGhostPreview()
    {
        if (ghostBlocks == null)
            return;

        for (int i = 0; i < ghostBlocks.Length; i++)
        {
            if (ghostBlocks[i] != null)
                Destroy(ghostBlocks[i]);
        }

        ghostBlocks = null;
    }
}
