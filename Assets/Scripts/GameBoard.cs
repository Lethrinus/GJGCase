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
        GenerateBoard();
        yield return new WaitForSeconds(0.1f);
        UpdateAllBlockSprites();
        yield return new WaitForSeconds(0.2f);
        _isReady = true;
    }

    private void Update()
    {
        if (!_isReady) return;

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

    private void TryBlast(GameObject clickedBlock)
    {
        if (!_isReady) return;

        Vector2Int? pos = FindBlockPosition(clickedBlock);
        if (!pos.HasValue) return;

        List<int> group = GetConnectedGroup(GetIndex(pos.Value.x, pos.Value.y));
        if (group.Count < 2)
        {
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

        if (groupSize >= 7)
        {
            StartCoroutine(CameraShake(0.36f, 0.24f));
        }

        foreach (var index in group)
        {
            if (_blocks[index] is not null)
            {
                _blocksAnimating++;
                var bb = _blocks[index].GetComponent<BlockBehavior>();
                if (bb is not null)
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

        UpdateAllBlockSprites();
        
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
        Vector2Int cell = new Vector2Int(index / columns, index % columns);
        int r = cell.x, c = cell.y;

        if (r - 1 >= 0) yield return GetIndex(r - 1, c);
        if (r + 1 < rows) yield return GetIndex(r + 1, c);
        if (c - 1 >= 0) yield return GetIndex(r, c - 1);
        if (c + 1 < columns) yield return GetIndex(r, c + 1);
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
