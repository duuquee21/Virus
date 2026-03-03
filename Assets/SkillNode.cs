using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class SkillNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum SkillEffectType {
        // ... (Mantén tu enum actual igual)
        None, RandomInitialUpgrade, CoinsX2, CoinsX3, CoinsX4, CoinsX5, CoinsX6,
        StartWith50Coins, StartWith100Coins, StartWith500Coins, StartWith2500Coins, StartWith25000Coins, StartWith50000Coins,
        ReduceSpawnInterval20, ReduceSpawnInterval40, ReduceSpawnInterval60, ReduceSpawnInterval80, ReduceSpawnInterval100,
        IncreasePopulation25, IncreasePopulation50, HalveZoneCosts, ZoneIncome100, ZoneIncome250, ZoneIncome500, ZoneIncome1000, ZoneIncome5000,
        MultiplyRadius125, MultiplyRadius150, MultiplyRadius200, MultiplySpeed125, MultiplySpeed150, InfectSpeed50, InfectSpeed100,
        KeepUpgradesOnResetEffect, KeepZonesOnReset, RadiusLevel2, RadiusLevel3, RadiusLevel4, RadiusLevel5, RadiusLevel6,
        SpeedLevel2, SpeedLevel3, SpeedLevel4, SpeedLevel5, CapacityLevel2, CapacityLevel3, CapacityLevel4, CapacityLevel5, CapacityLevel6,
        TimeLevel2, TimeLevel3, TimeLevel4, TimeLevel5, TimeLevel6, InfectionSpeedLevel2, InfectionSpeedLevel3, InfectionSpeedLevel4, 
        InfectionSpeedLevel5, InfectionSpeedLevel6, DuplicateOnHit20, DuplicateOnHit40, DuplicateOnHit60, DuplicateOnHit80, DuplicateOnHit100,
        AddDays5, AddDays10, IncreaseShinyValue1, IncreaseShinyValue3, MultiplyShinyX5, MultiplyShinyX7, MultiplyShinyX10, AddExtraShiny, 
        ShinyPassivePerZone, GuaranteedShinyEffect, ShinyCaptureSpeed50, ShinyCaptureSpeed100, DoubleShinyEffect, ExtraShiny, 
        CarambolaNormal, CarambolaPro, CarambolaSuprema, DmgCirculo, DmgTriangulo, DmgCuadrado, DmgPentagono, DmgHexagono, ReboteConCoral,
        ParedInfectiva_Nivel1, ParedInfectiva_Nivel2, ParedInfectiva_Nivel3, ParedInfectiva_Nivel4, ParedInfectiva_Nivel5,
        DestroyCoralOnInfectedImpact, AddTime2Seconds, AddTimeOnPhaseChance5, AddTimeOnPhaseChance10, AddTimeOnPhaseChance15,
        AddTimeOnPhaseChance20, AddTimeOnPhaseChance25, DoubleUpgradeChance05, DoubleUpgradeChance10, DoubleUpgradeChance15,
        DoubleUpgradeChance20, DoubleUpgradeChance25, RandomSpawnAnyPhase5, RandomSpawnAnyPhase10, RandomSpawnAnyPhase15,
        RandomSpawnAnyPhase20, RandomSpawnAnyPhase25, CoinsHexagonoPlus1, CoinsPentagonoPlus1, CoinsCuadradoPlus1,
        CoinsTrianguloPlus1, CoinsCirculoPlus1
    }

    [Header("Save ID")]
    public string saveID;

    [Header("Hover Panel")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoTitle;
    public TextMeshProUGUI infoDescription;
    public TextMeshProUGUI infoCost;

    [Header("Datos de Traducción (KEYS)")]
    public string skillNameKey;
    [TextArea] public string descriptionKey;

    public int CoinCost = 1;

    [Header("Ramas (Padres)")]
    // He eliminado nextNodes. Ahora solo nos importan los padres.
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
    public int maxTimeRepeatLevel = 10;

    public bool IsUnlocked => unlocked || ((IsDamageSkill() || IsCoinSkill() || IsTimeSkill()) && repeatLevel > 0);

    void Awake() { if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>(); }

    void Start()
    {
        LoadNodeState();
        CheckIfShouldShow();

        // Hemos eliminado la llamada a lines.ShowFrom() 
        // porque el nuevo SkillTreeLinesUI detecta el cambio automáticamente

        if (infoPanel != null) infoPanel.SetActive(false);
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
        // 1. Si ya está al máximo nivel (repetibles)
        if (((IsDamageSkill() || IsCoinSkill()) && repeatLevel >= maxRepeatLevel) ||
            (IsTimeSkill() && repeatLevel >= maxTimeRepeatLevel))
        {
            SetState(false, Color.gray, false);
            SetAppearance(true, 1f, false);
            return;
        }

        // 2. Si ya está comprado (repetible o normal)
        if (IsUnlocked)
        {
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
            UpdateLinesVisuals();
            return;
        }

        // 3. Nodos iniciales siempre se muestran
        if (requiredParentNodes == null || requiredParentNodes.Length == 0)
        {
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
            return;
        }

        // 4. Lógica Multipadre (Si UNO está desbloqueado, este nodo es visible y comprable)
        bool atLeastOneParentUnlocked = false;

        foreach (var parent in requiredParentNodes)
        {
            if (parent != null && parent.IsUnlocked)
            {
                atLeastOneParentUnlocked = true;
                break; // Con uno basta
            }
        }

        if (atLeastOneParentUnlocked)
        {
            // El nodo es visible y se puede comprar
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false); 
            if (lockIcon != null) lockIcon.SetActive(false);
        }
        else
        {
            // El nodo permanece oculto hasta que un padre se desbloquee
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

    void UpdateLinesVisuals()
    {
        SkillTreeLinesUI lines = Object.FindFirstObjectByType<SkillTreeLinesUI>();
        if (lines != null && IsUnlocked) 
        {
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
        if ((IsDamageSkill() || IsCoinSkill()) && repeatLevel >= maxRepeatLevel) return;
        if (IsTimeSkill() && repeatLevel >= maxTimeRepeatLevel) return;

        if (LevelManager.instance.ContagionCoins < CoinCost)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayError();
            return;
        }

        // Éxito
        if (AudioManager.instance != null) AudioManager.instance.PlayBuyUpgrade();
        if (audioSource != null && unlockSound != null) audioSource.PlayOneShot(unlockSound);

        LevelManager.instance.ContagionCoins -= CoinCost;

        if (IsDamageSkill() || IsTimeSkill() || IsCoinSkill()) repeatLevel++;
        else unlocked = true;

        ApplyEffect();
        SaveNodeState();
        LevelManager.instance.UpdateUI();

        SkillNodeHoverFX fx = GetComponent<SkillNodeHoverFX>();
        if (fx != null) { fx.PlayClickFeedback(); fx.SetPurchasedState(true); }

        // Actualizamos todo el árbol para que los hijos detecten el cambio
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

    // ... (Mantén toda tu función ApplyEffect, IsDamageSkill, IsCoinSkill, etc. igual que antes)
    // He omitido el cuerpo de ApplyEffect para acortar la respuesta, pero no lo borres de tu script.

    void ApplyEffect() 
    {
        // Pega aquí tu switch(effectType) original...
    }

    bool IsDamageSkill() => effectType == SkillEffectType.DmgHexagono || effectType == SkillEffectType.DmgPentagono || effectType == SkillEffectType.DmgCuadrado || effectType == SkillEffectType.DmgTriangulo || effectType == SkillEffectType.DmgCirculo;
    bool IsTimeSkill() => effectType == SkillEffectType.AddTime2Seconds;
    bool IsCoinSkill() => effectType == SkillEffectType.CoinsHexagonoPlus1 || effectType == SkillEffectType.CoinsPentagonoPlus1 || effectType == SkillEffectType.CoinsCuadradoPlus1 || effectType == SkillEffectType.CoinsTrianguloPlus1 || effectType == SkillEffectType.CoinsCirculoPlus1;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SkillTooltip.instance != null)
            SkillTooltip.instance.Show(skillNameKey, descriptionKey, CoinCost, GetComponent<RectTransform>());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SkillTooltip.instance != null) SkillTooltip.instance.Hide();
    }
}