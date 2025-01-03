using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class GameBoard : MonoBehaviour
{
    [Header("Block Prefabs (One for Each Color)")]
    public GameObject[] blockPrefabs;

    public int rows = 10;
    public int columns = 12;
    public float blockSize = 1.0f;

    private GameObject[,] blocks;

    void Start()
    {
        blocks = new GameObject[rows, columns];
        GenerateBoard();
    }

    void GenerateBoard()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector2 position = new Vector2(col * blockSize, row * blockSize);
                int colorIndex = Random.Range(0, blockPrefabs.Length);

                GameObject blockPrefab = blockPrefabs[colorIndex];
                if (blockPrefab == null)
                {
                    Debug.LogError("Block prefab is missing or not assigned.");
                    continue;
                }

                GameObject block = Instantiate(blockPrefab, position, Quaternion.identity, transform);
                if (block == null)
                {
                    Debug.LogError($"Failed to instantiate block at ({row}, {col}).");
                    continue;
                }

                blocks[row, col] = block;
            }
        }
    }

    void CenterGrid()
    {
        float offsetX = (columns - 1) * blockSize / 2;
        float offsetY = (rows - 1) * blockSize / 2;
        transform.position = new Vector2(-offsetX, -offsetY);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                GameObject clickedBlock = hit.collider.gameObject;
                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < columns; col++)
                    {
                        if (blocks[row, col] == clickedBlock)
                        {
                            DetectAndUpdateMatches(row, col);
                            return;
                        }
                    }
                }
            }
        }
    }

    void DetectAndUpdateMatches(int startRow, int startCol)
    {
        var matches = DetectMatches(startRow, startCol);
        if (matches.Count >= 2)
        {
            foreach (var pos in matches)
            {
                Destroy(blocks[pos.x, pos.y]);
                blocks[pos.x, pos.y] = null;
            }

            UpdateBoardAfterRemoval(matches);
        }
    }

    List<Vector2Int> DetectMatches(int startRow, int startCol)
    {
        var matches = new List<Vector2Int>();

        // Validate starting block
        if (blocks[startRow, startCol] == null)
            return matches;

        var targetBlock = blocks[startRow, startCol];
        var blockBehavior = targetBlock.GetComponent<BlockBehavior>();
        if (blockBehavior == null)
        {
            Debug.LogError($"Block at ({startRow}, {startCol}) is missing BlockBehavior.");
            return matches;
        }

        var targetSprite = blockBehavior.GetSprite();
        if (targetSprite == null)
        {
            Debug.LogError($"Block at ({startRow}, {startCol}) has no sprite set.");
            return matches;
        }

        bool[,] visited = new bool[rows, columns];

        void FloodFill(int row, int col)
        {
            if (row < 0 || col < 0 || row >= rows || col >= columns || visited[row, col])
                return;

            GameObject block = blocks[row, col];
            if (block == null)
                return;

            var behavior = block.GetComponent<BlockBehavior>();
            if (behavior == null || behavior.GetSprite() != targetSprite)
                return;

            visited[row, col] = true;
            matches.Add(new Vector2Int(row, col));

            FloodFill(row + 1, col);
            FloodFill(row - 1, col);
            FloodFill(row, col + 1);
            FloodFill(row, col - 1);
        }

        FloodFill(startRow, startCol);
        return matches;
    }

    void UpdateBoardAfterRemoval(List<Vector2Int> matches)
    {
        foreach (var pos in matches)
        {
            Destroy(blocks[pos.x, pos.y]); 
            blocks[pos.x, pos.y] = null;  
        }
        
        for (int col = 0; col < columns; col++)
        {
            int emptyRow = rows - 1;
            for (int row = rows - 1; row >= 0; row--)
            {
                if (blocks[row, col] != null)
                {
                    if (row != emptyRow)
                    {
                        blocks[emptyRow, col] = blocks[row, col];
                        blocks[row, col] = null;
                        blocks[emptyRow, col].transform.position = new Vector2(col * blockSize, emptyRow * blockSize);
                    }
                    emptyRow--;
                }
            }
            
            for (int row = emptyRow; row >= 0; row--)
            {
                int colorIndex = Random.Range(0, blockPrefabs.Length);
                GameObject blockPrefab = blockPrefabs[colorIndex];
                Vector2 spawnPosition = new Vector2(col * blockSize, (rows + row) * blockSize); 
                GameObject newBlock = Instantiate(blockPrefab, spawnPosition, Quaternion.identity, transform);

                blocks[row, col] = newBlock;

                
                StartCoroutine(MoveBlockToPosition(newBlock, new Vector2(col * blockSize, row * blockSize)));
            }
        }

        
        if (!HasAnyMatches())
        {
            ShuffleBoard();
        }
    }

    IEnumerator MoveBlockToPosition(GameObject block, Vector2 targetPosition)
    {
        float speed = 25f; 
        while ((Vector2)block.transform.position != targetPosition)
        {
            block.transform.position = Vector2.MoveTowards(block.transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }
    }

    bool HasAnyMatches()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (blocks[row, col] == null) continue;

                var matches = DetectMatches(row, col);
                if (matches.Count >= 2)
                {
                    return true; // At least one match exists
                }
            }
        }
        return false; // No matches found
    }



    void ShuffleBoard()
    {
        List<GameObject> blockList = new List<GameObject>();
        foreach (var block in blocks) if (block != null) blockList.Add(block);

        for (int i = blockList.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            var temp = blockList[i];
            blockList[i] = blockList[randomIndex];
            blockList[randomIndex] = temp;
        }

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                blocks[row, col] = blockList[row * columns + col];
                blocks[row, col].transform.position = new Vector2(col * blockSize, row * blockSize);
            }
        }
    }
}
