using System;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
public class CrateBehavior : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float popDuration = 0.05f;   
    [SerializeField] private float blastDuration = 0.1f; 
    private Vector3 _originalScale;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("CrateBehavior: SpriteRenderer not assigned!");
        }
        _originalScale = transform.localScale;
    }
    
    public void Blast(Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(_originalScale * 1.2f, popDuration).SetEase(Ease.InOutQuad));
        seq.Append(transform.DOScale(0f, blastDuration).SetEase(Ease.OutQuad));
        if (spriteRenderer is not null)
        {
            seq.Join(spriteRenderer.DOFade(0f, blastDuration).SetEase(Ease.OutQuad));
        }
        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
            Destroy(gameObject);
        });
    }
}