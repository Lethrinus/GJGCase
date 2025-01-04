using UnityEngine;

public class BlockBehavior : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    [Header("Sprites for this block")]
    public Sprite defaultSprite;
    public Sprite spriteA;
    public Sprite spriteB;
    public Sprite spriteC;

    [Header("Thresholds")]
    public int thresholdA = 5;
    public int thresholdB = 8; 
    public int thresholdC = 10; 

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"SpriteRenderer not found on block: {gameObject.name}");
        }
    }

  
    public void UpdateSpriteBasedOnGroupSize(int groupSize)
    {
        if (spriteRenderer == null) return;

        if (groupSize >= thresholdC)
        {
            spriteRenderer.sprite = spriteC;
        }
        else if (groupSize >= thresholdB)
        {
            spriteRenderer.sprite = spriteB;
        }
        else if (groupSize >= thresholdA)
        {
            spriteRenderer.sprite = spriteA;
        }
        else
        {
            spriteRenderer.sprite = defaultSprite;
        }
    }
}