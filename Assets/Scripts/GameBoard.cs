using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
    public BlockPool blockPool;
    public int rows = 10;
    public int columns = 12;
    public float blockSize = 1f;
    public int thresholdA = 4;
    public int thresholdB = 7;
    public int thresholdC = 9;
    public float moveSpeed = 40f;
    public Color backgroundColorA = new Color(0.1f, 0.2f, 0.8f);
    public Color backgroundColorB = new Color(0.3f, 0.4f, 0.95f);
    public float backgroundLerpTime = 10f;
    public float shuffleFadeDuration = 0.4f;

    private GameObject[] _blockGrid;
    private bool _isReady;
    private int _blocksAnimating;
    private float _lerpT;
    private bool _lerpForward = true;

    private List<int> group = new List<int>();
    private Stack<int> stack = new Stack<int>();

    private void Start()
    {
        if (blockPool == null)
        {
            Debug.LogError("GameBoard: BlockPool not assigned!");
            return;
        }

        _blockGrid = new GameObject[rows * columns];
        StartCoroutine(InitializeBoard());

        InputHandler inputHandler = FindObjectOfType<InputHandler>();
        if (inputHandler != null)
            inputHandler.OnBlockClicked += TryBlast;
        else
            Debug.LogError("GameBoard: InputHandler not found!");
    }

    private IEnumerator InitializeBoard()
    {
        GenerateBoard();
        yield return new WaitForSeconds(0.1f);
        UpdateAllBlockSprites();
        yield return new WaitForSeconds(0.2f);

        if (IsDeadlock())
            yield return StartCoroutine(ResolveDeadlockOnce());

        _isReady = true;
    }

    private void Update()
    {
        if (!_isReady) return;
        LerpBackgroundColor();
    }

    private void GenerateBoard()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int prefabIndex = Random.Range(0, blockPool.blockPrefabs.Length);
                Vector2 spawnPos = GetBlockPosition(r, c);
                GameObject block = blockPool.GetBlock(prefabIndex, spawnPos, transform);
                if (block == null)
                {
                    Debug.LogError($"GameBoard: Failed to create block (Row: {r}, Column: {c})");
                    continue;
                }
                _blockGrid[GetIndex(r, c)] = block;
                BlockBehavior bb = block.GetComponent<BlockBehavior>();
                if (bb != null)
                {
                    SetBlockProperties(bb, prefabIndex);
                    bb.OnBlockDestroyed += HandleBlockDestroyed;
                }
                else
                {
                    Debug.LogError($"GameBoard: BlockBehavior missing (Row: {r}, Column: {c})");
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

    private void SetBlockProperties(BlockBehavior bb, int prefabIndex)
    {
        bb.thresholdA = thresholdA;
        bb.thresholdB = thresholdB;
        bb.thresholdC = thresholdC;
        bb.prefabIndex = prefabIndex;
        bb.colorID = prefabIndex;
        bb.ResetBlock();
    }

    private void TryBlast(GameObject clickedBlock)
    {
        if (!_isReady) return;
        Vector2Int? pos = FindBlockPosition(clickedBlock);
        if (!pos.HasValue) return;

        List<int> groupList = GetConnectedGroup(GetIndex(pos.Value.x, pos.Value.y));
        if (groupList.Count < 2)
        {
            BlockBehavior bb = clickedBlock.GetComponent<BlockBehavior>();
            if (bb != null)
                bb.StartBuzz();
            return;
        }

        _isReady = false;
        StartCoroutine(RemoveGroupWithAnimation(groupList));
    }

    private Vector2Int? FindBlockPosition(GameObject block)
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                if (_blockGrid[GetIndex(r, c)] == block)
                    return new Vector2Int(r, c);
        return null;
    }

    private IEnumerator RemoveGroupWithAnimation(List<int> groupList)
    {
        if (groupList.Count >= 7)
            StartCoroutine(CameraShake(0.36f, 0.24f));

        foreach (var index in groupList)
        {
            if (_blockGrid[index] != null)
            {
                _blocksAnimating++;
                BlockBehavior bb = _blockGrid[index].GetComponent<BlockBehavior>();
                if (bb != null)
                {
                    StartCoroutine(bb.BlastAnimation(0.3f, () =>
                    {
                        blockPool.ReturnBlock(_blockGrid[index], bb.prefabIndex);
                        _blockGrid[index] = null;
                        _blocksAnimating--;
                    }));
                }
            }
        }

        while (_blocksAnimating > 0)
            yield return null;

        yield return StartCoroutine(UpdateBoardAfterRemoval());
        _isReady = true;
    }

    private IEnumerator UpdateBoardAfterRemoval()
    {
        yield return new WaitForSeconds(0.05f);

        for (int c = 0; c < columns; c++)
        {
            int writeRow = rows - 1;
            for (int r = rows - 1; r >= 0; r--)
            {
                int index = GetIndex(r, c);
                if (_blockGrid[index] != null)
                {
                    if (r != writeRow)
                    {
                        _blockGrid[GetIndex(writeRow, c)] = _blockGrid[index];
                        _blockGrid[index] = null;
                        StartCoroutine(MoveBlock(_blockGrid[GetIndex(writeRow, c)], GetBlockPosition(writeRow, c)));
                    }
                    writeRow--;
                }
            }

            for (int newRow = writeRow; newRow >= 0; newRow--)
            {
                int prefabIndex = Random.Range(0, blockPool.blockPrefabs.Length);
                Vector2 spawnPos = GetBlockPosition(-1, c);
                GameObject newBlock = blockPool.GetBlock(prefabIndex, spawnPos, transform);
                if (newBlock == null)
                {
                    Debug.LogError($"GameBoard: Failed to create block during update (Row: {newRow}, Column: {c})");
                    continue;
                }
                _blockGrid[GetIndex(newRow, c)] = newBlock;
                BlockBehavior bb = newBlock.GetComponent<BlockBehavior>();
                if (bb != null)
                {
                    SetBlockProperties(bb, prefabIndex);
                    bb.OnBlockDestroyed += HandleBlockDestroyed;
                }
                StartCoroutine(MoveBlock(newBlock, GetBlockPosition(newRow, c)));
            }
        }

        yield return new WaitForSeconds(0.5f);
        UpdateAllBlockSprites();

        if (IsDeadlock())
            yield return StartCoroutine(ResolveDeadlockOnce());
    }

    private IEnumerator MoveBlock(GameObject block, Vector2 targetPos)
    {
        while (block)
        {
            Vector2 current = block.transform.localPosition;
            if ((current - targetPos).sqrMagnitude < 0.0001f)
            {
                block.transform.localPosition = targetPos;
                yield break;
            }

            block.transform.localPosition = Vector2.MoveTowards(current, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void UpdateAllBlockSprites()
    {
        for (int i = 0; i < _blockGrid.Length; i++)
        {
            if (_blockGrid[i] == null) continue;
            group.Clear();
            stack.Clear();

            group = GetConnectedGroup(i);
            int size = group.Count;
            foreach (var index in group)
            {
                BlockBehavior bb = _blockGrid[index]?.GetComponent<BlockBehavior>();
                bb?.UpdateSpriteBasedOnGroupSize(size);
            }
        }
    }

    private List<int> GetConnectedGroup(int start)
    {
        group.Clear();
        stack.Clear();

        if (_blockGrid[start] == null) return group;

        BlockBehavior startBB = _blockGrid[start].GetComponent<BlockBehavior>();
        if (startBB == null) return group;

        int colorID = startBB.colorID;
        stack.Push(start);

        while (stack.Count > 0)
        {
            int current = stack.Pop();
            if (group.Contains(current)) continue;
            group.Add(current);

            foreach (int neighbor in GetNeighbors(current))
            {
                if (_blockGrid[neighbor] != null)
                {
                    BlockBehavior neighborBB = _blockGrid[neighbor].GetComponent<BlockBehavior>();
                    if (neighborBB != null && neighborBB.colorID == colorID && !group.Contains(neighbor))
                        stack.Push(neighbor);
                }
            }
        }
        return group;
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

    private bool IsDeadlock()
    {
        for (int i = 0; i < _blockGrid.Length; i++)
        {
            if (_blockGrid[i] == null) continue;

            BlockBehavior currentBB = _blockGrid[i].GetComponent<BlockBehavior>();
            if (currentBB == null) continue;

            int colorID = currentBB.colorID;
            group.Clear();
            stack.Push(i);
            int groupSize = 0;

            while (stack.Count > 0 && groupSize < 2)
            {
                int current = stack.Pop();
                if (group.Contains(current)) continue;
                group.Add(current);
                groupSize++;

                foreach (int neighbor in GetNeighbors(current))
                {
                    if (_blockGrid[neighbor] != null)
                    {
                        BlockBehavior neighborBB = _blockGrid[neighbor].GetComponent<BlockBehavior>();
                        if (neighborBB != null && neighborBB.colorID == colorID && !group.Contains(neighbor))
                            stack.Push(neighbor);
                    }
                }
            }

            if (groupSize >= 2)
                return false;
        }
        return true;
    }

    private IEnumerator ResolveDeadlockOnce()
    {
        yield return StartCoroutine(FadeBlocksOut(shuffleFadeDuration));

        if (blockPool.blockPrefabs.Length == 0)
        {
            Debug.LogError("GameBoard: No prefabs in BlockPool!");
            yield break;
        }

        int groupColorID = Random.Range(0, blockPool.blockPrefabs.Length);
        int index1 = Random.Range(0, _blockGrid.Length);
        int index2;
        do
        {
            index2 = Random.Range(0, _blockGrid.Length);
        } while (index2 == index1);

        List<int> allColorIDs = new List<int>(_blockGrid.Length);
        for (int i = 0; i < _blockGrid.Length; i++)
        {
            if (_blockGrid[i] != null)
            {
                BlockBehavior bb = _blockGrid[i].GetComponent<BlockBehavior>();
                if (bb != null)
                {
                    if (i == index1 || i == index2)
                        allColorIDs.Add(groupColorID);
                    else
                        allColorIDs.Add(Random.Range(0, blockPool.blockPrefabs.Length));
                }
                else
                {
                    allColorIDs.Add(Random.Range(0, blockPool.blockPrefabs.Length));
                }
            }
            else
            {
                allColorIDs.Add(Random.Range(0, blockPool.blockPrefabs.Length));
            }
        }

        FisherYatesShuffle(allColorIDs);

        for (int i = 0; i < _blockGrid.Length; i++)
        {
            if (_blockGrid[i] != null)
            {
                BlockBehavior bb = _blockGrid[i].GetComponent<BlockBehavior>();
                if (bb != null)
                    blockPool.ReturnBlock(_blockGrid[i], bb.prefabIndex);
                _blockGrid[i] = null;
            }
        }

        for (int i = 0; i < allColorIDs.Count; i++)
        {
            int row = i / columns;
            int col = i % columns;
            int colorID = Mathf.Clamp(allColorIDs[i], 0, blockPool.blockPrefabs.Length - 1);
            Vector2 pos = GetBlockPosition(row, col);
            GameObject newBlock = blockPool.GetBlock(colorID, pos, transform);
            if (newBlock == null)
            {
                Debug.LogError($"GameBoard: Failed to create block during deadlock resolution (Row: {row}, Column: {col})");
                continue;
            }
            _blockGrid[i] = newBlock;
            BlockBehavior bb = newBlock.GetComponent<BlockBehavior>();
            if (bb != null)
            {
                SetBlockProperties(bb, colorID);
                bb.OnBlockDestroyed += HandleBlockDestroyed;
            }
        }

        UpdateAllBlockSprites();
        yield return StartCoroutine(FadeBlocksIn(shuffleFadeDuration));
    }

    private void FisherYatesShuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            int temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }

    private IEnumerator FadeBlocksOut(float duration)
    {
        float elapsed = 0f;
        List<SpriteRenderer> srs = new List<SpriteRenderer>();
        foreach (var t in _blockGrid)
            if (t != null)
            {
                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr)
                    srs.Add(sr);
            }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(1f, 0f, t);
            foreach (var sr in srs)
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var sr in srs)
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
    }

    private IEnumerator FadeBlocksIn(float duration)
    {
        float elapsed = 0f;
        List<SpriteRenderer> srs = new List<SpriteRenderer>();
        Dictionary<SpriteRenderer, Color> targetColors = new Dictionary<SpriteRenderer, Color>();

        foreach (var t in _blockGrid)
            if (t != null)
            {
                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr)
                {
                    srs.Add(sr);
                    targetColors[sr] = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
                }
            }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(0f, 1f, t);
            foreach (var sr in srs)
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var sr in srs)
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
    }

    private void LerpBackgroundColor()
    {
        if (!Camera.main) return;

        _lerpT += _lerpForward ? Time.deltaTime / backgroundLerpTime : -Time.deltaTime / backgroundLerpTime;
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
            Camera.main.transform.position = new Vector3(originalPos.x + offsetX, originalPos.y + offsetY, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.position = originalPos;
    }

    private void HandleBlockDestroyed(BlockBehavior block)
    {
        block.OnBlockDestroyed -= HandleBlockDestroyed;
    }
}
