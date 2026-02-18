using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class ShopButtonFinal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("--- CONFIGURACIÓN GENERAL ---")]
    public int price = 100;

    [Header("--- REFERENCIAS VISUALES (SPRITES) ---")]
    [Tooltip("Fase 1: Imagen cuando NO tienes dinero (Gris)")]
    [SerializeField] private Sprite lockedSprite;

    [Tooltip("Fase 2: Imagen cuando SÍ tienes dinero pero el ratón NO está encima (Blanco)")]
    [SerializeField] private Sprite availableSprite;

    [Tooltip("Fase 3: Imagen cuando pasas el ratón por encima y puedes comprar")]
    [SerializeField] private Sprite hoverSprite; // <--- NUEVO

    [Tooltip("Fase 4: Imagen cuando ya está comprado")]
    [SerializeField] private Sprite purchasedSprite;

    [Header("--- TEXTOS INFORMATIVOS ---")]
    [TextArea] public string textLocked = "Mejora bloqueada: Necesitas 100g";
    [TextArea] public string textAvailable = "Comprar Espada: 100g";
    [TextArea] public string textPurchased = "¡Ya tienes este objeto!";

    [Header("--- AJUSTES DE ANIMACIÓN (TWEENS) ---")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float animationDuration = 0.2f;

    [Header("Configuración del Movimiento (Shake)")]
    [Tooltip("Fuerza del movimiento lateral al pasar el ratón")]
    [SerializeField] private float shakeStrength = 5f;
    [Tooltip("Vibración del movimiento (cuanto más alto, más rápido vibra)")]
    [SerializeField] private int shakeVibrato = 10;

    // Estado interno
    private Image targetImage;
    private bool isPurchased = false;
    private bool canAfford = false;
    private Vector3 originalPosition;

    void Awake()
    {
        targetImage = GetComponent<Image>();
        originalPosition = transform.localPosition;
    }

    void OnEnable()
    {
        RefreshState();
    }

    // Define el estado base (Fase 1, 2 o 4) sin contar el ratón
    public void RefreshState()
    {
        if (isPurchased)
        {
            SetSprite(purchasedSprite);
            return;
        }

        // Comprobamos dinero (Aquí simulo que siempre tienes dinero con 'true')
        canAfford = CheckMoney(price);

        if (canAfford)
        {
            // FASE 2: Disponible (Reposo)
            SetSprite(availableSprite);
        }
        else
        {
            // FASE 1: Bloqueado
            SetSprite(lockedSprite);
        }
    }

    private void SetSprite(Sprite sprite)
    {
        if (sprite != null) targetImage.sprite = sprite;
    }

    // ---------------------------------------------------------
    // EVENTOS DEL RATÓN
    // ---------------------------------------------------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Actualizamos estado lógico por si ganaste dinero justo ahora
        // (Nota: Si tu juego usa eventos, mejor llamar a RefreshState desde ahí, pero esto es seguro)
        if (!isPurchased) canAfford = CheckMoney(price);

        string textToShow = "";

        if (isPurchased)
        {
            // FASE 4: Comprado
            textToShow = textPurchased;
        }
        else if (!canAfford)
        {
            // FASE 1: Bloqueado
            textToShow = textLocked;
        }
        else
        {
            // FASE 3: Disponible + Ratón Encima
            // Cambiamos al sprite de Hover
            if (hoverSprite != null) SetSprite(hoverSprite);

            textToShow = textAvailable;
            PlayHoverAnimation();
        }

        TooltipManager.Instance.ShowTooltip(textToShow, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
        StopAnimations();

        // Al salir el ratón, si NO lo hemos comprado, volvemos a su estado base
        if (!isPurchased)
        {
            // Si podíamos pagarlo, vuelve a Fase 2 (Available), si no, Fase 1 (Locked)
            SetSprite(canAfford ? availableSprite : lockedSprite);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isPurchased) return;

        if (CheckMoney(price))
        {
            BuyItem();
        }
        else
        {
            // Feedback de error (Shake visual sin cambiar sprite)
            transform.DOKill();
            transform.localPosition = originalPosition;
            transform.DOShakePosition(0.3f, new Vector3(5, 0, 0), 20);
        }
    }

    // ---------------------------------------------------------
    // ANIMACIONES Y COMPRA
    // ---------------------------------------------------------

    private void PlayHoverAnimation()
    {
        // Escalar
        transform.DOScale(hoverScale, animationDuration).SetEase(Ease.OutBack);

        // Shake Infinito
        transform.DOShakePosition(1f, new Vector3(shakeStrength, 0, 0), shakeVibrato, 0, false, true)
                 .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopAnimations()
    {
        transform.DOKill();
        transform.DOScale(1f, 0.2f);
        transform.DOLocalMove(originalPosition, 0.2f);
    }

    private void BuyItem()
    {
        isPurchased = true;

        // FASE 4: Cambiar sprite a comprado definitivamente
        SetSprite(purchasedSprite);

        // Efecto visual de compra
        StopAnimations(); // Detenemos el shake infinito
        transform.DOScale(1.2f, 0.1f).SetLoops(2, LoopType.Yoyo);

        // Actualizar Tooltip
        TooltipManager.Instance.ShowTooltip(textPurchased, transform.position);
    }

    private bool CheckMoney(int cost)
    {
        // TODO: Conectar con tu sistema de dinero real
        return true;
    }
}