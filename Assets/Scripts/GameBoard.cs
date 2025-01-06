using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A collapse/blast board with:
///  - colorID-based matching
///  - threshold-based icons
///  - robust deadlock detection (single-swap + limited shuffle)
///  - correct spawning above each column
///  - high-speed safe block movement
/// 
/// Notes on optimization:
///  - We keep BFS checks minimal (only when needed).
///  - We lock input while blocks animate.
///  - We do immediate sprite refresh if single-swap is valid, so icons remain consistent.
/// </summary>
public class GameBoard : MonoBehaviour
{
    [Header("Block Prefabs (up to K=6)")]
    public GameObject[] blockPrefabs;

    [Header("Board Dimensions (M,N up to 10)")]
    public int rows = 10;
    public int columns = 12;
    public float blockSize = 1f;

    [Header("Thresholds (A < B < C)")]
    public int thresholdA = 4; 
    public int thresholdB = 7; 
    public int thresholdC = 9; 

    [Header("Movement Speed")]
    public float moveSpeed = 40f; 
    
    private GameObject[,] blocks;
    
    private bool isReady = false;

    private void Start()
    {
        blocks = new GameObject[rows, columns];
        StartCoroutine(InitializeBoard());
    }

    
    private IEnumerator InitializeBoard()
    {
        GenerateBoard();
        CenterGrid();

        yield return new WaitForSeconds(0.1f);

        UpdateAllBlockSprites();

        
        if (CheckForDeadlock())
        {
            Debug.Log("[GameBoard] Deadlock at start, attempting to resolve...");
            ResolveDeadlock();
            yield return new WaitForSeconds(0.3f);
            UpdateAllBlockSprites();
        }

        yield return new WaitForSeconds(0.2f);
        isReady = true;
    }

 
    private void GenerateBoard()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int idx = Random.Range(0, blockPrefabs.Length);
                Vector2 position = GetBlockPosition(r, c);

                GameObject block = Instantiate(blockPrefabs[idx], position, Quaternion.identity, transform);
                blocks[r, c] = block;

                var bb = block.GetComponent<BlockBehavior>();
                if (bb != null)
                {
                    bb.thresholdA = thresholdA;
                    bb.thresholdB = thresholdB;
                    bb.thresholdC = thresholdC;
                }
            }
        }
    }

    private Vector2 GetBlockPosition(int row, int col)
    {
        float x = col * blockSize;
        float y = (rows - 1 - row) * blockSize;
        return new Vector2(x, y);
    }

  
    private void CenterGrid()
    {
        float offsetX = (columns - 1) * blockSize * 0.5f;
        float offsetY = (rows - 1) * blockSize * 0.5f;
        transform.position = new Vector3(-offsetX, offsetY, 0f);
    }

    private void Update()
    {
        if (!isReady) return;

        
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mp, Vector2.zero);

            if (hit.collider != null)
            {
                TryBlast(hit.collider.gameObject);
            }
        }
    }

  
    private void TryBlast(GameObject clickedBlock)
    {
        if (!isReady) return;

        Vector2Int? pos = FindBlockPosition(clickedBlock);
        if (!pos.HasValue) return;

      
        List<Vector2Int> group = GetConnectedGroup(pos.Value);
        if (group.Count < 2) return; 

      
        isReady = false;

    
        foreach (var p in group)
        {
            if (blocks[p.x, p.y] != null)
            {
                Destroy(blocks[p.x, p.y]);
                blocks[p.x, p.y] = null;
            }
        }

       
        StartCoroutine(UpdateBoardAfterRemoval());
    }

    private Vector2Int? FindBlockPosition(GameObject block)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (blocks[r, c] == block)
                    return new Vector2Int(r, c);
            }
        }
        return null;
    }

    
    private List<Vector2Int> GetConnectedGroup(Vector2Int start)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        if (blocks[start.x, start.y] == null) return result;

        int colorID = blocks[start.x, start.y].GetComponent<BlockBehavior>().colorID;

        bool[,] visited = new bool[rows, columns];
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();
            int rr = current.x;
            int cc = current.y;

            if (visited[rr, cc]) continue;
            visited[rr, cc] = true;
            result.Add(current);

            foreach (var nbr in GetNeighbors(current))
            {
                int nr = nbr.x;
                int nc = nbr.y;
                if (!visited[nr, nc] && blocks[nr, nc] != null)
                {
                    var nb = blocks[nr, nc].GetComponent<BlockBehavior>();
                    if (nb != null && nb.colorID == colorID)
                    {
                        stack.Push(nbr);
                    }
                }
            }
        }

        return result;
    }

   
    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        int r = cell.x, c = cell.y;

        if (r - 1 >= 0)     yield return new Vector2Int(r - 1, c);
        if (r + 1 < rows)   yield return new Vector2Int(r + 1, c);
        if (c - 1 >= 0)     yield return new Vector2Int(r, c - 1);
        if (c + 1 < columns)yield return new Vector2Int(r, c + 1);
    }

 
    private IEnumerator UpdateBoardAfterRemoval()
    {
        yield return new WaitForSeconds(0.1f);

        
        for (int c = 0; c < columns; c++)
        {
            int writeRow = rows - 1; 
            for (int r = rows - 1; r >= 0; r--)
            {
                if (blocks[r, c] != null)
                {
                    if (r != writeRow)
                    {
                        blocks[writeRow, c] = blocks[r, c];
                        blocks[r, c] = null;

                  
                        Vector2 targetPos = GetBlockPosition(writeRow, c);
                        StartCoroutine(MoveBlock(blocks[writeRow, c], targetPos));
                    }
                    writeRow--;
                }
            }

            
            for (int newRow = writeRow; newRow >= 0; newRow--)
            {
                int idx = Random.Range(0, blockPrefabs.Length);
                
                Vector2 spawnPos = GetBlockPosition(-1, c);
                GameObject newBlock = Instantiate(blockPrefabs[idx], spawnPos, Quaternion.identity, transform);
                blocks[newRow, c] = newBlock;

                var bb = newBlock.GetComponent<BlockBehavior>();
                if (bb != null)
                {
                    bb.thresholdA = thresholdA;
                    bb.thresholdB = thresholdB;
                    bb.thresholdC = thresholdC;
                }

               
                Vector2 finalPos = GetBlockPosition(newRow, c);
                StartCoroutine(MoveBlock(newBlock, finalPos));
            }
        }

        yield return new WaitForSeconds(0.5f);

      
        UpdateAllBlockSprites();

     
        if (CheckForDeadlock())
        {
            Debug.Log("[GameBoard] Deadlock after removal, resolving...");
            ResolveDeadlock();
            yield return new WaitForSeconds(0.3f);
            UpdateAllBlockSprites();
        }

     
        isReady = true;
    }

    
    private IEnumerator MoveBlock(GameObject block, Vector2 targetPos)
    {
        if (block == null) yield break;

        while (block != null)
        {
            Vector2 current = block.transform.localPosition;
            if ((current - targetPos).sqrMagnitude < 0.0001f)
            {
             
                block.transform.localPosition = targetPos;
                yield break;
            }

            block.transform.localPosition = Vector2.MoveTowards(
                current,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }
    }

    private void UpdateAllBlockSprites()
    {
        bool[,] visited = new bool[rows, columns];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (blocks[r, c] == null || visited[r, c]) 
                    continue;

                List<Vector2Int> group = GetConnectedGroup(new Vector2Int(r, c));
                int size = group.Count;

                
                foreach (var p in group)
                {
                    visited[p.x, p.y] = true;
                    var bb = blocks[p.x, p.y]?.GetComponent<BlockBehavior>();
                    bb?.UpdateSpriteBasedOnGroupSize(size);
                }
            }
        }
    }

    // ------------------------------------------------------------------
    //                         DEADLOCK DETECTION
    // ------------------------------------------------------------------

   
    private bool CheckForDeadlock()
    {
        bool[,] visited = new bool[rows, columns];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (blocks[r, c] == null || visited[r, c]) 
                    continue;

                var group = GetConnectedGroup(new Vector2Int(r, c));
                
                foreach (var p in group)
                    visited[p.x, p.y] = true;

                if (group.Count >= 2)
                    return false; 
            }
        }
        return true;
    }

    
    private void ResolveDeadlock()
    {
        isReady = false;

       
        if (TrySingleSwapToCreateMatch())
        {
           
            UpdateAllBlockSprites();
            Debug.Log("[GameBoard] Deadlock resolved by single-swap.");
            return;
        }

        int attempts = 0;
        while (attempts < 10)
        {
            RandomShuffle();
            if (!CheckForDeadlock())
            {
                UpdateAllBlockSprites();
                Debug.Log("[GameBoard] Deadlock resolved by random shuffle.");
                return;
            }
            attempts++;
        }

        Debug.LogWarning("[GameBoard] Could not resolve deadlock after multiple shuffles!");
    }

  
    private bool TrySingleSwapToCreateMatch()
    {
        for (int r1 = 0; r1 < rows; r1++)
        {
            for (int c1 = 0; c1 < columns; c1++)
            {
                if (blocks[r1, c1] == null) continue;
                var bb1 = blocks[r1, c1].GetComponent<BlockBehavior>();
                if (bb1 == null) continue;

                for (int r2 = 0; r2 < rows; r2++)
                {
                    for (int c2 = 0; c2 < columns; c2++)
                    {
                        
                        if (r1 == r2 && c1 == c2) continue;
                        if (blocks[r2, c2] == null) continue;

                        var bb2 = blocks[r2, c2].GetComponent<BlockBehavior>();
                        if (bb2 == null) continue;
                        
                        int temp = bb1.colorID;
                        bb1.colorID = bb2.colorID;
                        bb2.colorID = temp;

                      
                        if (FormsARealMatch(r1, c1) || FormsARealMatch(r2, c2))
                        {
                          
                            return true;
                        }

                        
                        temp = bb1.colorID;
                        bb1.colorID = bb2.colorID;
                        bb2.colorID = temp;
                    }
                }
            }
        }
        return false;
    }

  
    private bool FormsARealMatch(int r, int c)
    {
        if (blocks[r, c] == null) return false;
        var bb = blocks[r, c].GetComponent<BlockBehavior>();
        if (bb == null) return false;

        int targetColor = bb.colorID;

        bool[,] visited = new bool[rows, columns];
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(r, c));

        int groupCount = 0;

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();
            int rr = current.x;
            int cc = current.y;

            if (visited[rr, cc]) continue;
            visited[rr, cc] = true;
            groupCount++;

            foreach (var nbr in GetNeighbors(current))
            {
                int nr = nbr.x;
                int nc = nbr.y;
                if (!visited[nr, nc] && blocks[nr, nc] != null)
                {
                    var neighborBB = blocks[nr, nc].GetComponent<BlockBehavior>();
                    if (neighborBB != null && neighborBB.colorID == targetColor)
                    {
                        stack.Push(nbr);
                    }
                }
            }
        }

        return (groupCount >= 2);
    }

   
    private void RandomShuffle()
    {
        
        List<int> colorList = new List<int>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (blocks[r, c] != null)
                {
                    var bb = blocks[r, c].GetComponent<BlockBehavior>();
                    if (bb != null) colorList.Add(bb.colorID);
                }
            }
        }

       
        for (int i = colorList.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = colorList[i];
            colorList[i] = colorList[j];
            colorList[j] = temp;
        }

        
        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (blocks[r, c] != null)
                {
                    var bb = blocks[r, c].GetComponent<BlockBehavior>();
                    if (bb != null)
                    {
                        bb.colorID = colorList[index];
                    }
                    index++;
                }
            }
        }
    }
}
