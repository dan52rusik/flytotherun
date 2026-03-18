using UnityEngine;

/// <summary>
/// Данные о форме блока. Форма задается массивом локальных координат (смещений).
/// Например, для L-фигуры это могут быть: (0,0), (0,1), (0,2), (1,0)
/// </summary>
public class Shape : MonoBehaviour
{
    [Header("Настройки формы")]
    [Tooltip("Координаты клеток, из которых состоит фигура, относительно её центра")]
    public Vector2Int[] blockOffsets;

    /// <summary>
    /// Возвращает абсолютные координаты на сетке, если фигура будет помещена в originGridPos.
    /// </summary>
    /// <param name="originGridPos">Координата ячейки, над которой "висит" центр фигуры</param>
    public Vector2Int[] GetGridPositions(Vector2Int originGridPos)
    {
        Vector2Int[] positions = new Vector2Int[blockOffsets.Length];
        for (int i = 0; i < blockOffsets.Length; i++)
        {
            positions[i] = originGridPos + blockOffsets[i];
        }
        return positions;
    }
}
