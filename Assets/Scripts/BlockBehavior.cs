using UnityEngine;

public class BlockBehavior : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    [Header("Sprites for this block")]
    public Sprite defaultSprite;
    public Sprite spriteA;
    public Sprite spriteB;
    public Sprite spriteC;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"SpriteRenderer not found on block: {gameObject.name}");
        }
        else
        {
            spriteRenderer.sprite = defaultSprite;
        }
    }

    public void UpdateSpriteBasedOnGroupSize(int groupSize)
    {
        if (groupSize >= 10)
        {
            spriteRenderer.sprite = spriteC;
        }
        else if (groupSize >= 8)
        {
            spriteRenderer.sprite = spriteB;
        }
        else if (groupSize >= 5)
        {
            spriteRenderer.sprite = spriteA;
        }
        else
        {
            spriteRenderer.sprite = defaultSprite;
        }
    }

    public Sprite GetSprite()
    {
        return spriteRenderer.sprite;
    }
}