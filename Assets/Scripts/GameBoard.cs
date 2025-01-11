using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private GameObject[] _blocks; 
    private bool _isReady = false;
    private int _blocksAnimating = 0;
    private float _lerpT = 0f;
    private bool _lerpForward = true;

    private void Start()
    {
        _blocks = new GameObject[rows * columns];
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
        _isReady = true;
    }

    private void Update()
    {
        if (!_isReady) return;

        if (Input.GetMouseButtonDown(0))
        {
           Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                 RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

               if (hit.collider != null)
               {
                  TryBlast(hit.collider.gameObject);
               }
        }
        LerpBackgroundColor();
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
                _blocks[GetIndex(r, c)] = block;

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

    private int GetIndex(int row, int col)
    {
        return row * columns + col;
    }

    private void TryBlast(GameObject clickedBlock)
    {
        if (!_isReady) return;

        Vector2Int? pos = FindBlockPosition(clickedBlock);
        if (!pos.HasValue) return;

        List<int> group = GetConnectedGroup(GetIndex(pos.Value.x, pos.Value.y));
        if (group.Count < 2)
        {
            var bb = clickedBlock.GetComponent<BlockBehavior>();
            if (bb != null)
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

        if (groupSize >= 7)
        {
            StartCoroutine(CameraShake(0.36f, 0.24f));
        }

        foreach (var index in group)
        {
            if (_blocks[index] != null)
            {
                _blocksAnimating++;
                var bb = _blocks[index].GetComponent<BlockBehavior>();
                if (bb != null)
                {
                    StartCoroutine(bb.BlastAnimation(0.3f, onComplete: () =>
                    {
                        _blocksAnimating--;
                    }));
                }
            }
        }

        foreach (var index in group)
        {
            _blocks[index] = null;
        }

        while (_blocksAnimating > 0)
            yield return null;

        yield return StartCoroutine(UpdateBoardAfterRemoval());
        _isReady = true;
    }

    private IEnumerator UpdateBoardAfterRemoval()
    {
        yield return new WaitForSeconds(0.1f);

        for (int c = 0; c < columns; c++)
        {
            int writeRow = rows - 1;
            for (int r = rows - 1; r >= 0; r--)
            {
                int index = GetIndex(r, c);
                if (_blocks[index] != null)
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

            for (int newRow = writeRow; newRow >= 0; newRow--)
            {
                int idx = Random.Range(0, blockPrefabs.Length);
                Vector2 spawnPos = GetBlockPosition(-1, c);

                GameObject newBlock = Instantiate(blockPrefabs[idx], spawnPos, Quaternion.identity, transform);
                _blocks[GetIndex(newRow, c)] = newBlock;

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
                if (!visited[neighbor] && _blocks[neighbor] != null)
                {
                    var nb = _blocks[neighbor].GetComponent<BlockBehavior>();
                    if (nb != null && nb.colorID == colorID)
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
        Vector2Int cell = new Vector2Int(index / columns, index % columns);
        int r = cell.x, c = cell.y;

        if (r - 1 >= 0) yield return GetIndex(r - 1, c);
        if (r + 1 < rows) yield return GetIndex(r + 1, c);
        if (c - 1 >= 0) yield return GetIndex(r, c - 1);
        if (c + 1 < columns) yield return GetIndex(r, c + 1);
    }

    private bool CheckForDeadlock()
    {
        bool[] visited = new bool[_blocks.Length];
        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i] == null || visited[i]) continue;

            List<int> group = GetConnectedGroup(i);
            foreach (int index in group)
                visited[index] = true;

            if (group.Count >= 2) return false;
        }
        return true;
    }

    private void ResolveDeadlock()
    {
        if (TrySingleSwapToCreateMatch())
        {
            UpdateAllBlockSprites();
            return;
        }

        RandomShuffle();
    }

    private bool TrySingleSwapToCreateMatch()
    {
        for (int i1 = 0; i1 < _blocks.Length; i1++)
        {
            if (_blocks[i1] == null) continue;

            for (int i2 = i1 + 1; i2 < _blocks.Length; i2++)
            {
                if (_blocks[i2] == null) continue;

                var bb1 = _blocks[i1].GetComponent<BlockBehavior>();
                var bb2 = _blocks[i2].GetComponent<BlockBehavior>();
                if (bb1 == null || bb2 == null) continue;

                int temp = bb1.colorID;
                bb1.colorID = bb2.colorID;
                bb2.colorID = temp;

                if (FormsARealMatch(i1) || FormsARealMatch(i2))
                {
                    return true;
                }

                bb1.colorID = temp;
                bb2.colorID = bb1.colorID;
            }
        }
        return false;
    }

    private bool FormsARealMatch(int index)
    {
        if (_blocks[index] == null) return false;

        List<int> group = GetConnectedGroup(index);
        return group.Count >= 2;
    }

    private void RandomShuffle()
    {
        List<int> colorList = new List<int>();
        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i] == null) continue;

            var bb = _blocks[i].GetComponent<BlockBehavior>();
            if (bb != null)
            {
                colorList.Add(bb.colorID);
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
        for (int i = 0; i < _blocks.Length; i++)
        {
            if (_blocks[i] == null) continue;

            var bb = _blocks[i].GetComponent<BlockBehavior>();
            if (bb != null)
            {
                bb.colorID = colorList[index];
                index++;
            }
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
