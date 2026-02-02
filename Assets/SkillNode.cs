using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))] // Necesario para la transparencia
public class SkillNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum SkillEffectType
    {
        None, RandomInitialUpgrade,
        CoinsX2, CoinsX3, CoinsX4, CoinsX5, CoinsX6,
        StartWith50Coins, StartWith100Coins, StartWith500Coins, StartWith2500Coins, StartWith25000Coins, StartWith50000Coins,
        ReduceSpawnInterval20, ReduceSpawnInterval40, ReduceSpawnInterval60, ReduceSpawnInterval80, ReduceSpawnInterval100,
        IncreasePopulation25, IncreasePopulation50,
        AddDays5, AddDays10,
        IncreaseShinyValue1,
        IncreaseShinyValue3,
        MultiplyShinyX5,
        MultiplyShinyX7,
        MultiplyShinyX10,
        HalveZoneCosts,
        AddExtraShiny,
        ZoneIncome100,
        ZoneIncome250,
        ZoneIncome500,
        ZoneIncome1000,
        ZoneIncome5000,
        ShinyPassivePerZone
    }

    [Header("Datos")]
    public string skillName;
    [TextArea] public string description;
    public int shinyCost = 1;

    [Header("Ramas")]
    public SkillNode[] nextNodes;
    public SkillNode[] requiredParentNodes;

    [Header("Efecto")]
    public SkillEffectType effectType = SkillEffectType.None;

    [Header("UI References")]
    public Button button;
    public GameObject lockIcon;
    public Image nodeImage;
    public TextMeshProUGUI skillNameText;
    public CanvasGroup canvasGroup; // Para controlar visibilidad sin desactivar

    [Header("Audio")]
    public AudioSource audioSource; // Arrastra un AudioSource aquí o al objeto padre
    public AudioClip unlockSound;   // Arrastra aquí tu sonido de "clinck"

    private bool unlocked = false;
    public bool IsUnlocked => unlocked;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        CheckIfShouldShow();
    }

    public void CheckIfShouldShow()
    {
        // 1. SI YA ESTÁ COMPRADO
        if (unlocked)
        {
            SetAppearance(true, 1f, false); // Visible, opaco, sin clics (ya es tuyo)
            SetState(false, Color.gray, false);
            return;
        }

        // 2. SI ES NODO ROOT (Sin padres)
        if (requiredParentNodes == null || requiredParentNodes.Length == 0)
        {
            SetAppearance(true, 1f, true); // Visible, opaco, clickable
            SetState(true, Color.white, false);
            return;
        }

        // 3. COMPROBACIÓN DE PADRES
        bool allParentsUnlocked = true;
        bool atLeastOneParentUnlocked = false;

        foreach (var parent in requiredParentNodes)
        {
            if (parent != null && parent.IsUnlocked)
                atLeastOneParentUnlocked = true;
            else
                allParentsUnlocked = false;
        }

        // --- LÓGICA DE REVELACIÓN ---
        if (allParentsUnlocked)
        {
            // DISPONIBLE: Se ve perfecto y se puede comprar
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
        }
        else if (atLeastOneParentUnlocked)
        {
            // FANTASMA: Se ve la línea y una sombra del nodo, pero no se puede clicar
            // Alpha 0.15f hace que sea una "sugerencia" visual
            SetAppearance(true, 0.15f, false);
            SetState(false, Color.black, true);
        }
        else
        {
            // OCULTO: Si ningún padre está comprado, desaparece todo
            SetAppearance(false, 0f, false);
        }

        // Forzar a las líneas a actualizarse
        UpdateLinesVisuals();
    }

    void SetAppearance(bool isActive, float alpha, bool canClick)
    {
        gameObject.SetActive(isActive);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = canClick; // Evita que el Tooltip salga en nodos fantasma
        }

        if (isActive && alpha > 0)
        {
            // Asegurar que el texto y demás hijos se vean (tu bug anterior)
            foreach (Transform child in transform) child.gameObject.SetActive(true);
            if (skillNameText != null) skillNameText.enabled = true;
        }
    }

    void UpdateLinesVisuals()
    {
        SkillTreeLinesUI lines = FindObjectOfType<SkillTreeLinesUI>();
        if (lines != null && requiredParentNodes != null)
        {
            RectTransform myRect = GetComponent<RectTransform>();
            foreach (var parent in requiredParentNodes)
            {
                if (parent != null && parent.IsUnlocked)
                {
                    // Esto dibuja la línea gris desde el padre hacia este nodo fantasma
                    lines.ShowFrom(parent.GetComponent<RectTransform>());
                }
            }
        }
    }

    void SetState(bool isInteractable, Color color, bool showLock)
    {
        if (button != null) button.interactable = isInteractable;
        if (nodeImage != null) nodeImage.color = color;
        if (lockIcon != null) lockIcon.SetActive(showLock);
    }

    public void TryUnlock()
    {
        if (unlocked || (button != null && !button.interactable)) return;
        if (Guardado.instance.shinyDNA < shinyCost) return;

        // --- NUEVO: REPRODUCIR SONIDO ---
        if (audioSource != null && unlockSound != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }
        // -------------------------------

        Guardado.instance.shinyDNA -= shinyCost;
        unlocked = true;

        SetState(false, Color.gray, false);
        ApplyEffect();

        if (nextNodes != null)
        {
            foreach (var child in nextNodes)
            {
                if (child != null) child.CheckIfShouldShow();
            }
        }

        LevelManager.instance.UpdateUI();
    }

    void ApplyEffect()
    {
        if (Guardado.instance == null) return;
        switch (effectType)
        {
            case SkillEffectType.RandomInitialUpgrade: Guardado.instance.AssignRandomInitialUpgrade(); break;
            case SkillEffectType.CoinsX2: Guardado.instance.SetCoinMultiplier(2); break;
            case SkillEffectType.CoinsX3: Guardado.instance.SetCoinMultiplier(3); break;
            case SkillEffectType.CoinsX4: Guardado.instance.SetCoinMultiplier(4); break;
            case SkillEffectType.CoinsX5: Guardado.instance.SetCoinMultiplier(5); break;
            case SkillEffectType.CoinsX6: Guardado.instance.SetCoinMultiplier(6); break;
            case SkillEffectType.StartWith50Coins: Guardado.instance.SetStartingCoins(50); break;
            case SkillEffectType.StartWith100Coins: Guardado.instance.SetStartingCoins(100); break;
            case SkillEffectType.StartWith500Coins: Guardado.instance.SetStartingCoins(500); break;
            case SkillEffectType.StartWith2500Coins: Guardado.instance.SetStartingCoins(2500); break;
            case SkillEffectType.StartWith25000Coins: Guardado.instance.SetStartingCoins(25000); break;
            case SkillEffectType.StartWith50000Coins: Guardado.instance.SetStartingCoins(50000); break;
            case SkillEffectType.ReduceSpawnInterval20: Guardado.instance.AddSpawnSpeedBonus(0.20f); break;
            case SkillEffectType.ReduceSpawnInterval40: Guardado.instance.AddSpawnSpeedBonus(0.40f); break;
            case SkillEffectType.ReduceSpawnInterval60: Guardado.instance.AddSpawnSpeedBonus(0.60f); break;
            case SkillEffectType.ReduceSpawnInterval80: Guardado.instance.AddSpawnSpeedBonus(0.80f); break;
            case SkillEffectType.ReduceSpawnInterval100: Guardado.instance.AddSpawnSpeedBonus(1.00f); break;
            case SkillEffectType.IncreasePopulation25: Guardado.instance.AddPopulationBonus(0.25f); break;
            case SkillEffectType.IncreasePopulation50: Guardado.instance.AddPopulationBonus(0.50f); break;
            case SkillEffectType.AddDays5: Guardado.instance.AddBonusDays(5); break;
            case SkillEffectType.AddDays10: Guardado.instance.AddBonusDays(10); break;
            case SkillEffectType.HalveZoneCosts: Guardado.instance.ActivateZoneDiscount(); break;
            case SkillEffectType.AddExtraShiny: Guardado.instance.AddExtraShiny(); break;
            case SkillEffectType.ZoneIncome100: Guardado.instance.SetZonePassiveIncome(100); break;
            case SkillEffectType.ZoneIncome250: Guardado.instance.SetZonePassiveIncome(250); break;
            case SkillEffectType.ZoneIncome500: Guardado.instance.SetZonePassiveIncome(500); break;
            case SkillEffectType.ZoneIncome1000: Guardado.instance.SetZonePassiveIncome(1000); break;
            case SkillEffectType.ZoneIncome5000: Guardado.instance.SetZonePassiveIncome(5000); break;
            case SkillEffectType.IncreaseShinyValue1: Guardado.instance.IncreaseShinyValueSum(1); break;
            case SkillEffectType.IncreaseShinyValue3: Guardado.instance.IncreaseShinyValueSum(3); break;
            case SkillEffectType.MultiplyShinyX5: Guardado.instance.SetShinyMultiplier(5); break;
            case SkillEffectType.MultiplyShinyX7: Guardado.instance.SetShinyMultiplier(7); break;
            case SkillEffectType.MultiplyShinyX10: Guardado.instance.SetShinyMultiplier(10); break;
            case SkillEffectType.ShinyPassivePerZone:
                Guardado.instance.SetShinyPassiveIncome(1); // Da 1 por zona
                break;
        }
        if (LevelManager.instance != null) LevelManager.instance.RecalculateTotalDaysUntilCure();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (canvasGroup != null && canvasGroup.alpha > 0.5f && SkillTooltip.instance)
            SkillTooltip.instance.Show(skillName, description, shinyCost);
    }
    public void OnPointerExit(PointerEventData eventData) { if (SkillTooltip.instance) SkillTooltip.instance.Hide(); }
}