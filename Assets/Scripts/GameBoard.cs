using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    [Header("Block Prefabs (One for Each Color)")]
    public GameObject[] blockPrefabs;

    [Header("Board Dimensions")]
    public int rows = 10;
    public int columns = 10;
    public float blockSize = 1.0f;

    [Header("Thresholds for Group Sizes")]
    public int thresholdA = 5;  
    public int thresholdB = 8;
    public int thresholdC = 10;

    private GameObject[,] blocks;
    private bool isReady = false;

    void Start()
    {
        blocks = new GameObject[rows, columns];
        StartCoroutine(InitializeBoard());
    }

    IEnumerator InitializeBoard()
    {
        GenerateBoard();
        CenterGrid();
        yield return new WaitForSeconds(0.5f);
        isReady = true;
    }

    void GenerateBoard()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector2 position = new Vector2(col * blockSize, (rows - 1 - row) * blockSize);
                int colorIndex = Random.Range(0, blockPrefabs.Length);

                GameObject blockPrefab = blockPrefabs[colorIndex];
                if (blockPrefab != null)
                {
                    GameObject block = Instantiate(blockPrefab, position, Quaternion.identity, transform);
                    blocks[row, col] = block;

                    var blockBehavior = block.GetComponent<BlockBehavior>();
                    if (blockBehavior != null)
                    {
                        blockBehavior.thresholdA = thresholdA;
                        blockBehavior.thresholdB = thresholdB;
                        blockBehavior.thresholdC = thresholdC;
                    }
                }
            }
        }
    }

    void CenterGrid()
    {
        float offsetX = (columns - 1) * blockSize / 2;
        float offsetY = (rows - 1) * blockSize / 2;
        transform.position = new Vector3(-offsetX, offsetY, 0);
    }

    void Update()
    {
        if (!isReady) return; 

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                DetectAndUpdateMatches(hit.collider.gameObject);
            }
        }
    }

    void DetectAndUpdateMatches(GameObject clickedBlock)
    {
        if (!isReady) return; 

        Vector2Int? blockPosition = FindBlockPosition(clickedBlock);
        if (blockPosition.HasValue)
        {
            var matches = DetectMatches(blockPosition.Value);

            if (matches.Count >= 2)
            {
                isReady = false; 

                foreach (var pos in matches)
                {
                    var blockBehavior = blocks[pos.x, pos.y]?.GetComponent<BlockBehavior>();
                    if (blockBehavior != null)
                    {
                        blockBehavior.UpdateSpriteBasedOnGroupSize(matches.Count);
                    }
                }

                foreach (var pos in matches)
                {
                    Destroy(blocks[pos.x, pos.y]);
                    blocks[pos.x, pos.y] = null;
                }

                StartCoroutine(UpdateBoardAfterRemoval(matches));
            }
        }
    }
    

    Vector2Int? FindBlockPosition(GameObject block)
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (blocks[row, col] == block)
                {
                    return new Vector2Int(row, col);
                }
            }
        }

        return null;
    }

    List<Vector2Int> DetectMatches(Vector2Int start)
    {
        List<Vector2Int> matches = new List<Vector2Int>();
        if (blocks[start.x, start.y] == null) return matches;

        Sprite targetSprite = blocks[start.x, start.y].GetComponent<BlockBehavior>()?.GetComponent<SpriteRenderer>()?.sprite;
        if (targetSprite == null) return matches;

        bool[,] visited = new bool[rows, columns];

        void FloodFill(int x, int y)
        {
            if (x < 0 || y < 0 || x >= rows || y >= columns || visited[x, y] || blocks[x, y] == null) return;

            Sprite sprite = blocks[x, y].GetComponent<BlockBehavior>()?.GetComponent<SpriteRenderer>().sprite;
            if (sprite != targetSprite) return;

            visited[x, y] = true;
            matches.Add(new Vector2Int(x, y));

            FloodFill(x + 1, y);
            FloodFill(x - 1, y);
            FloodFill(x, y + 1);
            FloodFill(x, y - 1);
        }

        FloodFill(start.x, start.y);
        return matches;
    }

 IEnumerator UpdateBoardAfterRemoval(List<Vector2Int> matches)
{
    yield return new WaitForSeconds(0.1f); // Delay for visual feedback

    foreach (var pos in matches)
    {
        blocks[pos.x, pos.y] = null; // Clear matched blocks
    }

    for (int col = 0; col < columns; col++)
    {
        int emptyRow = rows - 1; // Start checking from the top

        // Shift existing blocks down
        for (int row = rows - 1; row >= 0; row--)
        {
            if (blocks[row, col] != null)
            {
                if (row != emptyRow)
                {
                    blocks[emptyRow, col] = blocks[row, col];
                    blocks[row, col] = null;

                    // Move the block to its new position
                    StartCoroutine(MoveBlockToPosition(blocks[emptyRow, col],
                        new Vector2(col * blockSize, (rows - 1 - emptyRow) * blockSize)));
                }

                emptyRow--;
            }
        }

        // Spawn new blocks at the top to fill empty spaces
        for (int spawnRow = emptyRow; spawnRow >= 0; spawnRow--)
        {
            int colorIndex = Random.Range(0, blockPrefabs.Length);

            // Proper spawn position directly above the grid
            Vector2 spawnPosition = new Vector2(col * blockSize, (rows + (emptyRow - spawnRow)) * blockSize);

            blocks[spawnRow, col] = Instantiate(
                blockPrefabs[colorIndex],
                spawnPosition,
                Quaternion.identity,
                transform
            );

            var newBlockBehavior = blocks[spawnRow, col].GetComponent<BlockBehavior>();
            if (newBlockBehavior != null)
            {
                newBlockBehavior.UpdateSpriteBasedOnGroupSize(1);
            }

            // Move the new block to its correct position in the grid
            StartCoroutine(MoveBlockToPosition(blocks[spawnRow, col],
                new Vector2(col * blockSize, (rows - 1 - spawnRow) * blockSize)));
        }
    }

    yield return new WaitForSeconds(0.5f); // Allow animations to complete
    isReady = true; // Enable player actions
}

    IEnumerator MoveBlockToPosition(GameObject block, Vector2 targetPosition)
    {
        float speed = 35f;

        if (block == null) yield break;

        Vector2 start = block.transform.localPosition;

        while (block != null && (Vector2)block.transform.localPosition != targetPosition)
        {
            if (block != null)
            {
                block.transform.localPosition = Vector2.MoveTowards(
                    block.transform.localPosition,
                    targetPosition,
                    speed * Time.deltaTime
                );
            }
            yield return null;
        }
    }
}