using UnityEngine;

public class BlockBehavior : MonoBehaviour
{
    [Header("Color ID (0..K-1) for Matching")]
    public int colorID;

    [Header("Sprites by Threshold")]
    public Sprite defaultSprite; 
    public Sprite spriteA;        
    public Sprite spriteB;       
    public Sprite spriteC;      

    [Header("Thresholds (A < B < C)")]
    public int thresholdA = 4; 
    public int thresholdB = 7; 
    public int thresholdC = 9;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (!spriteRenderer)
            Debug.LogError($"[BlockBehavior] Missing SpriteRenderer on {name}");
    }
    public void UpdateSpriteBasedOnGroupSize(int groupSize)
    {
        if (!spriteRenderer) return;
        if (groupSize < thresholdA)
        {
            spriteRenderer.sprite = defaultSprite;
        }
        else if (groupSize < thresholdB)
        {
            spriteRenderer.sprite = spriteA;
        }
        else if (groupSize < thresholdC)
        {
            spriteRenderer.sprite = spriteB;
        }
        else
        {
            spriteRenderer.sprite = spriteC;
        }
    }
}