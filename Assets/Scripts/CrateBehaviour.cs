using System;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))] 
public class CrateBehavior : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;  
    [SerializeField] private float popDuration = 0.1f;
    [SerializeField] private float blastDuration = 0.15f;
    private Vector3 originalScale;

    private void Awake()
    {
        
        originalScale = transform.localScale;
    }
    
    public void Blast(Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(originalScale * 1.2f, popDuration).SetEase(Ease.InOutQuad));
        seq.Append(transform.DOScale(0f, blastDuration).SetEase(Ease.OutQuad));
        if (spriteRenderer != null)
        {
            seq.Join(spriteRenderer.DOFade(0f, blastDuration));
        }
        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
            Destroy(gameObject);
        });
    }
}