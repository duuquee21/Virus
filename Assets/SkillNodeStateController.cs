using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

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
    public Sprite lockedNextLevelSprite;

    [Header("Efectos")]
    private ParticleSystem disabledParticles;
    private bool hasPlayedDisabledEffect = false;

    [Header("Configuración de Animación")]
    public float animationDuration = 0.5f;
    public float overshootFactor = 1.2f; // Qué tanto se agranda antes de volver a su tamaño
    private Coroutine animationCoroutine;

    // Variable para rastrear si el estado anterior era "Bloqueado por padres"
    private bool wasLockedByParent = true;

    private bool isHovered = false;

    void Awake()
    {
        disabledParticles = GetComponentInChildren<ParticleSystem>();
    }

    void Start()
    {
        // 1. Determinar el estado inicial
        bool currentlyLocked = !IsParentUnlocked();
        wasLockedByParent = currentlyLocked;

        // 2. Si NO está bloqueado al empezar, asegurar escala 1
        // Si está bloqueado, podrías querer una escala específica (ej: 0.8f) o dejarla en 1
        if (!currentlyLocked)
        {
            transform.localScale = Vector3.one;
        }
        else
        {
            // Opcional: Si quieres que los bloqueados se vean un poco más chicos por diseño:
            // transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            // Si quieres que midan lo mismo que todos:
            transform.localScale = Vector3.one;
        }

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
            fx.SetPurchasedState(IsAtLimit());
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;

    void UpdateState()
    {
        bool currentlyLocked = !IsParentUnlocked();

        // 1. Si ya está desbloqueado / Al límite
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

        // 2. LÓGICA PARA EL SEGUNDO NIVEL (Locked)
        if (currentlyLocked)
        {
            SetVisual(lockedNextLevelSprite, false, currentlyLocked);
            if (costText != null) costText.text = "???";
            return;
        }

        // 3. Si no hay suficiente dinero
        if (LevelManager.instance.ContagionCoins < skillNode.CoinCost)
        {
            SetVisual(notEnoughMoneySprite, false, currentlyLocked);
            return;
        }

        // 4. Estado Hover
        if (isHovered && hoverSprite != null)
        {
            SetVisual(hoverSprite, true, currentlyLocked);
            return;
        }

        // 5. Estado Normal
        SetVisual(normalSprite, true, currentlyLocked);
    }

    void SetVisual(Sprite sprite, bool isInteractable, bool isCurrentlyLocked)
    {
        if (nodeImage != null && sprite != null)
        {
            // DETECTAR TRANSICIÓN: Si antes estaba bloqueado y ahora NO lo está
            if (wasLockedByParent && !isCurrentlyLocked)
            {
                nodeImage.sprite = sprite;
                TriggerUnlockAnimation();
            }
            else if (nodeImage.sprite != sprite)
            {
                // Cambio normal de sprite sin animación especial
                nodeImage.sprite = sprite;
            }
        }

        // Actualizamos la memoria del estado de bloqueo para el siguiente frame
        wasLockedByParent = isCurrentlyLocked;

        if (button != null)
            button.interactable = isInteractable;

        if (costText != null && sprite != lockedNextLevelSprite)
            costText.text = skillNode.CoinCost.ToString();
    }

    private void TriggerUnlockAnimation()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

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

            // 1. Animación de Rotación (0 a 360)
            float currentZ = Mathf.Lerp(startEuler.z, targetZ, t);
            transform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, currentZ);

            // 2. Animación de Escala (0 -> peakScale -> 1)
            // Usamos una curva de evaluación simple o un Evaluate manual
            if (t < 0.7f) // Crecimiento hasta el pico
            {
                float tScale = t / 0.7f;
                transform.localScale = Vector3.Lerp(initialScale, peakScale, tScale);
            }
            else // Regreso al tamaño normal (1)
            {
                float tScale = (t - 0.7f) / 0.3f;
                transform.localScale = Vector3.Lerp(peakScale, targetScale, tScale);
            }

            yield return null;
        }

        // Limpieza final para asegurar precisión
        transform.localEulerAngles = startEuler;
        transform.localScale = targetScale;
        animationCoroutine = null;
    }

    // --- Métodos de comprobación originales ---
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