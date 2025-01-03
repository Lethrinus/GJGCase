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
                    blocks[row, col] = Instantiate(blockPrefab, position, Quaternion.identity, transform);
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
        Vector2Int? blockPosition = FindBlockPosition(clickedBlock);
        if (blockPosition.HasValue)
        {
            var matches = DetectMatches(blockPosition.Value);

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

        Sprite targetSprite = blocks[start.x, start.y].GetComponent<BlockBehavior>()?.GetComponent<SpriteRenderer>().sprite;
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

    void UpdateBoardAfterRemoval(List<Vector2Int> matches)
    {
        
        foreach (var pos in matches)
        {
            blocks[pos.x, pos.y] = null;
        }
        
        for (int col = 0; col < columns; col++)
        {
            for (int row = rows - 1; row >= 0; row--)
            {
                if (blocks[row, col] == null)
                {
                    for (int aboveRow = row - 1; aboveRow >= 0; aboveRow--)
                    {
                        if (blocks[aboveRow, col] != null)
                        {
                            blocks[row, col] = blocks[aboveRow, col];
                            blocks[aboveRow, col] = null;
                            StartCoroutine(MoveBlockToPosition(blocks[row, col], new Vector2(col * blockSize, (rows - 1 - row) * blockSize)));
                            break;
                        }
                    }
                }
            }
        }

        
        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (blocks[row, col] == null)
                {
                    int colorIndex = Random.Range(0, blockPrefabs.Length);
                    blocks[row, col] = Instantiate(blockPrefabs[colorIndex], new Vector2(col * blockSize, (rows + row) * blockSize), Quaternion.identity, transform);
                    StartCoroutine(MoveBlockToPosition(blocks[row, col], new Vector2(col * blockSize, (rows - 1 - row) * blockSize)));
                }
            }
        }
    }
    IEnumerator MoveBlockToPosition(GameObject block, Vector2 targetPosition)
    {
        float speed = 10f; 
        while ((Vector2)block.transform.position != targetPosition)
        {
            block.transform.position = Vector2.MoveTowards(block.transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }
    }
}
