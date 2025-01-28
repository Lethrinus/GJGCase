using UnityEngine;

public class BlockPool : MonoBehaviour
{
    public BlockBehavior[] blockPrefabs;

    public BlockBehavior GetBlock(int index, Vector2 position, Transform parent)
    {
        if (blockPrefabs == null || blockPrefabs.Length == 0) return null;
        if (index < 0 || index >= blockPrefabs.Length) index = 0;
        var b = Instantiate(blockPrefabs[index], parent);
        b.transform.localPosition = position;
        return b;
    }

    public void ReturnBlock(BlockBehavior block, int index)
    {
        if (block) Destroy(block.gameObject);
    }
}