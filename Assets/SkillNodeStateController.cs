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
    public Sprite lockedNextLevelSprite; // <-- NUEVO: Sprite para el segundo nivel (candado)

    [Header("Efectos")]
    private ParticleSystem disabledParticles;
    private bool hasPlayedDisabledEffect = false;

    private bool isHovered = false;

    void Awake()
    {
        disabledParticles = GetComponentInChildren<ParticleSystem>();
    }
    void Start()
    {
        // Usamos una pequeña espera o aseguramos que el nodo tenga sus datos
        RefreshScale();
    }
    void Update()
    {
        if (skillNode == null || LevelManager.instance == null)
            return;

        UpdateState();
    }

    public void RefreshScale()
    {
        var fx = GetComponent<SkillNodeHoverFX>();
        if (fx != null && skillNode != null)
        {
            // Forzamos la carga de datos del ScriptableObject/Nodo si fuera necesario
            // skillNode.LoadNodeState(); 
            fx.SetPurchasedState(IsAtLimit());
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;

    void UpdateState()
    {
        // 1. Si ya está desbloqueado / Al límite (Máximo nivel)
        if (IsAtLimit())
        {
            SetVisual(disabledSprite, false);
            if (!hasPlayedDisabledEffect && disabledParticles != null)
            {
                disabledParticles.Play();
                hasPlayedDisabledEffect = true;
            }
            return;
        }

        // 2. LÓGICA PARA EL SEGUNDO NIVEL (Nietos)
        // Si el nodo es visible pero NO es comprable todavía porque sus padres están bloqueados
        if (!IsParentUnlocked())
        {
            SetVisual(lockedNextLevelSprite, false);
            if (costText != null) costText.text = "???"; // Opcional: ocultar costo
            return;
        }

        // 3. Si no hay suficiente dinero
        if (LevelManager.instance.ContagionCoins < skillNode.CoinCost)
        {
            SetVisual(notEnoughMoneySprite, false);
            return;
        }

        // 4. Estado Hover (Comprable)
        if (isHovered && hoverSprite != null)
        {
            SetVisual(hoverSprite, true);
            return;
        }

        // 5. Estado Normal (Disponible para comprar)
        SetVisual(normalSprite, true);
    }

    void SetVisual(Sprite sprite, bool isInteractable)
    {
        if (nodeImage != null && sprite != null)
            nodeImage.sprite = sprite;

        if (button != null)
            button.interactable = isInteractable;

        if (costText != null && sprite != lockedNextLevelSprite)
            costText.text = skillNode.CoinCost.ToString();
    }

    // Función auxiliar para saber si el nodo es comprable ahora mismo
    bool IsParentUnlocked()
    {
        if (skillNode.isStartingNode || skillNode.requiredParentNodes == null || skillNode.requiredParentNodes.Length == 0)
            return true;

        foreach (var parent in skillNode.requiredParentNodes)
        {
            if (parent != null && parent.IsUnlocked) return true;
        }
        return false;
    }

    bool IsAtLimit()
    {
        if (skillNode == null) return false;
        if (skillNode.IsUnlocked) return true;

        // Comprobación de repetibles
        bool isTime = skillNode.effectType == SkillNode.SkillEffectType.AddTime2Seconds;
        if (isTime && skillNode.repeatLevel >= skillNode.maxTimeRepeatLevel) return true;

        bool isRepeatable = (
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

        if (isRepeatable && skillNode.repeatLevel >= skillNode.maxRepeatLevel) return true;

        return false;
    }
}