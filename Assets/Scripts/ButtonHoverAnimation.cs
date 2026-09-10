
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
        AudioManager.Instance.PlayUISFX("buttonpop");
        currentTween?.Kill();
        // SetUpdate(true) = tempo non scalato: senza, il tween resta fermo quando il menu
        // di pausa è aperto (Time.timeScale a 0 azzera anche l'avanzamento di DOTween).
        currentTween = transform.DOScale(originalScale * hoverScale, duration).SetEase(ease).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale, duration).SetEase(ease).SetUpdate(true);
    }

    private void OnDisable()
    {
        currentTween?.Kill();
        transform.localScale = originalScale;
    }
}
