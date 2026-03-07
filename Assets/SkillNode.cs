using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization.Settings; // <-- ESTA ES LA BUENA (No la de UnityEditor)

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
        HalveZoneCosts,
        ZoneIncome100, ZoneIncome250, ZoneIncome500, ZoneIncome1000, ZoneIncome5000,
        MultiplyRadius125, MultiplyRadius150, MultiplyRadius200,
        MultiplySpeed125, MultiplySpeed150,
        InfectSpeed50, InfectSpeed100,
        KeepUpgradesOnResetEffect,
        KeepZonesOnReset,
        RadiusLevel2, RadiusLevel3, RadiusLevel4, RadiusLevel5, RadiusLevel6,
        SpeedLevel2, SpeedLevel3, SpeedLevel4, SpeedLevel5,
        CapacityLevel2, CapacityLevel3, CapacityLevel4, CapacityLevel5, CapacityLevel6,
        TimeLevel2, TimeLevel3, TimeLevel4, TimeLevel5, TimeLevel6,
        InfectionSpeedLevel2, InfectionSpeedLevel3, InfectionSpeedLevel4, InfectionSpeedLevel5, InfectionSpeedLevel6,
        DuplicateOnHit20, DuplicateOnHit40, DuplicateOnHit60, DuplicateOnHit80, DuplicateOnHit100,
        AddDays5, AddDays10, IncreaseShinyValue1, IncreaseShinyValue3, MultiplyShinyX5, MultiplyShinyX7,
        MultiplyShinyX10, AddExtraShiny, ShinyPassivePerZone, GuaranteedShinyEffect, ShinyCaptureSpeed50,
        ShinyCaptureSpeed100, DoubleShinyEffect, ExtraShiny, CarambolaNormal, CarambolaPro, CarambolaSuprema, DmgCirculo,
        DmgTriangulo,
        DmgCuadrado,
        DmgPentagono,
        DmgHexagono, ReboteConCoral,
        ParedInfectiva_Nivel1,
        ParedInfectiva_Nivel2,
        ParedInfectiva_Nivel3,
        ParedInfectiva_Nivel4,
        ParedInfectiva_Nivel5,
        DestroyCoralOnInfectedImpact,
        AddTime2Seconds,
        AddTimeOnPhaseChance5,
        AddTimeOnPhaseChance10,
        AddTimeOnPhaseChance15,
        AddTimeOnPhaseChance20,
        AddTimeOnPhaseChance25,
        DoubleUpgradeChance05,
        DoubleUpgradeChance10,
        DoubleUpgradeChance15,
        DoubleUpgradeChance20,
        DoubleUpgradeChance25,
        RandomSpawnAnyPhase5,
        RandomSpawnAnyPhase10,
        RandomSpawnAnyPhase15,
        RandomSpawnAnyPhase20,
        RandomSpawnAnyPhase25,
        CoinsHexagonoPlus1,
        CoinsPentagonoPlus1,
        CoinsCuadradoPlus1,
        CoinsTrianguloPlus1,
        CoinsCirculoPlus1,
        InfectSpeedPhase0_10, InfectSpeedPhase0_20, InfectSpeedPhase0_30, InfectSpeedPhase0_40, InfectSpeedPhase0_50,
        InfectSpeedPhase1_10, InfectSpeedPhase1_20, InfectSpeedPhase1_30, InfectSpeedPhase1_40, InfectSpeedPhase1_50,
        InfectSpeedPhase2_10, InfectSpeedPhase2_20, InfectSpeedPhase2_30, InfectSpeedPhase2_40, InfectSpeedPhase2_50,
        InfectSpeedPhase3_10, InfectSpeedPhase3_20, InfectSpeedPhase3_30, InfectSpeedPhase3_40, InfectSpeedPhase3_50,
        InfectSpeedPhase4_10, InfectSpeedPhase4_20, InfectSpeedPhase4_30, InfectSpeedPhase4_40, InfectSpeedPhase4_50
    }

    [Header("Save ID")]
    public string saveID;

    [Header("Balance Override (Pruebas)")]
    public bool useOverride = false;
    public int overrideInt = 0;
    public float overrideFloat = 0f;
    public int overrideIndex = 0;

    [Header("Hover Panel")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoTitle;
    public TextMeshProUGUI infoDescription;
    public TextMeshProUGUI infoCost;

    [Header("Datos de Traducción (KEYS)")]
    public string skillNameKey;
    [TextArea] public string descriptionKey;

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

    [Header("Configuración Inicial")]
    public bool isStartingNode = false;

    private bool unlocked = false;

    public int repeatLevel = 0;
    public int maxRepeatLevel = 5;
    [Header("Límite especial tiempo extra")]
    public int maxTimeRepeatLevel = 10;

    public bool IsUnlocked =>
        unlocked ||
        ((IsDamageSkill() || IsCoinSkill() || IsTimeSkill()) && repeatLevel > 0);

    void Awake() { if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>(); }

    void Start()
    {
        LoadNodeState();
        CheckIfShouldShow();

        if (infoPanel != null) infoPanel.SetActive(false);

        if (requiredParentNodes != null)
        {
            foreach (var p in requiredParentNodes)
            {
                if (p == null)
                    Debug.LogError($"<color=red>¡ERROR!</color> El nodo <b>{gameObject.name}</b> tiene un espacio vacío en sus padres.");
                else if (!p.gameObject.scene.IsValid())
                    Debug.LogWarning($"<color=orange>AVISO:</color> El nodo <b>{gameObject.name}</b> referencia a un PADRE que es un PREFAB, no un objeto de la escena.");
            }
        }
    }

    public void LoadNodeState()
    {
        if (string.IsNullOrEmpty(saveID)) return;

        int estadoGuardado = PlayerPrefs.GetInt("Skill_" + saveID + "_Unlocked", -1);

        if (estadoGuardado == 1)
        {
            unlocked = true;
        }
        else if (estadoGuardado == 0)
        {
            unlocked = false;
        }
        else
        {
            if (isStartingNode)
                unlocked = true;
            else
                unlocked = false;
        }

        repeatLevel = PlayerPrefs.GetInt("Skill_" + saveID + "_Repeat", 0);
    }

    public void CheckIfShouldShow()
    {
        if (((IsDamageSkill() || IsCoinSkill()) && repeatLevel >= maxRepeatLevel) ||
            (IsTimeSkill() && repeatLevel >= maxTimeRepeatLevel))
        {
            SetAppearance(true, 1f, false);
            SetState(false, Color.gray, false);
            return;
        }

        if (IsUnlocked)
        {
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
            return;
        }

        if (isStartingNode)
        {
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
            return;
        }

        if (requiredParentNodes == null || requiredParentNodes.Length == 0)
        {
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
            return;
        }

        bool isDirectChild = false;
        bool isGrandChild = false;

        foreach (var parent in requiredParentNodes)
        {
            if (parent == null) continue;

            if (parent.IsUnlocked)
            {
                isDirectChild = true;
                break;
            }

            if (parent.requiredParentNodes != null)
            {
                foreach (var grandParent in parent.requiredParentNodes)
                {
                    if (grandParent != null && grandParent.IsUnlocked)
                    {
                        isGrandChild = true;
                    }
                }
            }
        }

        if (isDirectChild)
        {
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
        }
        else if (isGrandChild)
        {
            SetAppearance(true, 0.5f, false);
            SetState(false, Color.gray, true);
        }
        else
        {
            SetAppearance(false, 0f, false);
        }
    }

    void SetAppearance(bool isActive, float alpha, bool canClick)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = canClick;
            canvasGroup.interactable = canClick;
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
        if (!IsDamageSkill() && !IsCoinSkill() && !IsTimeSkill() && unlocked) return;

        if ((IsDamageSkill() || IsCoinSkill()) && repeatLevel >= maxRepeatLevel)
            return;

        if (IsTimeSkill() && repeatLevel >= maxTimeRepeatLevel)
            return;

        if (LevelManager.instance.ContagionCoins < CoinCost)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlayError();
            return;
        }

        if (AudioManager.instance != null)
            AudioManager.instance.PlayBuyUpgrade();

        if (audioSource != null && unlockSound != null)
            audioSource.PlayOneShot(unlockSound);

        LevelManager.instance.ContagionCoins -= CoinCost;

        if (IsDamageSkill() || IsTimeSkill() || IsCoinSkill())
            repeatLevel++;
        else
            unlocked = true;

        ApplyEffect();
        SaveNodeState();

        LevelManager.instance.UpdateUI();

        SkillNodeHoverFX fx = GetComponent<SkillNodeHoverFX>();
        if (fx != null)
        {
            fx.PlayClickFeedback();
            fx.SetPurchasedState(true);
        }

        RefreshAllNodes();
    }

    void RefreshAllNodes()
    {
        SkillNode[] allNodes = FindObjectsOfType<SkillNode>(true);

        foreach (var node in allNodes)
        {
            node.CheckIfShouldShow();
        }
    }

    void SaveNodeState()
    {
        if (string.IsNullOrEmpty(saveID)) return;

        PlayerPrefs.SetInt("Skill_" + saveID + "_Unlocked", unlocked ? 1 : 0);
        PlayerPrefs.SetInt("Skill_" + saveID + "_Repeat", repeatLevel);
        PlayerPrefs.Save();
    }

    void ApplyEffect()
    {
        if (Guardado.instance == null) return;

        Debug.Log($"<color=green>[SkillTree]</color> Aplicando efecto: <b>{effectType}</b> del nodo: {skillNameKey}");

        int GetInt(int defaultValue) => useOverride ? overrideInt : defaultValue;
        float GetFloat(float defaultValue) => useOverride ? overrideFloat : defaultValue;

        void SetInfectSpeedPerPhase(int defaultIndex, float defaultValue)
        {
            int idx = useOverride ? overrideIndex : defaultIndex;
            float val = useOverride ? overrideFloat : defaultValue;

            var arr = Guardado.instance.infectSpeedPerPhase;
            if (arr == null) return;
            if (idx < 0 || idx >= arr.Length) return;

            arr[idx] = val;
        }

        switch (effectType)
        {
            case SkillEffectType.RandomInitialUpgrade:
                Guardado.instance.AssignRandomInitialUpgrade();
                break;

            case SkillEffectType.CoinsX2:
            case SkillEffectType.CoinsX3:
            case SkillEffectType.CoinsX4:
            case SkillEffectType.CoinsX5:
            case SkillEffectType.CoinsX6:
                Guardado.instance.AddCoinMultiplier(1);
                break;

            case SkillEffectType.StartWith50Coins: Guardado.instance.AddStartingCoins(50); break;
            case SkillEffectType.StartWith100Coins: Guardado.instance.AddStartingCoins(100); break;
            case SkillEffectType.StartWith500Coins: Guardado.instance.AddStartingCoins(500); break;
            case SkillEffectType.StartWith2500Coins: Guardado.instance.AddStartingCoins(2500); break;
            case SkillEffectType.StartWith25000Coins: Guardado.instance.AddStartingCoins(25000); break;
            case SkillEffectType.StartWith50000Coins: Guardado.instance.AddStartingCoins(50000); break;

            case SkillEffectType.ReduceSpawnInterval20: Guardado.instance.AddSpawnSpeedBonus(0.5f); break;
            case SkillEffectType.ReduceSpawnInterval40: Guardado.instance.AddSpawnSpeedBonus(0.40f); break;
            case SkillEffectType.ReduceSpawnInterval60: Guardado.instance.AddSpawnSpeedBonus(0.60f); break;
            case SkillEffectType.ReduceSpawnInterval80: Guardado.instance.AddSpawnSpeedBonus(0.80f); break;
            case SkillEffectType.ReduceSpawnInterval100: Guardado.instance.AddSpawnSpeedBonus(1.00f); break;

            case SkillEffectType.ZoneIncome100: Guardado.instance.AddZonePassiveIncome(100); break;
            case SkillEffectType.ZoneIncome250: Guardado.instance.AddZonePassiveIncome(250); break;
            case SkillEffectType.ZoneIncome500: Guardado.instance.AddZonePassiveIncome(500); break;
            case SkillEffectType.ZoneIncome1000: Guardado.instance.AddZonePassiveIncome(1000); break;
            case SkillEffectType.ZoneIncome5000: Guardado.instance.AddZonePassiveIncome(5000); break;

            case SkillEffectType.MultiplyRadius125: Guardado.instance.AddRadiusMultiplier(0.25f); break;
            case SkillEffectType.MultiplyRadius150: Guardado.instance.AddRadiusMultiplier(0.50f); break;
            case SkillEffectType.MultiplyRadius200: Guardado.instance.AddRadiusMultiplier(1.00f); break;

            case SkillEffectType.IncreasePopulation25: Guardado.instance.AddPopulationBonus(1f); break;
            case SkillEffectType.IncreasePopulation50: Guardado.instance.AddPopulationBonus(0.50f); break;
            case SkillEffectType.HalveZoneCosts: Guardado.instance.ActivateZoneDiscount(); break;

            case SkillEffectType.RadiusLevel2:
            case SkillEffectType.RadiusLevel3:
            case SkillEffectType.RadiusLevel4:
            case SkillEffectType.RadiusLevel5:
            case SkillEffectType.RadiusLevel6:
                VirusRadiusController.instance.UpgradeRadius();
                break;

            case SkillEffectType.SpeedLevel2:
            case SkillEffectType.SpeedLevel3:
            case SkillEffectType.SpeedLevel4:
            case SkillEffectType.SpeedLevel5:
                SpeedUpgradeController.instance.UpgradeSpeed();
                break;

            case SkillEffectType.TimeLevel2:
            case SkillEffectType.TimeLevel3:
            case SkillEffectType.TimeLevel4:
            case SkillEffectType.TimeLevel5:
            case SkillEffectType.TimeLevel6:
                TimeUpgradeController.instance.UpgradeTime();
                break;

            case SkillEffectType.InfectionSpeedLevel2:
            case SkillEffectType.InfectionSpeedLevel3:
            case SkillEffectType.InfectionSpeedLevel4:
            case SkillEffectType.InfectionSpeedLevel5:
            case SkillEffectType.InfectionSpeedLevel6:
                InfectionSpeedUpgradeController.instance.UpgradeInfectionSpeed();
                break;

            case SkillEffectType.MultiplySpeed125: Guardado.instance.SetSpeedMultiplier(GetFloat(1.25f)); break;
            case SkillEffectType.MultiplySpeed150: Guardado.instance.SetSpeedMultiplier(GetFloat(1.50f)); break;

            case SkillEffectType.InfectSpeed50: Guardado.instance.SetInfectionSpeedBonus(GetFloat(0.50f)); break;
            case SkillEffectType.InfectSpeed100: Guardado.instance.SetInfectionSpeedBonus(GetFloat(1.00f)); break;

            case SkillEffectType.KeepUpgradesOnResetEffect:
                Guardado.instance.keepUpgradesOnReset = true;
                break;

            case SkillEffectType.KeepZonesOnReset:
                Guardado.instance.ActivateKeepZones();
                break;

            case SkillEffectType.DuplicateOnHit20: Guardado.instance.SetDuplicateProbability(GetFloat(0.20f)); break;
            case SkillEffectType.DuplicateOnHit40: Guardado.instance.SetDuplicateProbability(GetFloat(0.40f)); break;
            case SkillEffectType.DuplicateOnHit60: Guardado.instance.SetDuplicateProbability(GetFloat(0.60f)); break;
            case SkillEffectType.DuplicateOnHit80: Guardado.instance.SetDuplicateProbability(GetFloat(0.80f)); break;
            case SkillEffectType.DuplicateOnHit100: Guardado.instance.SetDuplicateProbability(GetFloat(1.00f)); break;

            // Sustituye los tres cases anteriores por este:
            case SkillEffectType.CarambolaNormal:
            case SkillEffectType.CarambolaPro:
            case SkillEffectType.CarambolaSuprema:
                Guardado.instance.SubirNivelCarambola();
                break;

            case SkillEffectType.ParedInfectiva_Nivel1:
                Guardado.instance.ActivarParedInfectiva();
                Guardado.instance.AddNivelParedInfectiva(1);
                break;

            case SkillEffectType.ReboteConCoral:
                Guardado.instance.ReboteConCoral();
                break;

            case SkillEffectType.DmgHexagono:
                Guardado.instance.dañoExtraHexagono += GetInt(1);
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.DmgPentagono:
                Guardado.instance.dañoExtraPentagono += GetInt(1);
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.DmgCuadrado:
                Guardado.instance.dañoExtraCuadrado += GetInt(1);
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.DmgTriangulo:
                Guardado.instance.dañoExtraTriangulo += GetInt(1);
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.DmgCirculo:
                Guardado.instance.dañoExtraCirculo += GetInt(1);
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.DestroyCoralOnInfectedImpact:
                Guardado.instance.destroyCoralOnInfectedImpact = true;
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.AddTime2Seconds:
                Guardado.instance.AddExtraBaseTime(GetFloat(2f));
                break;

            case SkillEffectType.AddTimeOnPhaseChance5: Guardado.instance.AddAddTimeOnPhaseChance(0.05f); break;
            case SkillEffectType.AddTimeOnPhaseChance10: Guardado.instance.AddAddTimeOnPhaseChance(0.05f); break;
            case SkillEffectType.AddTimeOnPhaseChance15: Guardado.instance.AddAddTimeOnPhaseChance(0.05f); break;
            case SkillEffectType.AddTimeOnPhaseChance20: Guardado.instance.AddAddTimeOnPhaseChance(0.05f); break;
            case SkillEffectType.AddTimeOnPhaseChance25: Guardado.instance.AddAddTimeOnPhaseChance(0.05f); break;

            case SkillEffectType.DoubleUpgradeChance05: Guardado.instance.AddDoubleUpgradeChance(0.05f); break;
            case SkillEffectType.DoubleUpgradeChance10: Guardado.instance.AddDoubleUpgradeChance(0.05f); break;
            case SkillEffectType.DoubleUpgradeChance15: Guardado.instance.AddDoubleUpgradeChance(0.05f); break;
            case SkillEffectType.DoubleUpgradeChance20: Guardado.instance.AddDoubleUpgradeChance(0.05f); break;
            case SkillEffectType.DoubleUpgradeChance25: Guardado.instance.AddDoubleUpgradeChance(0.05f); break;

            case SkillEffectType.RandomSpawnAnyPhase5:
                Guardado.instance.AddRandomSpawnPhaseChance(0.05f); break;
            case SkillEffectType.RandomSpawnAnyPhase10:
                Guardado.instance.AddRandomSpawnPhaseChance(0.05f); break;
            case SkillEffectType.RandomSpawnAnyPhase15:
                Guardado.instance.AddRandomSpawnPhaseChance(0.05f); break;
            case SkillEffectType.RandomSpawnAnyPhase20:
                Guardado.instance.AddRandomSpawnPhaseChance(0.05f); break;
            case SkillEffectType.RandomSpawnAnyPhase25:
                Guardado.instance.AddRandomSpawnPhaseChance(0.05f); break;

            case SkillEffectType.CoinsCirculoPlus1:
                Guardado.instance.coinsExtraCirculo += GetInt(1);
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.CoinsTrianguloPlus1:
                Guardado.instance.coinsExtraTriangulo += GetInt(1);
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.CoinsCuadradoPlus1:
                Guardado.instance.coinsExtraCuadrado += GetInt(1);
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.CoinsPentagonoPlus1:
                Guardado.instance.coinsExtraPentagono += GetInt(1);
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.CoinsHexagonoPlus1:
                Guardado.instance.coinsExtraHexagono += GetInt(1);
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.InfectSpeedPhase0_10: Guardado.instance.AddInfectSpeedPerPhase(0, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase0_20: Guardado.instance.AddInfectSpeedPerPhase(0, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase0_30: Guardado.instance.AddInfectSpeedPerPhase(0, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase0_40: Guardado.instance.AddInfectSpeedPerPhase(0, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase0_50: Guardado.instance.AddInfectSpeedPerPhase(0, 0.10f); break;

            case SkillEffectType.InfectSpeedPhase1_10: Guardado.instance.AddInfectSpeedPerPhase(1, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase1_20: Guardado.instance.AddInfectSpeedPerPhase(1, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase1_30: Guardado.instance.AddInfectSpeedPerPhase(1, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase1_40: Guardado.instance.AddInfectSpeedPerPhase(1, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase1_50: Guardado.instance.AddInfectSpeedPerPhase(1, 0.10f); break;

            case SkillEffectType.InfectSpeedPhase2_10: Guardado.instance.AddInfectSpeedPerPhase(2, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase2_20: Guardado.instance.AddInfectSpeedPerPhase(2, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase2_30: Guardado.instance.AddInfectSpeedPerPhase(2, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase2_40: Guardado.instance.AddInfectSpeedPerPhase(2, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase2_50: Guardado.instance.AddInfectSpeedPerPhase(2, 0.10f); break;

            case SkillEffectType.InfectSpeedPhase3_10: Guardado.instance.AddInfectSpeedPerPhase(3, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase3_20: Guardado.instance.AddInfectSpeedPerPhase(3, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase3_30: Guardado.instance.AddInfectSpeedPerPhase(3, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase3_40: Guardado.instance.AddInfectSpeedPerPhase(3, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase3_50: Guardado.instance.AddInfectSpeedPerPhase(3, 0.10f); break;

            case SkillEffectType.InfectSpeedPhase4_10: Guardado.instance.AddInfectSpeedPerPhase(4, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase4_20: Guardado.instance.AddInfectSpeedPerPhase(4, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase4_30: Guardado.instance.AddInfectSpeedPerPhase(4, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase4_40: Guardado.instance.AddInfectSpeedPerPhase(4, 0.10f); break;
            case SkillEffectType.InfectSpeedPhase4_50: Guardado.instance.AddInfectSpeedPerPhase(4, 0.10f); break;

            default:
                Debug.LogWarning($"El efecto {effectType} no tiene un Debug específico implementado.");
                break;
        }
    }

    bool IsDamageSkill()
    {
        return effectType == SkillEffectType.DmgHexagono ||
               effectType == SkillEffectType.DmgPentagono ||
               effectType == SkillEffectType.DmgCuadrado ||
               effectType == SkillEffectType.DmgTriangulo ||
               effectType == SkillEffectType.DmgCirculo;
    }

    bool IsTimeSkill()
    {
        return effectType == SkillEffectType.AddTime2Seconds;
    }

    bool IsCoinSkill()
    {
        return effectType == SkillEffectType.CoinsHexagonoPlus1 ||
               effectType == SkillEffectType.CoinsPentagonoPlus1 ||
               effectType == SkillEffectType.CoinsCuadradoPlus1 ||
               effectType == SkillEffectType.CoinsTrianguloPlus1 ||
               effectType == SkillEffectType.CoinsCirculoPlus1;
    }

    string GetPreviewValues()
    {
        bool comprado = repeatLevel > 0;

        if (Guardado.instance == null) return "";

        Guardado g = Guardado.instance;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        float baseTime = 10f + g.extraBaseTime;

        switch (effectType)
        {
            // -------------------------
            // TIEMPO
            // -------------------------

            case SkillEffectType.AddTime2Seconds:
                {
                    float actual = baseTime;
                    float despues = baseTime + 2f;

                    if (comprado)
                        sb.AppendLine($"Tiempo: {actual:F1}s");
                    else
                        sb.AppendLine($"Tiempo: {actual:F1}s → {despues:F1}s");

                    break;
                }

            // -------------------------
            // DAÑO
            // -------------------------

            case SkillEffectType.DmgHexagono:
                {
                    int actual = 1 + g.dañoExtraHexagono;

                    if (comprado)
                        sb.AppendLine($"Daño Hexágono: {actual}");
                    else
                        sb.AppendLine($"Daño Hexágono: {actual} → {actual + 1}");

                    break;
                }

            case SkillEffectType.DmgPentagono:
                {
                    int actual = 2 + g.dañoExtraPentagono;

                    if (comprado)
                        sb.AppendLine($"Daño Pentágono: {actual}");
                    else
                        sb.AppendLine($"Daño Pentágono: {actual} → {actual + 1}");

                    break;
                }

            case SkillEffectType.DmgCuadrado:
                {
                    int actual = 3 + g.dañoExtraCuadrado;

                    if (comprado)
                        sb.AppendLine($"Daño Cuadrado: {actual}");
                    else
                        sb.AppendLine($"Daño Cuadrado: {actual} → {actual + 1}");

                    break;
                }

            case SkillEffectType.DmgTriangulo:
                {
                    int actual = 4 + g.dañoExtraTriangulo;

                    if (comprado)
                        sb.AppendLine($"Daño Triángulo: {actual}");
                    else
                        sb.AppendLine($"Daño Triángulo: {actual} → {actual + 1}");

                    break;
                }

            case SkillEffectType.DmgCirculo:
                {
                    int actual = 5 + g.dañoExtraCirculo;

                    if (comprado)
                        sb.AppendLine($"Daño Círculo: {actual}");
                    else
                        sb.AppendLine($"Daño Círculo: {actual} → {actual + 1}");

                    break;
                }

            // -------------------------
            // MONEDAS
            // -------------------------

            case SkillEffectType.CoinsHexagonoPlus1:
                {
                    int actual = 1 + g.coinsExtraHexagono;

                    if (comprado)
                        sb.AppendLine($"Monedas Hexágono: {actual}");
                    else
                        sb.AppendLine($"Monedas Hexágono: {actual} → {actual + 1}");

                    break;
                }

            case SkillEffectType.CoinsPentagonoPlus1:
                {
                    int actual = 2 + g.coinsExtraPentagono;

                    if (comprado)
                        sb.AppendLine($"Monedas Pentágono: {actual}");
                    else
                        sb.AppendLine($"Monedas Pentágono: {actual} → {actual + 1}");

                    break;
                }

            case SkillEffectType.CoinsCuadradoPlus1:
                {
                    int actual = 3 + g.coinsExtraCuadrado;

                    if (comprado)
                        sb.AppendLine($"Monedas Cuadrado: {actual}");
                    else
                        sb.AppendLine($"Monedas Cuadrado: {actual} → {actual + 1}");

                    break;
                }

            case SkillEffectType.CoinsTrianguloPlus1:
                {
                    int actual = 4 + g.coinsExtraTriangulo;

                    if (comprado)
                        sb.AppendLine($"Monedas Triángulo: {actual}");
                    else
                        sb.AppendLine($"Monedas Triángulo: {actual} → {actual + 1}");

                    break;
                }

            case SkillEffectType.CoinsCirculoPlus1:
                {
                    int actual = 5 + g.coinsExtraCirculo;

                    if (comprado)
                        sb.AppendLine($"Monedas Círculo: {actual}");
                    else
                        sb.AppendLine($"Monedas Círculo: {actual} → {actual + 1}");

                    break;
                }

            // -------------------------
            // RADIO
            // -------------------------

            case SkillEffectType.MultiplyRadius125:
                {
                    float actual = g.radiusMultiplier;
                    float despues = actual + 0.25f;

                    if (comprado)
                        sb.AppendLine($"Radio Virus: {actual:F2}");
                    else
                        sb.AppendLine($"Radio Virus: {actual:F2} → {despues:F2}");

                    break;
                }

            case SkillEffectType.MultiplyRadius150:
                {
                    float actual = g.radiusMultiplier;
                    float despues = actual + 0.50f;

                    if (comprado)
                        sb.AppendLine($"Radio Virus: {actual:F2}");
                    else
                        sb.AppendLine($"Radio Virus: {actual:F2} → {despues:F2}");

                    break;
                }

            case SkillEffectType.MultiplyRadius200:
                {
                    float actual = g.radiusMultiplier;
                    float despues = actual + 1f;

                    if (comprado)
                        sb.AppendLine($"Radio Virus: {actual:F2}");
                    else
                        sb.AppendLine($"Radio Virus: {actual:F2} → {despues:F2}");

                    break;
                }

            // -------------------------
            // VELOCIDAD
            // -------------------------

            case SkillEffectType.MultiplySpeed125:
                {
                    float actual = g.speedMultiplier;
                    float despues = actual + 0.25f;

                    if (comprado)
                        sb.AppendLine($"Velocidad Virus: {actual:F2}");
                    else
                        sb.AppendLine($"Velocidad Virus: {actual:F2} → {despues:F2}");

                    break;
                }

            case SkillEffectType.MultiplySpeed150:
                {
                    float actual = g.speedMultiplier;
                    float despues = actual + 0.50f;

                    if (comprado)
                        sb.AppendLine($"Velocidad Virus: {actual:F2}");
                    else
                        sb.AppendLine($"Velocidad Virus: {actual:F2} → {despues:F2}");

                    break;
                }

            // -------------------------
            // VELOCIDAD INFECCIÓN
            // -------------------------

            case SkillEffectType.InfectSpeed50:
                {
                    float actual = g.infectSpeedMultiplier;
                    float despues = actual + 0.50f;

                    if (comprado)
                        sb.AppendLine($"Velocidad infección: {actual:F2}");
                    else
                        sb.AppendLine($"Velocidad infección: {actual:F2} → {despues:F2}");

                    break;
                }

            case SkillEffectType.InfectSpeed100:
                {
                    float actual = g.infectSpeedMultiplier;
                    float despues = actual + 1f;

                    if (comprado)
                        sb.AppendLine($"Velocidad infección: {actual:F2}");
                    else
                        sb.AppendLine($"Velocidad infección: {actual:F2} → {despues:F2}");

                    break;
                }


            // -------------------------
            // CAPTURA POR FASE (TIEMPO REAL)
            // -------------------------

            case SkillEffectType.InfectSpeedPhase0_10:
            case SkillEffectType.InfectSpeedPhase0_20:
            case SkillEffectType.InfectSpeedPhase0_30:
            case SkillEffectType.InfectSpeedPhase0_40:
            case SkillEffectType.InfectSpeedPhase0_50:
                {
                    float resistencia = 1f;
                    float actualVel = g.infectSpeedPerPhase[0];
                    float nuevaVel = actualVel + 0.10f;

                    float tiempoActual = (PersonaInfeccion.globalInfectTime * resistencia) / actualVel;
                    float tiempoNuevo = (PersonaInfeccion.globalInfectTime * resistencia) / nuevaVel;

                    if (comprado)
                        sb.AppendLine($"Tiempo captura Hexágono: {tiempoActual:F2}s");
                    else
                        sb.AppendLine($"Tiempo captura Hexágono: {tiempoActual:F2}s → {tiempoNuevo:F2}s");

                    break;
                }

            case SkillEffectType.InfectSpeedPhase1_10:
            case SkillEffectType.InfectSpeedPhase1_20:
            case SkillEffectType.InfectSpeedPhase1_30:
            case SkillEffectType.InfectSpeedPhase1_40:
            case SkillEffectType.InfectSpeedPhase1_50:
                {
                    float resistencia = 1.2f;
                    float actualVel = g.infectSpeedPerPhase[1];
                    float nuevaVel = actualVel + 0.10f;

                    float tiempoActual = (PersonaInfeccion.globalInfectTime * resistencia) / actualVel;
                    float tiempoNuevo = (PersonaInfeccion.globalInfectTime * resistencia) / nuevaVel;

                    if (comprado)
                        sb.AppendLine($"Tiempo captura Pentágono: {tiempoActual:F2}s");
                    else
                        sb.AppendLine($"Tiempo captura Pentágono: {tiempoActual:F2}s → {tiempoNuevo:F2}s");

                    break;
                }

            case SkillEffectType.InfectSpeedPhase2_10:
            case SkillEffectType.InfectSpeedPhase2_20:
            case SkillEffectType.InfectSpeedPhase2_30:
            case SkillEffectType.InfectSpeedPhase2_40:
            case SkillEffectType.InfectSpeedPhase2_50:
                {
                    float resistencia = 1.5f;
                    float actualVel = g.infectSpeedPerPhase[2];
                    float nuevaVel = actualVel + 0.10f;

                    float tiempoActual = (PersonaInfeccion.globalInfectTime * resistencia) / actualVel;
                    float tiempoNuevo = (PersonaInfeccion.globalInfectTime * resistencia) / nuevaVel;

                    if (comprado)
                        sb.AppendLine($"Tiempo captura Cuadrado: {tiempoActual:F2}s");
                    else
                        sb.AppendLine($"Tiempo captura Cuadrado: {tiempoActual:F2}s → {tiempoNuevo:F2}s");

                    break;
                }

            case SkillEffectType.InfectSpeedPhase3_10:
            case SkillEffectType.InfectSpeedPhase3_20:
            case SkillEffectType.InfectSpeedPhase3_30:
            case SkillEffectType.InfectSpeedPhase3_40:
            case SkillEffectType.InfectSpeedPhase3_50:
                {
                    float resistencia = 1.8f;
                    float actualVel = g.infectSpeedPerPhase[3];
                    float nuevaVel = actualVel + 0.10f;

                    float tiempoActual = (PersonaInfeccion.globalInfectTime * resistencia) / actualVel;
                    float tiempoNuevo = (PersonaInfeccion.globalInfectTime * resistencia) / nuevaVel;

                    if (comprado)
                        sb.AppendLine($"Tiempo captura Triángulo: {tiempoActual:F2}s");
                    else
                        sb.AppendLine($"Tiempo captura Triángulo: {tiempoActual:F2}s → {tiempoNuevo:F2}s");

                    break;
                }

            case SkillEffectType.InfectSpeedPhase4_10:
            case SkillEffectType.InfectSpeedPhase4_20:
            case SkillEffectType.InfectSpeedPhase4_30:
            case SkillEffectType.InfectSpeedPhase4_40:
            case SkillEffectType.InfectSpeedPhase4_50:
                {
                    float resistencia = 2.2f;
                    float actualVel = g.infectSpeedPerPhase[4];
                    float nuevaVel = actualVel + 0.10f;

                    float tiempoActual = (PersonaInfeccion.globalInfectTime * resistencia) / actualVel;
                    float tiempoNuevo = (PersonaInfeccion.globalInfectTime * resistencia) / nuevaVel;

                    if (comprado)
                        sb.AppendLine($"Tiempo captura Círculo: {tiempoActual:F2}s");
                    else
                        sb.AppendLine($"Tiempo captura Círculo: {tiempoActual:F2}s → {tiempoNuevo:F2}s");

                    break;
                }
            // -------------------------
            // PROBABILIDADES
            // -------------------------

            case SkillEffectType.AddTimeOnPhaseChance5:
            case SkillEffectType.AddTimeOnPhaseChance10:
            case SkillEffectType.AddTimeOnPhaseChance15:
            case SkillEffectType.AddTimeOnPhaseChance20:
            case SkillEffectType.AddTimeOnPhaseChance25:
                {
                    float actual = g.addTimeOnPhaseChance * 100f;
                    float despues = (g.addTimeOnPhaseChance + 0.05f) * 100f;

                    if (comprado)
                        sb.AppendLine($"Bonus tiempo fase: {actual:F0}%");
                    else
                        sb.AppendLine($"Bonus tiempo fase: {actual:F0}% → {despues:F0}%");

                    break;
                }

            case SkillEffectType.DoubleUpgradeChance05:
            case SkillEffectType.DoubleUpgradeChance10:
            case SkillEffectType.DoubleUpgradeChance15:
            case SkillEffectType.DoubleUpgradeChance20:
            case SkillEffectType.DoubleUpgradeChance25:
                {
                    float actual = g.doubleUpgradeChance * 100f;
                    float despues = (g.doubleUpgradeChance + 0.05f) * 100f;

                    if (comprado)
                        sb.AppendLine($"Upgrade doble: {actual:F0}%");
                    else
                        sb.AppendLine($"Upgrade doble: {actual:F0}% → {despues:F0}%");

                    break;
                }

            // -------------------------
            // POBLACIÓN
            // -------------------------

            case SkillEffectType.IncreasePopulation25:
                {
                    float actualTotal = (PopulationManager.instance.GetRoundInitialPopulation());
                    float despuesTotal = (PopulationManager.instance.GetRoundInitialPopulation() + 1);

                    if (comprado)
                        sb.AppendLine($"Población máxima: {actualTotal:F0}");
                    else
                        sb.AppendLine($"Población máxima: {actualTotal:F0} → {despuesTotal:F0}");

                    break;
                }

        
            // -------------------------
            // PARED INFECTIVA
            // -------------------------

            case SkillEffectType.ParedInfectiva_Nivel1:
            case SkillEffectType.ParedInfectiva_Nivel2:
            case SkillEffectType.ParedInfectiva_Nivel3:
            case SkillEffectType.ParedInfectiva_Nivel4:
            case SkillEffectType.ParedInfectiva_Nivel5:
                {
                    int actual = g.nivelParedInfectiva;
                    int despues = actual + 1;

                    if (comprado)
                        sb.AppendLine($"Pared infectiva: Nivel {actual}");
                    else
                        sb.AppendLine($"Pared infectiva: Nivel {actual} → Nivel {despues}");

                    break;
                }

            // -------------------------
            // RADIO LEVEL (AUMENTO REAL)
            // -------------------------

            case SkillEffectType.RadiusLevel2:
                {
                    float actual = g.radiusMultiplier;
                    float despues = actual + 0.25f;

                    if (comprado)
                        sb.AppendLine($"Radio del virus: {actual:F2}");
                    else
                        sb.AppendLine($"Radio del virus: {actual:F2} → {despues:F2}");

                    break;
                }

            case SkillEffectType.RadiusLevel3:
                {
                    float actual = g.radiusMultiplier;
                    float despues = actual + 0.25f;

                    if (comprado)
                        sb.AppendLine($"Radio del virus: {actual:F2}");
                    else
                        sb.AppendLine($"Radio del virus: {actual:F2} → {despues:F2}");

                    break;
                }

            case SkillEffectType.RadiusLevel4:
                {
                    float actual = g.radiusMultiplier;
                    float despues = actual + 0.25f;

                    if (comprado)
                        sb.AppendLine($"Radio del virus: {actual:F2}");
                    else
                        sb.AppendLine($"Radio del virus: {actual:F2} → {despues:F2}");

                    break;
                }

            case SkillEffectType.RadiusLevel5:
                {
                    float actual = g.radiusMultiplier;
                    float despues = actual + 0.25f;

                    if (comprado)
                        sb.AppendLine($"Radio del virus: {actual:F2}");
                    else
                        sb.AppendLine($"Radio del virus: {actual:F2} → {despues:F2}");

                    break;
                }

            case SkillEffectType.RadiusLevel6:
                {
                    float actual = g.radiusMultiplier;
                    float despues = actual + 0.25f;

                    if (comprado)
                        sb.AppendLine($"Radio del virus: {actual:F2}");
                    else
                        sb.AppendLine($"Radio del virus: {actual:F2} → {despues:F2}");

                    break;
                }

            // -------------------------
            // INFECTION SPEED LEVEL
            // -------------------------

            case SkillEffectType.InfectionSpeedLevel2:
            case SkillEffectType.InfectionSpeedLevel3:
            case SkillEffectType.InfectionSpeedLevel4:
            case SkillEffectType.InfectionSpeedLevel5:
            case SkillEffectType.InfectionSpeedLevel6:
                {
                    float actual = g.infectSpeedMultiplier;
                    float despues = actual + 0.25f;

                    if (comprado)
                        sb.AppendLine($"Velocidad infección: {actual:F2}");
                    else
                        sb.AppendLine($"Velocidad infección: {actual:F2} → {despues:F2}");

                    break;
                }
            // -------------------------
            // INTERVALO DE SPAWN
            // -------------------------

            case SkillEffectType.ReduceSpawnInterval20:
            case SkillEffectType.ReduceSpawnInterval40:
            case SkillEffectType.ReduceSpawnInterval60:
            case SkillEffectType.ReduceSpawnInterval80:
            case SkillEffectType.ReduceSpawnInterval100:
                {
                    float baseInterval = 2f; // intervalo base del juego
                    float actualBonus = g.spawnSpeedBonus;
                    float nuevoBonus = actualBonus + 0.20f;

                    float actual = baseInterval / (1f + actualBonus);
                    float despues = baseInterval / (1f + nuevoBonus);

                    if (comprado)
                        sb.AppendLine($"Intervalo aparición: {actual:F2}s");
                    else
                        sb.AppendLine($"Intervalo aparición: {actual:F2}s → {despues:F2}s");

                    break;
                }
            // -------------------------
            // DUPLICAR IMPACTO
            // -------------------------

            case SkillEffectType.DuplicateOnHit20:
                {
                    float actual = g.probabilidadDuplicarChoque * 100f;
                    float despues = 20f;

                    if (comprado)
                        sb.AppendLine($"Duplicar impacto: {actual:F0}%");
                    else
                        sb.AppendLine($"Duplicar impacto: {actual:F0}% → {despues:F0}%");

                    break;
                }

            case SkillEffectType.DuplicateOnHit40:
                {
                    float actual = g.probabilidadDuplicarChoque * 100f;
                    float despues = 40f;

                    if (comprado)
                        sb.AppendLine($"Duplicar impacto: {actual:F0}%");
                    else
                        sb.AppendLine($"Duplicar impacto: {actual:F0}% → {despues:F0}%");

                    break;
                }

            case SkillEffectType.DuplicateOnHit60:
                {
                    float actual = g.probabilidadDuplicarChoque * 100f;
                    float despues = 60f;

                    if (comprado)
                        sb.AppendLine($"Duplicar impacto: {actual:F0}%");
                    else
                        sb.AppendLine($"Duplicar impacto: {actual:F0}% → {despues:F0}%");

                    break;
                }

            case SkillEffectType.DuplicateOnHit80:
                {
                    float actual = g.probabilidadDuplicarChoque * 100f;
                    float despues = 80f;

                    if (comprado)
                        sb.AppendLine($"Duplicar impacto: {actual:F0}%");
                    else
                        sb.AppendLine($"Duplicar impacto: {actual:F0}% → {despues:F0}%");

                    break;
                }

            case SkillEffectType.DuplicateOnHit100:
                {
                    float actual = g.probabilidadDuplicarChoque * 100f;
                    float despues = 100f;

                    if (comprado)
                        sb.AppendLine($"Duplicar impacto: {actual:F0}%");
                    else
                        sb.AppendLine($"Duplicar impacto: {actual:F0}% → {despues:F0}%");

                    break;
                }
            // -------------------------
            // SPEED LEVEL
            // -------------------------

            case SkillEffectType.SpeedLevel2:
            case SkillEffectType.SpeedLevel3:
            case SkillEffectType.SpeedLevel4:
            case SkillEffectType.SpeedLevel5:
                {
                    // Convertimos el resultado de la función a int
                    int actual = (int)SpeedUpgradeController.instance.GetCurrentSpeed();
                    int despues = actual + 20;

                    if (comprado)
                        sb.AppendLine($"Velocidad del virus: {actual}");
                    else
                        sb.AppendLine($"Velocidad del virus: {actual} → {despues}");

                    break;
                }
            case SkillEffectType.RandomSpawnAnyPhase5:
            case SkillEffectType.RandomSpawnAnyPhase10:
            case SkillEffectType.RandomSpawnAnyPhase15:
            case SkillEffectType.RandomSpawnAnyPhase20:
            case SkillEffectType.RandomSpawnAnyPhase25:
                {
                    float actual = g.randomSpawnPhaseChance * 100f;
                    float despues = (g.randomSpawnPhaseChance + 0.05f) * 100f;

                    if (comprado)
                        sb.AppendLine($"Spawn aleatorio: {actual:F0}%");
                    else
                        sb.AppendLine($"Spawn aleatorio: {actual:F0}% → {despues:F0}%");

                    break;
                }
        }

        return sb.ToString();
    }

    // --------------------------------------------------------------
    // MAGIA DE LA TRADUCCIÓN NATIVA (¡AHORA SÍ ESTÁ BIEN!)
    // --------------------------------------------------------------
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SkillTooltip.instance != null)
        {
            // Busca la traducción en tu tabla TextosUI
            string localizedName = LocalizationSettings.StringDatabase.GetLocalizedString("TextosJuego", skillNameKey);
            string localizedDesc = LocalizationSettings.StringDatabase.GetLocalizedString("TextosJuego", descriptionKey);

            // Si por algún motivo falta la traducción, mandamos la Key para no dejarlo en blanco
            if (string.IsNullOrEmpty(localizedName)) localizedName = skillNameKey;
            if (string.IsNullOrEmpty(localizedDesc)) localizedDesc = descriptionKey;

            string preview = GetPreviewValues();

            if (!string.IsNullOrEmpty(preview))
            {
                localizedDesc += "\n\n" + preview;
            }

            SkillTooltip.instance.Show(
                localizedName,
                localizedDesc,
                CoinCost,
                GetComponent<RectTransform>()
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SkillTooltip.instance != null)
            SkillTooltip.instance.Hide();
    }

    IEnumerator RebuildTree()
    {
        yield return null;

        var nodes = FindObjectsOfType<SkillNode>(true);

        foreach (var node in nodes)
            node.LoadNodeState();

        foreach (var node in nodes)
            node.gameObject.SetActive(false);

        foreach (var node in nodes)
            node.CheckIfShouldShow();
    }
}