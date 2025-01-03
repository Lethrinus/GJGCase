using UnityEngine;
using System.Collections.Generic;

public class GameBoard : MonoBehaviour
{
    public GameObject blockPrefab;      // Block prefab

    [Header("Sprite Setup")]
    [SerializeField]
    private List<Sprite[]> colorSprites = new List<Sprite[]>(); // List of sprite arrays for each color

    public int rows = 10;               // Number of rows
    public int columns = 12;            // Number of columns
    public float blockSize = 1.0f;      // Size of each block

    private GameObject[,] blocks;       // 2D array to store blocks

    void Start()
    {
        blocks = new GameObject[rows, columns];
        GenerateBoard();
    }

    void GenerateBoard()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                // Calculate block position
                Vector2 position = new Vector2(column * blockSize, row * blockSize);
                GameObject block = Instantiate(blockPrefab, position, Quaternion.identity, transform);

                // Randomly select a color
                int colorIndex = Random.Range(0, colorSprites.Count);
                Sprite[] selectedSprites = colorSprites[colorIndex];

                // Assign the sprites for the selected color
                BlockBehavior blockBehavior = block.GetComponent<BlockBehavior>();
                blockBehavior.SetSprites(selectedSprites[0], selectedSprites[1], selectedSprites[2], selectedSprites[3]);

                // Add the block to the grid
                blocks[row, column] = block;
            }
        }
        CenterGrid();
    }

    void CenterGrid()
    {
        float offsetX = (columns - 1) * blockSize / 2;
        float offsetY = (rows - 1) * blockSize / 2;
        transform.position = new Vector2(-offsetX, -offsetY);
    }


    public List<Vector2Int> DetectGroup(int startRow, int startCol)
    {
        List<Vector2Int> group = new List<Vector2Int>();
        Sprite targetSprite = blocks[startRow, startCol].GetComponent<BlockBehavior>().GetSprite();
        bool[,] visited = new bool[rows, columns];

        void FloodFill(int row, int col)
        {
            if (row < 0 || col < 0 || row >= rows || col >= columns) return;
            if (visited[row, col]) return;

            GameObject block = blocks[row, col];
            if (block == null || block.GetComponent<BlockBehavior>().GetSprite() != targetSprite) return;

            visited[row, col] = true;
            group.Add(new Vector2Int(row, col));

            FloodFill(row + 1, col);
            FloodFill(row - 1, col);
            FloodFill(row, col + 1);
            FloodFill(row, col - 1);
        }

        FloodFill(startRow, startCol);
        return group;
    }

    public void RemoveGroup(List<Vector2Int> group)
    {
        foreach (Vector2Int pos in group)
        {
            Destroy(blocks[pos.x, pos.y]);
            blocks[pos.x, pos.y] = null;
        }
        FillEmptySpaces();

        // Update icons for remaining blocks
        foreach (Vector2Int pos in group)
        {
            if (blocks[pos.x, pos.y] != null)
            {
                BlockBehavior blockBehavior = blocks[pos.x, pos.y].GetComponent<BlockBehavior>();
                blockBehavior.UpdateSpriteBasedOnGroupSize(group.Count);
            }
        }
    }

    void FillEmptySpaces()
    {
        for (int col = 0; col < columns; col++)
        {
            List<GameObject> columnBlocks = new List<GameObject>();

            // Collect blocks in the column
            for (int row = 0; row < rows; row++)
            {
                if (blocks[row, col] != null)
                {
                    columnBlocks.Add(blocks[row, col]);
                    blocks[row, col] = null;
                }
            }

            // Drop blocks to fill gaps
            for (int row = 0; row < columnBlocks.Count; row++)
            {
                blocks[row, col] = columnBlocks[row];
                blocks[row, col].transform.position = new Vector2(col * blockSize, row * blockSize);
            }

            // Add new blocks
            for (int row = columnBlocks.Count; row < rows; row++)
            {
                Vector2 position = new Vector2(col * blockSize, row * blockSize);
                GameObject newBlock = Instantiate(blockPrefab, position, Quaternion.identity, transform);

                int colorIndex = Random.Range(0, colorSprites.Count);
                Sprite[] selectedSprites = colorSprites[colorIndex];

                BlockBehavior blockBehavior = newBlock.GetComponent<BlockBehavior>();
                blockBehavior.SetSprites(selectedSprites[0], selectedSprites[1], selectedSprites[2], selectedSprites[3]);

                blocks[row, col] = newBlock;
            }
        }
    }
}
