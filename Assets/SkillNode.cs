using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum SkillEffectType
    {
        None, RandomInitialUpgrade,
        CoinsX2, CoinsX3, CoinsX4, CoinsX5,
        StartWith50Coins, StartWith100Coins, StartWith500Coins, StartWith2500Coins, StartWith25000Coins, StartWith50000Coins,
        ReduceSpawnInterval20, ReduceSpawnInterval40, ReduceSpawnInterval60, ReduceSpawnInterval80, ReduceSpawnInterval100,
        IncreasePopulation25, IncreasePopulation50,
        AddDays5, AddDays10,
        IncreaseShinyValue1, // Suma +1
        IncreaseShinyValue3, // Suma +3
        MultiplyShinyX5,     // Multiplica x5 (Base)
        MultiplyShinyX7,     // Multiplica x7 (Base)
        MultiplyShinyX10,    // Multiplica x10 (Base)
        HalveZoneCosts
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

    [Header("UI")]
    public Button button;
    public GameObject lockIcon;

    private bool unlocked = false;
    public bool IsUnlocked => unlocked;

    void Start()
    {
        LockVisual();
    }

    public void CheckIfShouldShow()
    {
        if (requiredParentNodes != null && requiredParentNodes.Length > 0)
        {
            foreach (var parent in requiredParentNodes)
            {
                if (parent == null || !parent.IsUnlocked) return;
            }
        }
        gameObject.SetActive(true);
    }

    public void TryUnlock()
    {
        if (unlocked) return;
        if (Guardado.instance.shinyDNA < shinyCost) return;

        Guardado.instance.shinyDNA -= shinyCost;
        unlocked = true;

        UnlockVisual();
        ApplyEffect();

        SkillTreeLinesUI lines = FindObjectOfType<SkillTreeLinesUI>();
        RectTransform myRect = GetComponent<RectTransform>();

        if (transform.parent && lines)
        {
            RectTransform parentRect = transform.parent.GetComponent<RectTransform>();
            if (parentRect) lines.Unlock(parentRect, myRect);
        }

        foreach (var node in nextNodes)
        {
            if (node != null)
            {
                node.CheckIfShouldShow();
                if (lines && node.gameObject.activeSelf) lines.ShowFrom(myRect);
            }
        }

        LevelManager.instance.UpdateUI();
    }

    void ApplyEffect()
    {
        switch (effectType)
        {
            case SkillEffectType.RandomInitialUpgrade: Guardado.instance.AssignRandomInitialUpgrade(); break;
            case SkillEffectType.CoinsX2: Guardado.instance.SetCoinMultiplier(2); break;
            case SkillEffectType.CoinsX3: Guardado.instance.SetCoinMultiplier(3); break;
            case SkillEffectType.CoinsX4: Guardado.instance.SetCoinMultiplier(4); break;
            case SkillEffectType.CoinsX5: Guardado.instance.SetCoinMultiplier(5); break;
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

            // --- LÓGICA DE SUMA SHINY ---
            case SkillEffectType.IncreaseShinyValue1:
                Guardado.instance.IncreaseShinyValueSum(1);
                break;
            case SkillEffectType.IncreaseShinyValue3:
                Guardado.instance.IncreaseShinyValueSum(3);
                break;

            // --- LÓGICA DE MULTIPLICADOR SHINY ---
            case SkillEffectType.MultiplyShinyX5:
                Guardado.instance.SetShinyMultiplier(5);
                break;
            case SkillEffectType.MultiplyShinyX7:
                Guardado.instance.SetShinyMultiplier(7);
                break;
            case SkillEffectType.MultiplyShinyX10:
                Guardado.instance.SetShinyMultiplier(10);
                break;
        }

        if (LevelManager.instance != null) LevelManager.instance.RecalculateTotalDaysUntilCure();
    }

    void UnlockVisual()
    {
        button.interactable = false;
        button.image.color = Color.gray;
        if (lockIcon) lockIcon.SetActive(false);
    }

    void LockVisual()
    {
        button.interactable = true;
        button.image.color = Color.white;
        foreach (var node in nextNodes)
        {
            if (node && node.requiredParentNodes.Length > 1) node.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { SkillTooltip.instance.Show(skillName, description, shinyCost); }
    public void OnPointerExit(PointerEventData eventData) { SkillTooltip.instance.Hide(); }
}