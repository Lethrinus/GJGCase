using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class BoardManager : MonoBehaviour
{
    public BoardData boardData;
    public BoardGenerator boardGenerator;
    public DeadlockResolver deadlockResolver;
    public BlockPool blockPool;
    public BoardConfig boardConfig;
    bool isReady;
    void Start()
    {
        int rows = BoardSettings.Rows;
        int cols = BoardSettings.Columns;
        boardData.Initialize(rows, cols, boardConfig.blockSize);
        boardGenerator.GenerateBoard(boardData, transform, boardConfig.thresholdA, boardConfig.thresholdB, boardConfig.thresholdC);
        CenterCamera();
        InputHandler ih = FindObjectOfType<InputHandler>();
        if (ih) ih.OnBlockClicked += OnBlockClicked;
        UpdateAllBlockSprites();
        if (deadlockResolver.IsDeadlock(boardData))
        {
            ResolveDeadlockSequence(() => { isReady = true; });
        }
        else
        {
            isReady = true;
        }
    }
    void OnDestroy()
    {
        InputHandler ih = FindObjectOfType<InputHandler>();
        if (ih) ih.OnBlockClicked -= OnBlockClicked;
    }
    void OnBlockClicked(BlockBehavior clicked)
    {
        if (!isReady || clicked == null) return;
        int? idx = FindBlockIndex(clicked);
        if (!idx.HasValue) return;
        List<int> group = GetConnectedGroup(idx.Value);
        if (group.Count < 2)
        {
            clicked.StartBuzz();
            return;
        }
        isReady = false;
        RemoveGroupSequence(group, () =>
        {
            UpdateBoardSequence(() =>
            {
                isReady = true;
            });
        });
    }
    void RemoveGroupSequence(List<int> group, System.Action onComplete)
    {
        if (group.Count >= 7) CameraShake(boardConfig.shakeDuration, boardConfig.shakeMagnitude);
        Sequence removeSeq = DOTween.Sequence();
        foreach (int i in group)
        {
            var block = boardData.blockGrid[i];
            if (!block) continue;
            Sequence blast = DOTween.Sequence();
            blast.Join(block.transform.DOScale(block.transform.localScale * 1.5f, 0.3f));
            if (block.SpriteRenderer) blast.Join(block.SpriteRenderer.DOFade(0f, 0.3f));
            blast.OnComplete(() =>
            {
                boardGenerator.ReturnBlock(boardData, i);
            });
            removeSeq.Join(blast);
        }
        removeSeq.OnComplete(() => onComplete());
    }
    void UpdateBoardSequence(System.Action onComplete)
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
                seq.Join(
                    block.transform.DOLocalMove(targetPos, dur).SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            // Bounce only if it lands on row 0
                            if (writeRow == 0)
                            {
                                block.transform.DOJump(targetPos, 0.6f, 1, 0.75f).SetEase(Ease.OutCubic);
                            }
                        })
                );
            }
            writeRow--;
        }
    }

    for (int newRow = writeRow; newRow >= 0; newRow--)
    {
        var nb = boardGenerator.SpawnBlock(boardData, newRow, c, transform,
                                           boardConfig.thresholdA, boardConfig.thresholdB, boardConfig.thresholdC);
        if (nb)
        {
            Vector2 tp = boardData.GetBlockPosition(newRow, c);
            float dist = Vector2.Distance(nb.transform.localPosition, tp);
            float dur = dist / boardConfig.moveSpeed;
            seq.Join(
                nb.transform.DOLocalMove(tp, dur).SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        // Removed the if-check here, so new blocks always bounce:
                        nb.transform.DOJump(tp, 0.6f, 1, 0.75f).SetEase(Ease.OutCubic);
                    })
            );
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
    void ResolveDeadlockSequence(System.Action onComplete)
    {
        deadlockResolver.ResolveDeadlockOnceNoCoroutines(boardData, transform, boardConfig.shuffleFadeDuration,
            boardConfig.thresholdA, boardConfig.thresholdB, boardConfig.thresholdC, onComplete);
    }
    int? FindBlockIndex(BlockBehavior block)
    {
        for (int i = 0; i < boardData.blockGrid.Length; i++)
        {
            if (boardData.blockGrid[i] == block) return i;
        }
        return null;
    }
    List<int> GetConnectedGroup(int start)
    {
        List<int> group = new List<int>();
        Stack<int> stack = new Stack<int>();
        var begin = boardData.blockGrid[start];
        if (!begin) return group;
        int colID = begin.colorID;
        stack.Push(start);
        while (stack.Count > 0)
        {
            int cur = stack.Pop();
            if (group.Contains(cur)) continue;
            group.Add(cur);
            foreach (int nb in GetNeighbors(cur))
            {
                var b = boardData.blockGrid[nb];
                if (b && b.colorID == colID && !group.Contains(nb)) stack.Push(nb);
            }
        }
        return group;
    }
    IEnumerable<int> GetNeighbors(int i)
    {
        int r = i / boardData.columns;
        int c = i % boardData.columns;
        if (r - 1 >= 0) yield return boardData.GetIndex(r - 1, c);
        if (r + 1 < boardData.rows) yield return boardData.GetIndex(r + 1, c);
        if (c - 1 >= 0) yield return boardData.GetIndex(r, c - 1);
        if (c + 1 < boardData.columns) yield return boardData.GetIndex(r, c + 1);
    }
    void UpdateAllBlockSprites()
    {
        List<int> group = new List<int>();
        Stack<int> stack = new Stack<int>();
        for (int i = 0; i < boardData.blockGrid.Length; i++)
        {
            var blk = boardData.blockGrid[i];
            if (!blk) continue;
            group.Clear();
            stack.Clear();
            int colID = blk.colorID;
            stack.Push(i);
            while (stack.Count > 0)
            {
                int cur = stack.Pop();
                if (group.Contains(cur)) continue;
                group.Add(cur);
                foreach (int nb in GetNeighbors(cur))
                {
                    var b = boardData.blockGrid[nb];
                    if (b && b.colorID == colID && !group.Contains(nb)) stack.Push(nb);
                }
            }
            int sz = group.Count;
            foreach (int x in group)
            {
                var b = boardData.blockGrid[x];
                if (b) b.UpdateSpriteBasedOnGroupSize(sz);
            }
        }
    }
    void CenterCamera()
    {
        var cam = Camera.main;
        if (!cam) return;
        float w = (boardData.columns - 1) * boardData.blockSize;
        float h = (boardData.rows - 1) * boardData.blockSize;
        float cx = w * 0.5f;
        float cy = h * 0.5f;
        cam.transform.position = new Vector3(cx, cy, cam.transform.position.z);
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;
        float ar = Screen.width / (float)Screen.height;
        float halfCamH;
        if (halfW / ar > halfH) halfCamH = (halfW / ar);
        else halfCamH = halfH;
        float margin = 3f;
        float zoom = 1.1f;
        cam.orthographicSize = (halfCamH + margin) * zoom;
    }
    void CameraShake(float dur, float mag)
    {
        var cam = Camera.main;
        if (cam) cam.DOShakePosition( dur, mag, 20, 90, false);
    }
}
