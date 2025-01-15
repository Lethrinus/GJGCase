using UnityEngine;

/// <summary>
/// Responsible for creating, positioning, and replenishing blocks on the board.
/// </summary>
public class BoardGenerator : MonoBehaviour
{
    public BlockPool blockPool;

    /// <summary>
    /// Generate the initial board. You can call this from BoardManager's Start or Init routine.
    /// </summary>
    public void GenerateBoard(BoardData boardData, Transform parent, 
                              int thresholdA, int thresholdB, int thresholdC)
    {
        if (blockPool == null)
        {
            Debug.LogError("BoardGenerator: No BlockPool assigned!");
            return;
        }

        for (int r = 0; r < boardData.rows; r++)
        {
            for (int c = 0; c < boardData.columns; c++)
            {
                int prefabIndex = Random.Range(0, blockPool.blockPrefabs.Length);
                Vector2 spawnPos = boardData.GetBlockPosition(r, c);

                GameObject block = blockPool.GetBlock(prefabIndex, spawnPos, parent);
                if (block == null)
                {
                    Debug.LogError($"BoardGenerator: Failed to create block (R:{r}, C:{c})");
                    continue;
                }

                boardData.blockGrid[boardData.GetIndex(r, c)] = block;

                // Setup block properties
                BlockBehavior bb = block.GetComponent<BlockBehavior>();
                if (bb != null)
                {
                    bb.thresholdA = thresholdA;
                    bb.thresholdB = thresholdB;
                    bb.thresholdC = thresholdC;
                    bb.prefabIndex = prefabIndex;
                    bb.colorID = prefabIndex;
                    bb.ResetBlock();
                }
            }
        }
    }

    /// <summary>
    /// Spawn a new block at a certain (row, col). Typically used during 'gravity' refill.
    /// </summary>
    public GameObject SpawnBlock(BoardData boardData, int row, int col, Transform parent, 
                                 int thresholdA, int thresholdB, int thresholdC)
    {
        if (blockPool == null) return null;

        int prefabIndex = Random.Range(0, blockPool.blockPrefabs.Length);
        Vector2 spawnPos = boardData.GetBlockPosition(-1, col); // spawn above the board
        GameObject newBlock = blockPool.GetBlock(prefabIndex, spawnPos, parent);
        if (newBlock != null)
        {
            int idx = boardData.GetIndex(row, col);
            boardData.blockGrid[idx] = newBlock;

            BlockBehavior bb = newBlock.GetComponent<BlockBehavior>();
            if (bb != null)
            {
                bb.thresholdA = thresholdA;
                bb.thresholdB = thresholdB;
                bb.thresholdC = thresholdC;
                bb.prefabIndex = prefabIndex;
                bb.colorID = prefabIndex;
                bb.ResetBlock();
            }
        }
        return newBlock;
    }

    /// <summary>
    /// Reclaim a block back to the pool.
    /// </summary>
    public void ReturnBlock(BoardData boardData, int index)
    {
        if (boardData.blockGrid[index] == null) return;
        BlockBehavior bb = boardData.blockGrid[index].GetComponent<BlockBehavior>();
        if (bb != null)
            blockPool.ReturnBlock(boardData.blockGrid[index], bb.prefabIndex);

        boardData.blockGrid[index] = null;
    }
}
