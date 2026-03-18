using System.Collections.Generic;
using UnityEngine;
// using YG; // <- Заглушка: пространство имен плагина YandexGame SDK
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Настройки сетки")]
    public int rows = 8;
    public int columns = 8;
    public float cellSize = 1f; // Расстояние между центрами ячеек
    public Vector2 startPosition; // Левый нижний угол сетки

    [Header("Ссылки для визуала")]
    public GameObject cellPrefab; // Префаб пустой ячейки поля
    public GameObject blockPrefab; // Префаб кубика (сегмента фигуры), который остается на поле
    public ParticleSystem clearEffectPrefab; // "Сочный" эффект уничтожения
    [Header("UI")]
    public Text scoreText; // Текст для отображения очков

    private bool[,] grid; // Состояние сетки: true = занято
    private GameObject[,] gridVisuals; // Ссылки на визуал кубиков (чтобы потом их удалять)
    
    // Пул для кубиков
    private Queue<GameObject> blockPool = new Queue<GameObject>();
    private const float cellZ = 0.15f;
    private const float placedBlockZ = -0.15f;
    private const int pointsPerPlacedCell = 5;
    private const int pointsPerClearedLine = 120;
    private const int pointsPerConsoleCycle = 250;

    private int score = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        MatrixTheme.ApplyCameraTheme();
        MatrixConsoleUI.EnsureExists();
        if (scoreText == null)
        {
            MatrixScoreUI scoreUI = MatrixScoreUI.EnsureExists();
            scoreText = scoreUI.scoreValueText;
        }
        MatrixConsoleUI.onConsoleCycleCompleted += HandleConsoleCycleCompleted;
        InitializeGrid();
        UpdateScoreUI();
    }

    private void OnDestroy()
    {
        MatrixConsoleUI.onConsoleCycleCompleted -= HandleConsoleCycleCompleted;
    }

    private void InitializeGrid()
    {
        grid = new bool[columns, rows];
        gridVisuals = new GameObject[columns, rows];

        // Создаем подложку поля
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                grid[x, y] = false;
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

    // Перевод координат сетки в мировые (для правильного спавна и отрисовки)
    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(startPosition.x + gridPos.x * cellSize, startPosition.y + gridPos.y * cellSize, 0f);
    }
    
    // Перевод мировых координат курсора в ближайшие координаты сетки
    public Vector2Int GetGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - startPosition.x) / cellSize);
        int y = Mathf.RoundToInt((worldPos.y - startPosition.y) / cellSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Проверяет, свободна ли область на сетке для данной фигуры
    /// </summary>
    public bool CanPlaceShape(Shape shape, Vector2Int originGridPos)
    {
        Vector2Int[] positions = shape.GetGridPositions(originGridPos);
        foreach (Vector2Int pos in positions)
        {
            // Проверка: фигура выходит за границы сетки?
            if (pos.x < 0 || pos.x >= columns || pos.y < 0 || pos.y >= rows)
                return false;
            
            // Проверка: клетка уже занята?
            if (grid[pos.x, pos.y])
                return false;
        }
        return true;
    }

    /// <summary>
    /// Физически устанавливает фигуру на поле
    /// </summary>
    public void PlaceShape(Shape shape, Vector2Int originGridPos)
    {
        Vector2Int[] positions = shape.GetGridPositions(originGridPos);
        foreach (Vector2Int pos in positions)
        {
            grid[pos.x, pos.y] = true;
            
            // "Печатаем" кубики на поле из пула
            GameObject block = GetBlockFromPool(GetWorldPosition(pos));
            gridVisuals[pos.x, pos.y] = block;
        }

        AddScore(positions.Length * pointsPerPlacedCell);
        CheckAndClearLines();
    }

    /// <summary>
    /// Алгоритм проверки сгоревших линий (как строк, так и столбцов)
    /// </summary>
    private void CheckAndClearLines()
    {
        List<int> linesToClearX = new List<int>();
        List<int> linesToClearY = new List<int>();

        // Сканируем столбцы на заполненность
        for (int x = 0; x < columns; x++)
        {
            bool isFull = true;
            for (int y = 0; y < rows; y++)
            {
                if (!grid[x, y]) { isFull = false; break; }
            }
            if (isFull) linesToClearX.Add(x);
        }

        // Сканируем строки на заполненность
        for (int y = 0; y < rows; y++)
        {
            bool isFull = true;
            for (int x = 0; x < columns; x++)
            {
                if (!grid[x, y]) { isFull = false; break; }
            }
            if (isFull) linesToClearY.Add(y);
        }

        int comboCount = linesToClearX.Count + linesToClearY.Count;

        // Если что-то собрали — удаляем и начисляем комбо
        if (comboCount > 0)
        {
            Debug.Log($"COMBO x{comboCount}!");
            AddScore(comboCount * pointsPerClearedLine);
            
            AudioManager.Instance?.PlayClear(comboCount); // Вызов озвучки
            
            foreach (int x in linesToClearX) ClearColumn(x);
            foreach (int y in linesToClearY) ClearRow(y);
        }

        // Вызываем проверку Game Over (может ли игрок сделать ход оставшимися фигурами)
        SpawnManager.Instance.CheckCanPlay();
    }

    private void ClearColumn(int x) { for (int y = 0; y < rows; y++) ClearCell(x, y); }
    private void ClearRow(int y) { for (int x = 0; x < columns; x++) ClearCell(x, y); }

    private void ClearCell(int x, int y)
    {
        if (grid[x, y])
        {
            grid[x, y] = false;
            
            if (gridVisuals[x, y] != null)
            {
                MatrixConsoleUI.EmitFromWorld(gridVisuals[x, y].transform.position);
                    
                ReturnBlockToPool(gridVisuals[x, y]); // Возвращаем кубик в пул, вместо Destroy
                gridVisuals[x, y] = null;
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
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
        Debug.Log("GAME OVER! Ваш счет: " + score);
        AudioManager.Instance?.PlayGameOver();
        
        // --- ЗАГЛУШКА Yandex Games SDK ---
        // Раскомментируйте using YG; наверху
        // И вызывайте показ полноэкранной рекламы и сохранение рекорда
        // YG.YandexGame.FullscreenShow();
        // YG.YandexGame.NewLeaderboardScores("BlockBlastLeaderboard", score);
        // ---------------------------------
    }
}

