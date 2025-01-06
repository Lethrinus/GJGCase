using UnityEngine;
using System.Collections;

/// <summary>
/// Each block has:
///  - A colorID (0..K-1) for matching
///  - Sprites for different thresholds (default, A, B, C)
///  - A "buzz" animation if not blastable
///  - A "blast" animation if it's being removed
/// </summary>
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

   
    private Vector3 originalScale;
    private Vector3 originalRotation;

   
    private Coroutine currentBuzzRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (!spriteRenderer)
            Debug.LogError($"[BlockBehavior] Missing SpriteRenderer on {name}");

     
        originalScale = transform.localScale;
        originalRotation = transform.localEulerAngles;
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

    
    public void StartBuzz(
        float duration = 0.3f, 
        float scaleAmplitude = 0.05f, 
        float rotateAmplitude = 10f, 
        float frequency = 25f)
    {
        if (currentBuzzRoutine != null)
        {
            StopCoroutine(currentBuzzRoutine);
            transform.localScale = originalScale;
            transform.localEulerAngles = originalRotation;
        }

        currentBuzzRoutine = StartCoroutine(
            BuzzScaleRotate(duration, scaleAmplitude, rotateAmplitude, frequency)
        );
    }

 
    private IEnumerator BuzzScaleRotate(
        float duration, 
        float scaleAmplitude, 
        float rotateAmplitude, 
        float frequency)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float sinVal = Mathf.Sin(elapsed * frequency);

            
            float sOffset = sinVal * scaleAmplitude;
            transform.localScale = originalScale + new Vector3(sOffset, sOffset, 0f);

            float rOffset = sinVal * rotateAmplitude;
            transform.localEulerAngles = new Vector3(
                originalRotation.x,
                originalRotation.y,
                originalRotation.z + rOffset
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        
        transform.localScale = originalScale;
        transform.localEulerAngles = originalRotation;

      
        currentBuzzRoutine = null;
    }

    public IEnumerator BlastAnimation(float duration, System.Action onComplete = null)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (!sr)
        {
            Destroy(gameObject);
            onComplete?.Invoke();
            yield break;
        }

        Vector3 initialScale = transform.localScale;
        Color initialColor = sr.color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;

           
            float scaleFactor = Mathf.Lerp(1f, 1.5f, t);
            transform.localScale = initialScale * scaleFactor;

          
            float alpha = Mathf.Lerp(1f, 0f, t);
            sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

      
        transform.localScale = initialScale * 1.5f;
        sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, 0f);

        Destroy(gameObject);
        onComplete?.Invoke();
    }
}
