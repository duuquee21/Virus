using UnityEngine;
using UnityEngine.EventSystems;

public class SkillNodeHoverFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform rect;
    public RectTransform infoPanel; // opcional si quieres que se mueva también

    [Header("Escala")]
    public float hoverScale = 1.1f;
    public float speed = 8f;

    [Header("Movimiento lateral")]
    public float hoverOffsetX = 10f;

    private Vector3 originalScale;
    private Vector2 originalPos;
    private Vector2 targetPos;
    private Vector3 targetScale;

    void Start()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        originalScale = rect.localScale;
        originalPos = rect.anchoredPosition;

        targetScale = originalScale;
        targetPos = originalPos;
    }

    void Update()
    {
        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.unscaledDeltaTime * speed);
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, Time.unscaledDeltaTime * speed);

        if (infoPanel != null)
        {
            infoPanel.localScale = rect.localScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
        targetPos = originalPos + new Vector2(hoverOffsetX, 0);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        targetPos = originalPos;
    }

    // Animación al comprar
    public void PlayClickFeedback()
    {
        StartCoroutine(ClickPunch());
    }

    System.Collections.IEnumerator ClickPunch()
    {
        rect.localScale = originalScale * 0.9f;
        yield return new WaitForSecondsRealtime(0.05f);
        rect.localScale = originalScale * 1.15f;
        yield return new WaitForSecondsRealtime(0.05f);
        rect.localScale = originalScale;
    }
}
