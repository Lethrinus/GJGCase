using UnityEngine;

/// <summary>
/// Stores board size, the grid, and utility methods for indexing.
/// </summary>
[System.Serializable]
public class BoardData
{
    [HideInInspector] public int rows;
    [HideInInspector] public int columns;
    [HideInInspector] public float blockSize = 1f;

    /// <summary>
    /// Internal storage for block references in the board.
    /// </summary>
    public GameObject[] blockGrid;

    /// <summary>
    /// Initialize the board data arrays with the given dimensions.
    /// </summary>
    public void Initialize(int rows, int columns, float blockSize)
    {
        this.rows = rows;
        this.columns = columns;
        this.blockSize = blockSize;

        blockGrid = new GameObject[rows * columns];
    }

    /// <summary>
    /// Converts (row, col) to index in blockGrid array.
    /// </summary>
    public int GetIndex(int row, int col)
    {
        return row * columns + col;
    }

    /// <summary>
    /// Returns the world position for a block at (row, col).
    /// </summary>
    public Vector2 GetBlockPosition(int row, int col)
    {
        float x = col * blockSize;
        // We place row=0 at bottom, so invert row:
        float y = (rows - 1 - row) * blockSize;
        return new Vector2(x, y);
    }
}