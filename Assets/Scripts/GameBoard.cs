using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A collapse/blast board with:
///  - colorID-based matching
///  - threshold-based icons
///  - single-swap deadlock fix + random shuffle
///  - blocks remain at transform.position= (0,0,0)
///  - buzz animation for non-blastable
///  - blast animation + waiting logic so empty spaces fill immediately
///  - high-speed safe movement
///  - camera shake on large blast
///  - particle effects for blast & buzz
///  - background color lerp
/// </summary>
public class GameBoard : MonoBehaviour
{
    [Header("Block Prefabs (each with unique colorID)")]
    public GameObject[] blockPrefabs;
    

    [Header("Board Dimensions")]
    public int rows = 10;
    public int columns = 10;
    public float blockSize = 1f;

    [Header("Thresholds (A < B < C)")]
    public int thresholdA = 4;
    public int thresholdB = 7;
    public int thresholdC = 9;

    [Header("Movement Speed")]
    public float moveSpeed = 40f;

    [Header("Background Lerp Settings")]
    public Color backgroundColorA = new Color(0.1f, 0.2f, 0.8f);
    public Color backgroundColorB = new Color(0.3f, 0.4f, 0.95f);
    public float backgroundLerpTime = 10f;

    private float lerpT = 0f;
    private bool lerpForward = true;

    private GameObject[,] blocks;
    private bool isReady = false;
    private int _blocksAnimating = 0;

    private void Start()
    {
        blocks = new GameObject[rows, columns];
        StartCoroutine(InitializeBoard());
    }

    private IEnumerator InitializeBoard()
    {
        GenerateBoard();
        yield return new WaitForSeconds(0.1f);

        UpdateAllBlockSprites();

        if (CheckForDeadlock())
        {
            ResolveDeadlock();
            yield return new WaitForSeconds(0.3f);
            UpdateAllBlockSprites();
        }

        yield return new WaitForSeconds(0.2f);
        isReady = true;
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

       
        LerpBackgroundColor();
    }

    private void LerpBackgroundColor()
    {
        if (!Camera.main) return;
        
        float direction = lerpForward ? 1f : -1f;
        lerpT += direction * (Time.deltaTime / backgroundLerpTime);

        if (lerpT >= 1f)
        {
            lerpT = 1f;
            lerpForward = false;
        }
        else if (lerpT <= 0f)
        {
            lerpT = 0f;
            lerpForward = true;
        }

        Camera.main.backgroundColor = Color.Lerp(backgroundColorA, backgroundColorB, lerpT);
    }

    private void GenerateBoard()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int idx = Random.Range(0, blockPrefabs.Length);
                Vector2 spawnPos = GetBlockPosition(r, c);

                GameObject block = Instantiate(blockPrefabs[idx], spawnPos, Quaternion.identity, transform);
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

    private void TryBlast(GameObject clickedBlock)
    {
        if (!isReady) return;

        Vector2Int? pos = FindBlockPosition(clickedBlock);
        if (!pos.HasValue) return;

        List<Vector2Int> group = GetConnectedGroup(pos.Value);
        if (group.Count < 2)
        {
            
            var bb = clickedBlock.GetComponent<BlockBehavior>();
            if (bb != null)
            {
                bb.StartBuzz();
            }
            return;
        }
        
        isReady = false;
        StartCoroutine(RemoveGroupWithAnimation(group));
    }

    private void SpawnParticleAndDestroy(GameObject particlePrefab, Vector3 position)
    {
        if (particlePrefab == null) return;

        GameObject p = Instantiate(particlePrefab, position, Quaternion.identity);
        ParticleSystem ps = p.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            float totalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(p, totalDuration);
        }
        else
        {
            
            Destroy(p, 2f);
        }
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

    private IEnumerator RemoveGroupWithAnimation(List<Vector2Int> group)
    {
        int groupSize = group.Count;

       
        if (groupSize >= 7)
        {
            StartCoroutine(CameraShake(0.36f, 0.24f));
        }

        
        foreach (var p in group)
        {
            if (blocks[p.x, p.y] != null)
            {
                _blocksAnimating++;
                var bb = blocks[p.x, p.y].GetComponent<BlockBehavior>();
                if (bb != null)
                {
                    StartCoroutine(bb.BlastAnimation(0.3f, onComplete: () =>
                    {
                        _blocksAnimating--;
                    }));
                }
            }
        }

      
        foreach (var p in group)
        {
            blocks[p.x, p.y] = null;
        }

        
        while (_blocksAnimating > 0)
            yield return null;
        yield return StartCoroutine(UpdateBoardAfterRemoval());

        isReady = true;
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
            if (visited[current.x, current.y]) continue;
            visited[current.x, current.y] = true;
            result.Add(current);

            foreach (var nbr in GetNeighbors(current))
            {
                if (!visited[nbr.x, nbr.y] && blocks[nbr.x, nbr.y] != null)
                {
                    var nb = blocks[nbr.x, nbr.y].GetComponent<BlockBehavior>();
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
        int r = cell.x;
        int c = cell.y;

        if (r - 1 >= 0) yield return new Vector2Int(r - 1, c);
        if (r + 1 < rows) yield return new Vector2Int(r + 1, c);
        if (c - 1 >= 0) yield return new Vector2Int(r, c - 1);
        if (c + 1 < columns) yield return new Vector2Int(r, c + 1);
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
    }

    private IEnumerator MoveBlock(GameObject block, Vector2 targetPos)
    {
        if (!block) yield break;

        while (block)
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
                if (blocks[r, c] == null || visited[r, c]) continue;

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

    private bool CheckForDeadlock()
    {
        bool[,] visited = new bool[rows, columns];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (blocks[r, c] == null || visited[r, c]) continue;

                var group = GetConnectedGroup(new Vector2Int(r, c));
                foreach (var p in group)
                    visited[p.x, p.y] = true;

                if (group.Count >= 2) return false;
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

                        int tmp = bb1.colorID;
                        bb1.colorID = bb2.colorID;
                        bb2.colorID = tmp;

                        if (FormsARealMatch(r1, c1) || FormsARealMatch(r2, c2))
                        {
                            return true;
                        }

                        // swap back
                        tmp = bb1.colorID;
                        bb1.colorID = bb2.colorID;
                        bb2.colorID = tmp;
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

        int count = 0;

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            int rr = current.x;
            int cc = current.y;
            if (visited[rr, cc]) continue;
            visited[rr, cc] = true;
            count++;

            foreach (var nbr in GetNeighbors(current))
            {
                if (!visited[nbr.x, nbr.y] && blocks[nbr.x, nbr.y] != null)
                {
                    var nbb = blocks[nbr.x, nbr.y].GetComponent<BlockBehavior>();
                    if (nbb != null && nbb.colorID == targetColor)
                    {
                        stack.Push(nbr);
                    }
                }
            }
        }

        return (count >= 2);
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
                    if (bb != null)
                        colorList.Add(bb.colorID);
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

    // --------------------------- CAMERA SHAKE ---------------------------
    private IEnumerator CameraShake(float duration, float magnitude)
    {
        if (!Camera.main) yield break;

        Vector3 originalPos = Camera.main.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            Camera.main.transform.position = new Vector3(
                originalPos.x + offsetX,
                originalPos.y + offsetY,
                originalPos.z
            );
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Reset
        Camera.main.transform.position = originalPos;
    }
}
