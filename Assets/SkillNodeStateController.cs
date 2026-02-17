using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillNodeStateController : MonoBehaviour
{
    public SkillNode skillNode;
    public Button button;
    public Image nodeImage;
    public TextMeshProUGUI costText;

    [Header("Colores")]
    public Color normalColor = Color.white;
    public Color notEnoughMoneyColor = new Color(1f, 0.4f, 0.4f);
    public Color disabledColor = Color.gray;

    void Update()
    {
        if (skillNode == null || LevelManager.instance == null)
            return;

        UpdateState();
    }

    void UpdateState()
    {
        // Si ya está desbloqueado (y no es repetible)
        if (skillNode.IsUnlocked)
        {
            SetVisual(disabledColor, false);
            return;
        }

        // Si no tiene monedas suficientes → FASE 1
        if (LevelManager.instance.contagionCoins < skillNode.CoinCost)
        {
            SetVisual(notEnoughMoneyColor, false);
            return;
        }

        // Si puede comprar
        SetVisual(normalColor, true);
    }

    void SetVisual(Color color, bool interactable)
    {
        if (nodeImage != null)
            nodeImage.color = color;

        if (button != null)
            button.interactable = interactable;

        if (costText != null)
            costText.text = skillNode.CoinCost.ToString();
    }
}
