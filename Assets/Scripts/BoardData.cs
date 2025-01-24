using UnityEngine;

[System.Serializable]
public class BoardData
{
    public int rows;
    public int columns;
    
    public float blockWidth = 2.3f;
    public float blockHeight = 2.3f;

    public BlockBehavior[] blockGrid;

    public void Initialize(int r, int c, float width, float height)
    {
        rows = r;
        columns = c;
        blockWidth = width;
        blockHeight = height;
        blockGrid = new BlockBehavior[rows * columns];
    }

    public int GetIndex(int row, int col)
    {
        return row * columns + col;
    }

    public Vector2 GetBlockPosition(int row, int col)
    {
        float x = col * blockWidth;
        float y = (rows - 1 - row) * blockHeight;
        return new Vector2(x, y);
    }
}