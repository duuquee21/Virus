using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillNodeStateController : MonoBehaviour
{
    public SkillNode skillNode;
    public Button button;
    public Image nodeImage;
    public TextMeshProUGUI costText;

    [Header("Sprites de Estado")]
    public Sprite normalSprite;
    public Sprite notEnoughMoneySprite;
    public Sprite disabledSprite;

    void Update()
    {
        if (skillNode == null || LevelManager.instance == null)
            return;

        UpdateState();
    }

    void UpdateState()
    {
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
