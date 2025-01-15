using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Checks if the board has any possible moves (no deadlock) and resolves if needed.
/// </summary>
public class DeadlockResolver : MonoBehaviour
{
    public BoardGenerator boardGenerator;
    public BlockPool blockPool;

    /// <summary>
    /// Checks if the board is in deadlock: returns true if no group of >= 2.
    /// </summary>
    public bool IsDeadlock(BoardData boardData)
    {
        for (int i = 0; i < boardData.blockGrid.Length; i++)
        {
            GameObject blockGo = boardData.blockGrid[i];
            if (blockGo == null) continue;

            BlockBehavior currentBb = blockGo.GetComponent<BlockBehavior>();
            if (currentBb == null) continue;

            int colorID = currentBb.colorID;
            
            // We'll do a quick BFS or DFS to see if we can find 2 connected of the same color
            List<int> group = new List<int>();
            Stack<int> stack = new Stack<int>();

            stack.Push(i);
            int groupSize = 0;

            while (stack.Count > 0 && groupSize < 2)
            {
                int currentIndex = stack.Pop();
                if (group.Contains(currentIndex)) continue;
                group.Add(currentIndex);
                groupSize++;

                // Check neighbors
                foreach (int neighborIndex in GetNeighbors(boardData, currentIndex))
                {
                    GameObject neighborGo = boardData.blockGrid[neighborIndex];
                    if (neighborGo is null) continue;
                    BlockBehavior neighborBb = neighborGo.GetComponent<BlockBehavior>();
                    if (neighborBb is not null && neighborBb.colorID == colorID && !group.Contains(neighborIndex))
                        stack.Push(neighborIndex);
                }
            }

            if (groupSize >= 2)
                return false; // Not a deadlock
        }
        return true;
    }

    /// <summary>
    /// Resolves deadlock by shuffling or forcibly creating at least one match, etc.
    /// </summary>
    public IEnumerator ResolveDeadlockOnce(BoardData boardData, Transform parent, 
                                           float shuffleFadeDuration, 
                                           float thresholdA, float thresholdB, float thresholdC)
    {
       yield return StartCoroutine(FadeBlocksOut(boardData, shuffleFadeDuration));

    // 2. Return old blocks to pool
    for (int i = 0; i < boardData.blockGrid.Length; i++)
    {
        if (boardData.blockGrid[i] != null)
        {
            BlockBehavior bb = boardData.blockGrid[i].GetComponent<BlockBehavior>();
            if (bb != null)
                blockPool.ReturnBlock(boardData.blockGrid[i], bb.prefabIndex);
            boardData.blockGrid[i] = null;
        }
    }

    // 3. Generate a new list of colorIDs (ensuring at least one pair)
    List<int> allColorIDs = new List<int>();
    for (int i = 0; i < boardData.blockGrid.Length; i++)
    {
        allColorIDs.Add(Random.Range(0, blockPool.blockPrefabs.Length));
    }
    // Force at least one pair:
    if (allColorIDs.Count >= 2)
    {
        int indexA = Random.Range(0, allColorIDs.Count);
        int indexB;
        do { indexB = Random.Range(0, allColorIDs.Count); } while (indexB == indexA);
        int colorID = Random.Range(0, blockPool.blockPrefabs.Length);
        allColorIDs[indexA] = colorID;
        allColorIDs[indexB] = colorID;
    }

    // Optional shuffle
    FisherYatesShuffle(allColorIDs);

    // 4. Spawn new blocks at correct row/col
    for (int i = 0; i < allColorIDs.Count; i++)
    {
        int row = i / boardData.columns;
        int col = i % boardData.columns;

        // Place directly in final position:
        Vector2 spawnPos = boardData.GetBlockPosition(row, col);
        GameObject newBlock = blockPool.GetBlock(allColorIDs[i], spawnPos, parent);

        if (newBlock != null)
        {
            boardData.blockGrid[i] = newBlock;
            BlockBehavior bb = newBlock.GetComponent<BlockBehavior>();
            if (bb != null)
            {
                bb.colorID = allColorIDs[i];
                bb.prefabIndex = allColorIDs[i];
                bb.ResetBlock();
            }
        }
    }

    // 5. Fade in
    yield return StartCoroutine(FadeBlocksIn(boardData, shuffleFadeDuration));
}

    private IEnumerable<int> GetNeighbors(BoardData boardData, int index)
    {
        int r = index / boardData.columns;
        int c = index % boardData.columns;

        // up
        if (r - 1 >= 0) yield return boardData.GetIndex(r - 1, c);
        // down
        if (r + 1 < boardData.rows) yield return boardData.GetIndex(r + 1, c);
        // left
        if (c - 1 >= 0) yield return boardData.GetIndex(r, c - 1);
        // right
        if (c + 1 < boardData.columns) yield return boardData.GetIndex(r, c + 1);
    }

    private void FisherYatesShuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }

    private IEnumerator FadeBlocksOut(BoardData boardData, float duration)
    {
        float elapsed = 0f;
        List<SpriteRenderer> srs = new List<SpriteRenderer>();
        foreach (var blockGo in boardData.blockGrid)
        {
            if (blockGo == null) continue;
            SpriteRenderer sr = blockGo.GetComponent<SpriteRenderer>();
            if (sr) srs.Add(sr);
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(1f, 0f, t);
            foreach (var sr in srs)
            {
                if (sr != null)
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var sr in srs)
            if (sr != null)
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
    }

    private IEnumerator FadeBlocksIn(BoardData boardData, float duration)
    {
        float elapsed = 0f;
        List<SpriteRenderer> srs = new List<SpriteRenderer>();

        foreach (var blockGo in boardData.blockGrid)
        {
            if (blockGo == null) continue;
            SpriteRenderer sr = blockGo.GetComponent<SpriteRenderer>();
            if (sr)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
                srs.Add(sr);
            }
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(0f, 1f, t);
            foreach (var sr in srs)
            {
                if (sr != null)
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var sr in srs)
            if (sr != null)
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
    }
}
