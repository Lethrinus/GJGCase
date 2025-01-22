using UnityEngine;
using System.Collections.Generic;

public class BlockPool : MonoBehaviour
{
    public BlockBehavior[] blockPrefabs;
    Queue<BlockBehavior>[] pools;
    void Awake()
    {
        pools = new Queue<BlockBehavior>[blockPrefabs.Length];
        for (int i = 0; i < blockPrefabs.Length; i++)
        {
            pools[i] = new Queue<BlockBehavior>();
        }
    }
    public BlockBehavior GetBlock(int prefabIndex, Vector2 pos, Transform parent)
    {
        if (prefabIndex < 0 || prefabIndex >= blockPrefabs.Length) return null;
        BlockBehavior block;
        if (pools[prefabIndex].Count > 0)
        {
            block = pools[prefabIndex].Dequeue();
            block.gameObject.SetActive(true);
            block.transform.position = pos;
            block.transform.parent = parent;
        }
        else
        {
            block = Instantiate(blockPrefabs[prefabIndex], pos, Quaternion.identity, parent);
        }
        block.transform.localScale = Vector3.one;
        block.transform.localEulerAngles = Vector3.zero;
        var sr = block.SpriteRenderer;
        if (sr)
        {
            var c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
        block.ResetBlock();
        return block;
    }
    public void ReturnBlock(BlockBehavior block, int prefabIndex)
    {
        if (prefabIndex < 0 || prefabIndex >= blockPrefabs.Length)
        {
            Destroy(block.gameObject);
            return;
        }
        block.gameObject.SetActive(false);
        block.transform.parent = transform;
        pools[prefabIndex].Enqueue(block);
    }
}