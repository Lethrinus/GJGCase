using UnityEngine;
using DG.Tweening;
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
                foreach (var n in GetNeighbors(data, cur))
                {
                    var nb = data.blockGrid[n];
                    if (nb && nb.colorID == colorID && !group.Contains(n)) stack.Push(n);
                }
            }
            if (size >= 2) return false;
        }
        return true;
    }
    public void ResolveDeadlockOnceNoCoroutines(BoardData data, Transform parent, float fadeTime, int tA, int tB, int tC, System.Action onComplete)
    {
        Sequence seq = DOTween.Sequence();
        List<SpriteRenderer> srs = new List<SpriteRenderer>();
        for (int i = 0; i < data.blockGrid.Length; i++)
        {
            var b = data.blockGrid[i];
            if (!b) continue;
            var sr = b.SpriteRenderer;
            if (sr) srs.Add(sr);
        }
        foreach (var sr in srs) seq.Join(sr.DOFade(0f, fadeTime));
        seq.OnComplete(() =>
        {
            for (int i = 0; i < data.blockGrid.Length; i++)
            {
                var b = data.blockGrid[i];
                if (b)
                {
                    blockPool.ReturnBlock(b, b.prefabIndex);
                    data.blockGrid[i] = null;
                }
            }
            int size = data.blockGrid.Length;
            List<int> colors = new List<int>();
            for (int i = 0; i < size; i++) colors.Add(Random.Range(0, blockPool.blockPrefabs.Length));
            List<(int,int)> pairs = new List<(int,int)>();
            for (int r = 0; r < data.rows; r++)
            {
                for (int c = 0; c < data.columns; c++)
                {
                    int index = r * data.columns + c;
                    if (c + 1 < data.columns) pairs.Add((index, r*data.columns + (c+1)));
                    if (r + 1 < data.rows) pairs.Add((index, (r+1)*data.columns + c));
                }
            }
            int fA = -1; 
            int fB = -1;
            if (pairs.Count > 0)
            {
                var chosen = pairs[Random.Range(0, pairs.Count)];
                fA = chosen.Item1;
                fB = chosen.Item2;
                int forceColor = Random.Range(0, blockPool.blockPrefabs.Length);
                colors[fA] = forceColor;
                colors[fB] = forceColor;
            }
            if (fA >= 0 && fB >= 0)
            {
                List<int> shuf = new List<int>();
                for (int i = 0; i < size; i++) if (i != fA && i != fB) shuf.Add(i);
                for (int i = shuf.Count - 1; i > 0; i--)
                {
                    int sp = Random.Range(0, i + 1);
                    int ia = shuf[i];
                    int ib = shuf[sp];
                    int temp = colors[ia];
                    colors[ia] = colors[ib];
                    colors[ib] = temp;
                }
            }
            else
            {
                for (int i = colors.Count - 1; i > 0; i--)
                {
                    int sp = Random.Range(0, i + 1);
                    int temp = colors[i];
                    colors[i] = colors[sp];
                    colors[sp] = temp;
                }
            }
            for (int i = 0; i < colors.Count; i++)
            {
                int r = i / data.columns;
                int c = i % data.columns;
                Vector2 pos = data.GetBlockPosition(r, c);
                var nb = blockPool.GetBlock(colors[i], pos, parent);
                if (nb)
                {
                    data.blockGrid[i] = nb;
                    nb.colorID = colors[i];
                    nb.prefabIndex = colors[i];
                    nb.thresholdA = tA;
                    nb.thresholdB = tB;
                    nb.thresholdC = tC;
                    nb.ResetBlock();
                    nb.SetSortingOrder(r);
                }
            }
            Sequence inSeq = DOTween.Sequence();
            List<SpriteRenderer> newSrs = new List<SpriteRenderer>();
            for (int i = 0; i < data.blockGrid.Length; i++)
            {
                var b = data.blockGrid[i];
                if (!b) continue;
                var sr = b.SpriteRenderer;
                if (sr)
                {
                    var col = sr.color;
                    col.a = 0f;
                    sr.color = col;
                    newSrs.Add(sr);
                }
            }
            foreach (var sr in newSrs) inSeq.Join(sr.DOFade(1f, fadeTime));
            inSeq.OnComplete(() => { if (onComplete != null) onComplete(); });
        });
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
