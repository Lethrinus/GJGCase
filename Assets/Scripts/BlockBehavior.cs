using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
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

    private SpriteRenderer _spriteRenderer;
    private Vector3 _originalScale;
    private Vector3 _originalRotation;
    private Coroutine _currentBuzzRoutine;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (!_spriteRenderer)
            Debug.LogError("[BlockBehavior] Missing SpriteRenderer!");

        _originalScale = transform.localScale;
        _originalRotation = transform.localEulerAngles;
    }

    // hover effect 
    private void OnMouseEnter()
    {
        transform.localScale = _originalScale * 1.1f;
    }
    private void OnMouseExit()
    {
        transform.localScale = _originalScale;
    }

    public void UpdateSpriteBasedOnGroupSize(int groupSize)
    {
        if (!_spriteRenderer) return;

        if (groupSize < thresholdA)
        {
            _spriteRenderer.sprite = defaultSprite;
        }
        else if (groupSize < thresholdB)
        {
            _spriteRenderer.sprite = spriteA;
        }
        else if (groupSize < thresholdC)
        {
            _spriteRenderer.sprite = spriteB;
        }
        else
        {
            _spriteRenderer.sprite = spriteC;
        }
    }

    //  buzz ( If cannot be blasted -> the buzz effect)
    public void StartBuzz(
        float duration = 0.3f, 
        float scaleAmplitude = 0.05f, 
        float rotateAmplitude = 10f, 
        float frequency = 25f
    )
    {
        if (_currentBuzzRoutine != null)
        {
            StopCoroutine(_currentBuzzRoutine);
            transform.localScale = _originalScale;
            transform.localEulerAngles = _originalRotation;
        }

        _currentBuzzRoutine = StartCoroutine(
            BuzzScaleRotate(duration, scaleAmplitude, rotateAmplitude, frequency)
        );
    }

    private IEnumerator BuzzScaleRotate(
        float duration, 
        float scaleAmplitude, 
        float rotateAmplitude, 
        float frequency
    )
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float sinVal = Mathf.Sin(elapsed * frequency);

            float sOffset = sinVal * scaleAmplitude;
            transform.localScale = _originalScale + new Vector3(sOffset, sOffset, 0f);

            float rOffset = sinVal * rotateAmplitude;
            transform.localEulerAngles = new Vector3(
                _originalRotation.x,
                _originalRotation.y,
                _originalRotation.z + rOffset
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // reset
        transform.localScale = _originalScale;
        transform.localEulerAngles = _originalRotation;
        _currentBuzzRoutine = null;
    }

    // blast animation
    public IEnumerator BlastAnimation(float duration, System.Action onComplete = null)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (!sr)
        {
            Destroy(gameObject);
            onComplete?.Invoke();
            yield break;
        }

        Vector3 initScale = transform.localScale;
        Color initColor = sr.color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // scale a little bit   
            float scaleFactor = Mathf.Lerp(1f, 1.5f, t);
            transform.localScale = initScale * scaleFactor;

            // drag the alpha to 0 
            float alpha = Mathf.Lerp(1f, 0f, t);
            sr.color = new Color(initColor.r, initColor.g, initColor.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // final
        transform.localScale = initScale * 1.5f;
        sr.color = new Color(initColor.r, initColor.g, initColor.b, 0f);

        Destroy(gameObject);
        onComplete?.Invoke();
    }
}
