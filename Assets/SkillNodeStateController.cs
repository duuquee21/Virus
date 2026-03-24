using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class SkillNodeStateController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
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
    public Sprite lockedNextLevelSprite;

    [Header("Efectos")]
    private ParticleSystem disabledParticles;
    private bool hasPlayedDisabledEffect = false;

    [Header("Configuración de Animación")]
    public float animationDuration = 0.5f;
    public float overshootFactor = 1.2f;
    private Coroutine animationCoroutine;

    private bool wasLockedByParent = true;
    private bool isHovered = false;

    void Awake()
    {
        disabledParticles = GetComponentInChildren<ParticleSystem>();
    }

    void Start()
    {
        bool currentlyLocked = !IsParentUnlocked();
        wasLockedByParent = currentlyLocked;

      
        RefreshScale();
    }

    void Update()
    {
        if (skillNode == null || LevelManager.instance == null) return;
        UpdateState();
    }

    public void RefreshScale()
    {
        var fx = GetComponent<SkillNodeHoverFX>();
        if (fx != null && skillNode != null)
        {
            fx.SetPurchasedState(IsAtLimit());
        }
    }

    // --- RATÓN Y MANDO UNIFICADOS PARA EL SPRITE VISUAL ---
    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;
    public void OnSelect(BaseEventData eventData) => isHovered = true;
    public void OnDeselect(BaseEventData eventData) => isHovered = false;

    void UpdateState()
    {
        bool currentlyLocked = !IsParentUnlocked();

        if (IsAtLimit())
        {
            SetVisual(disabledSprite, false, currentlyLocked);
            if (!hasPlayedDisabledEffect && disabledParticles != null)
            {
                disabledParticles.Play();
                hasPlayedDisabledEffect = true;
            }
            return;
        }

        if (currentlyLocked)
        {
            SetVisual(lockedNextLevelSprite, false, currentlyLocked);
            if (costText != null) costText.text = "???";
            return;
        }

        if (LevelManager.instance.ContagionCoins < skillNode.CoinCost)
        {
            SetVisual(notEnoughMoneySprite, false, currentlyLocked);
            return;
        }

        if (isHovered && hoverSprite != null)
        {
            SetVisual(hoverSprite, true, currentlyLocked);
            return;
        }

        SetVisual(normalSprite, true, currentlyLocked);
    }

    void SetVisual(Sprite sprite, bool isInteractable, bool isCurrentlyLocked)
    {
        if (nodeImage != null && sprite != null)
        {
            if (wasLockedByParent && !isCurrentlyLocked)
            {
                nodeImage.sprite = sprite;
                TriggerUnlockAnimation();
            }
            else if (nodeImage.sprite != sprite)
            {
                nodeImage.sprite = sprite;
            }
        }

        wasLockedByParent = isCurrentlyLocked;

        if (button != null) button.interactable = isInteractable;
        if (costText != null && sprite != lockedNextLevelSprite) costText.text = skillNode.CoinCost.ToString();
    }

    private void TriggerUnlockAnimation()
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(UnlockAnimationRoutine());
    }

    private IEnumerator UnlockAnimationRoutine()
    {
        float elapsed = 0f;
        Vector3 initialScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;
        Vector3 peakScale = Vector3.one * overshootFactor;
        Vector3 startEuler = transform.localEulerAngles;
        float targetZ = startEuler.z + 360f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float currentZ = Mathf.Lerp(startEuler.z, targetZ, t);
            transform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, currentZ);

            if (t < 0.7f) transform.localScale = Vector3.Lerp(initialScale, peakScale, t / 0.7f);
            else transform.localScale = Vector3.Lerp(peakScale, targetScale, (t - 0.7f) / 0.3f);

            yield return null;
        }
        var fx = GetComponent<SkillNodeHoverFX>();
        if (fx != null)
        {
            transform.localScale = fx.purchasedScale < 1f && IsAtLimit() ?
                                   Vector3.one * fx.purchasedScale : Vector3.one;
        }
        else
        {
            transform.localScale = Vector3.one;
        }

        animationCoroutine = null;
    }

    bool IsParentUnlocked()
    {
        if (skillNode.isStartingNode || skillNode.requiredParentNodes == null || skillNode.requiredParentNodes.Length == 0) return true;
        foreach (var parent in skillNode.requiredParentNodes) if (parent != null && parent.IsUnlocked) return true;
        return false;
    }

    bool IsAtLimit()
    {
        if (skillNode == null) return false;
        if (skillNode.IsUnlocked) return true;

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