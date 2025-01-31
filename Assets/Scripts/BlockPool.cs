using System.Collections.Generic;
using UnityEngine;

public class BlockPool : MonoBehaviour
{
    public BlockBehavior[] blockPrefabs;

    private Dictionary<int, Queue<BlockBehavior>> pool = new Dictionary<int, Queue<BlockBehavior>>();

    void Awake()
    {
        // Each prefab index gets its own queue
        for (int i = 0; i < blockPrefabs.Length; i++)
        {
            pool[i] = new Queue<BlockBehavior>();
        }
    }

    public BlockBehavior GetBlock(int index, Vector2 position, Transform parent)
    {
        if (index < 0 || index >= blockPrefabs.Length) index = 0;
        BlockBehavior block;

        // Reuse or Instantiate
        if (pool[index].Count > 0)
        {
            block = pool[index].Dequeue();
            block.gameObject.SetActive(true);
        }
        else
        {
            block = Instantiate(blockPrefabs[index], parent);
        }

        block.transform.SetParent(parent);
        block.transform.localPosition = position;
        return block;
    }

    public void ReturnBlock(BlockBehavior block, int index)
    {
        if (!block) return;
        if (index < 0 || index >= blockPrefabs.Length)
        {
            Destroy(block.gameObject);
            return;
        }

        // Make inactive and add back to the queue
        block.gameObject.SetActive(false);
        block.transform.SetParent(transform);
        pool[index].Enqueue(block);
    }
}