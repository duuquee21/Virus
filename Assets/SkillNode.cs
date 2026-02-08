using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
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
        IncreaseShinyValue1, IncreaseShinyValue3,
        MultiplyShinyX5, MultiplyShinyX7, MultiplyShinyX10,
        HalveZoneCosts, AddExtraShiny,
        ZoneIncome100, ZoneIncome250, ZoneIncome500, ZoneIncome1000, ZoneIncome5000,
        ShinyPassivePerZone,
        MultiplyRadius125, MultiplyRadius150, MultiplyRadius200,
        GuaranteedShinyEffect,
        MultiplySpeed125, MultiplySpeed150,
        InfectSpeed50, InfectSpeed100,
        KeepUpgradesOnResetEffect,
        ShinyCaptureSpeed50,  // +50% velocidad contra Shinies
        ShinyCaptureSpeed100 ,
        DoubleShinyEffect,
        KeepZonesOnReset,
        ExtraShiny,
        RadiusLevel2, RadiusLevel3,RadiusLevel4,RadiusLevel5, RadiusLevel6,
        SpeedLevel2, SpeedLevel3, SpeedLevel4, SpeedLevel5,
        CapacityLevel2, CapacityLevel3, CapacityLevel4, CapacityLevel5, CapacityLevel6,
        TimeLevel2, TimeLevel3, TimeLevel4, TimeLevel5, TimeLevel6,
        InfectionSpeedLevel2, InfectionSpeedLevel3, InfectionSpeedLevel4, InfectionSpeedLevel5, InfectionSpeedLevel6,// +100% velocidad contra Shinies
    }

    [Header("Datos")]
    public string skillName;
    [TextArea] public string description;
    public int CoinCost = 1;

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
    public CanvasGroup canvasGroup;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip unlockSound;

    private bool unlocked = false;
    public bool IsUnlocked => unlocked;

    void Awake() { if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>(); }
    void Start() { CheckIfShouldShow(); }

    public void CheckIfShouldShow()
    {
        if (unlocked)
        {
            SetAppearance(true, 1f, false);
            SetState(false, Color.gray, false);
            return;
        }

        if (requiredParentNodes == null || requiredParentNodes.Length == 0)
        {
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
            return;
        }

        bool allParentsUnlocked = true;
        bool atLeastOneParentUnlocked = false;

        foreach (var parent in requiredParentNodes)
        {
            if (parent != null && parent.IsUnlocked) atLeastOneParentUnlocked = true;
            else allParentsUnlocked = false;
        }

        if (allParentsUnlocked) { SetAppearance(true, 1f, true); SetState(true, Color.white, false); }
        else if (atLeastOneParentUnlocked) { SetAppearance(true, 0.15f, false); SetState(false, Color.black, true); }
        else { SetAppearance(false, 0f, false); }

        UpdateLinesVisuals();
    }

    void SetAppearance(bool isActive, float alpha, bool canClick)
    {
        gameObject.SetActive(isActive);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = canClick;
        }
        if (isActive && alpha > 0)
        {
            foreach (Transform child in transform) child.gameObject.SetActive(true);
            if (skillNameText != null) skillNameText.enabled = true;
        }
    }

    void UpdateLinesVisuals()
    {
        SkillTreeLinesUI lines = Object.FindFirstObjectByType<SkillTreeLinesUI>();
        if (lines != null && requiredParentNodes != null)
        {
            foreach (var parent in requiredParentNodes)
            {
                if (parent != null && parent.IsUnlocked) lines.ShowFrom(parent.GetComponent<RectTransform>());
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
        if (unlocked) return;

        // 1. Cambiamos la comprobación de ADN Shiny por Monedas de Contagio
        // Usamos LevelManager.instance.contagionCoins
        if (LevelManager.instance.contagionCoins < CoinCost)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayError();
            return;
        }

        // 2. Ejecutamos la compra
        if (AudioManager.instance != null) AudioManager.instance.PlayBuyUpgrade();

        if (audioSource != null && unlockSound != null) audioSource.PlayOneShot(unlockSound);

        // 3. Restamos de las monedas de contagio en lugar de ADN
        LevelManager.instance.contagionCoins -= CoinCost;

        unlocked = true;

        SetState(false, Color.gray, false);
        ApplyEffect();

        if (nextNodes != null)
        {
            foreach (var child in nextNodes) if (child != null) child.CheckIfShouldShow();
        }

        // Actualizamos toda la UI para que se refleje el gasto de monedas
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
            case SkillEffectType.ShinyPassivePerZone: Guardado.instance.SetShinyPassiveIncome(1); break;

            case SkillEffectType.MultiplyRadius125:
                Guardado.instance.SetRadiusMultiplier(1.25f);
                if (VirusRadiusController.instance != null) VirusRadiusController.instance.ApplyScale();
                break;
            case SkillEffectType.MultiplyRadius150:
                Guardado.instance.SetRadiusMultiplier(1.50f);
                if (VirusRadiusController.instance != null) VirusRadiusController.instance.ApplyScale();
                break;
            case SkillEffectType.MultiplyRadius200:
                Guardado.instance.SetRadiusMultiplier(2.00f);
                if (VirusRadiusController.instance != null) VirusRadiusController.instance.ApplyScale();
                break;

            case SkillEffectType.GuaranteedShinyEffect:
                Guardado.instance.ActivateGuaranteedShiny();
                break;

            case SkillEffectType.MultiplySpeed125:
                Guardado.instance.SetSpeedMultiplier(1.25f);
                if (VirusMovement.instance != null) VirusMovement.instance.ApplySpeedMultiplier();
                break;
            case SkillEffectType.MultiplySpeed150:
                Guardado.instance.SetSpeedMultiplier(1.50f);
                if (VirusMovement.instance != null) VirusMovement.instance.ApplySpeedMultiplier();
                break;

            case SkillEffectType.InfectSpeed50:
                Guardado.instance.SetInfectSpeedMultiplier(1.5f);
                break;
            case SkillEffectType.InfectSpeed100:
                Guardado.instance.SetInfectSpeedMultiplier(2.0f);
                break;

            // --- NUEVOS EFECTOS PARA SHINIES ---
            case SkillEffectType.ShinyCaptureSpeed50:
                Guardado.instance.SetShinyCaptureMultiplier(1.5f);
                break;
            case SkillEffectType.ShinyCaptureSpeed100:
                Guardado.instance.SetShinyCaptureMultiplier(2.0f);
                break;
            case SkillEffectType.KeepUpgradesOnResetEffect:
                Guardado.instance.ActivateKeepUpgrades();
                break;
            //case SkillEffectType.DoubleShinyEffect:
                //Guardado.instance.ActivateDoubleShiny();
                //break;
            // En SkillNode.cs, dentro de ApplyEffect()
            case SkillEffectType.KeepZonesOnReset:
                Guardado.instance.ActivateKeepZones();
                break;
            case SkillEffectType.ExtraShiny:
                Guardado.instance.AddExtraShinyLevel(); // Cada compra suma +1 al contador
                break;
            case SkillEffectType.RadiusLevel2:
                VirusRadiusController.instance.SetLevel(2);
                break;
            case SkillEffectType.RadiusLevel3:
                VirusRadiusController.instance.SetLevel(3);
                break;
            case SkillEffectType.RadiusLevel4:
                VirusRadiusController.instance.SetLevel(4);
                break;
            case SkillEffectType.RadiusLevel5:
                VirusRadiusController.instance.SetLevel(5);
                break;
            case SkillEffectType.RadiusLevel6:
                VirusRadiusController.instance.SetLevel(6);
                break;
            case SkillEffectType.SpeedLevel2:
                SpeedUpgradeController.instance.SetLevel(2);
                break;
            case SkillEffectType.SpeedLevel3:
                SpeedUpgradeController.instance.SetLevel(3);
                break;
            case SkillEffectType.SpeedLevel4:
                 SpeedUpgradeController.instance.SetLevel(4);
                 break;
            case SkillEffectType.SpeedLevel5:
                SpeedUpgradeController.instance.SetLevel(5);
                break;
            case SkillEffectType.CapacityLevel2:
                CapacityUpgradeController.instance.SetLevel(2);
                break;
            case SkillEffectType.CapacityLevel3:
            CapacityUpgradeController.instance.SetLevel(3);
                break;
            case SkillEffectType.CapacityLevel4:
            CapacityUpgradeController.instance.SetLevel(4);
                break;
            case SkillEffectType.CapacityLevel5:
            CapacityUpgradeController.instance.SetLevel(5);
                break;
            case SkillEffectType.CapacityLevel6:
            CapacityUpgradeController.instance.SetLevel(6);
                break;
            case SkillEffectType.TimeLevel2:
                TimeUpgradeController.instance.SetLevel(2);
                break;
            case SkillEffectType.TimeLevel3:
            TimeUpgradeController.instance.SetLevel(3);
                break;
            case SkillEffectType.TimeLevel4:
            TimeUpgradeController.instance.SetLevel(4);
                break;
            case SkillEffectType.TimeLevel5:
            TimeUpgradeController.instance.SetLevel(5);
                break;
            case SkillEffectType.TimeLevel6:
            TimeUpgradeController.instance.SetLevel(6);
                break;
            case SkillEffectType.InfectionSpeedLevel2:
                InfectionSpeedUpgradeController.instance.SetLevel(2);
                break;
            case SkillEffectType.InfectionSpeedLevel3:
                InfectionSpeedUpgradeController.instance.SetLevel(3);
                break;
            case SkillEffectType.InfectionSpeedLevel4:
            InfectionSpeedUpgradeController.instance.SetLevel(4);
                break;
            case SkillEffectType.InfectionSpeedLevel5:
            InfectionSpeedUpgradeController.instance.SetLevel(5);
                break;
            case SkillEffectType.InfectionSpeedLevel6:
            InfectionSpeedUpgradeController.instance.SetLevel(6);
                break;
        }
        if (LevelManager.instance != null) LevelManager.instance.RecalculateTotalDaysUntilCure();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (canvasGroup != null && canvasGroup.alpha > 0.5f && SkillTooltip.instance)
            SkillTooltip.instance.Show(skillName, description, CoinCost);
    }
    public void OnPointerExit(PointerEventData eventData) { if (SkillTooltip.instance) SkillTooltip.instance.Hide(); }
}