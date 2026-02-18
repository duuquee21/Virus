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
        if (isHovered && hoverSprite != null)
        {
            SetVisual(hoverSprite);
            return;
        }

        if (skillNode.IsUnlocked)
        {
            SetVisual(disabledSprite);
            return;
        }

        if (LevelManager.instance.contagionCoins < skillNode.CoinCost)
        {
            SetVisual(notEnoughMoneySprite);
            return;
        }

        SetVisual(normalSprite);
    }

    void SetVisual(Sprite sprite)
    {
        if (nodeImage != null && sprite != null)
            nodeImage.sprite = sprite;

        if (button != null)
            button.interactable = true;

        if (costText != null)
            costText.text = skillNode.CoinCost.ToString();
    }
}
