using UnityEngine;


// Responsible for creating, positioning, and replenishing blocks on the board.

public class BoardGenerator : MonoBehaviour
{
    public BlockPool blockPool;

    
    // Generate the initial board. You can call this from BoardManager's Start or Init routine.
    
    public void GenerateBoard(BoardData boardData, Transform parent, 
                              int thresholdA, int thresholdB, int thresholdC)
    {
        if (blockPool is null)
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
                if (bb is not null)
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

    
    // Spawn a new block at a certain (row, col). Typically used during 'gravity' refill.
    
    public GameObject SpawnBlock(BoardData boardData, int row, int col, Transform parent, 
                                 int thresholdA, int thresholdB, int thresholdC)
    {
        if (blockPool is null) return null;

        int prefabIndex = Random.Range(0, blockPool.blockPrefabs.Length);
        Vector2 spawnPos = boardData.GetBlockPosition(-1, col); // spawn above the board
        GameObject newBlock = blockPool.GetBlock(prefabIndex, spawnPos, parent);
        if (newBlock is not null)
        {
            int idx = boardData.GetIndex(row, col);
            boardData.blockGrid[idx] = newBlock;

            BlockBehavior bb = newBlock.GetComponent<BlockBehavior>();
            if (bb is not null)
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

    
    // Reclaim a block back to the pool.
    
    public void ReturnBlock(BoardData boardData, int index)
    {
        if (boardData.blockGrid[index] is null) return;
        BlockBehavior bb = boardData.blockGrid[index].GetComponent<BlockBehavior>();
        if (bb is not null)
            blockPool.ReturnBlock(boardData.blockGrid[index], bb.prefabIndex);

        boardData.blockGrid[index] = null;
    }
}
