using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

// Da mettere sullo stesso GameObject di un Button (o di qualsiasi elemento con un Graphic
// che riceve il raycast della UI): al passaggio del mouse scala il bottone con un tween
// DOTween, tornando alla scala originale quando il cursore esce.
public class ButtonHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private Ease ease = Ease.OutBack;

    private Vector3 originalScale;
    private Tween currentTween;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale * hoverScale, duration).SetEase(ease);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale, duration).SetEase(ease);
    }

    private void OnDisable()
    {
        currentTween?.Kill();
        transform.localScale = originalScale;
    }
}
