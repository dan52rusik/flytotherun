using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid")]
    public int rows = 8;
    public int columns = 8;
    public float cellSize = 1f;
    public Vector2 startPosition;

    [Header("Visuals")]
    public GameObject cellPrefab;
    public GameObject blockPrefab;
    public ParticleSystem clearEffectPrefab;

    [Header("UI")]
    public Text scoreText;

    private bool[,] grid;
    private GameObject[,] gridVisuals;
    private readonly Queue<GameObject> blockPool = new Queue<GameObject>();

    private const float cellZ = 0.15f;
    private const float placedBlockZ = -0.15f;
    private const int pointsPerPlacedCell = 5;
    private const int pointsPerClearedLine = 120;
    private const int pointsPerConsoleCycle = 250;

    private int score;
    private bool hasUsedContinue;
    private bool isGameOverFlowActive;
    private bool isWaitingForRewardedContinue;

    public bool IsInteractionLocked => isGameOverFlowActive || isWaitingForRewardedContinue;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        MatrixTheme.ApplyCameraTheme();
        MatrixConsoleUI.EnsureExists();
        MatrixGameOverUI.EnsureExists().Hide();

        if (scoreText == null)
        {
            MatrixScoreUI scoreUI = MatrixScoreUI.EnsureExists();
            scoreText = scoreUI.scoreValueText;
        }

        MatrixConsoleUI.onConsoleCycleCompleted += HandleConsoleCycleCompleted;
#if RewardedAdv_yg
        YG.YG2.onErrorRewardedAdv += HandleRewardedContinueError;
#endif
        InitializeGrid();
        UpdateScoreUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        MatrixConsoleUI.onConsoleCycleCompleted -= HandleConsoleCycleCompleted;
#if RewardedAdv_yg
        YG.YG2.onErrorRewardedAdv -= HandleRewardedContinueError;
#endif
    }

    private void InitializeGrid()
    {
        grid = new bool[columns, rows];
        gridVisuals = new GameObject[columns, rows];

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 cellPosition = GetWorldPosition(new Vector2Int(x, y));
                cellPosition.z = cellZ;
                GameObject cell = Instantiate(cellPrefab, cellPosition, Quaternion.identity, transform);
                MatrixTheme.ApplyToObject(cell, MatrixSurfaceType.Cell);
            }
        }
    }

    private GameObject GetBlockFromPool(Vector3 position)
    {
        position.z = placedBlockZ;

        if (blockPool.Count > 0)
        {
            GameObject block = blockPool.Dequeue();
            block.transform.position = position;
            block.SetActive(true);
            MatrixTheme.ApplyToObject(block, MatrixSurfaceType.Block);
            return block;
        }

        GameObject createdBlock = Instantiate(blockPrefab, position, Quaternion.identity, transform);
        MatrixTheme.ApplyToObject(createdBlock, MatrixSurfaceType.Block);
        return createdBlock;
    }

    private void ReturnBlockToPool(GameObject block)
    {
        block.SetActive(false);
        blockPool.Enqueue(block);
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(startPosition.x + gridPos.x * cellSize, startPosition.y + gridPos.y * cellSize, 0f);
    }

    public Vector2Int GetGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - startPosition.x) / cellSize);
        int y = Mathf.RoundToInt((worldPos.y - startPosition.y) / cellSize);
        return new Vector2Int(x, y);
    }

    public bool CanPlaceShape(Shape shape, Vector2Int originGridPos)
    {
        if (shape == null || shape.blockOffsets == null)
            return false;

        return CanPlaceOffsets(shape.blockOffsets, originGridPos);
    }

    public bool CanPlaceOffsets(Vector2Int[] offsets, Vector2Int originGridPos)
    {
        if (offsets == null || offsets.Length == 0)
            return false;

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2Int pos = originGridPos + offsets[i];
            if (pos.x < 0 || pos.x >= columns || pos.y < 0 || pos.y >= rows)
                return false;

            if (grid[pos.x, pos.y])
                return false;
        }

        return true;
    }

    public bool HasAnyPlacement(Vector2Int[] offsets)
    {
        if (offsets == null || offsets.Length == 0)
            return false;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (CanPlaceOffsets(offsets, new Vector2Int(x, y)))
                    return true;
            }
        }

        return false;
    }

    public void PlaceShape(Shape shape, Vector2Int originGridPos)
    {
        Vector2Int[] positions = shape.GetGridPositions(originGridPos);
        foreach (Vector2Int pos in positions)
        {
            grid[pos.x, pos.y] = true;
            GameObject block = GetBlockFromPool(GetWorldPosition(pos));
            gridVisuals[pos.x, pos.y] = block;
        }

        AddScore(positions.Length * pointsPerPlacedCell);
        CheckAndClearLines();
    }

    private void CheckAndClearLines()
    {
        List<int> columnsToClear = new List<int>();
        List<int> rowsToClear = new List<int>();

        for (int x = 0; x < columns; x++)
        {
            bool isFull = true;
            for (int y = 0; y < rows; y++)
            {
                if (!grid[x, y])
                {
                    isFull = false;
                    break;
                }
            }

            if (isFull)
                columnsToClear.Add(x);
        }

        for (int y = 0; y < rows; y++)
        {
            bool isFull = true;
            for (int x = 0; x < columns; x++)
            {
                if (!grid[x, y])
                {
                    isFull = false;
                    break;
                }
            }

            if (isFull)
                rowsToClear.Add(y);
        }

        int comboCount = columnsToClear.Count + rowsToClear.Count;
        if (comboCount > 0)
        {
            AddScore(comboCount * pointsPerClearedLine);
            AudioManager.Instance?.PlayClear(comboCount);

            foreach (int x in columnsToClear)
                ClearColumn(x);

            foreach (int y in rowsToClear)
                ClearRow(y);
        }

        SpawnManager.Instance?.CheckCanPlay();
    }

    private void ClearColumn(int x)
    {
        for (int y = 0; y < rows; y++)
            ClearCell(x, y);
    }

    private void ClearRow(int y)
    {
        for (int x = 0; x < columns; x++)
            ClearCell(x, y);
    }

    private void ClearCell(int x, int y)
    {
        if (!grid[x, y])
            return;

        grid[x, y] = false;

        if (gridVisuals[x, y] != null)
        {
            MatrixConsoleUI.EmitFromWorld(gridVisuals[x, y].transform.position);
            ReturnBlockToPool(gridVisuals[x, y]);
            gridVisuals[x, y] = null;
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    private void AddScore(int value)
    {
        if (value <= 0)
            return;

        score += value;
        UpdateScoreUI();
    }

    private void HandleConsoleCycleCompleted(int cycleNumber)
    {
        AddScore(pointsPerConsoleCycle);
    }

    public void GameOver()
    {
        if (isGameOverFlowActive || isWaitingForRewardedContinue)
            return;

        Debug.Log("GAME OVER! Score: " + score);
        AudioManager.Instance?.PlayGameOver();

        isGameOverFlowActive = true;

        MatrixGameOverUI popup = MatrixGameOverUI.EnsureExists();
        popup.Show(score, !hasUsedContinue, RequestRewardedContinue, RestartGame);
    }

    private void RequestRewardedContinue()
    {
        if (hasUsedContinue || isWaitingForRewardedContinue)
            return;

        isWaitingForRewardedContinue = true;
        MatrixGameOverUI.EnsureExists().SetBusy(true, "opening reward stream...");

#if RewardedAdv_yg
        YG.YG2.RewardedAdvShow("continue_run", HandleRewardedContinueGranted);
#else
        HandleRewardedContinueGranted();
#endif
    }

    private void HandleRewardedContinueGranted()
    {
        hasUsedContinue = true;
        isWaitingForRewardedContinue = false;
        isGameOverFlowActive = false;
        MatrixGameOverUI.EnsureExists().Hide();

        bool continueSpawned = SpawnManager.Instance != null && SpawnManager.Instance.RespawnPlayableShapesForContinue();
        if (!continueSpawned)
            RestartGame();
    }

    private void HandleRewardedContinueError()
    {
        if (!isWaitingForRewardedContinue)
            return;

        isWaitingForRewardedContinue = false;
        isGameOverFlowActive = true;
        MatrixGameOverUI.EnsureExists().SetBusy(false, "reward stream unavailable");
    }

    private void RestartGame()
    {
        isWaitingForRewardedContinue = false;
        isGameOverFlowActive = false;
        MatrixGameOverUI.EnsureExists().Hide();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
