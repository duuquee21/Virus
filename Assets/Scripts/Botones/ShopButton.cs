using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ShopButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Configuración Específica")]
    public int price = 100;

    [Header("Referencias Visuales")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private Sprite purchasedBackgroundSprite;

    [Header("Ajustes de Animación")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private Color errorBorderColor = Color.red;

    [Header("Textos")]
    [TextArea] public string descriptionText = "Espada: 100g";
    [SerializeField] private string purchasedText = "Ya es tuyo";

    private Color initialBorderColor;
    private bool isPurchased = false;

    void Start()
    {
        if (borderImage != null) initialBorderColor = borderImage.color;
    }

    // FASE 2: Entra el ratón
    public void OnPointerEnter(PointerEventData eventData)
    {
        // EL TEXTO: Se muestra SIEMPRE (esté comprado o no)
        string textToShow = isPurchased ? "OBJETO ADQUIRIDO" : descriptionText;
        TooltipManager.Instance.ShowTooltip(textToShow, transform.position);

        // LA ANIMACIÓN: Solo se ejecuta si NO está comprado
        if (!isPurchased)
        {
            transform.DOScale(hoverScale, 0.2f).SetEase(Ease.OutBack);
        }
    }

    // FASE 3: Sale el ratón
    public void OnPointerExit(PointerEventData eventData)
    {
        // EL TEXTO: Se oculta SIEMPRE
        TooltipManager.Instance.HideTooltip();

        // LA ANIMACIÓN: Solo intentamos resetear la escala si NO está comprado
        // (Opcional: puedes dejar que resetee siempre por seguridad, no afecta visualmente si ya está en 1f)
        if (!isPurchased)
        {
            transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        }
    }

    // FASE 4 y 5: Click
    public void OnPointerClick(PointerEventData eventData)
    {
        // AQUÍ SÍ MANTENEMOS EL BLOQUEO
        // Si ya está comprado, no hacemos nada al hacer clic (ni comprar, ni error).
        if (isPurchased) return;

        if (CheckMoney(price))
        {
            BuyItem();
        }
        else
        {
            PlayErrorAnimation();
        }
    }

    private void BuyItem()
    {
        isPurchased = true;

        borderImage.gameObject.SetActive(false);

        // Al comprar, ocultamos el tooltip momentáneamente o lo actualizamos si quisieras
        TooltipManager.Instance.HideTooltip();

        if (purchasedBackgroundSprite != null) backgroundImage.sprite = purchasedBackgroundSprite;

        // Efecto visual de compra exitosa
        transform.DOScale(1f, 0.3f).SetEase(Ease.OutElastic);
    }

    private void PlayErrorAnimation()
    {
        // Mueve todo el botón
        transform.DOShakePosition(0.4f, strength: 10f, vibrato: 20);

        // Pone rojo SOLO el borde
        borderImage.DOKill();
        borderImage.DOColor(errorBorderColor, 0.1f)
                   .SetLoops(2, LoopType.Yoyo)
                   .OnComplete(() => borderImage.color = initialBorderColor);
    }

    private bool CheckMoney(int cost)
    {
        // Tu lógica de dinero aquí
        return true;
    }
}