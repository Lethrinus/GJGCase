using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;

public class BoardManager : MonoBehaviour
{
    // Inspector-assigned references
    public BoardData boardData;
    public BoardGenerator boardGenerator;
    public DeadlockResolver deadlockResolver;
    public BoardConfig boardConfig;
    public BlockPool blockPool;
    public ColorPalette colorPalette;
    public ParticlePool particlePool;
    public Transform environmentParent;

    public InputHandler inputHandler;
    public CrateBehavior cratePrefab;

    // Dictionary tracking cells reserved for crates
    private readonly Dictionary<int, CrateBehavior> _crateGrid = new();
    
    // Gameplay counters
    private int _movesLeft;
    private int _blocksDestroyed;
    private int _targetBlocksDestroyed; 
    private int _targetCratesDestroyed; 
    private bool _isReady;
    
    //DOTween setup
    [SerializeField] private int tweenersCapacity = 500;
    [SerializeField] private int sequencesCapacity = 200;
    
    private void Awake()
    {
        DOTween.SetTweensCapacity(tweenersCapacity, sequencesCapacity);
    }
    private void Start()
    {
        boardConfig = LevelTracker.selectedConfig;
        if (boardConfig == null)
        {
            Debug.LogWarning("No BoardConfig found. Using default config.");
        }
        
        if (boardConfig.environmentPrefab != null)
        {
            GameObject envInstance = Instantiate(
                boardConfig.environmentPrefab,
                environmentParent ? environmentParent : transform
            );
            envInstance.name = boardConfig.environmentPrefab.name;
        }

        if (boardConfig.uiCanvasPrefab != null)
        {
            CanvasRoot uiCanvasInstance = Instantiate(
                boardConfig.uiCanvasPrefab,
                environmentParent ? environmentParent : transform
            );
            uiCanvasInstance.name = boardConfig.uiCanvasPrefab.name;
            GoalMoveUI gmUI = uiCanvasInstance.goalMoveUI;
            if (gmUI != null)
            {
                gmUI.boardManager = this;
                gmUI.boardConfig = boardConfig;
            }
        }
        

        boardData.Initialize(boardConfig.rows, boardConfig.columns,
            boardConfig.blockWidth, boardConfig.blockHeight,
            boardConfig.useShapeMask ? boardConfig.cellMask : null);

        boardGenerator.GenerateBoard(boardData, transform,
            boardConfig.thresholdA, boardConfig.thresholdB, boardConfig.thresholdC);

        
        if (boardConfig.useCrates && cratePrefab != null)
        {
            InitializeCrates();
        }

        _movesLeft = boardConfig.initialMoves;
        _blocksDestroyed = 0;
        _targetBlocksDestroyed = 0;
        _targetCratesDestroyed = 0;

        CenterCamera();

        if (inputHandler != null)
            inputHandler.OnBlockClicked += OnBlockClicked;
        else
            Debug.LogError("InputHandler not assigned in BoardManager!");

        UpdateAllBlockSprites();

        if (deadlockResolver.IsDeadlock(boardData))
            ResolveDeadlockSequence(() => { _isReady = true; });
        else
            _isReady = true;
    }
    
    private void OnDestroy()
    {
        DOTween.KillAll();
        if (inputHandler != null)
            inputHandler.OnBlockClicked -= OnBlockClicked;
    }
    

    public int GetMovesLeft() { return _movesLeft; }
    public int GetBlocksDestroyed() { return _blocksDestroyed; }
    public int GetTargetBlocksDestroyed() { return _targetBlocksDestroyed; }
    public int GetTargetCratesDestroyed() { return _targetCratesDestroyed; }
    

    private void OnBlockClicked(BlockBehavior clicked)
    {
        if (!_isReady || clicked == null)
            return;

        int? idx = FindBlockIndex(clicked);
        if (!idx.HasValue)
            return;

        List<int> group = GetConnectedGroup(idx.Value);
        if (group.Count < 2)
        {
            clicked.StartBuzz();
            return;
        }

        _isReady = false;

       
        if (!boardConfig.useCrates)
        {
            int groupCount = group.Count;
            for (int i = 0; i < groupCount; i++)
            {
                int cellIndex = group[i];
                BlockBehavior block = boardData.blockGrid[cellIndex];
                if (block != null && block.colorID == boardConfig.targetBlockColorID)
                    _targetBlocksDestroyed++;
            }
        }

        
        Action removalComplete = () =>
        {
            if (boardConfig.useCrates)
            {
                RemoveAdjacentCrates(group, () =>
                {
                    _movesLeft--;
                    _blocksDestroyed += group.Count;
                    UpdateBoardSequence(() => { _isReady = true; });
                });
            }
            else
            {
                _movesLeft--;
                _blocksDestroyed += group.Count;
                UpdateBoardSequence(() => { _isReady = true; });
            }
        };

        if (group.Count >= 4)
            GatherAndRemoveGroupSequence(clicked.transform.position, group, removalComplete);
        else
            RemoveGroupWithParticlePool(group, removalComplete);
    }

    

    private void InitializeCrates()
    {
        int startRow = boardData.rows - 3; 
        int cols = boardData.columns;
        for (int r = startRow; r < boardData.rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (!boardData.IsValidCell(r, c))
                    continue;
                int index = boardData.GetIndex(r, c);
                Vector2 pos = boardData.GetBlockPosition(r, c);
                if (boardData.blockGrid[index] != null)
                    boardGenerator.ReturnBlock(boardData, index);
                CrateBehavior crate = Instantiate(cratePrefab, transform);
                crate.transform.localPosition = pos;
                _crateGrid[index] = crate;
            }
        }
    }

    private void RemoveAdjacentCrates(List<int> group, Action onComplete)
    {
        HashSet<int> cratesToBlast = new HashSet<int>();
        foreach (int cellIndex in group)
        {
            foreach (int neighbor in GetNeighbors(cellIndex))
            {
                if (_crateGrid.ContainsKey(neighbor))
                    cratesToBlast.Add(neighbor);
            }
        }
        if (cratesToBlast.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }
    
        int remaining = cratesToBlast.Count;
    
        foreach (int crateIndex in cratesToBlast)
        {
            if (_crateGrid.TryGetValue(crateIndex, out CrateBehavior crate))
            {
                crate.Blast(() =>
                {
                    
                    _crateGrid.Remove(crateIndex);
                    _targetCratesDestroyed++;
                    remaining--;
                    if (remaining <= 0)
                        onComplete?.Invoke();
                });
            }
            else
            {
                remaining--;
                if (remaining <= 0)
                    onComplete?.Invoke();
            }
        }
    }

    
    private void UpdateBoardSequence(Action onComplete)
    {
        Sequence seq = DOTween.Sequence();
        int cols = boardData.columns;
        for (int col = 0; col < cols; col++)
        {
            List<int> validRows = new List<int>();
            for (int row = boardData.rows - 1; row >= 0; row--)
            {
                int cellIndex = boardData.GetIndex(row, col);
                if (boardData.IsValidCell(row, col) && (!boardConfig.useCrates || !_crateGrid.ContainsKey(cellIndex)))
                    validRows.Add(row);
            }

            int writeIndex = 0;
            int validCount = validRows.Count;
            for (int readIndex = 0; readIndex < validCount; readIndex++)
            {
                int actualRow = validRows[readIndex];
                int blockIndex = boardData.GetIndex(actualRow, col);
                BlockBehavior block = boardData.blockGrid[blockIndex];
                if (block != null)
                {
                    if (readIndex != writeIndex)
                    {
                        int targetRow = validRows[writeIndex];
                        int wIdx = boardData.GetIndex(targetRow, col);
                        boardData.blockGrid[wIdx] = block;
                        boardData.blockGrid[blockIndex] = null;
                        block.SetSortingOrder(targetRow);
                        Vector2 targetPos = boardData.GetBlockPosition(targetRow, col);
                        float dist = Vector2.Distance(block.transform.localPosition, targetPos);
                        float dur = dist / boardConfig.moveSpeed;
                        block.transform.DOKill(true);
                        Sequence moveSeq = DOTween.Sequence()
                            .Append(block.transform.DOLocalMove(targetPos, dur).SetEase(Ease.Linear))
                            .AppendCallback(() =>
                            {
                                if (block != null && block.transform != null)
                                {
                                    block.transform.DOKill(true);
                                    block.transform.DOJump(targetPos, 0.6f, 1, 0.5f).SetEase(Ease.OutCubic);
                                }
                            });
                        seq.Join(moveSeq);
                    }
                    writeIndex++;
                }
            }
            for (int i = validCount - 1; i >= writeIndex; i--)
            {
                int spawnRow = validRows[i];
                BlockBehavior newBlock = boardGenerator.SpawnBlock(boardData, spawnRow, col, transform,
                    boardConfig.thresholdA, boardConfig.thresholdB, boardConfig.thresholdC);
                if (newBlock != null)
                {
                    Vector2 targetPos = boardData.GetBlockPosition(spawnRow, col);
                    float dist = Vector2.Distance(newBlock.transform.localPosition, targetPos);
                    float dur = dist / boardConfig.moveSpeed;
                    newBlock.transform.DOKill(true);
                    Sequence spawnSeq = DOTween.Sequence()
                        .Append(newBlock.transform.DOLocalMove(targetPos, dur).SetEase(Ease.Linear))
                        .AppendCallback(() =>
                        {
                            if (newBlock != null && newBlock.transform != null)
                            {
                                newBlock.transform.DOKill(true);
                                newBlock.transform.DOJump(targetPos, 0.6f, 1, 0.5f).SetEase(Ease.OutCubic);
                            }
                        });
                    seq.Join(spawnSeq);
                }
            }
        }
        seq.OnComplete(() =>
        {
            UpdateAllBlockSprites();
            if (deadlockResolver.IsDeadlock(boardData))
                ResolveDeadlockSequence(() => onComplete());
            else
                onComplete();
        });
    }

    private void ResolveDeadlockSequence(Action onComplete)
    {
        
        if (boardConfig.useCrates)
        {
            HashSet<int> reservedIndices = new HashSet<int>(_crateGrid.Keys);
            deadlockResolver.ResolveDeadlockFullRefill(boardData, transform, boardConfig.shuffleFadeDuration, boardConfig.thresholdA, boardConfig.thresholdB, boardConfig.thresholdC, () => { UpdateAllBlockSprites(); onComplete?.Invoke();},
                reservedIndices
            );
        }
        else
        {
            deadlockResolver.ResolveDeadlockFullRefill(boardData, transform, boardConfig.shuffleFadeDuration, boardConfig.thresholdA, boardConfig.thresholdB, boardConfig.thresholdC, () =>
                { UpdateAllBlockSprites(); onComplete?.Invoke(); }
            );
        }
    }

    private int? FindBlockIndex(BlockBehavior block)
    {
        int length = boardData.blockGrid.Length;
        for (int i = 0; i < length; i++)
        {
            if (boardData.blockGrid[i] == block)
                return i;
        }
        return null;
    }

    private List<int> GetConnectedGroup(int startIndex)
    {
        List<int> group = new List<int>();
        Stack<int> stack = new Stack<int>();
        BlockBehavior startBlock = boardData.blockGrid[startIndex];
        if (startBlock == null)
            return group;
        int targetColor = startBlock.colorID;
        stack.Push(startIndex);
        while (stack.Count > 0)
        {
            int current = stack.Pop();
            if (group.Contains(current))
                continue;
            group.Add(current);
            foreach (int neighbor in GetNeighbors(current))
            {
                BlockBehavior nb = boardData.blockGrid[neighbor];
                if (nb != null && nb.colorID == targetColor && !group.Contains(neighbor))
                    stack.Push(neighbor);
            }
        }
        return group;
    }

    private IEnumerable<int> GetNeighbors(int index)
    {
        int cols = boardData.columns;
        int row = index / cols;
        int col = index % cols;
        if (row > 0)
            yield return index - cols;
        if (row < boardData.rows - 1)
            yield return index + cols;
        if (col > 0)
            yield return index - 1;
        if (col < cols - 1)
            yield return index + 1;
    }

    private void UpdateAllBlockSprites()
    {
        int length = boardData.blockGrid.Length;
        bool[] visited = new bool[length];
        for (int i = 0; i < length; i++)
        {
            if (boardData.blockGrid[i] == null || visited[i])
                continue;
            int currentColor = boardData.blockGrid[i].colorID;
            List<int> group = new List<int>();
            Stack<int> stack = new Stack<int>();
            stack.Push(i);
            while (stack.Count > 0)
            {
                int cur = stack.Pop();
                if (visited[cur])
                    continue;
                visited[cur] = true;
                group.Add(cur);
                foreach (int neighbor in GetNeighbors(cur))
                {
                    BlockBehavior neighborBlock = boardData.blockGrid[neighbor];
                    if (neighborBlock != null && neighborBlock.colorID == currentColor && !visited[neighbor])
                        stack.Push(neighbor);
                }
            }
            int groupSize = group.Count;
            foreach (int index in group)
            {
                BlockBehavior block = boardData.blockGrid[index];
                if (block != null)
                    block.UpdateSpriteBasedOnGroupSize(groupSize);
            }
        }
    }

    private void CenterCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;
        float w = (boardData.columns - 1) * boardData.blockWidth;
        float h = (boardData.rows - 1) * boardData.blockHeight;
        float cx = w * 0.5f;
        float cy = h * 0.75f;
        float offsetY = 2f;
        cam.transform.position = new Vector3(cx, cy + offsetY, cam.transform.position.z);
        float halfW = w * 0.5f;
        float halfH = h * 0.5f;
        float ar = (float)Screen.width / Screen.height;
        float halfCamH = Mathf.Max(halfW / ar, halfH);
        float margin = 9f;
        float zoom = 1.1f;
        cam.orthographicSize = (halfCamH + margin) * zoom;
    }

    private Color GetColorFromID(int colorID)
    {
        if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0)
            return Color.white;
        if (colorID < 0 || colorID >= colorPalette.colors.Length)
            return Color.white;
        return colorPalette.colors[colorID];
    }
    
    
    private void GatherAndRemoveGroupSequence(Vector2 gatherPoint, List<int> group, Action onComplete)
    {
        Sequence seq = DOTween.Sequence();
        float shineDuration = 0.15f;
        float gatherDuration = 0.15f;
        float blastDuration = 0.3f;
        
        foreach (int i in group)
        {
            BlockBehavior block = boardData.blockGrid[i];
            if (block != null && block.SpriteRenderer != null)
            {
                block.SpriteRenderer.sortingOrder = 9999;
                block.SpriteRenderer.maskInteraction = SpriteMaskInteraction.None;
                
            }
        }
        
        foreach (int i in group)
        {
            BlockBehavior block = boardData.blockGrid[i];
            if (block != null)
            {
                seq.Join(block.transform.DOScale(block.transform.localScale * 1.2f, shineDuration)
                    .SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutQuad));
                if (block.SpriteRenderer != null)
                {
                    seq.Join(block.SpriteRenderer.DOColor(Color.white, shineDuration)
                        .SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutQuad));
                }
            }
        }
        seq.AppendInterval(0f);
        
        foreach (int i in group)
        {
            BlockBehavior block = boardData.blockGrid[i];
            if (block != null)
            {
                seq.Join(block.transform.DOMove(gatherPoint, gatherDuration).SetEase(Ease.InQuad));
                seq.Join(block.transform.DOScale(block.transform.localScale * 0.8f, gatherDuration).SetEase(Ease.OutQuad));
            }
        }
        seq.AppendInterval(0f);
        
        foreach (int i in group)
        {
            BlockBehavior block = boardData.blockGrid[i];
            if (block != null)
            {
                seq.Join(block.transform.DOScale(block.transform.localScale * 1.5f, blastDuration)
                    .SetEase(Ease.OutCubic));
                if (block.SpriteRenderer != null)
                {
                    seq.Join(block.SpriteRenderer.DOFade(0f, blastDuration).SetEase(Ease.OutCubic));
                }
            }
        }
        seq.OnComplete(() =>
        {
            foreach (int i in group)
            {
                BlockBehavior block = boardData.blockGrid[i];
                if (block != null && block.SpriteRenderer != null)
                {
                    block.SpriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
                boardGenerator.ReturnBlock(boardData, i);
            }
            onComplete?.Invoke();
        });
    }
    
    private void RemoveGroupWithParticlePool(List<int> group, Action onComplete)
    {
        foreach (int i in group)
        {
            BlockBehavior block = boardData.blockGrid[i];
            if (block != null)
            {
                if (particlePool != null)
                    particlePool.SpawnParticle(block.transform.position, GetColorFromID(block.colorID));
                boardGenerator.ReturnBlock(boardData, i);
            }
        }
        onComplete?.Invoke();
    }
}
