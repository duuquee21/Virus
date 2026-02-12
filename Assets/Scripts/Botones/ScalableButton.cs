using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening; // Importante: Instala DOTween

public class ScalableButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Configuración de UI")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Sprite purchasedSprite;

    [Header("Ajustes de Escala")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private Ease transitionEase = Ease.OutBack;

    [Header("Feedback de Error (Fase 5)")]
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private float shakeStrength = 10f;

    private Vector3 initialScale;
    private Color originalColor;
    private bool isPurchased = false;

    void Awake()
    {
        initialScale = Vector3.one * normalScale;
        transform.localScale = initialScale;
        originalColor = buttonImage.color;

        if (infoText != null) infoText.gameObject.SetActive(false);
    }

    // FASE 2: Mouse Enter
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isPurchased) return;

        // Animación de escala + movimiento sutil
        transform.DOScale(hoverScale, animationDuration).SetEase(transitionEase);
        if (infoText != null) infoText.gameObject.SetActive(true);

        OnHoverEnterEffect(); // Para partículas/sonido futuro
    }

    // FASE 3: Mouse Exit
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPurchased) return;

        transform.DOScale(normalScale, animationDuration).SetEase(transitionEase);
        if (infoText != null) infoText.gameObject.SetActive(false);
    }

    // FASE 4 y 5: Click
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isPurchased) return;

        // Lógica de compra (Ejemplo simple)
        bool canAfford = CheckEconomy();

        if (canAfford)
        {
            ExecutePurchase();
        }
        else
        {
            ExecuteErrorFeedback();
        }
    }

    private void ExecutePurchase()
    {
        isPurchased = true;
        buttonImage.sprite = purchasedSprite;

        // Animación de éxito (Fase 4)
        transform.DOScale(normalScale, animationDuration).SetEase(Ease.OutBounce);
        if (infoText != null) infoText.gameObject.SetActive(false);

        Debug.Log("Comprado!");
        // Aquí llamarías a tu sistema de partículas o sonidos
    }

    private void ExecuteErrorFeedback()
    {
        // Fase 5: Movimiento y color rojo
        buttonImage.DOColor(errorColor, 0.1f).SetLoops(2, LoopType.Yoyo);
        transform.DOShakePosition(0.3f, shakeStrength);
    }

    private bool CheckEconomy()
    {
        // Sustituir por tu lógica real de dinero
        return true;
    }

    // --- MÉTODOS PARA ESCALABILIDAD FUTURA ---
    protected virtual void OnHoverEnterEffect()
    {
        // Aquí podrás añadir sonidos o partículas sin romper la lógica base
    }
}