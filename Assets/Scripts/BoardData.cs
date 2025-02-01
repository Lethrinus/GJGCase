using UnityEngine;

[System.Serializable]
public class BoardData
{
    public int rows;
    public int columns;
    public float blockWidth = 2.3f;
    public float blockHeight = 2.3f;
    public BlockBehavior[] blockGrid;
    public bool[] cellMask; 

    public void Initialize(int r, int c, float width, float height, bool[] mask = null)
    {
        rows = r;
        columns = c;
        blockWidth = width;
        blockHeight = height;
        blockGrid = new BlockBehavior[rows * columns];
        cellMask = mask;
    }

    public int GetIndex(int row, int col)
    {
        return row * columns + col;
    }

    public bool IsValidCell(int row, int col)
    {
        if (cellMask == null || cellMask.Length == 0) return true;
        
        int index = GetIndex(row, col);
        if (index < 0 || index >= cellMask.Length) return false;
        return cellMask[index];
    }

    public Vector2 GetBlockPosition(int row, int col)
    {
        float x = col * blockWidth;
        float y = (rows - 1 - row) * blockHeight;
        return new Vector2(x, y);
    }
}