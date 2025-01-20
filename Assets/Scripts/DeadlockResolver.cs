using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DeadlockResolver : MonoBehaviour
{
    
    public BlockPool blockPool; 
    
    // Checks if the board is in deadlock: returns true if no group of >= 2.
    public bool IsDeadlock(BoardData boardData)
    {
        for (int i = 0; i < boardData.blockGrid.Length; i++)
        {
            GameObject blockGo = boardData.blockGrid[i];
            if (blockGo is null) continue;

            BlockBehavior currentBb = blockGo.GetComponent<BlockBehavior>();
            if (currentBb is null) continue;

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

    // Resolves deadlock by shuffling or forcibly creating at least one match.
   
    public IEnumerator ResolveDeadlockOnce(BoardData boardData, Transform parent, 
                                           float shuffleFadeDuration, 
                                           float thresholdA, float thresholdB, float thresholdC)
    {
       // 1) Fade out the current board
       yield return StartCoroutine(FadeBlocksOut(boardData, shuffleFadeDuration));

       // 2) Return old blocks to the pool
       for (int i = 0; i < boardData.blockGrid.Length; i++)
       {
           if (boardData.blockGrid[i] is not null)
           {
               BlockBehavior bb = boardData.blockGrid[i].GetComponent<BlockBehavior>();
               if (bb is not null)
                   blockPool.ReturnBlock(boardData.blockGrid[i], bb.prefabIndex);
               boardData.blockGrid[i] = null;
           }
       }

       // 3) Generate a random list for ALL cells
       int boardSize = boardData.blockGrid.Length;
       List<int> allColorIDs = new List<int>(boardSize);
       for (int i = 0; i < boardSize; i++)
       {
           allColorIDs.Add(Random.Range(0, blockPool.blockPrefabs.Length));
       }

       int forcedIndexA = -1;
       int forcedIndexB = -1;
       bool foundPair = false;

       // We'll try up to 50 times to find a valid neighbor (for small boards)
       const int maxTries = 50;
       for (int t = 0; t < maxTries; t++)
       {
           if (boardData.rows < 2 && boardData.columns < 2)
           {
               // Single cell board => can't force adjacency
               break;
           }
           int row = Random.Range(0, boardData.rows);
           int col = Random.Range(0, boardData.columns);

           bool canGoRight = (col < boardData.columns - 1);
           bool canGoDown  = (row < boardData.rows - 1);

           if (canGoRight || canGoDown)
           {
               foundPair = true;

               // Decide to go right or down
               int neighborRow = row;
               int neighborCol = col;
               if (canGoRight && canGoDown)
               {
                   // 50/50
                   if (Random.value < 0.5f) neighborCol++;
                   else                     neighborRow++;
               }
               else if (canGoRight)
               {
                   neighborCol++;
               }
               else // only canGoDown
               {
                   neighborRow++;
               }

               forcedIndexA = row * boardData.columns + col;
               forcedIndexB = neighborRow * boardData.columns + neighborCol;

               // Overwrite both cells with the same color ID
               int forcedColor = Random.Range(0, blockPool.blockPrefabs.Length);
               allColorIDs[forcedIndexA] = forcedColor;
               allColorIDs[forcedIndexB] = forcedColor;
               break;
           }
       }

       // Partial-shuffle the board, excluding the forced pair
       if (foundPair)
       {
           List<int> shuffleIndices = new List<int>();
           for (int i = 0; i < boardSize; i++)
           {
               if (i == forcedIndexA || i == forcedIndexB) continue;
               shuffleIndices.Add(i);
           }

           // Fisher-Yates only on those indices
           for (int i = shuffleIndices.Count - 1; i > 0; i--)
           {
               int swapPos = Random.Range(0, i + 1);
               int idxA = shuffleIndices[i];
               int idxB = shuffleIndices[swapPos];

               (allColorIDs[idxA], allColorIDs[idxB]) = (allColorIDs[idxB], allColorIDs[idxA]);
           }
       }
       else
       {
           // If no pair found, shuffle entire board normally
           FisherYatesShuffle(allColorIDs);
       }

       // 5) Spawn new blocks in final positions
       for (int i = 0; i < allColorIDs.Count; i++)
       {
           int row = i / boardData.columns;
           int col = i % boardData.columns;

           // Place directly in final position:
           Vector2 spawnPos = boardData.GetBlockPosition(row, col);
           GameObject newBlock = blockPool.GetBlock(allColorIDs[i], spawnPos, parent);

           if (newBlock is not null)
           {
               boardData.blockGrid[i] = newBlock;
               BlockBehavior bb = newBlock.GetComponent<BlockBehavior>();
               if (bb is not null)
               {
                   bb.colorID = allColorIDs[i];
                   bb.prefabIndex = allColorIDs[i];
                   bb.ResetBlock();
               }
           }
       }

       // 6) Fade in
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
            if (blockGo is null) continue;
            SpriteRenderer sr = blockGo.GetComponent<SpriteRenderer>();
            if (sr) srs.Add(sr);
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(1f, 0f, t);
            foreach (var sr in srs)
            {
                if (sr is not null)
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var sr in srs)
            if (sr is not null)
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
    }

    private IEnumerator FadeBlocksIn(BoardData boardData, float duration)
    {
        float elapsed = 0f;
        List<SpriteRenderer> srs = new List<SpriteRenderer>();

        foreach (var blockGo in boardData.blockGrid)
        {
            if (blockGo is null) continue;
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
                if (sr is not null)
                    sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var sr in srs)
            if (sr is not null)
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
    }
}
