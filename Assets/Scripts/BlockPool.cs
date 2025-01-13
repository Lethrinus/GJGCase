using System.Collections.Generic;
using UnityEngine;

public class BlockPool : MonoBehaviour
{
    public GameObject[] blockPrefabs;
    private Queue<GameObject>[] _pools;

    private void Awake()
    {
        if (blockPrefabs == null || blockPrefabs.Length == 0)
        {
            Debug.LogError("BlockPool: No prefabs assigned!");
            return;
        }

        _pools = new Queue<GameObject>[blockPrefabs.Length];
        for (int i = 0; i < blockPrefabs.Length; i++)
        {
            _pools[i] = new Queue<GameObject>();
        }
    }

    public GameObject GetBlock(int prefabIndex, Vector2 position, Transform parent)
    {
        if (prefabIndex < 0 || prefabIndex >= blockPrefabs.Length)
        {
            Debug.LogError($"BlockPool: Invalid prefab index {prefabIndex}.");
            return null;
        }

        GameObject block;
        if (_pools[prefabIndex].Count > 0)
        {
            block = _pools[prefabIndex].Dequeue();
            block.SetActive(true);
            block.transform.position = position;
            block.transform.parent = parent;
        }
        else
        {
            block = Instantiate(blockPrefabs[prefabIndex], position, Quaternion.identity, parent);
        }

        block.transform.localScale = Vector3.one;
        block.transform.localEulerAngles = Vector3.zero;

        SpriteRenderer sr = block.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color color = sr.color;
            color.a = 1f;
            sr.color = color;
        }

        BlockBehavior bb = block.GetComponent<BlockBehavior>();
        if (bb != null)
        {
            bb.ResetBlock();
        }

        return block;
    }

    public void ReturnBlock(GameObject block, int prefabIndex)
    {
        if (prefabIndex < 0 || prefabIndex >= blockPrefabs.Length)
        {
            Debug.LogError($"BlockPool: Invalid prefab index {prefabIndex}.");
            Destroy(block);
            return;
        }

        block.SetActive(false);
        block.transform.parent = transform;
        _pools[prefabIndex].Enqueue(block);
    }
}
