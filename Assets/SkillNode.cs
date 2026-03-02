using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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
        DmgTriangulo, DmgCuadrado, DmgPentagono, DmgHexagono, ReboteConCoral,
        ParedInfectiva_Nivel1, ParedInfectiva_Nivel2, ParedInfectiva_Nivel3, ParedInfectiva_Nivel4, ParedInfectiva_Nivel5,
        DestroyCoralOnInfectedImpact, AddTime2Seconds,
        AddTimeOnPhaseChance5, AddTimeOnPhaseChance10, AddTimeOnPhaseChance15, AddTimeOnPhaseChance20, AddTimeOnPhaseChance25,
        DoubleUpgradeChance05, DoubleUpgradeChance10, DoubleUpgradeChance15, DoubleUpgradeChance20, DoubleUpgradeChance25,
        RandomSpawnAnyPhase5, RandomSpawnAnyPhase10, RandomSpawnAnyPhase15, RandomSpawnAnyPhase20, RandomSpawnAnyPhase25,
        CoinsHexagonoPlus1, CoinsPentagonoPlus1, CoinsCuadradoPlus1, CoinsTrianguloPlus1, CoinsCirculoPlus1
    }

    [Header("Save ID")]
    public string saveID;

    [Header("Datos de Traducción (KEYS)")]
    public string skillNameKey;
    [TextArea] public string descriptionKey;

    [Header("Coste")]
    public int CoinCost = 1;

    [Header("Dependencias (Matriz)")]
    public SkillNode[] requiredParentNodes;
    [Tooltip("Si es false, se desbloquea si AL MENOS UNO de los padres está comprado (OR). Si es true, requiere TODOS (AND).")]
    public bool requiresAllParents = false;

    [Header("Efecto")]
    public SkillEffectType effectType = SkillEffectType.None;

    [Header("UI References")]
    public Button button;
    public GameObject lockIcon;
    public Image nodeImage;
    public CanvasGroup canvasGroup;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip unlockSound;

    [Header("Configuración Inicial")]
    public bool isStartingNode = false;

    private bool unlocked = false;
    public int repeatLevel = 0;
    public int maxRepeatLevel = 5;
    public int maxTimeRepeatLevel = 10;

    public bool IsUnlocked => unlocked;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        LoadNodeState();
        CheckIfShouldShow();

        // Si ya está desbloqueado, avisamos a las líneas para que se dibujen
        if (unlocked)
        {
            UpdateLinesVisuals();
        }

        // Diagnóstico de errores en el Inspector
        if (requiredParentNodes != null)
        {
            foreach (var p in requiredParentNodes)
            {
                if (p == null)
                    Debug.LogError($"<color=red>¡ERROR!</color> El nodo <b>{gameObject.name}</b> tiene un padre nulo.");
                else if (!p.gameObject.scene.IsValid())
                    Debug.LogWarning($"<color=orange>AVISO:</color> <b>{gameObject.name}</b> referencia a un PREFAB como padre.");
            }
        }
    }

    public void LoadNodeState()
    {
        if (string.IsNullOrEmpty(saveID)) return;

        int estadoGuardado = PlayerPrefs.GetInt("Skill_" + saveID + "_Unlocked", -1);

        if (estadoGuardado == 1) unlocked = true;
        else if (estadoGuardado == 0) unlocked = false;
        else unlocked = isStartingNode;

        repeatLevel = PlayerPrefs.GetInt("Skill_" + saveID + "_Repeat", 0);
    }

    public void CheckIfShouldShow()
    {
        // 1. Gestión de repetibles y máximos
        bool isMaxed = false;
        if ((IsDamageSkill() || IsCoinSkill()) && repeatLevel >= maxRepeatLevel) isMaxed = true;
        if (IsTimeSkill() && repeatLevel >= maxTimeRepeatLevel) isMaxed = true;

        // 2. Lógica de Padres (Matriz)
        bool allParentsUnlocked = true;
        bool atLeastOneParentUnlocked = false;

        if (requiredParentNodes != null && requiredParentNodes.Length > 0)
        {
            foreach (var parent in requiredParentNodes)
            {
                if (parent == null) continue;
                if (parent.IsUnlocked) atLeastOneParentUnlocked = true;
                else allParentsUnlocked = false;
            }
        }
        else
        {
            // Si no tiene padres, se comporta como nodo raíz
            atLeastOneParentUnlocked = true;
            allParentsUnlocked = true;
        }

        // Determinar si el nodo es COMPRABLE y si es VISIBLE
        bool canBePurchased = requiresAllParents ? allParentsUnlocked : atLeastOneParentUnlocked;
        bool isVisible = atLeastOneParentUnlocked || unlocked || isStartingNode;

        // 3. Aplicar estado visual y de interacción
        if (unlocked && !IsDamageSkill() && !IsCoinSkill() && !IsTimeSkill())
        {
            // Ya comprado (no repetible)
            SetAppearance(true, 1f, false);
            SetState(false, Color.white, false);
        }
        else if (isMaxed)
        {
            // Alcanzado nivel máximo
            SetAppearance(true, 1f, false);
            SetState(false, Color.gray, false);
        }
        else if (canBePurchased)
        {
            // Disponible para comprar
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
        }
        else if (isVisible)
        {
            // Bloqueado pero visible (se ve el camino pero falta un requisito)
            SetAppearance(true, 0.4f, false);
            SetState(false, Color.black, true);
        }
        else
        {
            // Oculto (niebla de guerra)
            SetAppearance(false, 0f, false);
        }

        UpdateLinesVisuals();
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
        // Validaciones de compra
        if (!IsDamageSkill() && !IsCoinSkill() && !IsTimeSkill() && unlocked) return;
        if ((IsDamageSkill() || IsCoinSkill()) && repeatLevel >= maxRepeatLevel) return;
        if (IsTimeSkill() && repeatLevel >= maxTimeRepeatLevel) return;

        if (LevelManager.instance.ContagionCoins < CoinCost)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayError();
            return;
        }

        // Proceso de compra
        LevelManager.instance.ContagionCoins -= CoinCost;
        if (AudioManager.instance != null) AudioManager.instance.PlayBuyUpgrade();
        if (audioSource != null && unlockSound != null) audioSource.PlayOneShot(unlockSound);

        if (IsDamageSkill() || IsTimeSkill() || IsCoinSkill()) repeatLevel++;
        else unlocked = true;

        ApplyEffect();
        SaveNodeState();
        LevelManager.instance.UpdateUI();

        // Feedback visual
        SkillNodeHoverFX fx = GetComponent<SkillNodeHoverFX>();
        if (fx != null)
        {
            fx.PlayClickFeedback();
            fx.SetPurchasedState(unlocked);
        }

        // REFRESCAR TODA LA MATRIZ (Grafo)
        RefreshAllNodes();
    }

    void RefreshAllNodes()
    {
        SkillNode[] allNodes = Object.FindObjectsByType<SkillNode>(FindObjectsSortMode.None);
        foreach (var node in allNodes)
        {
            node.CheckIfShouldShow();
        }
    }

    void UpdateLinesVisuals()
    {
        SkillTreeLinesUI lines = Object.FindAnyObjectByType<SkillTreeLinesUI>();
        if (lines != null && (unlocked || isStartingNode))
        {
            lines.ShowFrom(GetComponent<RectTransform>());
        }
    }

    void SaveNodeState()
    {
        if (string.IsNullOrEmpty(saveID)) return;
        PlayerPrefs.SetInt("Skill_" + saveID + "_Unlocked", unlocked ? 1 : 0);
        PlayerPrefs.SetInt("Skill_" + saveID + "_Repeat", repeatLevel);
        PlayerPrefs.Save();
    }

    // --- MÉTODOS DE IDENTIFICACIÓN DE TIPO ---
    bool IsDamageSkill() => effectType == SkillEffectType.DmgHexagono || effectType == SkillEffectType.DmgPentagono || effectType == SkillEffectType.DmgCuadrado || effectType == SkillEffectType.DmgTriangulo || effectType == SkillEffectType.DmgCirculo;
    bool IsTimeSkill() => effectType == SkillEffectType.AddTime2Seconds;
    bool IsCoinSkill() => effectType == SkillEffectType.CoinsHexagonoPlus1 || effectType == SkillEffectType.CoinsPentagonoPlus1 || effectType == SkillEffectType.CoinsCuadradoPlus1 || effectType == SkillEffectType.CoinsTrianguloPlus1 || effectType == SkillEffectType.CoinsCirculoPlus1;

    // --- INTERFAZ DE TOOLTIP ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SkillTooltip.instance != null)
            SkillTooltip.instance.Show(skillNameKey, descriptionKey, CoinCost, GetComponent<RectTransform>());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SkillTooltip.instance != null) SkillTooltip.instance.Hide();
    }

    // --- LÓGICA DE EFECTOS ---
    void ApplyEffect()
    {
        if (Guardado.instance == null) return;

        // Aquí iría tu switch(effectType) original... 
        // Se mantiene igual que en tu versión, llamando a Guardado.instance
        Debug.Log($"<color=green>[SkillTree]</color> Aplicando: {effectType}");
        // (He omitido el switch largo por brevedad, pero en tu script mantén el que ya tenías)
    }
}