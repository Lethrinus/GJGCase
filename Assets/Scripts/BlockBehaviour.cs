using UnityEngine;

public class BlockBehavior : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    // Sprites for this block's color
    private Sprite defaultSprite;
    private Sprite spriteA;
    private Sprite spriteB;
    private Sprite spriteC;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultSprite; // Initialize with the default sprite
    }

    // Dynamically set the sprites for this block's color
    public void SetSprites(Sprite defaultSprite, Sprite spriteA, Sprite spriteB, Sprite spriteC)
    {
        this.defaultSprite = defaultSprite;
        this.spriteA = spriteA;
        this.spriteB = spriteB;
        this.spriteC = spriteC;

        // Set the initial sprite
        spriteRenderer.sprite = defaultSprite;
    }

    // Updates the sprite based on the group size
    public void UpdateSpriteBasedOnGroupSize(int groupSize)
    {
        if (groupSize >= 10)
        {
            spriteRenderer.sprite = spriteC; // Large group
        }
        else if (groupSize >= 8)
        {
            spriteRenderer.sprite = spriteB; // Medium group
        }
        else if (groupSize >= 5)
        {
            spriteRenderer.sprite = spriteA; // Small group
        }
        else
        {
            spriteRenderer.sprite = defaultSprite; // Default sprite
        }
    }

    // Method to get the current sprite
    public Sprite GetSprite()
    {
        return spriteRenderer.sprite;
    }
}