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
    [SerializeField] SpriteRenderer spriteRenderer;
    Vector3 originalScale;
    Tween hoverTween;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }
    void OnMouseEnter()
    {
        if (hoverTween != null && hoverTween.IsActive()) hoverTween.Kill();
        hoverTween = transform.DOScale(originalScale * 1.1f, 0.15f).SetEase(Ease.OutQuad);
    }
    void OnMouseExit()
    {
        if (hoverTween != null && hoverTween.IsActive()) hoverTween.Kill();
        hoverTween = transform.DOScale(originalScale, 0.10f).SetEase(Ease.OutQuad);
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
        if (hoverTween != null && hoverTween.IsActive()) hoverTween.Kill();
        transform.DOPunchScale(Vector3.one * scaleAmplitude, duration, 10, 1).OnComplete(() => 
        {
            transform.localScale = originalScale;
        });
    }
    public void ResetBlock()
    {
        transform.localScale = originalScale;
        if (spriteRenderer && defaultSprite) spriteRenderer.sprite = defaultSprite;
        if (spriteRenderer)
        {
            var c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }
    public void SetSortingOrder(int row)
    {
        if (spriteRenderer) spriteRenderer.sortingOrder = 9 - row ;
    }
}
