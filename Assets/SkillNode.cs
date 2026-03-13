using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings; // <-- ESTA ES LA BUENA (No la de UnityEditor)
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class SkillNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly System.Collections.Generic.Dictionary<string, bool> runtimeUnlocked =
    new System.Collections.Generic.Dictionary<string, bool>();

    private static readonly System.Collections.Generic.Dictionary<string, int> runtimeRepeat =
        new System.Collections.Generic.Dictionary<string, int>();
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
        InfectSpeedPhase4_10, InfectSpeedPhase4_20, InfectSpeedPhase4_30, InfectSpeedPhase4_40, InfectSpeedPhase4_50,
        ParedInfectiva_Hexagono,
        ParedInfectiva_Pentagono,
        ParedInfectiva_Cuadrado,
        ParedInfectiva_Triangulo,
        ParedInfectiva_Circulo,
        UnlockExtraTimeLogic,
        ActivarCoralInfeccioso,
        MejorarCapacidadCoral,
        ActivarHojaNegra,
        ActivarAgujeroNegro,
        MejorarSpawnHojaNegra,
        MejorarDmgHojaNegra,
        MejorarSpawnAgujeroNegro
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

        // Primero: estado de la run actual
        if (runtimeUnlocked.TryGetValue(saveID, out bool runUnlocked))
        {
            unlocked = runUnlocked;
            repeatLevel = runtimeRepeat.TryGetValue(saveID, out int runRepeat) ? runRepeat : 0;
            return;
        }

        // Si no hay estado de run, cargamos del save real
        int estadoGuardado = PlayerPrefs.GetInt("Skill_" + saveID + "_Unlocked", -1);

        if (estadoGuardado == 1)
            unlocked = true;
        else if (estadoGuardado == 0)
            unlocked = false;
        else
            unlocked = isStartingNode;

        repeatLevel = PlayerPrefs.GetInt("Skill_" + saveID + "_Repeat", 0);

        // Guardamos también en memoria de run
        runtimeUnlocked[saveID] = unlocked;
        runtimeRepeat[saveID] = repeatLevel;
    }
    public void CheckIfShouldShow()
    {
        // Si llegó al máximo nivel
        if (((IsDamageSkill() || IsCoinSkill()) && repeatLevel >= maxRepeatLevel) ||
            (IsTimeSkill() && repeatLevel >= maxTimeRepeatLevel))
        {
            // CAMBIO: El tercer parámetro ahora es TRUE para que el ratón lo detecte
            SetAppearance(true, 1f, true);
            // El botón se desactiva aquí (es el primer parámetro de SetState)
            SetState(false, Color.gray, false);
            return;
        }

        if (IsUnlocked)
        {
            // CAMBIO: Aseguramos que se pueda detectar el ratón
            SetAppearance(true, 1f, true);

            // Si es una habilidad que NO se puede repetir, desactivamos el botón tras comprarla
            bool canStillBuy = IsDamageSkill() || IsCoinSkill() || IsTimeSkill();
            SetState(canStillBuy, Color.white, false);
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
        // 1. Validaciones iniciales para ver si ya se compró o llegó al máximo
        if (!IsDamageSkill() && !IsCoinSkill() && !IsTimeSkill() && unlocked) return;

        if ((IsDamageSkill() || IsCoinSkill()) && repeatLevel >= maxRepeatLevel)
            return;

        if (IsTimeSkill() && repeatLevel >= maxTimeRepeatLevel)
            return;

        // 2. Comprobamos PRIMERO si tiene dinero suficiente
        if (LevelManager.instance.ContagionCoins < CoinCost)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlayError();
            return;
        }

        // 3. Cobramos las monedas
        LevelManager.instance.ContagionCoins -= CoinCost;

        // 4. Reproducimos los sonidos de compra
        if (AudioManager.instance != null)
            AudioManager.instance.PlayBuyUpgrade();

        if (audioSource != null && unlockSound != null)
            audioSource.PlayOneShot(unlockSound);

        // 5. Actualizamos el estado de desbloqueo / niveles (UNA SOLA VEZ)
        if (IsDamageSkill() || IsTimeSkill() || IsCoinSkill())
            repeatLevel++;
        else
            unlocked = true;

        // Guardar estado en memoria de la run
        runtimeUnlocked[saveID] = unlocked;
        runtimeRepeat[saveID] = repeatLevel;

        // 6. Aplicamos el efecto (UNA SOLA VEZ)
        ApplyEffect();

        // 7. Actualizamos la Interfaz y damos Feedback visual
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

    public void SaveNodeState()
    {
        if (string.IsNullOrEmpty(saveID)) return;

        PlayerPrefs.SetInt("Skill_" + saveID + "_Unlocked", unlocked ? 1 : 0);
        PlayerPrefs.SetInt("Skill_" + saveID + "_Repeat", repeatLevel);
    }
    public void ResetNodeState()
    {
        unlocked = isStartingNode;
        repeatLevel = 0;

        if (!string.IsNullOrEmpty(saveID))
        {
            runtimeUnlocked[saveID] = unlocked;
            runtimeRepeat[saveID] = repeatLevel;
        }

        CheckIfShouldShow();
    }

    public static void ClearRuntimeState()
    {
        runtimeUnlocked.Clear();
        runtimeRepeat.Clear();
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
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.StartWith50Coins: Guardado.instance.AddStartingCoins(50); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.StartWith100Coins: Guardado.instance.AddStartingCoins(100); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.StartWith500Coins: Guardado.instance.AddStartingCoins(500); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.StartWith2500Coins: Guardado.instance.AddStartingCoins(2500); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.StartWith25000Coins: Guardado.instance.AddStartingCoins(25000); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.StartWith50000Coins: Guardado.instance.AddStartingCoins(50000); Guardado.instance.SaveData(); break; //old

            case SkillEffectType.ReduceSpawnInterval20: Guardado.instance.AddSpawnSpeedBonus(0.5f); Guardado.instance.SaveData(); break;
            case SkillEffectType.ReduceSpawnInterval40: Guardado.instance.AddSpawnSpeedBonus(0.40f); Guardado.instance.SaveData(); break;
            case SkillEffectType.ReduceSpawnInterval60: Guardado.instance.AddSpawnSpeedBonus(0.60f); Guardado.instance.SaveData(); break;
            case SkillEffectType.ReduceSpawnInterval80: Guardado.instance.AddSpawnSpeedBonus(0.80f); Guardado.instance.SaveData(); break;
            case SkillEffectType.ReduceSpawnInterval100: Guardado.instance.AddSpawnSpeedBonus(1.00f); Guardado.instance.SaveData(); break;

            case SkillEffectType.ZoneIncome100: Guardado.instance.AddZonePassiveIncome(100); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.ZoneIncome250: Guardado.instance.AddZonePassiveIncome(250); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.ZoneIncome500: Guardado.instance.AddZonePassiveIncome(500); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.ZoneIncome1000: Guardado.instance.AddZonePassiveIncome(1000); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.ZoneIncome5000: Guardado.instance.AddZonePassiveIncome(5000); Guardado.instance.SaveData(); break; //old

            case SkillEffectType.MultiplyRadius125: Guardado.instance.AddRadiusMultiplier(0.25f); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.MultiplyRadius150: Guardado.instance.AddRadiusMultiplier(0.50f); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.MultiplyRadius200: Guardado.instance.AddRadiusMultiplier(1.00f); Guardado.instance.SaveData(); break; //old

            case SkillEffectType.IncreasePopulation25: Guardado.instance.AddPopulationBonus(5f); Guardado.instance.SaveData(); break;
            case SkillEffectType.IncreasePopulation50: Guardado.instance.AddPopulationBonus(0.50f); Guardado.instance.SaveData(); break;
            case SkillEffectType.HalveZoneCosts: Guardado.instance.ActivateZoneDiscount(); Guardado.instance.SaveData(); break; //old

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

            case SkillEffectType.MultiplySpeed125: Guardado.instance.SetSpeedMultiplier(GetFloat(1.25f)); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.MultiplySpeed150: Guardado.instance.SetSpeedMultiplier(GetFloat(1.50f)); Guardado.instance.SaveData(); break; //old

            case SkillEffectType.InfectSpeed50: Guardado.instance.SetInfectionSpeedBonus(GetFloat(0.50f)); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.InfectSpeed100: Guardado.instance.SetInfectionSpeedBonus(GetFloat(1.00f)); Guardado.instance.SaveData(); break; //old

            case SkillEffectType.KeepUpgradesOnResetEffect:
                Guardado.instance.keepUpgradesOnReset = true;
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.KeepZonesOnReset:
                Guardado.instance.ActivateKeepZones();
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.DuplicateOnHit20: Guardado.instance.SetDuplicateProbability(GetFloat(0.20f)); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.DuplicateOnHit40: Guardado.instance.SetDuplicateProbability(GetFloat(0.40f)); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.DuplicateOnHit60: Guardado.instance.SetDuplicateProbability(GetFloat(0.60f)); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.DuplicateOnHit80: Guardado.instance.SetDuplicateProbability(GetFloat(0.80f)); Guardado.instance.SaveData(); break; //old
            case SkillEffectType.DuplicateOnHit100: Guardado.instance.SetDuplicateProbability(GetFloat(1.00f)); Guardado.instance.SaveData(); break; //old

            // Sustituye los tres cases anteriores por este:
            case SkillEffectType.CarambolaNormal:
            case SkillEffectType.CarambolaPro:
            case SkillEffectType.CarambolaSuprema:
                Guardado.instance.SubirNivelCarambola();
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.ParedInfectiva_Nivel1:
                Guardado.instance.ActivarParedInfectiva();
                Guardado.instance.AddNivelParedInfectiva(1);
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.ParedInfectiva_Hexagono:
                Guardado.instance.ActivarParedInfectiva(); // Activa la habilidad general
                Guardado.instance.AddNivelParedInfectivaPorFigura(0); // Fase 0 = Hexágono
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.ParedInfectiva_Pentagono:
                Guardado.instance.ActivarParedInfectiva(); // Activa la habilidad general
                Guardado.instance.AddNivelParedInfectivaPorFigura(1); // Fase 1 = Pentágono
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.ParedInfectiva_Cuadrado:
                Guardado.instance.AddNivelParedInfectivaPorFigura(2); // Fase 2 = Cuadrado
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.ParedInfectiva_Triangulo:
                Guardado.instance.AddNivelParedInfectivaPorFigura(3); // Fase 3 = Triángulo
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.ParedInfectiva_Circulo:
                Guardado.instance.AddNivelParedInfectivaPorFigura(4); // Fase 4 = Círculo
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.ReboteConCoral:
                Guardado.instance.ReboteConCoral();
                Guardado.instance.SaveData();
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
                Guardado.instance.dañoExtraCuadrado += GetInt(2);
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.DmgTriangulo:
                Guardado.instance.dañoExtraTriangulo += GetInt(3);
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.DmgCirculo:
                Guardado.instance.dañoExtraCirculo += GetInt(4);
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.DestroyCoralOnInfectedImpact:
                Guardado.instance.destroyCoralOnInfectedImpact = true;
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.AddTime2Seconds:
                Guardado.instance.AddExtraBaseTime(GetFloat(2f));
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.AddTimeOnPhaseChance5: Guardado.instance.AddAddTimeOnPhaseChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.AddTimeOnPhaseChance10: Guardado.instance.AddAddTimeOnPhaseChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.AddTimeOnPhaseChance15: Guardado.instance.AddAddTimeOnPhaseChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.AddTimeOnPhaseChance20: Guardado.instance.AddAddTimeOnPhaseChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.AddTimeOnPhaseChance25: Guardado.instance.AddAddTimeOnPhaseChance(0.05f); Guardado.instance.SaveData(); break;

            case SkillEffectType.DoubleUpgradeChance05: Guardado.instance.AddDoubleUpgradeChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.DoubleUpgradeChance10: Guardado.instance.AddDoubleUpgradeChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.DoubleUpgradeChance15: Guardado.instance.AddDoubleUpgradeChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.DoubleUpgradeChance20: Guardado.instance.AddDoubleUpgradeChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.DoubleUpgradeChance25: Guardado.instance.AddDoubleUpgradeChance(0.05f); Guardado.instance.SaveData(); break;

            case SkillEffectType.RandomSpawnAnyPhase5:
                Guardado.instance.AddRandomSpawnPhaseChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.RandomSpawnAnyPhase10:
                Guardado.instance.AddRandomSpawnPhaseChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.RandomSpawnAnyPhase15:
                Guardado.instance.AddRandomSpawnPhaseChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.RandomSpawnAnyPhase20:
                Guardado.instance.AddRandomSpawnPhaseChance(0.05f); Guardado.instance.SaveData(); break;
            case SkillEffectType.RandomSpawnAnyPhase25:
                Guardado.instance.AddRandomSpawnPhaseChance(0.05f); Guardado.instance.SaveData(); break;

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

            case SkillEffectType.InfectSpeedPhase0_10: Guardado.instance.AddInfectSpeedPerPhase(0, 0.20f); break;
            case SkillEffectType.InfectSpeedPhase0_20: Guardado.instance.AddInfectSpeedPerPhase(0, 0.20f); break;
            case SkillEffectType.InfectSpeedPhase0_30: Guardado.instance.AddInfectSpeedPerPhase(0, 0.20f); break;
            case SkillEffectType.InfectSpeedPhase0_40: Guardado.instance.AddInfectSpeedPerPhase(0, 0.20f); break;
            case SkillEffectType.InfectSpeedPhase0_50: Guardado.instance.AddInfectSpeedPerPhase(0, 0.20f); break;

            case SkillEffectType.InfectSpeedPhase1_10: Guardado.instance.AddInfectSpeedPerPhase(1, 0.30f); break;
            case SkillEffectType.InfectSpeedPhase1_20: Guardado.instance.AddInfectSpeedPerPhase(1, 0.30f); break;
            case SkillEffectType.InfectSpeedPhase1_30: Guardado.instance.AddInfectSpeedPerPhase(1, 0.30f); break;
            case SkillEffectType.InfectSpeedPhase1_40: Guardado.instance.AddInfectSpeedPerPhase(1, 0.30f); break;
            case SkillEffectType.InfectSpeedPhase1_50: Guardado.instance.AddInfectSpeedPerPhase(1, 0.30f); break;

            case SkillEffectType.InfectSpeedPhase2_10: Guardado.instance.AddInfectSpeedPerPhase(2, 0.40f); break;
            case SkillEffectType.InfectSpeedPhase2_20: Guardado.instance.AddInfectSpeedPerPhase(2, 0.40f); break;
            case SkillEffectType.InfectSpeedPhase2_30: Guardado.instance.AddInfectSpeedPerPhase(2, 0.40f); break;
            case SkillEffectType.InfectSpeedPhase2_40: Guardado.instance.AddInfectSpeedPerPhase(2, 0.40f); break;
            case SkillEffectType.InfectSpeedPhase2_50: Guardado.instance.AddInfectSpeedPerPhase(2, 0.40f); break;

            case SkillEffectType.InfectSpeedPhase3_10: Guardado.instance.AddInfectSpeedPerPhase(3, 0.50f); break;
            case SkillEffectType.InfectSpeedPhase3_20: Guardado.instance.AddInfectSpeedPerPhase(3, 0.50f); break;
            case SkillEffectType.InfectSpeedPhase3_30: Guardado.instance.AddInfectSpeedPerPhase(3, 0.50f); break;
            case SkillEffectType.InfectSpeedPhase3_40: Guardado.instance.AddInfectSpeedPerPhase(3, 0.50f); break;
            case SkillEffectType.InfectSpeedPhase3_50: Guardado.instance.AddInfectSpeedPerPhase(3, 0.50f); break;

            case SkillEffectType.InfectSpeedPhase4_10: Guardado.instance.AddInfectSpeedPerPhase(4, 0.60f); break;
            case SkillEffectType.InfectSpeedPhase4_20: Guardado.instance.AddInfectSpeedPerPhase(4, 0.60f); break;
            case SkillEffectType.InfectSpeedPhase4_30: Guardado.instance.AddInfectSpeedPerPhase(4, 0.60f); break;
            case SkillEffectType.InfectSpeedPhase4_40: Guardado.instance.AddInfectSpeedPerPhase(4, 0.60f); break;
            case SkillEffectType.InfectSpeedPhase4_50: Guardado.instance.AddInfectSpeedPerPhase(4, 0.60f); break;

            case SkillEffectType.UnlockExtraTimeLogic:
                // Llamamos al método que creamos en el paso anterior en Guardado.cs
                Guardado.instance.hasExtraTimeUnlock = true;
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.ActivarCoralInfeccioso:
                Guardado.instance.coralInfeciosoActivo = true;
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.MejorarCapacidadCoral:
                // Aumentamos en 1 la capacidad por cada compra (o usa overrideInt si prefieres un valor fijo)
                Guardado.instance.coralCapacity += GetInt(1);
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.MejorarSpawnHojaNegra:
                // Suma el valor de overrideFloat al spawn actual
                Guardado.instance.hojaSpawnRate += GetFloat(0.25f);
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.MejorarDmgHojaNegra:
                // Suma el valor de overrideInt a las fases/daño
                Guardado.instance.hojaFases += GetInt(1);
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.ActivarAgujeroNegro:
                Guardado.instance.agujeroNegroData = true;
                Guardado.instance.SaveData();
                Debug.Log("<color=purple>Agujero Negro activado permanentemente</color>");
                break;

            case SkillEffectType.MejorarSpawnAgujeroNegro:
                Guardado.instance.agujeroSpawnRate += GetFloat(0.25f);
                Guardado.instance.SaveData();
                break;

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
    private string GetTexto(string clave)
    {
        var texto = LocalizationSettings.StringDatabase.GetLocalizedString("TextosJuego", clave);
        if (string.IsNullOrEmpty(texto)) return clave; // Si falta la traducción, muestra la clave en pantalla
        return texto;
    }


    string GetPreviewValues()
    {
        bool comprado = (repeatLevel > 0) || unlocked;

        if (Guardado.instance == null) return "";

        Guardado g = Guardado.instance;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        int GetInt(int defaultValue) => useOverride ? overrideInt : defaultValue;
        float GetFloat(float defaultValue) => useOverride ? overrideFloat : defaultValue;

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
                        sb.AppendLine($"{GetTexto("prev_tiempo")}: {actual:F1}s");
                    else
                        sb.AppendLine($"{GetTexto("prev_tiempo")}: {actual:F1}s → {despues:F1}s");
                    break;
                }

            // -------------------------
            // DAÑO
            // -------------------------
            case SkillEffectType.DmgHexagono:
                {
                    int actual = 1 + g.dañoExtraHexagono;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_dano")} {GetTexto("fase_hex")}: {actual}");
                    else sb.AppendLine($"{GetTexto("prev_dano")} {GetTexto("fase_hex")}: {actual} → {actual + 1}");
                    break;
                }
            case SkillEffectType.DmgPentagono:
                {
                    int actual = 1 + g.dañoExtraPentagono;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_dano")} {GetTexto("fase_pent")}: {actual}");
                    else sb.AppendLine($"{GetTexto("prev_dano")} {GetTexto("fase_pent")}: {actual} → {actual + 1}");
                    break;
                }
            case SkillEffectType.DmgCuadrado:
                {
                    int actual = 2 + g.dañoExtraCuadrado;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_dano")} {GetTexto("fase_cuad")}: {actual}");
                    else sb.AppendLine($"{GetTexto("prev_dano")} {GetTexto("fase_cuad")}: {actual} → {actual + 1}");
                    break;
                }
            case SkillEffectType.DmgTriangulo:
                {
                    int actual = 3 + g.dañoExtraTriangulo;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_dano")} {GetTexto("fase_tri")}: {actual}");
                    else sb.AppendLine($"{GetTexto("prev_dano")} {GetTexto("fase_tri")}: {actual} → {actual + 1}");
                    break;
                }
            case SkillEffectType.DmgCirculo:
                {
                    int actual = 4 + g.dañoExtraCirculo;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_dano")} {GetTexto("fase_circ")}: {actual}");
                    else sb.AppendLine($"{GetTexto("prev_dano")} {GetTexto("fase_circ")}: {actual} → {actual + 1}");
                    break;
                }

            // -------------------------
            // MONEDAS
            // -------------------------
            case SkillEffectType.CoinsHexagonoPlus1:
                {
                    int actual = 1 + g.coinsExtraHexagono;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_monedas")} {GetTexto("fase_hex")}: {actual}");
                    else sb.AppendLine($"{GetTexto("prev_monedas")} {GetTexto("fase_hex")}: {actual} → {actual + 1}");
                    break;
                }
            case SkillEffectType.CoinsPentagonoPlus1:
                {
                    int actual = 2 + g.coinsExtraPentagono;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_monedas")} {GetTexto("fase_pent")}: {actual}");
                    else sb.AppendLine($"{GetTexto("prev_monedas")} {GetTexto("fase_pent")}: {actual} → {actual + 1}");
                    break;
                }
            case SkillEffectType.CoinsCuadradoPlus1:
                {
                    int actual = 3 + g.coinsExtraCuadrado;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_monedas")} {GetTexto("fase_cuad")}: {actual}");
                    else sb.AppendLine($"{GetTexto("prev_monedas")} {GetTexto("fase_cuad")}: {actual} → {actual + 1}");
                    break;
                }
            case SkillEffectType.CoinsTrianguloPlus1:
                {
                    int actual = 4 + g.coinsExtraTriangulo;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_monedas")} {GetTexto("fase_tri")}: {actual}");
                    else sb.AppendLine($"{GetTexto("prev_monedas")} {GetTexto("fase_tri")}: {actual} → {actual + 1}");
                    break;
                }
            case SkillEffectType.CoinsCirculoPlus1:
                {
                    int actual = 5 + g.coinsExtraCirculo;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_monedas")} {GetTexto("fase_circ")}: {actual}");
                    else sb.AppendLine($"{GetTexto("prev_monedas")} {GetTexto("fase_circ")}: {actual} → {actual + 1}");
                    break;
                }

            // -------------------------
            // RADIO
            // -------------------------
            case SkillEffectType.MultiplyRadius125:
            case SkillEffectType.MultiplyRadius150:
            case SkillEffectType.MultiplyRadius200:
            case SkillEffectType.RadiusLevel2:
            case SkillEffectType.RadiusLevel3:
            case SkillEffectType.RadiusLevel4:
            case SkillEffectType.RadiusLevel5:
            case SkillEffectType.RadiusLevel6:
                {
                    float actual = g.radiusMultiplier;
                    float bonus = (effectType == SkillEffectType.MultiplyRadius200) ? 1f :
                                  (effectType == SkillEffectType.MultiplyRadius150) ? 0.5f : 0.25f;
                    float despues = actual + bonus;

                    if (comprado) sb.AppendLine($"{GetTexto("prev_radio")}: {actual:F2}");
                    else sb.AppendLine($"{GetTexto("prev_radio")}: {actual:F2} → {despues:F2}");
                    break;
                }

            // -------------------------
            // VELOCIDAD VIRUS
            // -------------------------
            case SkillEffectType.MultiplySpeed125:
            case SkillEffectType.MultiplySpeed150:
                {
                    float actual = g.speedMultiplier;
                    float bonus = (effectType == SkillEffectType.MultiplySpeed150) ? 0.5f : 0.25f;
                    float despues = actual + bonus;

                    if (comprado) sb.AppendLine($"{GetTexto("prev_vel_virus")}: {actual:F2}");
                    else sb.AppendLine($"{GetTexto("prev_vel_virus")}: {actual:F2} → {despues:F2}");
                    break;
                }
            case SkillEffectType.SpeedLevel2:
            case SkillEffectType.SpeedLevel3:
            case SkillEffectType.SpeedLevel4:
            case SkillEffectType.SpeedLevel5:
                {
                    int actual = (int)SpeedUpgradeController.instance.GetCurrentSpeed();
                    int despues = actual + 20;

                    if (comprado) sb.AppendLine($"{GetTexto("prev_vel_virus")}: {actual}");
                    else sb.AppendLine($"{GetTexto("prev_vel_virus")}: {actual} → {despues}");
                    break;
                }

            // -------------------------
            // VELOCIDAD INFECCIÓN
            // -------------------------
            case SkillEffectType.InfectSpeed50:
            case SkillEffectType.InfectSpeed100:
            case SkillEffectType.InfectionSpeedLevel2:
            case SkillEffectType.InfectionSpeedLevel3:
            case SkillEffectType.InfectionSpeedLevel4:
            case SkillEffectType.InfectionSpeedLevel5:
            case SkillEffectType.InfectionSpeedLevel6:
                {
                    float actual = g.infectSpeedMultiplier;
                    float bonus = (effectType == SkillEffectType.InfectSpeed100) ? 1f :
                                  (effectType == SkillEffectType.InfectSpeed50) ? 0.5f : 0.25f;
                    float despues = actual + bonus;

                    if (comprado) sb.AppendLine($"{GetTexto("prev_vel_infec")}: {actual:F2}");
                    else sb.AppendLine($"{GetTexto("prev_vel_infec")}: {actual:F2} → {despues:F2}");
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

                    if (comprado) sb.AppendLine($"{GetTexto("prev_tiempo_cap")} {GetTexto("fase_hex")}: {tiempoActual:F2}s");
                    else sb.AppendLine($"{GetTexto("prev_tiempo_cap")} {GetTexto("fase_hex")}: {tiempoActual:F2}s → {tiempoNuevo:F2}s");
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

                    if (comprado) sb.AppendLine($"{GetTexto("prev_tiempo_cap")} {GetTexto("fase_pent")}: {tiempoActual:F2}s");
                    else sb.AppendLine($"{GetTexto("prev_tiempo_cap")} {GetTexto("fase_pent")}: {tiempoActual:F2}s → {tiempoNuevo:F2}s");
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

                    if (comprado) sb.AppendLine($"{GetTexto("prev_tiempo_cap")} {GetTexto("fase_cuad")}: {tiempoActual:F2}s");
                    else sb.AppendLine($"{GetTexto("prev_tiempo_cap")} {GetTexto("fase_cuad")}: {tiempoActual:F2}s → {tiempoNuevo:F2}s");
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

                    if (comprado) sb.AppendLine($"{GetTexto("prev_tiempo_cap")} {GetTexto("fase_tri")}: {tiempoActual:F2}s");
                    else sb.AppendLine($"{GetTexto("prev_tiempo_cap")} {GetTexto("fase_tri")}: {tiempoActual:F2}s → {tiempoNuevo:F2}s");
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

                    if (comprado) sb.AppendLine($"{GetTexto("prev_tiempo_cap")} {GetTexto("fase_circ")}: {tiempoActual:F2}s");
                    else sb.AppendLine($"{GetTexto("prev_tiempo_cap")} {GetTexto("fase_circ")}: {tiempoActual:F2}s → {tiempoNuevo:F2}s");
                    break;
                }

            // -------------------------
            // PROBABILIDADES
            // -------------------------
            case SkillEffectType.ParedInfectiva_Hexagono:
                {
                    int idx = 0; // El índice del hexágono es 0
                    float nivelActual = g.probParedInfectiva[idx];
                    float probActual = nivelActual * 25f;
                    float probSiguiente = (nivelActual + 1) * 25f;

                    if (comprado)
                        sb.AppendLine($"Prob. Infección Hexágono: {probActual}%");
                    else
                        sb.AppendLine($"Prob. Infección Hexágono: {probActual}% → {probSiguiente}%");
                    break;
                }
            case SkillEffectType.ParedInfectiva_Pentagono:
            case SkillEffectType.ParedInfectiva_Cuadrado:
            case SkillEffectType.ParedInfectiva_Triangulo:
            case SkillEffectType.ParedInfectiva_Circulo:
                {
                    // Determinamos el índice según el tipo de efecto
                    int idx = (effectType == SkillEffectType.ParedInfectiva_Pentagono) ? 1 :
                              (effectType == SkillEffectType.ParedInfectiva_Cuadrado) ? 2 :
                              (effectType == SkillEffectType.ParedInfectiva_Triangulo) ? 3 : 4;

                    float nivelActual = g.probParedInfectiva[idx];
                    float probActual = nivelActual * 25f; // Mostramos en formato 0-100%
                    float probSiguiente = (nivelActual + 1) * 25f;

                    if (comprado) // Si no es repetible y ya se compró
                        sb.AppendLine($"Prob. Infección: {probActual}%");
                    else
                        sb.AppendLine($"Prob. Infección: {probActual}% → {probSiguiente}%");

                    break;
                }
            case SkillEffectType.AddTimeOnPhaseChance5:
            case SkillEffectType.AddTimeOnPhaseChance10:
            case SkillEffectType.AddTimeOnPhaseChance15:
            case SkillEffectType.AddTimeOnPhaseChance20:
            case SkillEffectType.AddTimeOnPhaseChance25:
                {
                    float actual = g.addTimeOnPhaseChance * 100f;
                    float despues = (g.addTimeOnPhaseChance + 0.05f) * 100f;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_bonus_fase")}: {actual:F0}%");
                    else sb.AppendLine($"{GetTexto("prev_bonus_fase")}: {actual:F0}% → {despues:F0}%");
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
                    if (comprado) sb.AppendLine($"{GetTexto("prev_upg_doble")}: {actual:F0}%");
                    else sb.AppendLine($"{GetTexto("prev_upg_doble")}: {actual:F0}% → {despues:F0}%");
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
                    if (comprado) sb.AppendLine($"{GetTexto("prev_spawn_rnd")}: {actual:F0}%");
                    else sb.AppendLine($"{GetTexto("prev_spawn_rnd")}: {actual:F0}% → {despues:F0}%");
                    break;
                }
            case SkillEffectType.DuplicateOnHit20:
            case SkillEffectType.DuplicateOnHit40:
            case SkillEffectType.DuplicateOnHit60:
            case SkillEffectType.DuplicateOnHit80:
            case SkillEffectType.DuplicateOnHit100:
                {
                    float actual = g.probabilidadDuplicarChoque * 100f;
                    float despues = (g.probabilidadDuplicarChoque + 0.20f) * 100f;
                    if (comprado) sb.AppendLine($"{GetTexto("prev_duplicar")}: {actual:F0}%");
                    else sb.AppendLine($"{GetTexto("prev_duplicar")}: {actual:F0}% → {despues:F0}%");
                    break;
                }

            // -------------------------
            // POBLACIÓN
            // -------------------------
            case SkillEffectType.IncreasePopulation25:
            case SkillEffectType.IncreasePopulation50:
                {
                    float actualTotal = PopulationManager.instance.GetRoundInitialPopulation();
                    float despuesTotal = actualTotal + 1; // Ajusta según tu lógica

                    if (comprado) sb.AppendLine($"{GetTexto("prev_pob_max")}: {actualTotal:F0}");
                    else sb.AppendLine($"{GetTexto("prev_pob_max")}: {actualTotal:F0} → {despuesTotal:F0}");
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

                    string GetFigura(int nivel) => nivel switch
                    {
                        2 => GetTexto("fase_pent"),
                        3 => GetTexto("fase_cuad"),
                        4 => GetTexto("fase_tri"),
                        5 => GetTexto("fase_circ"),
                        _ => GetTexto("prev_figuras")
                    };

                    string figuraActual = GetFigura(actual);
                    string figuraSiguiente = GetFigura(despues);

                    if (comprado)
                    {
                        // Se usa string.Format para inyectar la figura en la frase traducida
                        sb.AppendLine(string.Format(GetTexto("prev_pared_1"), figuraActual));
                    }
                    else
                    {
                        sb.AppendLine(string.Format(GetTexto("prev_pared_2"), figuraSiguiente));
                    }
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
                    float baseInterval = PopulationManager.instance.GetCurrentSpawnInterval(); // intervalo base del juego
                    float actualBonus = 0.5f;
                 

                    float actual = baseInterval;
                    float despues = baseInterval - actualBonus;

                    if (comprado) sb.AppendLine($"{GetTexto("prev_spawn_int")}: {actual:F2}s");
                    else sb.AppendLine($"{GetTexto("prev_spawn_int")}: {actual:F2}s → {despues:F2}s");
                    break;
                }


            case SkillEffectType.UnlockExtraTimeLogic:
                if (unlocked)
                    sb.AppendLine($"{GetTexto("status_unlocked")}");
                else
                    sb.AppendLine($"{GetTexto("extra_time_desc")}"); // Crea esta clave en tu Localization
                break;

            case SkillEffectType.MejorarCapacidadCoral:
                {
                    int actual = g.coralCapacity;
                    if (comprado)
                        sb.AppendLine($"{GetTexto("prev_capacidad")}: {actual}");
                    else
                        sb.AppendLine($"{GetTexto("prev_capacidad")}: {actual} → {actual + 1}");
                    break;
                }
               
            case SkillEffectType.ActivarCoralInfeccioso:
                if (unlocked)
                    sb.AppendLine($"{GetTexto("status_unlocked")}");
                else
                    sb.AppendLine($"{GetTexto("coral_infec_desc")}"); // Crea esta clave en tu Localization
                break;

            case SkillEffectType.ActivarHojaNegra:
                if (comprado)
                    sb.AppendLine($"{GetTexto("hojanegra_estado")}: {GetTexto("activado")}");
                else
                    sb.AppendLine($"{GetTexto("hojanegra_estado")}: {GetTexto("desactivado")}");
                break;

            case SkillEffectType.MejorarSpawnHojaNegra:
                {
                    float actual = g.hojaSpawnRate;
                    float extra = GetFloat(0.1f);
                    if (comprado) sb.AppendLine($"Spawn Hoja: {actual:F2}");
                    else sb.AppendLine($"Spawn Hoja: {actual:F2} → {(actual + extra):F2}");
                    break;
                }
            case SkillEffectType.MejorarDmgHojaNegra:
                {
                    int actual = g.hojaFases;
                    int extra = GetInt(1);
                    if (comprado) sb.AppendLine($"Fases Hoja: {actual}");
                    else sb.AppendLine($"Fases Hoja: {actual} → {actual + extra}");
                    break;
                }
            case SkillEffectType.MejorarSpawnAgujeroNegro:
                {
                    float actual = g.agujeroSpawnRate;
                    float extra = GetFloat(0.1f);
                    if (comprado) sb.AppendLine($"Spawn Agujero: {actual:F2}");
                    else sb.AppendLine($"Spawn Agujero: {actual:F2} → {(actual + extra):F2}");
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