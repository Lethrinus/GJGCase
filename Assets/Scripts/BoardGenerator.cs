using UnityEngine;
using DG.Tweening;

public class BoardGenerator : MonoBehaviour
{
    public BlockPool blockPool;

    public void GenerateBoard(BoardData data, Transform parent, int tA, int tB, int tC)
    {
        if (!blockPool) return;

        for (int r = 0; r < data.rows; r++)
        {
            for (int c = 0; c < data.columns; c++)
            {
                if (!data.IsValidCell(r, c)) continue;

                int index = Random.Range(0, blockPool.blockPrefabs.Length);
                Vector2 pos = data.GetBlockPosition(r, c);
                var block = blockPool.GetBlock(index, pos, parent);
                if (!block) continue;

                data.blockGrid[data.GetIndex(r, c)] = block;
                block.thresholdA = tA;
                block.thresholdB = tB;
                block.thresholdC = tC;
                block.prefabIndex = index;
                block.colorID = index;
                block.ResetBlock();
                block.SetSortingOrder(r);
            }
        }
    }

    public BlockBehavior SpawnBlock(BoardData data, int row, int col, Transform parent, int tA, int tB, int tC)
    {
        if (!blockPool) return null;
        if (!data.IsValidCell(row, col)) return null;

        int index = Random.Range(0, blockPool.blockPrefabs.Length);
        Vector2 abovePos = data.GetBlockPosition(-1, col);
        var block = blockPool.GetBlock(index, abovePos, parent);
        if (block)
        {
            data.blockGrid[data.GetIndex(row, col)] = block;
            block.thresholdA = tA;
            block.thresholdB = tB;
            block.thresholdC = tC;
            block.prefabIndex = index;
            block.colorID = index;
            block.ResetBlock();
            block.SetSortingOrder(row);
        }
        return block;
    }

    public void ReturnBlock(BoardData data, int index)
    {
        var block = data.blockGrid[index];
        if (!block) return;
        DOTween.Kill(block.transform);
        blockPool.ReturnBlock(block, block.prefabIndex);
        data.blockGrid[index] = null;
    }
}
