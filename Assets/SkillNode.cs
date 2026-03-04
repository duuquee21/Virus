using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;


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
        // Habilidades de Duplicación por Choque
        DuplicateOnHit20, DuplicateOnHit40, DuplicateOnHit60, DuplicateOnHit80, DuplicateOnHit100,
        // Referencias obsoletas
        AddDays5, AddDays10, IncreaseShinyValue1, IncreaseShinyValue3, MultiplyShinyX5, MultiplyShinyX7,
        MultiplyShinyX10, AddExtraShiny, ShinyPassivePerZone, GuaranteedShinyEffect, ShinyCaptureSpeed50,
        ShinyCaptureSpeed100, DoubleShinyEffect, ExtraShiny, CarambolaNormal, CarambolaPro, CarambolaSuprema, DmgCirculo,
        DmgTriangulo,
        DmgCuadrado,
        DmgPentagono,
        DmgHexagono, ReboteConCoral, // Sustituye ParedInfectiva por estos niveles
        ParedInfectiva_Nivel1, // Solo Hexágonos
        ParedInfectiva_Nivel2, // Hexágonos + Pentágonos
        ParedInfectiva_Nivel3, // + Cuadrados
        ParedInfectiva_Nivel4, // + Triángulos
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
        InfectSpeedPhase0_10,
        InfectSpeedPhase0_20,
        InfectSpeedPhase0_30,
        InfectSpeedPhase0_40,
        InfectSpeedPhase0_50,

        InfectSpeedPhase1_10,
        InfectSpeedPhase1_20,
        InfectSpeedPhase1_30,
        InfectSpeedPhase1_40,
        InfectSpeedPhase1_50,

        InfectSpeedPhase2_10,
        InfectSpeedPhase2_20,
        InfectSpeedPhase2_30,
        InfectSpeedPhase2_40,
        InfectSpeedPhase2_50,

        InfectSpeedPhase3_10,
        InfectSpeedPhase3_20,
        InfectSpeedPhase3_30,
        InfectSpeedPhase3_40,
        InfectSpeedPhase3_50,

        InfectSpeedPhase4_10,
        InfectSpeedPhase4_20,
        InfectSpeedPhase4_30,
        InfectSpeedPhase4_40,
        InfectSpeedPhase4_50
    }
    [Header("Save ID")]
    public string saveID;

    [Header("Hover Panel")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoTitle;
    public TextMeshProUGUI infoDescription;
    public TextMeshProUGUI infoCost;

    [Header("Datos de Traducción (KEYS)")]
    // CAMBIO 1: Ahora son Keys, no el texto final
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

    // Para habilidades repetibles
    public int repeatLevel = 0;
    public int maxRepeatLevel = 5; // puedes ajustarlo
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

        // NUEVO: Si el nodo ya está desbloqueado al empezar, avisamos a las líneas
      
        if (infoPanel != null) infoPanel.SetActive(false);


        // Diagnóstico rápido:
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

        // -1 significa: "No existe dato guardado", es la primera vez que jugamos
        int estadoGuardado = PlayerPrefs.GetInt("Skill_" + saveID + "_Unlocked", -1);

        if (estadoGuardado == 1)
        {
            // Si el guardado dice 1, es que YA lo compramos antes. Se queda comprado.
            unlocked = true;
        }
        else if (estadoGuardado == 0)
        {
            // Si el guardado dice 0, es que jugamos y NO lo compramos. Se queda bloqueado.
            unlocked = false;
        }
        else // estadoGuardado == -1 (Es una partida virgen)
        {
            // Aquí es donde manda el checkbox del Inspector
            if (isStartingNode)
            {
                unlocked = true;
                // Opcional: Guardamos ya que es nuestro para siempre
                // SaveNodeState(); 
            }
            else
            {
                unlocked = false;
            }
        }

        repeatLevel = PlayerPrefs.GetInt("Skill_" + saveID + "_Repeat", 0);
    }



    // SUSTITUYE TU FUNCIÓN CheckIfShouldShow POR ESTA:
    public void CheckIfShouldShow()
    {
        // 1️⃣ Si es repetible y está al máximo → visible pero gris
        if (((IsDamageSkill() || IsCoinSkill()) && repeatLevel >= maxRepeatLevel) ||
            (IsTimeSkill() && repeatLevel >= maxTimeRepeatLevel))
        {
            SetAppearance(true, 1f, false);
            SetState(false, Color.gray, false);
            return;
        }

        // 2️⃣ Si ya está desbloqueado → siempre visible
        if (IsUnlocked)
        {
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
            return;
        }

        // 3️⃣ Si es nodo inicial → siempre visible
        if (isStartingNode)
        {
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
            return;
        }

        // 4️⃣ Si no tiene padres → visible
        if (requiredParentNodes == null || requiredParentNodes.Length == 0)
        {
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
            return;
        }

        // ----------------------------------------------------
        // NUEVA LÓGICA: HIJO / NIETO
        // ----------------------------------------------------

        bool isDirectChild = false;
        bool isGrandChild = false;

        foreach (var parent in requiredParentNodes)
        {
            if (parent == null) continue;

            // 🔓 Padre desbloqueado → soy HIJO
            if (parent.IsUnlocked)
            {
                isDirectChild = true;
                break;
            }

            // 👀 Padre bloqueado pero abuelo desbloqueado → soy NIETO
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
            // HIJO → Visible y comprable
            SetAppearance(true, 1f, true);
            SetState(true, Color.white, false);
        }
        else if (isGrandChild)
        {
            // NIETO → Visible pero bloqueado
            SetAppearance(true, 0.5f, false);
            SetState(false, Color.gray, true);
        }
        else
        {
            // Muy profundo → oculto
            SetAppearance(false, 0f, false);
        }
    }
    void SetAppearance(bool isActive, float alpha, bool canClick)
    {
        // En lugar de desactivar el GameObject, lo mantenemos activo pero invisible
        // Esto permite que las líneas de SkillTreeLinesUI encuentren su posición
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = canClick;
            canvasGroup.interactable = canClick;
        }

        // Solo usamos SetActive(false) si realmente queremos que el nodo NO exista 
        // (por ejemplo, si ni siquiera debería estar en el árbol aún)
        // Pero para nodos "bloqueados" que deben recibir una línea, isActive debe ser true.
    }

   

    void SetState(bool isInteractable, Color color, bool showLock)
    {
        if (button != null) button.interactable = isInteractable;
        if (nodeImage != null) nodeImage.color = color;
        if (lockIcon != null) lockIcon.SetActive(showLock);
    }

    // ---------------------------------------------------------
    //  TRY UNLOCK (COMPLETO Y CORREGIDO)
    // ---------------------------------------------------------
    public void TryUnlock()
    {
        // 1. COMPROBACIONES DE SEGURIDAD
        // Si ya está desbloqueada y no es repetible, no hacemos nada
        if (!IsDamageSkill() && !IsCoinSkill() && !IsTimeSkill() && unlocked) return;

        // Si es repetible de Daño/Monedas y llegó al máximo, fuera
        if ((IsDamageSkill() || IsCoinSkill()) && repeatLevel >= maxRepeatLevel)
            return;

        // Si es repetible de Tiempo y llegó al máximo, fuera
        if (IsTimeSkill() && repeatLevel >= maxTimeRepeatLevel)
            return;

        // 2. COMPROBACIÓN DE DINERO
        if (LevelManager.instance.ContagionCoins < CoinCost)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlayError();
            return;
        }

        // 3. ÉXITO: SONIDOS Y PAGO
        if (AudioManager.instance != null)
            AudioManager.instance.PlayBuyUpgrade();

        if (audioSource != null && unlockSound != null)
            audioSource.PlayOneShot(unlockSound);

        LevelManager.instance.ContagionCoins -= CoinCost;

        // 4. ACTUALIZAR ESTADO (COMPRADO)
        if (IsDamageSkill() || IsTimeSkill() || IsCoinSkill())
            repeatLevel++;
        else
            unlocked = true;



        // 5. GUARDAR Y APLICAR EFECTO
        ApplyEffect();
        SaveNodeState(); // Guardamos inmediatamente para no perder datos

        // 6. ACTUALIZAR UI GLOBAL (Monedas)
        LevelManager.instance.UpdateUI();

        // 7. EFECTOS VISUALES DEL BOTÓN
        SkillNodeHoverFX fx = GetComponent<SkillNodeHoverFX>();
        if (fx != null)
        {
            fx.PlayClickFeedback();
            fx.SetPurchasedState(true);
        }

        // ---------------------------------------------------------
        // 8. EL PASO CLAVE: REFRESCAR TODO EL ÁRBOL
        // Esto hace que los hijos se den cuenta de que el padre ya está comprado
        RefreshAllNodes();
        // ---------------------------------------------------------
    }

    // ---------------------------------------------------------
    //  FUNCIÓN AUXILIAR PARA DESPERTAR A LOS HIJOS
    // ---------------------------------------------------------
    void RefreshAllNodes()
    {
        // Busca TODOS los nodos de la escena, incluso los que están apagados (true)
        SkillNode[] allNodes = FindObjectsOfType<SkillNode>(true);

        foreach (var node in allNodes)
        {
            // Obliga a cada nodo a revisar si sus padres ya están comprados
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

        // Debug general para saber qué nodo se acaba de activar (Usamos skillNameKey)
        Debug.Log($"<color=green>[SkillTree]</color> Aplicando efecto: <b>{effectType}</b> del nodo: {skillNameKey}");

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
            case SkillEffectType.HalveZoneCosts: Guardado.instance.ActivateZoneDiscount(); break;
            case SkillEffectType.ZoneIncome100: Guardado.instance.SetZonePassiveIncome(100); break;
            case SkillEffectType.ZoneIncome250: Guardado.instance.SetZonePassiveIncome(250); break;
            case SkillEffectType.ZoneIncome500: Guardado.instance.SetZonePassiveIncome(500); break;
            case SkillEffectType.ZoneIncome1000: Guardado.instance.SetZonePassiveIncome(1000); break;
            case SkillEffectType.ZoneIncome5000: Guardado.instance.SetZonePassiveIncome(5000); break;

            case SkillEffectType.MultiplyRadius125: Guardado.instance.SetRadiusMultiplier(1.25f); break;
            case SkillEffectType.MultiplyRadius150: Guardado.instance.SetRadiusMultiplier(1.50f); break;
            case SkillEffectType.MultiplyRadius200: Guardado.instance.SetRadiusMultiplier(2.00f); break;

            // --- DEBUGS DE NIVELES (Para los controladores de mejoras) ---
            case SkillEffectType.RadiusLevel2: VirusRadiusController.instance.SetLevel(2); Debug.Log("Mejora: Radius -> Nivel 2"); break;
            case SkillEffectType.RadiusLevel3: VirusRadiusController.instance.SetLevel(3); Debug.Log("Mejora: Radius -> Nivel 3"); break;
            case SkillEffectType.RadiusLevel4: VirusRadiusController.instance.SetLevel(4); Debug.Log("Mejora: Radius -> Nivel 4"); break;
            case SkillEffectType.RadiusLevel5: VirusRadiusController.instance.SetLevel(5); Debug.Log("Mejora: Radius -> Nivel 5"); break;
            case SkillEffectType.RadiusLevel6: VirusRadiusController.instance.SetLevel(6); Debug.Log("Mejora: Radius -> Nivel 6"); break;

            case SkillEffectType.SpeedLevel2: SpeedUpgradeController.instance.SetLevel(2); Debug.Log("Mejora: Speed -> Nivel 2"); break;
            case SkillEffectType.SpeedLevel3: SpeedUpgradeController.instance.SetLevel(3); Debug.Log("Mejora: Speed -> Nivel 3"); break;
            case SkillEffectType.SpeedLevel4: SpeedUpgradeController.instance.SetLevel(4); Debug.Log("Mejora: Speed -> Nivel 4"); break;
            case SkillEffectType.SpeedLevel5: SpeedUpgradeController.instance.SetLevel(5); Debug.Log("Mejora: Speed -> Nivel 5"); break;

            case SkillEffectType.CapacityLevel2: CapacityUpgradeController.instance.SetLevel(2); Debug.Log("Mejora: Capacity -> Nivel 2"); break;
            case SkillEffectType.CapacityLevel3: CapacityUpgradeController.instance.SetLevel(3); Debug.Log("Mejora: Capacity -> Nivel 3"); break;
            case SkillEffectType.CapacityLevel4: CapacityUpgradeController.instance.SetLevel(4); Debug.Log("Mejora: Capacity -> Nivel 4"); break;
            case SkillEffectType.CapacityLevel5: CapacityUpgradeController.instance.SetLevel(5); Debug.Log("Mejora: Capacity -> Nivel 5"); break;
            case SkillEffectType.CapacityLevel6: CapacityUpgradeController.instance.SetLevel(6); Debug.Log("Mejora: Capacity -> Nivel 6"); break;

            case SkillEffectType.TimeLevel2: TimeUpgradeController.instance.SetLevel(2); Debug.Log("Mejora: Time -> Nivel 2"); break;
            case SkillEffectType.TimeLevel3: TimeUpgradeController.instance.SetLevel(3); Debug.Log("Mejora: Time -> Nivel 3"); break;
            case SkillEffectType.TimeLevel4: TimeUpgradeController.instance.SetLevel(4); Debug.Log("Mejora: Time -> Nivel 4"); break;
            case SkillEffectType.TimeLevel5: TimeUpgradeController.instance.SetLevel(5); Debug.Log("Mejora: Time -> Nivel 5"); break;

            case SkillEffectType.TimeLevel6: TimeUpgradeController.instance.SetLevel(6); Debug.Log("Mejora: Time -> Nivel 6"); break;
            case SkillEffectType.InfectionSpeedLevel2: InfectionSpeedUpgradeController.instance.SetLevel(2); Debug.Log("Mejora: Infection Speed -> Nivel 2"); break;
            case SkillEffectType.InfectionSpeedLevel3: InfectionSpeedUpgradeController.instance.SetLevel(3); Debug.Log("Mejora: Infection Speed -> Nivel 3"); break;
            case SkillEffectType.InfectionSpeedLevel4: InfectionSpeedUpgradeController.instance.SetLevel(4); Debug.Log("Mejora: Infection Speed -> Nivel 4"); break;
            case SkillEffectType.InfectionSpeedLevel5: InfectionSpeedUpgradeController.instance.SetLevel(5); Debug.Log("Mejora: Infection Speed -> Nivel 5"); break;
            case SkillEffectType.InfectionSpeedLevel6: InfectionSpeedUpgradeController.instance.SetLevel(6); Debug.Log("Mejora: Infection Speed -> Nivel 6"); break;

            case SkillEffectType.MultiplySpeed125: Guardado.instance.SetSpeedMultiplier(1.25f); break;
            case SkillEffectType.MultiplySpeed150: Guardado.instance.SetSpeedMultiplier(1.50f); break;

            case SkillEffectType.InfectSpeed50: Guardado.instance.SetInfectionSpeedBonus(0.50f); break;
            case SkillEffectType.InfectSpeed100: Guardado.instance.SetInfectionSpeedBonus(1.00f); break;
            case SkillEffectType.KeepUpgradesOnResetEffect: Guardado.instance.keepUpgradesOnReset = true; break;
            // Reparación de Zonas (Usando el método que ya existe en Guardado)
            case SkillEffectType.KeepZonesOnReset:
                Guardado.instance.ActivateKeepZones();
                break;

            // Reparación de Duplicación (Cambiado de 'SetDuplicateOnHitChance' a 'SetDuplicateProbability')
            case SkillEffectType.DuplicateOnHit20: Guardado.instance.SetDuplicateProbability(0.20f); break;
            case SkillEffectType.DuplicateOnHit40: Guardado.instance.SetDuplicateProbability(0.40f); break;
            case SkillEffectType.DuplicateOnHit60: Guardado.instance.SetDuplicateProbability(0.60f); break;
            case SkillEffectType.DuplicateOnHit80: Guardado.instance.SetDuplicateProbability(0.80f); break;
            case SkillEffectType.DuplicateOnHit100: Guardado.instance.SetDuplicateProbability(1.00f); break;


            case SkillEffectType.CarambolaNormal: Guardado.instance.ActivarCarambolaNormal(); break;
            case SkillEffectType.CarambolaPro: Guardado.instance.ActivarCarambolaPro(); break;
            case SkillEffectType.CarambolaSuprema: Guardado.instance.ActivarCarambolaSuprema(); break;
            case SkillEffectType.ParedInfectiva_Nivel1:
                Guardado.instance.ActivarParedInfectiva();
                Guardado.instance.SetNivelParedInfectiva(1);
                break;

            case SkillEffectType.ParedInfectiva_Nivel2:
                Guardado.instance.ActivarParedInfectiva();
                Guardado.instance.SetNivelParedInfectiva(2);
                break;

            case SkillEffectType.ParedInfectiva_Nivel3:
                Guardado.instance.ActivarParedInfectiva();
                Guardado.instance.SetNivelParedInfectiva(3);
                break;

            case SkillEffectType.ParedInfectiva_Nivel4:
                Guardado.instance.ActivarParedInfectiva();
                Guardado.instance.SetNivelParedInfectiva(4);
                break;

            case SkillEffectType.ParedInfectiva_Nivel5:
                Guardado.instance.ActivarParedInfectiva();
                Guardado.instance.SetNivelParedInfectiva(5);
                break;

            case SkillEffectType.ReboteConCoral:
                Guardado.instance.ReboteConCoral();
                break;

            // --- DEBUGS DE DAÑO POR FORMA ---
            case SkillEffectType.DmgHexagono:
                Guardado.instance.dañoExtraHexagono += 1;
                Debug.Log("<color=magenta>Dmg Extra:</color> Hexágono activado -> Corresponde a <b>FASE 0</b> (Elemento 0 del Array)");
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.DmgPentagono:
                Guardado.instance.dañoExtraPentagono += 1;
                Debug.Log("<color=orange>Dmg Extra:</color> Pentágono activado -> Corresponde a <b>FASE 1</b> (Elemento 1 del Array)");
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.DmgCuadrado:
                Guardado.instance.dañoExtraCuadrado += 1;
                Debug.Log("<color=yellow>Dmg Extra:</color> Cuadrado activado -> Corresponde a <b>FASE 2</b> (Elemento 2 del Array)");
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.DmgTriangulo:
                Guardado.instance.dañoExtraTriangulo += 1;
                Debug.Log("<color=green>Dmg Extra:</color> Triángulo activado -> Corresponde a <b>FASE 3</b> (Elemento 3 del Array)");
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.DmgCirculo:
                Guardado.instance.dañoExtraCirculo += 1;
                Debug.Log("<color=cyan>Dmg Extra:</color> Círculo activado -> Corresponde a <b>FASE 4</b> (Elemento 4 del Array)");
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.DestroyCoralOnInfectedImpact:
                Guardado.instance.destroyCoralOnInfectedImpact = true;
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.AddTime2Seconds:
                Debug.Log("ENTRA EN LA HABILIDAD TIEMPO");
                Guardado.instance.AddExtraBaseTime(2f);
                break;
            case SkillEffectType.AddTimeOnPhaseChance5:
                Guardado.instance.SetAddTimeOnPhaseChance(0.05f);
                break;
            case SkillEffectType.AddTimeOnPhaseChance10:
                Guardado.instance.SetAddTimeOnPhaseChance(0.10f);
                break;

            case SkillEffectType.AddTimeOnPhaseChance15:
                Guardado.instance.SetAddTimeOnPhaseChance(0.15f);
                break;

            case SkillEffectType.AddTimeOnPhaseChance20:
                Guardado.instance.SetAddTimeOnPhaseChance(0.20f);
                break;

            case SkillEffectType.AddTimeOnPhaseChance25:
                Guardado.instance.SetAddTimeOnPhaseChance(0.25f);
                break;
            case SkillEffectType.DoubleUpgradeChance05: Guardado.instance.SetDoubleUpgradeChance(0.05f); break;
            case SkillEffectType.DoubleUpgradeChance10: Guardado.instance.SetDoubleUpgradeChance(0.10f); break;
            case SkillEffectType.DoubleUpgradeChance15: Guardado.instance.SetDoubleUpgradeChance(0.15f); break;
            case SkillEffectType.DoubleUpgradeChance20: Guardado.instance.SetDoubleUpgradeChance(0.20f); break;
            case SkillEffectType.DoubleUpgradeChance25: Guardado.instance.SetDoubleUpgradeChance(0.25f); break;
            case SkillEffectType.RandomSpawnAnyPhase5:
                Guardado.instance.SetRandomSpawnPhaseChance(0.05f);
                break;
            case SkillEffectType.RandomSpawnAnyPhase10:
                Guardado.instance.SetRandomSpawnPhaseChance(0.1f);
                break;
            case SkillEffectType.RandomSpawnAnyPhase15:
                Guardado.instance.SetRandomSpawnPhaseChance(0.15f);
                break;
            case SkillEffectType.RandomSpawnAnyPhase20:
                Guardado.instance.SetRandomSpawnPhaseChance(0.20f);
                break;
            case SkillEffectType.RandomSpawnAnyPhase25:
                Guardado.instance.SetRandomSpawnPhaseChance(0.25f);
                break;
            case SkillEffectType.CoinsCirculoPlus1:
                Guardado.instance.coinsExtraCirculo += 1;
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.CoinsTrianguloPlus1:
                Guardado.instance.coinsExtraTriangulo += 1;
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.CoinsCuadradoPlus1:
                Guardado.instance.coinsExtraCuadrado += 1;
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.CoinsPentagonoPlus1:
                Guardado.instance.coinsExtraPentagono += 1;
                Guardado.instance.SaveData();
                break;

            case SkillEffectType.CoinsHexagonoPlus1:
                Guardado.instance.coinsExtraHexagono += 1;
                Guardado.instance.SaveData();
                break;
            case SkillEffectType.InfectSpeedPhase0_10:
                Guardado.instance.infectSpeedPerPhase[0] = 1.1f;
                break;

            case SkillEffectType.InfectSpeedPhase0_20:
                Guardado.instance.infectSpeedPerPhase[0] = 1.2f;
                break;

            case SkillEffectType.InfectSpeedPhase0_30:
                Guardado.instance.infectSpeedPerPhase[0] = 1.3f;
                break;

            case SkillEffectType.InfectSpeedPhase0_40:
                Guardado.instance.infectSpeedPerPhase[0] = 1.4f;
                break;

            case SkillEffectType.InfectSpeedPhase0_50:
                Guardado.instance.infectSpeedPerPhase[0] = 1.5f;
                break;


            case SkillEffectType.InfectSpeedPhase1_10:
                Guardado.instance.infectSpeedPerPhase[1] = 1.1f;
                break;

            case SkillEffectType.InfectSpeedPhase1_20:
                Guardado.instance.infectSpeedPerPhase[1] = 1.2f;
                break;

            case SkillEffectType.InfectSpeedPhase1_30:
                Guardado.instance.infectSpeedPerPhase[1] = 1.3f;
                break;

            case SkillEffectType.InfectSpeedPhase1_40:
                Guardado.instance.infectSpeedPerPhase[1] = 1.4f;
                break;

            case SkillEffectType.InfectSpeedPhase1_50:
                Guardado.instance.infectSpeedPerPhase[1] = 1.5f;
                break;


            case SkillEffectType.InfectSpeedPhase2_10:
                Guardado.instance.infectSpeedPerPhase[2] = 1.1f;
                break;

            case SkillEffectType.InfectSpeedPhase2_20:
                Guardado.instance.infectSpeedPerPhase[2] = 1.2f;
                break;

            case SkillEffectType.InfectSpeedPhase2_30:
                Guardado.instance.infectSpeedPerPhase[2] = 1.3f;
                break;

            case SkillEffectType.InfectSpeedPhase2_40:
                Guardado.instance.infectSpeedPerPhase[2] = 1.4f;
                break;

            case SkillEffectType.InfectSpeedPhase2_50:
                Guardado.instance.infectSpeedPerPhase[2] = 1.5f;
                break;


            case SkillEffectType.InfectSpeedPhase3_10:
                Guardado.instance.infectSpeedPerPhase[3] = 1.1f;
                break;

            case SkillEffectType.InfectSpeedPhase3_20:
                Guardado.instance.infectSpeedPerPhase[3] = 1.2f;
                break;

            case SkillEffectType.InfectSpeedPhase3_30:
                Guardado.instance.infectSpeedPerPhase[3] = 1.3f;
                break;

            case SkillEffectType.InfectSpeedPhase3_40:
                Guardado.instance.infectSpeedPerPhase[3] = 1.4f;
                break;

            case SkillEffectType.InfectSpeedPhase3_50:
                Guardado.instance.infectSpeedPerPhase[3] = 1.5f;
                break;


            case SkillEffectType.InfectSpeedPhase4_10:
                Guardado.instance.infectSpeedPerPhase[4] = 1.1f;
                break;

            case SkillEffectType.InfectSpeedPhase4_20:
                Guardado.instance.infectSpeedPerPhase[4] = 1.2f;
                break;

            case SkillEffectType.InfectSpeedPhase4_30:
                Guardado.instance.infectSpeedPerPhase[4] = 1.3f;
                break;

            case SkillEffectType.InfectSpeedPhase4_40:
                Guardado.instance.infectSpeedPerPhase[4] = 1.4f;
                break;

            case SkillEffectType.InfectSpeedPhase4_50:
                Guardado.instance.infectSpeedPerPhase[4] = 1.5f;
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


    // --------------------------------------------------------------
    // CAMBIO IMPORTANTE: AQUÍ CONECTAMOS CON LA TRADUCCIÓN
    // --------------------------------------------------------------
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SkillTooltip.instance != null)
        {
            // CAMBIO 3: Pasamos las KEYS (skillNameKey y descriptionKey) y el RectTransform
            SkillTooltip.instance.Show(
                skillNameKey,
                descriptionKey,
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

        // 1️⃣ Cargar estados
        foreach (var node in nodes)
            node.LoadNodeState();

        // 2️⃣ Apagar todos
        foreach (var node in nodes)
            node.gameObject.SetActive(false);

        // 3️⃣ Evaluar TODOS los nodos
        foreach (var node in nodes)
            node.CheckIfShouldShow();
    }
}