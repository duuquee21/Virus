using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SkillNodeStateController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public SkillNode skillNode;
    public Button button;
    public Image nodeImage;
    public TextMeshProUGUI costText;

    [Header("Sprites de Estado")]
    public Sprite hoverSprite;
    private bool isHovered = false;
    public Sprite normalSprite;
    public Sprite notEnoughMoneySprite;
    public Sprite disabledSprite;

    void Update()
    {
        if (skillNode == null || LevelManager.instance == null)
            return;

        UpdateState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    void UpdateState()
    {
        // 1. Si ya está desbloqueado (prioridad máxima)
        if (skillNode.IsUnlocked)
        {
            // Pasamos 'false' para que el botón no sea interactuable
            SetVisual(disabledSprite, false);
            return;
        }

        // 2. Si no hay suficiente dinero
        if (LevelManager.instance.contagionCoins < skillNode.CoinCost)
        {
            // Pasamos 'false' para que el botón no sea interactuable
            SetVisual(notEnoughMoneySprite, false);
            return;
        }

        // 3. A partir de aquí, el nodo ES comprable. 
        // Si el ratón está encima, mostramos el hover.
        if (isHovered && hoverSprite != null)
        {
            // Pasamos 'true' porque se puede comprar
            SetVisual(hoverSprite, true);
            return;
        }

        // 4. Si se puede comprar pero el ratón NO está encima, estado normal.
        SetVisual(normalSprite, true);
    }

    // Añadimos el parámetro 'bool isInteractable' para controlar el botón
    void SetVisual(Sprite sprite, bool isInteractable)
    {
        if (nodeImage != null && sprite != null)
            nodeImage.sprite = sprite;

        if (button != null)
            button.interactable = isInteractable; // Activamos o desactivamos el botón aquí

        if (costText != null)
            costText.text = skillNode.CoinCost.ToString();
    }
}