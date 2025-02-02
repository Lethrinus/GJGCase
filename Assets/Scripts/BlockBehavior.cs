using System;
using UnityEngine;
using DG.Tweening;

public class BlockBehavior : MonoBehaviour
{
    public int colorID;
    public Sprite defaultSprite;
    public Sprite spriteA;
    public Sprite spriteB;
    public Sprite spriteC;
    public int thresholdA;
    public int thresholdB;
    public int thresholdC;
    public int prefabIndex;
    
    [SerializeField] private SpriteRenderer spriteRenderer;
    public SpriteRenderer SpriteRenderer => spriteRenderer;

    private Vector3 _originalScale;
    private Tween _hoverTween;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("BlockBehavior: SpriteRenderer not assigned!");
        }
        _originalScale = transform.localScale;
    }

    private void OnDestroy()
    {
        if (transform != null)
        {
            transform.DOKill(true);
        }
    }

    private void OnMouseEnter()
    {
        if (_hoverTween != null && _hoverTween.IsActive()) _hoverTween.Kill();
        _hoverTween = transform.DOScale(_originalScale * 1.1f, 0.15f).SetEase(Ease.OutQuad);
    }

    private void OnMouseExit()
    {
        if (_hoverTween != null && _hoverTween.IsActive()) _hoverTween.Kill();
        _hoverTween = transform.DOScale(_originalScale, 0.1f).SetEase(Ease.OutQuad);
    }

    public void UpdateSpriteBasedOnGroupSize(int size)
    {
        if (size <= thresholdA) spriteRenderer.sprite = defaultSprite;
        else if (size <= thresholdB) spriteRenderer.sprite = spriteA;
        else if (size <= thresholdC) spriteRenderer.sprite = spriteB;
        else spriteRenderer.sprite = spriteC;
    }

    public void StartBuzz(float duration = 0.5f, float scaleAmplitude = 0.03f)
    {
        if (_hoverTween != null && _hoverTween.IsActive()) _hoverTween.Kill();
        transform.DOPunchScale(Vector3.one * scaleAmplitude, duration).OnComplete(() => transform.localScale = _originalScale);
    }

    public void ResetBlock()
    {
        transform.localScale = _originalScale;
        if (spriteRenderer && defaultSprite) spriteRenderer.sprite = defaultSprite;
        if (spriteRenderer)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }

    public void SetSortingOrder(int row)
    {
        if (spriteRenderer) spriteRenderer.sortingOrder = 9 - row;
    }
}