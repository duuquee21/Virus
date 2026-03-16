using UnityEngine;
using UnityEngine.EventSystems;

public class SkillNodeHoverFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform rect;
    public RectTransform infoPanel;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hoverSound;
    [Range(0f, 1f)] public float volume = 0.5f;

    [Header("Estado Comprado")]
    public float purchasedScale = 0.95f;
    private bool isPurchased = false;

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

        // Si no asignaste un AudioSource, intentamos buscar uno en el objeto
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

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

        // --- EFECTO DE SONIDO ---
        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound, volume);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    public void SetPurchasedState(bool purchased)
    {
        isPurchased = purchased;
        // Forzamos que la escala base sea la de comprado o la normal
        originalScale = purchased ? Vector3.one * purchasedScale : Vector3.one;

        // IMPORTANTE: Actualizamos el target y la escala actual de inmediato
        targetScale = originalScale;
        if (rect == null) rect = GetComponent<RectTransform>();
        rect.localScale = originalScale;

        if (infoPanel != null)
        {
            infoPanel.localScale = originalScale;
        }
    }

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