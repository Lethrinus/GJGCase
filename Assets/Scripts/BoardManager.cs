using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using System;

public class BoardManager : MonoBehaviour
{
    public BoardData boardData;
    public BoardGenerator boardGenerator;
    public DeadlockResolver deadlockResolver;
    public BlockPool blockPool;
    public BoardConfig boardConfig;
    public ColorPalette colorPalette;
    public ParticlePool particlePool;

    int movesLeft; 
    int blocksDestroyed;
    bool isReady;

    void Start()
    {
        int rows = BoardSettings.Rows;
        int cols = BoardSettings.Columns;
        boardData.Initialize(rows, cols, boardConfig.blockWidth, boardConfig.blockHeight);
        boardGenerator.GenerateBoard(boardData, transform, boardConfig.thresholdA, boardConfig.thresholdB, boardConfig.thresholdC);

        movesLeft = boardConfig.initialMoves;
        blocksDestroyed = 0;            

        CenterCamera();
        InputHandler ih = FindObjectOfType<InputHandler>();
        if (ih) ih.OnBlockClicked += OnBlockClicked;
        UpdateAllBlockSprites();
        if (deadlockResolver.IsDeadlock(boardData)) ResolveDeadlockSequence(() => { isReady = true; });
        else isReady = true;
    }

    void OnDestroy()
    {
        InputHandler ih = FindObjectOfType<InputHandler>();
        if (ih) ih.OnBlockClicked -= OnBlockClicked;
    }

    void OnBlockClicked(BlockBehavior clicked)
    {
        if (!isReady || !clicked) return;
        int? idx = FindBlockIndex(clicked);
        if (!idx.HasValue) return;
        List<int> group = GetConnectedGroup(idx.Value);
        if (group.Count < 2)
        {
            clicked.StartBuzz();
            return;
        }
        isReady = false;
        if (group.Count >= 4)
        {
            GatherAndRemoveGroupSequence(clicked.transform.position, group, () =>
            {
                movesLeft--;
                blocksDestroyed += group.Count;  
                UpdateBoardSequence(() => { isReady = true; });
            });
        }
        else
        {
            RemoveGroupWithParticlePool(group, () =>
            {
                movesLeft--;
                blocksDestroyed += group.Count;
                UpdateBoardSequence(() => { isReady = true; });
            });
        }
    }

    void RemoveGroupWithParticlePool(List<int> group, Action onComplete)
    {
        foreach (int i in group)
        {
            var block = boardData.blockGrid[i];
            if (!block) continue;
            if (particlePool) particlePool.SpawnParticle(block.transform.position, GetColorFromID(block.colorID));
            boardGenerator.ReturnBlock(boardData, i);
        }
        onComplete?.Invoke();
    }

    Color GetColorFromID(int colorID)
    {
        if (!colorPalette || colorPalette.colors == null || colorPalette.colors.Length == 0) return Color.white;
        if (colorID < 0 || colorID >= colorPalette.colors.Length) colorID = 0;
        return colorPalette.colors[colorID];
    }

    void GatherAndRemoveGroupSequence(Vector2 gatherPoint, List<int> group, Action onComplete)
    {
        Sequence seq = DOTween.Sequence();
        float shineDuration = 0.2f;
        float gatherDuration = 0.25f;
        float blastDuration = 0.3f;

        foreach (int i in group)
        {
            var block = boardData.blockGrid[i];
            if (!block) continue;
            if (block.SpriteRenderer) block.SpriteRenderer.sortingOrder = 9999;
        }

        foreach (int i in group)
        {
            var block = boardData.blockGrid[i];
            if (!block) continue;
            seq.Join(block.transform.DOScale(block.transform.localScale * 1.2f, shineDuration).SetLoops(1, LoopType.Yoyo).SetEase(Ease.InOutQuad));
            if (block.SpriteRenderer) seq.Join(block.SpriteRenderer.DOColor(block.SpriteRenderer.color, shineDuration).SetLoops(1, LoopType.Yoyo).SetEase(Ease.InOutQuad));
        }
        seq.AppendInterval(0f);
        foreach (int i in group)
        {
            var block = boardData.blockGrid[i];
            if (!block) continue;
            seq.Join(block.transform.DOMove(gatherPoint, gatherDuration).SetEase(Ease.InQuad));
            seq.Join(block.transform.DOScale(block.transform.localScale * 0.8f, gatherDuration).SetEase(Ease.OutQuad));
        }
        seq.AppendInterval(0f);
        foreach (int i in group)
        {
            var block = boardData.blockGrid[i];
            if (!block) continue;
            seq.Join(block.transform.DOScale(block.transform.localScale * 1.5f, blastDuration));
            if (block.SpriteRenderer) seq.Join(block.SpriteRenderer.DOFade(0f, blastDuration));
        }
        seq.OnComplete(() =>
        {
            foreach (int i in group) boardGenerator.ReturnBlock(boardData, i);
            onComplete?.Invoke();
        });
    }

    void UpdateBoardSequence(Action onComplete)
    {
        Sequence seq = DOTween.Sequence();
        for (int c = 0; c < boardData.columns; c++)
        {
            int writeRow = boardData.rows - 1;
            for (int r = boardData.rows - 1; r >= 0; r--)
            {
                int idx = boardData.GetIndex(r, c);
                var block = boardData.blockGrid[idx];
                if (block)
                {
                    if (r != writeRow)
                    {
                        int wIdx = boardData.GetIndex(writeRow, c);
                        boardData.blockGrid[wIdx] = block;
                        boardData.blockGrid[idx] = null;
                        block.SetSortingOrder(writeRow);
                        Vector2 targetPos = boardData.GetBlockPosition(writeRow, c);
                        float dist = Vector2.Distance(block.transform.localPosition, targetPos);
                        float dur = dist / boardConfig.moveSpeed;
                        Sequence moveAndJump = DOTween.Sequence();
                        moveAndJump
                            .Append(block.transform.DOLocalMove(targetPos, dur).SetEase(Ease.Linear))
                            .AppendCallback(() => {
                                if (block && block.transform) block.transform.DOJump(targetPos, 0.6f, 1, 0.5f).SetEase(Ease.OutCubic);
                            });
                        seq.Join(moveAndJump);
                    }
                    writeRow--;
                }
            }
            for (int newRow = writeRow; newRow >= 0; newRow--)
            {
                var nb = boardGenerator.SpawnBlock(boardData, newRow, c, transform, boardConfig.thresholdA, boardConfig.thresholdB, boardConfig.thresholdC);
                if (nb)
                {
                    Vector2 tp = boardData.GetBlockPosition(newRow, c);
                    float dist = Vector2.Distance(nb.transform.localPosition, tp);
                    float dur = dist / boardConfig.moveSpeed;
                    Sequence moveAndJumpNew = DOTween.Sequence();
                    moveAndJumpNew
                        .Append(nb.transform.DOLocalMove(tp, dur).SetEase(Ease.Linear))
                        .AppendCallback(() => {
                            if (nb && nb.transform) nb.transform.DOJump(tp, 0.6f, 1, 0.5f).SetEase(Ease.OutCubic);
                        });
                    seq.Join(moveAndJumpNew);
                }
            }
        }
        seq.OnComplete(() =>
        {
            UpdateAllBlockSprites();
            if (deadlockResolver.IsDeadlock(boardData))
            {
                ResolveDeadlockSequence(() => onComplete());
            }
            else
            {
                onComplete();
            }
        });
    }

    void ResolveDeadlockSequence(Action onComplete)
    {
        deadlockResolver.ResolveDeadlockFullRefill(boardData, transform, boardConfig.shuffleFadeDuration, boardConfig.thresholdA, boardConfig.thresholdB, boardConfig.thresholdC, onComplete);
    }

    int? FindBlockIndex(BlockBehavior block)
    {
        for (int i = 0; i < boardData.blockGrid.Length; i++)
        {
            if (boardData.blockGrid[i] == block) return i;
        }
        return null;
    }

    List<int> GetConnectedGroup(int startIndex)
    {
        List<int> group = new List<int>();
        Stack<int> stack = new Stack<int>();
        var startBlock = boardData.blockGrid[startIndex];
        if (!startBlock) return group;
        int colID = startBlock.colorID;
        stack.Push(startIndex);
        while (stack.Count > 0)
        {
            int current = stack.Pop();
            if (group.Contains(current)) continue;
            group.Add(current);
            foreach (int neighborIndex in GetNeighbors(current))
            {
                var nb = boardData.blockGrid[neighborIndex];
                if (nb && nb.colorID == colID && !group.Contains(neighborIndex)) stack.Push(neighborIndex);
            }
        }
        return group;
    }

    IEnumerable<int> GetNeighbors(int index)
    {
        int r = index / boardData.columns;
        int c = index % boardData.columns;
        if (r - 1 >= 0) yield return boardData.GetIndex(r - 1, c);
        if (r + 1 < boardData.rows) yield return boardData.GetIndex(r + 1, c);
        if (c - 1 >= 0) yield return boardData.GetIndex(r, c - 1);
        if (c + 1 < boardData.columns) yield return boardData.GetIndex(r, c + 1);
    }

    void UpdateAllBlockSprites()
    {
        bool[] visited = new bool[boardData.blockGrid.Length];
        for (int i = 0; i < boardData.blockGrid.Length; i++)
        {
            if (boardData.blockGrid[i] == null || visited[i]) continue;
            int colorID = boardData.blockGrid[i].colorID;
            List<int> group = new List<int>();
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
                    var neighborBlock = boardData.blockGrid[neighbor];
                    if (neighborBlock && neighborBlock.colorID == colorID && !visited[neighbor]) stack.Push(neighbor);
                }
            }
            int groupSize = group.Count;
            foreach (int indexInGroup in group)
            {
                var blockInGroup = boardData.blockGrid[indexInGroup];
                if (blockInGroup) blockInGroup.UpdateSpriteBasedOnGroupSize(groupSize);
            }
        }
    }

    void CenterCamera()
    {
        Camera cam = Camera.main;
        if (!cam) return;
        float w = (boardData.columns - 1) * boardData.blockWidth;
        float h = (boardData.rows - 1) * boardData.blockHeight;
        float cx = w * 0.5f;
        float cy = h * 0.75f;
        float offsetY = 2f;
        cam.transform.position = new Vector3(cx, cy + offsetY, cam.transform.position.z);
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;
        float ar = Screen.width / (float)Screen.height;
        float halfCamH = Mathf.Max(halfW / ar, halfH);
        float margin = 9f;
        float zoom = 1.1f;
        cam.orthographicSize = (halfCamH + margin) * zoom;
    }
    public int GetMovesLeft()
    {
        return movesLeft;
    }

    public int GetBlocksDestroyed()
    {
        return blocksDestroyed;
    }
   
}
