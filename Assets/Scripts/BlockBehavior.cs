using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class BlockBehavior : MonoBehaviour
{
    public int colorID;
    public Sprite defaultSprite;
    public Sprite spriteA;
    public Sprite spriteB;
    public Sprite spriteC;

    public int thresholdA = 4;
    public int thresholdB = 7;
    public int thresholdC = 9;

    public int prefabIndex;

    public event Action<BlockBehavior> OnBlockDestroyed;

    private SpriteRenderer _spriteRenderer;
    private Vector3 _originalScale;
    private Vector3 _originalRotation;
    private Coroutine _currentBuzzRoutine;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalScale = transform.localScale;
        _originalRotation = transform.localEulerAngles;
    }

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
            _spriteRenderer.sprite = defaultSprite;
        else if (groupSize < thresholdB)
            _spriteRenderer.sprite = spriteA;
        else if (groupSize < thresholdC)
            _spriteRenderer.sprite = spriteB;
        else
            _spriteRenderer.sprite = spriteC;
    }

    public void StartBuzz(float duration = 0.3f, float scaleAmplitude = 0.05f, float rotateAmplitude = 10f, float frequency = 25f)
    {
        if (_currentBuzzRoutine != null)
        {
            StopCoroutine(_currentBuzzRoutine);
            transform.localScale = _originalScale;
            transform.localEulerAngles = _originalRotation;
        }

        _currentBuzzRoutine = StartCoroutine(BuzzScaleRotate(duration, scaleAmplitude, rotateAmplitude, frequency));
    }

    private IEnumerator BuzzScaleRotate(float duration, float scaleAmplitude, float rotateAmplitude, float frequency)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float sinVal = Mathf.Sin(elapsed * frequency);
            float sOffset = sinVal * scaleAmplitude;
            transform.localScale = _originalScale + new Vector3(sOffset, sOffset, 0f);
            float rOffset = sinVal * rotateAmplitude;
            transform.localEulerAngles = new Vector3(_originalRotation.x, _originalRotation.y, _originalRotation.z + rOffset);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = _originalScale;
        transform.localEulerAngles = _originalRotation;
        _currentBuzzRoutine = null;
    }

    public IEnumerator BlastAnimation(float duration, Action onComplete = null)
    {
        if (!_spriteRenderer)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 initScale = transform.localScale;
        Color initColor = _spriteRenderer.color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scaleFactor = Mathf.Lerp(1f, 1.5f, t);
            transform.localScale = initScale * scaleFactor;
            float alpha = Mathf.Lerp(1f, 0f, t);
            _spriteRenderer.color = new Color(initColor.r, initColor.g, initColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = initScale * 1.5f;
        _spriteRenderer.color = new Color(initColor.r, initColor.g, initColor.b, 0f);

        OnBlockDestroyed?.Invoke(this);
        onComplete?.Invoke();
    }

    public void ResetBlock()
    {
        transform.localScale = Vector3.one;
        transform.localEulerAngles = Vector3.zero;

        if (_spriteRenderer && defaultSprite)
            _spriteRenderer.sprite = defaultSprite;

        if (_spriteRenderer)
        {
            Color color = _spriteRenderer.color;
            color.a = 1f;
            _spriteRenderer.color = color;
        }
    }
}
