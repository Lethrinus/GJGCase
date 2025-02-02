using UnityEngine;
using DG.Tweening;
using System;
using System.Collections.Generic;

public class DeadlockResolver : MonoBehaviour
{
    public BlockPool blockPool;

    public bool IsDeadlock(BoardData data)
    {
        for (int i = 0; i < data.blockGrid.Length; i++)
        {
            var block = data.blockGrid[i];
            if (!block) continue;
            int colorID = block.colorID;
            List<int> group = new List<int>();
            Stack<int> stack = new Stack<int>();
            stack.Push(i);
            int size = 0;
            while (stack.Count > 0 && size < 2)
            {
                int cur = stack.Pop();
                if (group.Contains(cur)) continue;
                group.Add(cur);
                size++;
                foreach (int nb in GetNeighbors(data, cur))
                {
                    var nbBlock = data.blockGrid[nb];
                    if (nbBlock && nbBlock.colorID == colorID && !group.Contains(nb)) stack.Push(nb);
                }
            }
            if (size >= 2) return false;
        }
        return true;
    }

  public void ResolveDeadlockFullRefill(BoardData data, Transform parent, float fadeDuration, int thresholdA, int thresholdB, int thresholdC, Action onComplete, HashSet<int> reservedIndices = null) {
      Sequence fadeOutSeq = DOTween.Sequence(); 
      List<SpriteRenderer> spriteList = new List<SpriteRenderer>();
        for (int i = 0; i < data.blockGrid.Length; i++)
        {
            var block = data.blockGrid[i];
            if (block != null && block.SpriteRenderer != null)
                spriteList.Add(block.SpriteRenderer);
        }
        foreach (var sr in spriteList)
        {
            fadeOutSeq.Join(sr.DOFade(0f, fadeDuration));
        }
    
        fadeOutSeq.OnComplete(() =>
        {
       
            List<int> validIndices = new List<int>();
            List<int> colorIDs = new List<int>();
        
            for (int i = 0; i < data.blockGrid.Length; i++)
            {
                int row = i / data.columns;
                int col = i % data.columns;
            
           
                if (data.IsValidCell(row, col) && (reservedIndices == null || !reservedIndices.Contains(i)))
                {
                    validIndices.Add(i);
                    var oldBlock = data.blockGrid[i];
                    if (oldBlock != null)
                    {
                        colorIDs.Add(oldBlock.colorID);
                    }
                }
            }
        
        
            for (int i = 0; i < data.blockGrid.Length; i++)
            {
                var oldBlock = data.blockGrid[i];
                if (oldBlock != null)
                {
                
                    blockPool.ReturnBlock(oldBlock, oldBlock.prefabIndex);
                    data.blockGrid[i] = null;
                }
            }
        
        
            Shuffle(colorIDs);
        
            for (int j = 0; j < validIndices.Count; j++)
            {
                int i = validIndices[j];
                int row = i / data.columns;
                int col = i % data.columns;
                Vector2 pos = data.GetBlockPosition(row, col);
            
          
                int colorId = (j < colorIDs.Count) ? colorIDs[j] : UnityEngine.Random.Range(0, blockPool.blockPrefabs.Length);
            
                var newBlock = blockPool.GetBlock(colorId, pos, parent);
                data.blockGrid[i] = newBlock;
                newBlock.prefabIndex = colorId;
                newBlock.colorID = colorId;
                newBlock.thresholdA = thresholdA;
                newBlock.thresholdB = thresholdB;
                newBlock.thresholdC = thresholdC;
                newBlock.ResetBlock();
                newBlock.SetSortingOrder(row);
            
                if (newBlock.SpriteRenderer != null)
                {
                    Color c = newBlock.SpriteRenderer.color;
                    c.a = 0f;
                    newBlock.SpriteRenderer.color = c;
                }
            }
        
      
            Sequence fadeInSeq = DOTween.Sequence();
            foreach (int i in validIndices)
            {
                var block = data.blockGrid[i];
                if (block != null && block.SpriteRenderer != null)
                {
                    fadeInSeq.Join(block.SpriteRenderer.DOFade(1f, fadeDuration));
                }
            }
        
            fadeInSeq.OnComplete(() => { onComplete?.Invoke(); });
        });
}

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    IEnumerable<int> GetNeighbors(BoardData data, int index)
    {
        int r = index / data.columns;
        int c = index % data.columns;
        if (r - 1 >= 0) yield return data.GetIndex(r - 1, c);
        if (r + 1 < data.rows) yield return data.GetIndex(r + 1, c);
        if (c - 1 >= 0) yield return data.GetIndex(r, c - 1);
        if (c + 1 < data.columns) yield return data.GetIndex(r, c + 1);
    }
}
