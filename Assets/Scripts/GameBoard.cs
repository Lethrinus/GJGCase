using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    [Header("Block Prefabs (each has unique colorID)")]
    public GameObject[] blockPrefabs; 
    // prefab id matching here 

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

    [Header("Fade Animation")]
    public float shuffleFadeDuration = 0.4f; 

    private GameObject[] _blocks;
    private bool _isReady;
    private int _blocksAnimating; 
    private float _lerpT;
    private bool _lerpForward = true;

    private void Start()
    {
        _blocks = new GameObject[rows * columns];
        StartCoroutine(InitializeBoard());
    }

    private IEnumerator InitializeBoard()
    {
        // 1) random board generator 
        GenerateBoard();
        yield return new WaitForSeconds(0.1f);

        // 2) sprite update with bfs 
        UpdateAllBlockSprites();
        yield return new WaitForSeconds(0.2f);

        // 3) deadlock detection first when the game starts 
        if (IsDeadlock())
        {
            yield return StartCoroutine(ResolveDeadlockOnce());
        }

        _isReady = true;
    }

    private void Update()
    {
        if (!_isReady) return;

        // left click block blast
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main is not null)
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

                if (hit.collider is not null)
                {
                    TryBlast(hit.collider.gameObject);
                }
            }
        }

        LerpBackgroundColor();
    }

    
    // board creating 
    

    private void GenerateBoard()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int idx = Random.Range(0, blockPrefabs.Length);
                Vector2 spawnPos = GetBlockPosition(r, c);

                GameObject block = Instantiate(blockPrefabs[idx], spawnPos, Quaternion.identity, transform);
                _blocks[GetIndex(r, c)] = block;

                var bb = block.GetComponent<BlockBehavior>();
                if (bb is not null)
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

    private int GetIndex(int row, int col)
    {
        return row * columns + col;
    }
    
    // blast mechanic 
    
    private void TryBlast(GameObject clickedBlock)
    {
        if (!_isReady) return;

        Vector2Int? pos = FindBlockPosition(clickedBlock);
        if (!pos.HasValue) return;

        List<int> group = GetConnectedGroup(GetIndex(pos.Value.x, pos.Value.y));
        if (group.Count < 2)
        {
            // cannot be blasted = buzz effect 
            var bb = clickedBlock.GetComponent<BlockBehavior>();
            if (bb is not null)
            {
                bb.StartBuzz();
            }
            return;
        }

        _isReady = false;
        StartCoroutine(RemoveGroupWithAnimation(group));
    }

    private Vector2Int? FindBlockPosition(GameObject block)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (_blocks[GetIndex(r, c)] == block)
                    return new Vector2Int(r, c);
            }
        }
        return null;
    }

    private IEnumerator RemoveGroupWithAnimation(List<int> group)
    {
        int groupSize = group.Count;

        // for bigger groups camera shake effect 
        if (groupSize >= 7)
        {
            StartCoroutine(CameraShake(0.36f, 0.24f));
        }

        // blast animation 
        foreach (var index in group)
        {
            if (_blocks[index] is not null)
            {
                _blocksAnimating++;
                var bb = _blocks[index].GetComponent<BlockBehavior>();
                if (bb is not null)
                {
                    StartCoroutine(bb.BlastAnimation(0.3f, () =>
                    {
                        _blocksAnimating--;
                    }));
                }
            }
        }

        // array references update 
        foreach (var index in group)
        {
            _blocks[index] = null;
        }

        // wait for the animations are done 
        while (_blocksAnimating > 0)
            yield return null;

        // fill the spaces with new blocks 
        yield return StartCoroutine(UpdateBoardAfterRemoval());

        _isReady = true;
    }

    private IEnumerator UpdateBoardAfterRemoval()
    {
        yield return new WaitForSeconds(0.1f);

        // 1) fall the blocks 
        for (int c = 0; c < columns; c++)
        {
            int writeRow = rows - 1;
            for (int r = rows - 1; r >= 0; r--)
            {
                int index = GetIndex(r, c);
                if (_blocks[index] is not null)
                {
                    if (r != writeRow)
                    {
                        _blocks[GetIndex(writeRow, c)] = _blocks[index];
                        _blocks[index] = null;

                        Vector2 targetPos = GetBlockPosition(writeRow, c);
                        StartCoroutine(MoveBlock(_blocks[GetIndex(writeRow, c)], targetPos));
                    }
                    writeRow--;
                }
            }

            // 2) fill the spaces with new blocks  (row=-1 location spawn)
            for (int newRow = writeRow; newRow >= 0; newRow--)
            {
                int idx = Random.Range(0, blockPrefabs.Length);
                Vector2 spawnPos = GetBlockPosition(-1, c); 
                GameObject newBlock = Instantiate(blockPrefabs[idx], spawnPos, Quaternion.identity, transform);
                _blocks[GetIndex(newRow, c)] = newBlock;

                var bb = newBlock.GetComponent<BlockBehavior>();
                if (bb is not null)
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

        // bfs -> update sprite 
        UpdateAllBlockSprites();

        // deadlock control
        if (IsDeadlock())
        {
            // resolving deadlock with one shuffle 
            yield return StartCoroutine(ResolveDeadlockOnce());
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

   
    // bfs & sprite updater 
    

    private void UpdateAllBlockSprites()
    {
        bool[] visited = new bool[_blocks.Length];
        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i] == null || visited[i]) continue;

            List<int> group = GetConnectedGroup(i);
            int size = group.Count;
            foreach (var index in group)
            {
                visited[index] = true;
                var bb = _blocks[index]?.GetComponent<BlockBehavior>();
                bb?.UpdateSpriteBasedOnGroupSize(size);
            }
        }
    }

    private List<int> GetConnectedGroup(int start)
    {
        List<int> result = new List<int>();
        if (_blocks[start] == null) return result;

        int colorID = _blocks[start].GetComponent<BlockBehavior>().colorID;
        bool[] visited = new bool[_blocks.Length];
        Stack<int> stack = new Stack<int>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            int current = stack.Pop();
            if (visited[current]) continue;
            visited[current] = true;
            result.Add(current);

            foreach (int neighbor in GetNeighbors(current))
            {
                if (!visited[neighbor] && _blocks[neighbor] is not null)
                {
                    var nb = _blocks[neighbor].GetComponent<BlockBehavior>();
                    if (nb is not null && nb.colorID == colorID)
                    {
                        stack.Push(neighbor);
                    }
                }
            }
        }
        return result;
    }

    private IEnumerable<int> GetNeighbors(int index)
    {
        int r = index / columns;
        int c = index % columns;

        if (r - 1 >= 0) yield return GetIndex(r - 1, c);
        if (r + 1 < rows) yield return GetIndex(r + 1, c);
        if (c - 1 >= 0) yield return GetIndex(r, c - 1);
        if (c + 1 < columns) yield return GetIndex(r, c + 1);
    }

    
    // deadlock detection
    

    private bool IsDeadlock()
    {
        bool[] visited = new bool[_blocks.Length];
        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i] == null || visited[i]) continue;

            List<int> group = new List<int>();
            int colorID = _blocks[i].GetComponent<BlockBehavior>().colorID;

            Stack<int> stack = new Stack<int>();
            stack.Push(i);

            while (stack.Count > 0)
            {
                int current = stack.Pop();
                if (visited[current]) continue;
                visited[current] = true;
                group.Add(current);

                foreach (int neighbor in GetNeighbors(current))
                {
                    if (!visited[neighbor] && _blocks[neighbor] is not null)
                    {
                        var nb = _blocks[neighbor].GetComponent<BlockBehavior>();
                        if (nb is not null && nb.colorID == colorID)
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            }

            if (group.Count >= 2)
            {
                // if there is a group of 2 then there is no deadlock
                return false;
            }
        }
        return true; 
    }

   
    private IEnumerator ResolveDeadlockOnce()
    {
        // Fade Out
        yield return StartCoroutine(FadeBlocksOut(shuffleFadeDuration));

        // 1- collect the all colorID's
        List<int> allColorIDs = new List<int>(_blocks.Length);
        foreach (var t in _blocks)
        {
            allColorIDs.Add(t is not null
                ? t.GetComponent<BlockBehavior>().colorID
                // if null then random 
                : Random.Range(0, blockPrefabs.Length));
        }

        // 2) find at least 2 block for one color 
        Dictionary<int,int> colorCount = new Dictionary<int,int>();
        foreach (var cID in allColorIDs)
        {
            colorCount.TryAdd(cID, 0);
            colorCount[cID]++;
        }
        int colorWith2 = -1;
        foreach (var kvp in colorCount)
        {
            if (kvp.Value >= 2)
            {
                colorWith2 = kvp.Key;
                break;
            }
        }

        // 3) Fisher-Yates shuffle (one-time)
        for (int i = allColorIDs.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (allColorIDs[i], allColorIDs[swapIndex]) = (allColorIDs[swapIndex], allColorIDs[i]);
        }

        // 4) for guarantee at least 1 match  
        //   if  colorWith2 is not -1 then, index0 and index1 color change 
        if (colorWith2 is not -1)
        {
            // index0
            allColorIDs[0] = colorWith2;
            // index1
            allColorIDs[1] = colorWith2;
        }
        else
        {
            // if there is no color at least 2 of each 
            // fallback:
            
            if (allColorIDs.Count >= 2)
            {
                allColorIDs[0] = 0; 
                allColorIDs[1] = 0;
            }
        }

        // one time board reassign
        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i] is not null)
            {
                Destroy(_blocks[i]);
                _blocks[i] = null;
            }
        }

        // create array again
        _blocks = new GameObject[rows * columns];

        for (int i = 0; i < allColorIDs.Count; i++)
        {
            int row = i / columns;
            int col = i % columns;

            int colorID = allColorIDs[i];
            if (colorID < 0 || colorID >= blockPrefabs.Length)
            {
                // clamp
                colorID = Mathf.Clamp(colorID, 0, blockPrefabs.Length - 1);
            }

            Vector2 pos = GetBlockPosition(row, col);
            GameObject newBlock = Instantiate(blockPrefabs[colorID], pos, Quaternion.identity, transform);
            _blocks[i] = newBlock;

            var bb = newBlock.GetComponent<BlockBehavior>();
            if (bb is not null)
            {
                bb.thresholdA = thresholdA;
                bb.thresholdB = thresholdB;
                bb.thresholdC = thresholdC;
            }
        }

        // 6) BFS -> sprite update
        UpdateAllBlockSprites();

        // 7) Fade In
        yield return StartCoroutine(FadeBlocksIn(shuffleFadeDuration));
    }

    
    // Fade Effect Camera
    

    private IEnumerator FadeBlocksOut(float duration)
    {
        float elapsed = 0f;
        List<SpriteRenderer> srs = new List<SpriteRenderer>();
        Dictionary<SpriteRenderer, Color> initColors = new Dictionary<SpriteRenderer, Color>();

        foreach (var t in _blocks)
        {
            if (t == null) continue;
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr)
            {
                srs.Add(sr);
                initColors[sr] = sr.color;
            }
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(1f, 0f, t);
            foreach (var sr in srs)
            {
                Color c = initColors[sr];
                sr.color = new Color(c.r, c.g, c.b, alpha);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // final alpha = 0
        foreach (var sr in srs)
        {
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
        }
    }

    private IEnumerator FadeBlocksIn(float duration)
    {
        float elapsed = 0f;
        List<SpriteRenderer> srs = new List<SpriteRenderer>();
        Dictionary<SpriteRenderer, Color> targetColors = new Dictionary<SpriteRenderer, Color>();

        foreach (var t in _blocks)
        {
            if (t == null) continue;
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr)
            {
                srs.Add(sr);
                // target alpha = 1
                targetColors[sr] = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
                // start alpha = 0
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
            }
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(0f, 1f, t);
            foreach (var sr in srs)
            {
                Color final = targetColors[sr];
                sr.color = new Color(final.r, final.g, final.b, alpha);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // final alpha=1
        foreach (var sr in srs)
        {
            Color final = targetColors[sr];
            sr.color = final;
        }
    }

    private void LerpBackgroundColor()
    {
        if (!Camera.main) return;
        
        float direction = _lerpForward ? 1f : -1f;
        _lerpT += direction * (Time.deltaTime / backgroundLerpTime);

        if (_lerpT >= 1f)
        {
            _lerpT = 1f;
            _lerpForward = false;
        }
        else if (_lerpT <= 0f)
        {
            _lerpT = 0f;
            _lerpForward = true;
        }

        Camera.main.backgroundColor = Color.Lerp(backgroundColorA, backgroundColorB, _lerpT);
    }

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

        Camera.main.transform.position = originalPos;
    }
}
