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
                    var b = data.blockGrid[nb];
                    if (b && b.colorID == colorID && !group.Contains(nb))
                    {
                        stack.Push(nb);
                    }
                }
            }
            
            if (size >= 2) return false;
        }

        return true; 
    }
    
    public void ResolveDeadlockFullRefill(
        BoardData data,
        Transform parent,
        float fadeDuration,
        int thresholdA,
        int thresholdB,
        int thresholdC,
        Action onComplete)
    {
        
        var fadeOutSeq = DOTween.Sequence();
        var spriteList = new List<SpriteRenderer>();

        for (int i = 0; i < data.blockGrid.Length; i++)
        {
            var block = data.blockGrid[i];
            if (block && block.SpriteRenderer)
            {
                spriteList.Add(block.SpriteRenderer);
            }
        }

        foreach (var sr in spriteList)
        {
            fadeOutSeq.Join(sr.DOFade(0f, fadeDuration));
        }
        
        fadeOutSeq.OnComplete(() =>
        {
           
            List<int> colorIDs = new List<int>(data.blockGrid.Length);
            for (int i = 0; i < data.blockGrid.Length; i++)
            {
                var oldBlock = data.blockGrid[i];
                if (oldBlock)
                {
                    colorIDs.Add(oldBlock.colorID);
                }
            }

            for (int i = 0; i < data.blockGrid.Length; i++)
            {
                var oldBlock = data.blockGrid[i];
                if (oldBlock)
                {
                    blockPool.ReturnBlock(oldBlock, oldBlock.prefabIndex);
                    data.blockGrid[i] = null;
                }
            }
            
            Shuffle(colorIDs);

            
            if (colorIDs.Count > 1)
            {
                colorIDs[1] = colorIDs[0];
            }

            for (int i = 0; i < data.blockGrid.Length; i++)
            {
                int row = i / data.columns;
                int col = i % data.columns;
                Vector2 pos = data.GetBlockPosition(row, col);

                var newBlock = blockPool.GetBlock(colorIDs[i], pos, parent);
                data.blockGrid[i] = newBlock;
                newBlock.prefabIndex = colorIDs[i];
                newBlock.colorID = colorIDs[i];
                newBlock.thresholdA = thresholdA;
                newBlock.thresholdB = thresholdB;
                newBlock.thresholdC = thresholdC;
                newBlock.ResetBlock();
                newBlock.SetSortingOrder(row);

                
                if (newBlock.SpriteRenderer)
                {
                    var c = newBlock.SpriteRenderer.color;
                    c.a = 0f;
                    newBlock.SpriteRenderer.color = c;
                }
            }
            
            var fadeInSeq = DOTween.Sequence();
            for (int i = 0; i < data.blockGrid.Length; i++)
            {
                var block = data.blockGrid[i];
                if (block && block.SpriteRenderer)
                {
                    fadeInSeq.Join(block.SpriteRenderer.DOFade(1f, fadeDuration));
                }
            }

            fadeInSeq.OnComplete(() =>
            {
                onComplete?.Invoke();
            });
        });
    }
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
    
    IEnumerable<int> GetNeighbors(BoardData d, int i)
    {
        int r = i / d.columns;
        int c = i % d.columns;

        if (r - 1 >= 0) yield return d.GetIndex(r - 1, c);
        if (r + 1 < d.rows) yield return d.GetIndex(r + 1, c);
        if (c - 1 >= 0) yield return d.GetIndex(r, c - 1);
        if (c + 1 < d.columns) yield return d.GetIndex(r, c + 1);
    }
}