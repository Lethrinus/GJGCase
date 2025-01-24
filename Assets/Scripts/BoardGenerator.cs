using UnityEngine;

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
                         int index = Random.Range(0, blockPool.blockPrefabs.Length);
                         var pos = data.GetBlockPosition(r, c);
                         var block = blockPool.GetBlock(index, pos, parent);
                         if (block == null) continue;
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
        int index = Random.Range(0, blockPool.blockPrefabs.Length);
        var abovePos = data.GetBlockPosition(-1, col);
        var block = blockPool.GetBlock(index, abovePos, parent);
        if (block != null)
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
        blockPool.ReturnBlock(block, block.prefabIndex);
        data.blockGrid[index] = null;
    }
}