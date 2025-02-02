using System.Collections.Generic;
using UnityEngine;

public class BlockPool : MonoBehaviour
{
    public BlockBehavior[] blockPrefabs;

    private readonly Dictionary<int, Queue<BlockBehavior>> _pool = new();

    private void Awake()
    {
        for (int i = 0; i < blockPrefabs.Length; i++)
        {
            _pool[i] = new Queue<BlockBehavior>();
        }
    }

    public BlockBehavior GetBlock(int index, Vector2 position, Transform parent)
    {
        if (index < 0 || index >= blockPrefabs.Length) index = 0;
        BlockBehavior block;
        
        if (_pool[index].Count > 0)
        {
            block = _pool[index].Dequeue();
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
        
        block.gameObject.SetActive(false);
        block.transform.SetParent(transform);
        _pool[index].Enqueue(block);
    }
}