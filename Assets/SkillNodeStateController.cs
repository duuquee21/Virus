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
    public Sprite normalSprite;
    public Sprite notEnoughMoneySprite;
    public Sprite disabledSprite;

    [Header("Efectos")]
    private ParticleSystem disabledParticles;
    private bool hasPlayedDisabledEffect = false; // Control para que solo salte una vez

    private bool isHovered = false;

    void Awake()
    {
        // Buscamos el sistema de partículas en los hijos automáticamente al iniciar
        disabledParticles = GetComponentInChildren<ParticleSystem>();
    }

    void Update()
    {
        if (skillNode == null || LevelManager.instance == null)
            return;

        UpdateState();
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;

    void UpdateState()
    {
        // 1. Si ya está desbloqueado / Al límite
        if (IsAtLimit())
        {
            SetVisual(disabledSprite, false);

            // Lógica de partículas: Solo si no han saltado ya
            if (!hasPlayedDisabledEffect && disabledParticles != null)
            {
                disabledParticles.Play();
                hasPlayedDisabledEffect = true;
            }
            return;
        }

        // Reset del efecto si por alguna razón el nodo volviera a estar activo (opcional)
        // hasPlayedDisabledEffect = false; 

        // 2. Si no hay suficiente dinero
        if (LevelManager.instance.contagionCoins < skillNode.CoinCost)
        {
            SetVisual(notEnoughMoneySprite, false);
            return;
        }

        // 3. Estado Hover (Comprable)
        if (isHovered && hoverSprite != null)
        {
            SetVisual(hoverSprite, true);
            return;
        }

        // 4. Estado Normal
        SetVisual(normalSprite, true);
    }

    void SetVisual(Sprite sprite, bool isInteractable)
    {
        if (nodeImage != null && sprite != null)
            nodeImage.sprite = sprite;

        if (button != null)
            button.interactable = isInteractable;

        if (costText != null)
            costText.text = skillNode.CoinCost.ToString();
    }

    bool IsAtLimit()
    {
        if (skillNode == null) return false;
        if (skillNode.IsUnlocked) return true;

        if (skillNode.effectType == SkillNode.SkillEffectType.AddTime2Seconds &&
            skillNode.repeatLevel >= skillNode.maxTimeRepeatLevel)
            return true;

        // Simplificado para lectura
        bool isRepeatableType = (
            skillNode.effectType == SkillNode.SkillEffectType.DmgHexagono ||
            skillNode.effectType == SkillNode.SkillEffectType.DmgPentagono ||
            skillNode.effectType == SkillNode.SkillEffectType.DmgCuadrado ||
            skillNode.effectType == SkillNode.SkillEffectType.DmgTriangulo ||
            skillNode.effectType == SkillNode.SkillEffectType.DmgCirculo ||
            skillNode.effectType == SkillNode.SkillEffectType.CoinsHexagonoPlus1 ||
            skillNode.effectType == SkillNode.SkillEffectType.CoinsPentagonoPlus1 ||
            skillNode.effectType == SkillNode.SkillEffectType.CoinsCuadradoPlus1 ||
            skillNode.effectType == SkillNode.SkillEffectType.CoinsTrianguloPlus1 ||
            skillNode.effectType == SkillNode.SkillEffectType.CoinsCirculoPlus1
        );

        if (isRepeatableType && skillNode.repeatLevel >= skillNode.maxRepeatLevel)
            return true;

        return false;
    }
}