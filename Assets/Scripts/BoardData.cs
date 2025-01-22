using UnityEngine;

[System.Serializable]
public class BoardData
{
    public int rows;
    public int columns;
    public float blockSize = 2.3f;
    public BlockBehavior[] blockGrid;
    public void Initialize(int r, int c, float size)
    {
        rows = r;
        columns = c;
        blockSize = size;
        blockGrid = new BlockBehavior[rows * columns];
    }
    public int GetIndex(int row, int col)
    {
        return row * columns + col;
    }
    public Vector2 GetBlockPosition(int row, int col)
    {
        float x = col * blockSize;
        float y = (rows - 1 - row) * 2.555f;
        return new Vector2(x, y);
    }
}
