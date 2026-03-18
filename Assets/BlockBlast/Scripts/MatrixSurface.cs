using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class MatrixSurface : MonoBehaviour
{
    public MatrixSurfaceType surfaceType = MatrixSurfaceType.Block;

    private Renderer cachedRenderer;
    private Material runtimeMaterial;
    private float scrollSpeed;
    private float scrollOffset;

    private void Awake()
    {
        Setup();
    }

    private void OnEnable()
    {
        Setup();
    }

    public void Refresh()
    {
        Setup(true);
    }

    private void Update()
    {
        if (runtimeMaterial == null)
            return;

        scrollOffset += Time.deltaTime * scrollSpeed;
        MatrixTheme.ConfigureMaterial(runtimeMaterial, surfaceType, scrollOffset);
    }

    private void Setup(bool forceNewMaterial = false)
    {
        if (cachedRenderer == null)
            cachedRenderer = GetComponent<Renderer>();

        if (cachedRenderer == null)
            return;

        if (runtimeMaterial == null || forceNewMaterial)
        {
            runtimeMaterial = new Material(cachedRenderer.material);
            cachedRenderer.material = runtimeMaterial;
            if (surfaceType == MatrixSurfaceType.Cell)
            {
                scrollSpeed = 0.015f;
                scrollOffset = 0f;
            }
            else if (surfaceType == MatrixSurfaceType.Preview)
            {
                scrollSpeed = 0.08f;
                scrollOffset = 0.15f;
            }
            else
            {
                scrollSpeed = Random.Range(0.05f, 0.14f);
                scrollOffset = Random.Range(0f, 0.35f);
            }
        }

        MatrixTheme.ConfigureMaterial(runtimeMaterial, surfaceType, scrollOffset);
    }
}
