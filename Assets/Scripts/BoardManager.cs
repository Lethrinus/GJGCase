using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour
{
    [Header("Data/References")]
    public BoardData boardData;
    public BoardGenerator boardGenerator;
    public DeadlockResolver deadlockResolver;
    public BlockPool blockPool;

    [Header("Board Setup")]
    public float blockSize = 1f;
    public int thresholdA = 4;
    public int thresholdB = 7;
    public int thresholdC = 9;
    public float moveSpeed = 40f;

    [Header("Deadlock & Shuffle")]
    public float shuffleFadeDuration = 0.4f;

    [Header("Background Lerp")]
    public Color backgroundColorA = new Color(0.1f, 0.2f, 0.8f);
    public Color backgroundColorB = new Color(0.3f, 0.4f, 0.95f);
    public float backgroundLerpTime = 10f;

    private bool _isReady;
    private int _blocksAnimating;
    private float _lerpT;
    private bool _lerpForward = true;

    // We store this so we can call e.g. StartCoroutine(RemoveGroupWithAnimation).
    private Coroutine _currentRemovalRoutine;

    [Obsolete("Obsolete")]
    private void Start()
    {
        // Load row/column from BoardSettings (or anywhere)
        int rows = BoardSettings.Rows;
        int columns = BoardSettings.Columns;

        // 1) Initialize board data
        boardData.Initialize(rows, columns, blockSize);

        // 2) Generate board
        boardGenerator.GenerateBoard(boardData, transform, thresholdA, thresholdB, thresholdC);

        // 3) Optional: center camera
        CenterCamera();

        // 4) Wait a frame, then check for deadlock
        StartCoroutine(InitializeBoardRoutine());

        // 5) Setup Input
        InputHandler inputHandler = FindObjectOfType<InputHandler>();
        if (inputHandler != null)
            inputHandler.OnBlockClicked += OnBlockClicked;
        else
            Debug.LogError("BoardManager: InputHandler not found!");
    }

    private IEnumerator InitializeBoardRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        UpdateAllBlockSprites();
        yield return new WaitForSeconds(0.2f);

        if (deadlockResolver.IsDeadlock(boardData))
        {
            yield return StartCoroutine(deadlockResolver.ResolveDeadlockOnce(
                boardData, transform, shuffleFadeDuration, thresholdA, thresholdB, thresholdC));
        }

        _isReady = true;
    }

    private void Update()
    {
        if (!_isReady) return;
        LerpBackgroundColor();
    }

    [Obsolete("Obsolete")]
    private void OnDestroy()
    {
        // Clean up event
        InputHandler inputHandler = FindObjectOfType<InputHandler>();
        if (inputHandler != null)
            inputHandler.OnBlockClicked -= OnBlockClicked;
    }

    // -----------------------------------------------------------
    //  Interaction: If the block is part of a group >= 2, remove it
    // -----------------------------------------------------------
    private void OnBlockClicked(GameObject clickedBlock)
    {
        if (!_isReady || clickedBlock == null) return;

        int? index = FindBlockIndex(clickedBlock);
        if (!index.HasValue) return;

        List<int> group = GetConnectedGroup(index.Value);
        if (group.Count < 2)
        {
            // Just buzz the block
            BlockBehavior bb = clickedBlock.GetComponent<BlockBehavior>();
            bb?.StartBuzz();
            return;
        }

        // Remove the group
        _isReady = false;
        if (_currentRemovalRoutine != null)
            StopCoroutine(_currentRemovalRoutine);

        _currentRemovalRoutine = StartCoroutine(RemoveGroupWithAnimation(group));
    }

    private int? FindBlockIndex(GameObject block)
    {
        for (int i = 0; i < boardData.blockGrid.Length; i++)
        {
            if (boardData.blockGrid[i] == block)
                return i;
        }
        return null;
    }

    private List<int> GetConnectedGroup(int startIndex)
    {
        List<int> groupList = new List<int>();
        Stack<int> stack = new Stack<int>();

        GameObject startGo = boardData.blockGrid[startIndex];
        if (!startGo) return groupList;

        BlockBehavior startBb = startGo.GetComponent<BlockBehavior>();
        if (!startBb) return groupList;

        int colorID = startBb.colorID;

        stack.Push(startIndex);

        while (stack.Count > 0)
        {
            int current = stack.Pop();
            if (groupList.Contains(current)) continue;
            groupList.Add(current);

            foreach (int neighbor in GetNeighbors(current))
            {
                GameObject nGo = boardData.blockGrid[neighbor];
                if (nGo != null)
                {
                    BlockBehavior nBb = nGo.GetComponent<BlockBehavior>();
                    if (nBb && nBb.colorID == colorID && !groupList.Contains(neighbor))
                        stack.Push(neighbor);
                }
            }
        }
        return groupList;
    }

    private IEnumerable<int> GetNeighbors(int index)
    {
        int r = index / boardData.columns;
        int c = index % boardData.columns;

        if (r - 1 >= 0) yield return boardData.GetIndex(r - 1, c);
        if (r + 1 < boardData.rows) yield return boardData.GetIndex(r + 1, c);
        if (c - 1 >= 0) yield return boardData.GetIndex(r, c - 1);
        if (c + 1 < boardData.columns) yield return boardData.GetIndex(r, c + 1);
    }

    // -----------------------------------------------------------
    //  Removing and Updating
    // -----------------------------------------------------------
    private IEnumerator RemoveGroupWithAnimation(List<int> groupList)
    {
        // Optional: if big group, camera shake
        if (groupList.Count >= 7)
            StartCoroutine(CameraShake(0.36f, 0.24f));

        // Animate each block blast
        int blocksAnimating = 0;
        foreach (int idx in groupList)
        {
            GameObject blockGo = boardData.blockGrid[idx];
            if (!blockGo) continue;

            blocksAnimating++;
            BlockBehavior bb = blockGo.GetComponent<BlockBehavior>();
            if (bb != null)
            {
                StartCoroutine(bb.BlastAnimation(0.3f, () =>
                {
                    boardGenerator.ReturnBlock(boardData, idx);
                    blocksAnimating--;
                }));
            }
        }

        // Wait until all blasts finish
        while (blocksAnimating > 0)
            yield return null;

        yield return StartCoroutine(UpdateBoardAfterRemoval());
        _isReady = true;
    }

    private IEnumerator UpdateBoardAfterRemoval()
    {
        yield return new WaitForSeconds(0.05f);

        // Gravity-like shift
        for (int c = 0; c < boardData.columns; c++)
        {
            int writeRow = boardData.rows - 1;
            for (int r = boardData.rows - 1; r >= 0; r--)
            {
                int idx = boardData.GetIndex(r, c);
                if (boardData.blockGrid[idx] != null)
                {
                    if (r != writeRow)
                    {
                        int writeIndex = boardData.GetIndex(writeRow, c);
                        boardData.blockGrid[writeIndex] = boardData.blockGrid[idx];
                        boardData.blockGrid[idx] = null;
                        StartCoroutine(MoveBlock(boardData.blockGrid[writeIndex],
                                                 boardData.GetBlockPosition(writeRow, c)));
                    }
                    writeRow--;
                }
            }

            // Replenish the top empty spaces
            for (int newRow = writeRow; newRow >= 0; newRow--)
            {
                GameObject newBlock = boardGenerator.SpawnBlock(boardData, newRow, c, transform,
                                                                thresholdA, thresholdB, thresholdC);
                if (newBlock != null)
                {
                    StartCoroutine(MoveBlock(newBlock, boardData.GetBlockPosition(newRow, c)));
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        UpdateAllBlockSprites();

        if (deadlockResolver.IsDeadlock(boardData))
        {
            yield return StartCoroutine(deadlockResolver.ResolveDeadlockOnce(
                boardData, transform, shuffleFadeDuration, thresholdA, thresholdB, thresholdC));
        }
    }

    private IEnumerator MoveBlock(GameObject block, Vector2 targetPos)
    {
        while (block != null)
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
        // For each block, figure out group size and update sprite.
        // (We do this to update the correct sprite based on group size.)
        // Just an example, adapted from your original code.

        List<int> group = new List<int>();
        Stack<int> stack = new Stack<int>();

        for (int i = 0; i < boardData.blockGrid.Length; i++)
        {
            GameObject blockGo = boardData.blockGrid[i];
            if (!blockGo) continue;

            group.Clear();
            stack.Clear();

            // BFS/DFS to find group size
            BlockBehavior bb = blockGo.GetComponent<BlockBehavior>();
            if (bb == null) continue;

            int colorID = bb.colorID;
            stack.Push(i);

            while (stack.Count > 0)
            {
                int current = stack.Pop();
                if (group.Contains(current)) continue;
                group.Add(current);

                foreach (int neighbor in GetNeighbors(current))
                {
                    GameObject nGo = boardData.blockGrid[neighbor];
                    if (nGo != null)
                    {
                        BlockBehavior nBb = nGo.GetComponent<BlockBehavior>();
                        if (nBb != null && nBb.colorID == colorID && !group.Contains(neighbor))
                            stack.Push(neighbor);
                    }
                }
            }

            int size = group.Count;
            // Update sprite for each index in that group
            foreach (var idx in group)
            {
                var go = boardData.blockGrid[idx];
                if (!go) continue;
                var blockBehavior = go.GetComponent<BlockBehavior>();
                blockBehavior?.UpdateSpriteBasedOnGroupSize(size);
            }
        }
    }

    // -----------------------------------------------------------
    //  Utility: Camera centering, background color, camera shake
    // -----------------------------------------------------------
    private void CenterCamera()
    {
        Camera cam = Camera.main;
        if (!cam) return;

        float boardWidth = (boardData.columns - 1) * boardData.blockSize;
        float boardHeight = (boardData.rows - 1) * boardData.blockSize;

        float centerX = boardWidth * 0.5f;
        float centerY = boardHeight * 0.5f;

        cam.transform.position = new Vector3(centerX, centerY, cam.transform.position.z);

        // Fit camera
        float halfBoardWidth = boardWidth * 0.5f;
        float halfBoardHeight = boardHeight * 0.5f;
        float aspectRatio = Screen.width / (float)Screen.height;

        float halfCamHeight;

        if (halfBoardWidth / aspectRatio > halfBoardHeight)
        {
            var halfCamWidth = halfBoardWidth;
            halfCamHeight = halfCamWidth / aspectRatio;
        }
        else
        {
            halfCamHeight = halfBoardHeight;
/*
            halfCamWidth = halfCamHeight * aspectRatio;
*/
        }

        float margin = 3f;
        float zoomFactor = 1.1f;
        float finalSize = (halfCamHeight + margin) * zoomFactor;
        cam.orthographicSize = finalSize;
    }

    private void LerpBackgroundColor()
    {
        Camera cam = Camera.main;
        if (!cam) return;

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

        cam.backgroundColor = Color.Lerp(backgroundColorA, backgroundColorB, _lerpT);
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (!cam) yield break;

        Vector3 originalPos = cam.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            cam.transform.position = new Vector3(originalPos.x + offsetX, originalPos.y + offsetY, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.transform.position = originalPos;
    }
}
