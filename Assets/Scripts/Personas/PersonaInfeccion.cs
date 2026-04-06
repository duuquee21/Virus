using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PersonaInfeccion : MonoBehaviour
{
    public static float globalInfectTime = 2f;

    [Header("Sprites de Evolución")]
    public Sprite[] fasesSprites;
    public Sprite spriteInfectado;
    public Sprite[] contornosFases;
    public Color[] coloresFases;

    public static int[] evolucionesEntreFases = new int[5];
    public static int[] evolucionesPorChoque = new int[5];
    public static int[] evolucionesCarambola = new int[5];

    [Header("Dificultad por Fase")]
    [Tooltip("Multiplicador de tiempo: 1 = 2s, 1.5 = 3s, etc.")]
    public float[] resistenciaPorFase = { 1f, 1.2f, 1.5f, 1.8f, 2.2f };

    public static float dañoTotalZona = 0f;
    public static float dañoTotalChoque = 0f;
    public static float dañoTotalCarambola = 0f;

    public static float[] dañoZonaPorFase = new float[5];
    public static float[] dañoChoquePorFase = new float[5];
    public static float[] dañoCarambolaPorFase = new float[5];
    public static int[] golpesAlPlanetaPorFase = new int[5];

    [Header("Barras Falsas por Fase")]
    public GameObject[] prefabsBarraFalsa;
    private GameObject instanciaBarraActual;
    private GameObject[] barrasFalsasInstanciadas;
    private int barraFalsaActivaIndex = -1;

    [Header("Recompensa Económica (Coins)")]
    public int[] monedasPorFase = { 5, 4, 3, 2, 1 };
    private readonly int[] valorPorFase = { 1, 2, 3, 4, 5 };

    [Header("Ajustes de Daño")]
    public float[] dañoPorFasePredeterminado = { 1f, 2f, 3f, 4f, 5f };

    [Header("Referencias Visuales")]
    public SpriteRenderer spritePersona;
    public Image[] fillingBarImages;
    private Image[] fillingBarInnerImages;
    public GameObject infectionBarCanvas;
    public Color colorCargaInicial = Color.green;
    public Color colorCargaFinal = Color.red;

    [Header("Feedback Infección Final")]
    public float flashDuration = 0.1f;
    public float fadeDuration = 0.5f;
    public Color infectedColor = Color.red;

    [Header("Físicas")]
    public float fuerzaRetroceso = 8f;
    public float fuerzaRotacion = 5f;

    private float currentInfectionTime;

    private bool isInsideZone = false;
    public bool IsInsideZone => isInsideZone;
    public bool alreadyInfected = false;
    private Color originalColor;
    public int faseActual;
    private Transform transformInfector;
    private Movement movementScript;

    private Coroutine colorCoroutine;

    private ManagerAnimacionJugador managerAnimacion;

    public float shakeIntentarçsity = 0.05f;
    private Vector3 originalPosition;
    private Vector3 initialPosition;
    private bool positionSaved = false;
    private bool yaActivoSpawnPorFaseFinal = false;

    private Vector3 escalaOriginal;

    private Rigidbody2D rb;

    public ParticleSystem particulasDeFuego;
    public GameObject floatingTextPrefab;

    [Header("Ajustes del Rastro")]
    private TrailRenderer trail1;
    public TrailRenderer trail2;
    private bool trailActivatedThisCycle = false;

    private float lastProgressSent = -1f;
    private float ultimoSpawnParedTime = 0f;

    [Header("Optimización de FX")]
    [SerializeField] private float phaseEffectCooldown = 0.02f;
    private static float lastPhaseEffectTime = -999f;

    private WaitForSeconds trailWait;
    private WaitForSeconds flashCambioFaseWait;

    public float lastPlanetImpactTime = -999f;


    private static float ultimoTiempoTextoVolador = 0f;
    void Awake()
    {
        escalaOriginal = transform.localScale;
        trailWait = new WaitForSeconds(0.5f);
        flashCambioFaseWait = new WaitForSeconds(0.05f);
    }

    void Start()
    {
        movementScript = GetComponent<Movement>();
        rb = GetComponent<Rigidbody2D>();

        if (spritePersona == null)
            spritePersona = GetComponent<SpriteRenderer>();

        if (spritePersona != null)
            originalColor = spritePersona.color;

        if (infectionBarCanvas != null)
            infectionBarCanvas.SetActive(false);

        trail1 = GetComponent<TrailRenderer>();
        if (trail1 != null) trail1.emitting = false;
        if (trail2 != null) trail2.emitting = false;

        CachearBarrasInternas();
        PreinstanciarBarrasFalsas();

        ActualizarVisualFase();
        ActualizarProgresoBarras(0f);

        GameObject jugadorVirus = GameObject.FindGameObjectWithTag("Virus");
        if (jugadorVirus != null)
            managerAnimacion = jugadorVirus.GetComponent<ManagerAnimacionJugador>();
    }

    void OnEnable()
    {
        LevelTransitioner.OnTransitionStart += Desaparecer;

        if (PersonaManager.Instance != null)
            PersonaManager.Instance.RegistrarPersona(this);
    }

    void OnDisable()
    {
        LevelTransitioner.OnTransitionStart -= Desaparecer;

        if (PersonaManager.Instance != null)
            PersonaManager.Instance.DesregistrarPersona(this);
    }

    void OnDestroy()
    {
        if (PersonaManager.Instance != null)
            PersonaManager.Instance.DesregistrarPersona(this);

        if (PopulationManager.instance != null)
            PopulationManager.instance.UnregisterPersona(this.gameObject);
    }

    public enum TipoChoque
    {
        Wall,
        Carambola
    }

    public void SetInsideZoneManual(bool inside, Transform infector)
    {
        isInsideZone = inside;
        transformInfector = infector;
    }

    public void ActualizacionOptimizada(bool dentro, Transform infector, float deltaTime)
    {
        if (alreadyInfected)
            return;

        isInsideZone = dentro;
        transformInfector = dentro ? infector : null;

        if (isInsideZone)
        {
            if (infectionBarCanvas != null && !infectionBarCanvas.activeSelf)
                infectionBarCanvas.SetActive(true);

            if (instanciaBarraActual != null && instanciaBarraActual.activeSelf)
            {
                instanciaBarraActual.SetActive(false);
                barraFalsaActivaIndex = -1;
            }

            float resistencia = (faseActual < resistenciaPorFase.Length) ? resistenciaPorFase[faseActual] : 1f;
            float tiempoNecesario = globalInfectTime * resistencia;

            float multiplier = 1f;
            if (Guardado.instance != null)
            {
                multiplier = Guardado.instance.infectSpeedMultiplier;

                if (Guardado.instance.infectSpeedPerPhase != null &&
                    faseActual < Guardado.instance.infectSpeedPerPhase.Length)
                {
                    multiplier *= Guardado.instance.infectSpeedPerPhase[faseActual];
                }
            }

            currentInfectionTime += deltaTime * multiplier;
            float progresoActual = currentInfectionTime / tiempoNecesario;
            ActualizarProgresoSiCambio(progresoActual);

            if (currentInfectionTime >= tiempoNecesario)
                IntentarAvanzarFase();
        }
        else
        {
            if (currentInfectionTime > 0f)
            {
                currentInfectionTime -= deltaTime * 2f;
                if (currentInfectionTime < 0f)
                    currentInfectionTime = 0f;

                float resistencia = (faseActual < resistenciaPorFase.Length) ? resistenciaPorFase[faseActual] : 1f;
                float progresoActual = currentInfectionTime / (globalInfectTime * resistencia);
                ActualizarProgresoSiCambio(progresoActual);
            }
            else
            {
                if (infectionBarCanvas != null && infectionBarCanvas.activeSelf)
                    infectionBarCanvas.SetActive(false);

                ActualizarInstanciaBarraFalsa();
            }
        }
    }

    private void ActualizarProgresoSiCambio(float progreso)
    {
        progreso = Mathf.Clamp01(progreso);

        // Cambiamos de 0.01f a 0.05f. 
        // Reduce la carga del Canvas en un 80% al actualizar solo cada 5% de progreso.
        if (Mathf.Abs(progreso - lastProgressSent) > 0.01f)
        {
            ActualizarProgresoBarras(progreso);
            lastProgressSent = progreso;
        }
    }

    private void CachearBarrasInternas()
    {
        if (fillingBarImages == null)
            return;

        fillingBarInnerImages = new Image[fillingBarImages.Length];

        for (int i = 0; i < fillingBarImages.Length; i++)
        {
            if (fillingBarImages[i] != null && fillingBarImages[i].transform.childCount > 0)
            {
                fillingBarInnerImages[i] = fillingBarImages[i].transform.GetChild(0).GetComponent<Image>();
            }
        }
    }

    private void PreinstanciarBarrasFalsas()
    {
        if (prefabsBarraFalsa == null || prefabsBarraFalsa.Length == 0)
            return;

        barrasFalsasInstanciadas = new GameObject[prefabsBarraFalsa.Length];

        for (int i = 0; i < prefabsBarraFalsa.Length; i++)
        {
            if (prefabsBarraFalsa[i] == null)
                continue;

            GameObject go = Instantiate(prefabsBarraFalsa[i], transform);
            go.SetActive(false);
            barrasFalsasInstanciadas[i] = go;
        }
    }

    private void SetTrailsEmitting(bool emit)
    {
        if (trail1 != null) trail1.emitting = emit;
        if (trail2 != null) trail2.emitting = emit;
    }

    void ActualizarProgresoBarras(float progress)
    {
        if (fillingBarImages == null || fillingBarInnerImages == null)
            return;

        progress = Mathf.Clamp01(progress);
        float inverseProgress = 1f - progress;

        for (int i = 0; i < fillingBarImages.Length; i++)
        {
            if (fillingBarImages[i] == null)
                continue;

            bool debeEstarActiva = (i == faseActual);

            if (fillingBarImages[i].gameObject.activeSelf != debeEstarActiva)
                fillingBarImages[i].gameObject.SetActive(debeEstarActiva);

            if (!debeEstarActiva)
                continue;

            Image barraInterna = fillingBarInnerImages[i];
            if (barraInterna != null)
            {
                barraInterna.fillAmount = inverseProgress;
                barraInterna.color = Color.Lerp(colorCargaFinal, colorCargaInicial, inverseProgress);
            }
        }
    }

    private IEnumerator ActivarRastroTemporal()
    {
        SetTrailsEmitting(true);
        yield return trailWait;
        SetTrailsEmitting(false);
    }

    public static void ResetearEstadisticas()
    {
        dañoTotalZona = 0f;
        dañoTotalChoque = 0f;
        dañoTotalCarambola = 0f;

        for (int i = 0; i < 5; i++)
        {
            dañoZonaPorFase[i] = 0f;
            dañoChoquePorFase[i] = 0f;
            dañoCarambolaPorFase[i] = 0f;

            evolucionesEntreFases[i] = 0;
            evolucionesPorChoque[i] = 0;
            evolucionesCarambola[i] = 0;
            golpesAlPlanetaPorFase[i] = 0;
        }
    }

    public static void SaveStats()
    {
        PlayerPrefs.SetFloat("Stats_DanoTotalZona", dañoTotalZona);
        PlayerPrefs.SetFloat("Stats_DanoTotalChoque", dañoTotalChoque);
        PlayerPrefs.SetFloat("Stats_DanoTotalCarambola", dañoTotalCarambola);

        SaveFloatArray("Stats_DanoZonaPorFase", dañoZonaPorFase);
        SaveFloatArray("Stats_DanoChoquePorFase", dañoChoquePorFase);
        SaveFloatArray("Stats_DanoCarambolaPorFase", dañoCarambolaPorFase);

        SaveIntArray("Stats_GolpesAlPlanetaPorFase", golpesAlPlanetaPorFase);
        SaveIntArray("Stats_EvolucionesEntreFases", evolucionesEntreFases);
        SaveIntArray("Stats_EvolucionesPorChoque", evolucionesPorChoque);
        SaveIntArray("Stats_EvolucionesCarambola", evolucionesCarambola);

        PlayerPrefs.Save();
    }

    public static void LoadStats()
    {
        dañoTotalZona = PlayerPrefs.GetFloat("Stats_DanoTotalZona", 0f);
        dañoTotalChoque = PlayerPrefs.GetFloat("Stats_DanoTotalChoque", 0f);
        dañoTotalCarambola = PlayerPrefs.GetFloat("Stats_DanoTotalCarambola", 0f);

        LoadFloatArray("Stats_DanoZonaPorFase", dañoZonaPorFase);
        LoadFloatArray("Stats_DanoChoquePorFase", dañoChoquePorFase);
        LoadFloatArray("Stats_DanoCarambolaPorFase", dañoCarambolaPorFase);

        LoadIntArray("Stats_GolpesAlPlanetaPorFase", golpesAlPlanetaPorFase);
        LoadIntArray("Stats_EvolucionesEntreFases", evolucionesEntreFases);
        LoadIntArray("Stats_EvolucionesPorChoque", evolucionesPorChoque);
        LoadIntArray("Stats_EvolucionesCarambola", evolucionesCarambola);
    }

    public static void ClearSavedStats()
    {
        PlayerPrefs.DeleteKey("Stats_DanoTotalZona");
        PlayerPrefs.DeleteKey("Stats_DanoTotalChoque");
        PlayerPrefs.DeleteKey("Stats_DanoTotalCarambola");

        DeleteArrayKeys("Stats_DanoZonaPorFase", dañoZonaPorFase.Length);
        DeleteArrayKeys("Stats_DanoChoquePorFase", dañoChoquePorFase.Length);
        DeleteArrayKeys("Stats_DanoCarambolaPorFase", dañoCarambolaPorFase.Length);

        DeleteArrayKeys("Stats_GolpesAlPlanetaPorFase", golpesAlPlanetaPorFase.Length);
        DeleteArrayKeys("Stats_EvolucionesEntreFases", evolucionesEntreFases.Length);
        DeleteArrayKeys("Stats_EvolucionesPorChoque", evolucionesPorChoque.Length);
        DeleteArrayKeys("Stats_EvolucionesCarambola", evolucionesCarambola.Length);

        PlayerPrefs.Save();
    }

    private static void SaveIntArray(string prefix, int[] array)
    {
        if (array == null) return;

        for (int i = 0; i < array.Length; i++)
            PlayerPrefs.SetInt(prefix + "_" + i, array[i]);
    }

    private static void LoadIntArray(string prefix, int[] array)
    {
        if (array == null) return;

        for (int i = 0; i < array.Length; i++)
            array[i] = PlayerPrefs.GetInt(prefix + "_" + i, 0);
    }

    private static void SaveFloatArray(string prefix, float[] array)
    {
        if (array == null) return;

        for (int i = 0; i < array.Length; i++)
            PlayerPrefs.SetFloat(prefix + "_" + i, array[i]);
    }

    private static void LoadFloatArray(string prefix, float[] array)
    {
        if (array == null) return;

        for (int i = 0; i < array.Length; i++)
            array[i] = PlayerPrefs.GetFloat(prefix + "_" + i, 0f);
    }

    private static void DeleteArrayKeys(string prefix, int length)
    {
        for (int i = 0; i < length; i++)
            PlayerPrefs.DeleteKey(prefix + "_" + i);
    }

    public void EstablecerFaseDirecta(int fase)
    {
        faseActual = fase;
        currentInfectionTime = 0f;
        lastProgressSent = -1f;
        ActualizarVisualFase();
        ActualizarProgresoBarras(0f);
    }

    public void AplicarColor(Color nuevoColor)
    {
        if (spritePersona == null)
            return;

        nuevoColor.a = 1f;
        spritePersona.color = nuevoColor;
    }

    public void IntentarAvanzarFase(int cantidadFases = 1)
    {
        if (alreadyInfected || faseActual >= fasesSprites.Length)
            return;

        int faseAnterior = faseActual;
        currentInfectionTime = 0f;
        lastProgressSent = -1f;

        if (gameObject.activeInHierarchy)
            StartCoroutine(ActivarRastroTemporal());

        int steps = cantidadFases;

        if (steps == 1 && Guardado.instance != null)
        {
            float chanceDouble = Guardado.instance.doubleUpgradeChance;
            if (chanceDouble > 0f && Random.value < chanceDouble)
                steps = 2;
        }

        faseActual += steps;

        if (faseAnterior == 0 && faseActual > 0)
        {
            if (TutorialManager.instance != null)
                TutorialManager.instance.OnFirstPhaseAdvance();
            LevelManager.instance.StartTimer();
        }

        if (LevelManager.instance != null)
        {
            for (int i = faseAnterior; i < faseActual && i < valorPorFase.Length; i++)
            {
                int monedasADar = GetCoinsForPhase(i);
                LevelManager.instance.MostrarPuntosVoladores(transform.position, monedasADar);
                SpawnFloatingMoney(monedasADar);

                if (i < evolucionesEntreFases.Length)
                    evolucionesEntreFases[i]++;
            }
        }

        if (faseActual >= fasesSprites.Length)
        {
            faseActual = fasesSprites.Length - 1;
            BecomeInfected();
        }
        else
        {
            ActualizarVisualFase();
            IniciarCambioColor(FlashCambioFase());

            if (InfectionFeedback.instance != null && Time.time >= lastPhaseEffectTime + phaseEffectCooldown)
            {
                InfectionFeedback.instance.PlayPhaseChangeEffect(transform.position, originalColor);
                lastPhaseEffectTime = Time.time;
            }
            if (InfectionShaderController.instance != null)
                InfectionShaderController.instance.AcelerarShaderDeGolpe();
            if (transformInfector != null && movementScript != null)
            {
                Vector2 dirEmpuje = (transform.position - transformInfector.position).normalized;
                movementScript.AplicarEmpuje(dirEmpuje, fuerzaRetroceso, fuerzaRotacion);
            }
        }

        if (LevelManager.instance != null && !LevelManager.instance.timerStarted)
            LevelManager.instance.timerStarted = true;
    }

    void BecomeInfected()
    {
        alreadyInfected = true;

        gameObject.layer = LayerMask.NameToLayer("Infectado");

        if (colorCoroutine != null)
            StopCoroutine(colorCoroutine);

        if (spritePersona != null)
        {
            if (spriteInfectado != null)
                spritePersona.sprite = spriteInfectado;

            spritePersona.color = infectedColor;
        }

        OcultarTodasLasBarrasFalsas();

        if (infectionBarCanvas != null)
            infectionBarCanvas.SetActive(false);

        if (InfectionFeedback.instance != null)
            InfectionFeedback.instance.PlayEffect(transform.position, Color.white, false);

        if (particulasDeFuego != null)
            particulasDeFuego.Play();

        if (LevelManager.instance != null)
            LevelManager.instance.RegisterInfection();

        if (movementScript != null)
        {
            Vector2 dirEmpuje = Vector2.zero;

            if (transformInfector != null)
                dirEmpuje = (Vector2)(transform.position - transformInfector.position);

            if (dirEmpuje.sqrMagnitude < 0.0001f)
                dirEmpuje = Random.insideUnitCircle.normalized;
            else
                dirEmpuje = dirEmpuje.normalized;

            movementScript.AplicarEmpuje(dirEmpuje, fuerzaRetroceso, fuerzaRotacion);
        }

        transform.localScale = escalaOriginal * 1.125f;

        IniciarCambioColor(InfectionColorSequence());
    }

    public int GetMaxFaseIndex()
    {
        if (fasesSprites == null || fasesSprites.Length == 0)
            return 0;

        return fasesSprites.Length - 1;
    }

    void ActualizarVisualFase()
    {
        if (alreadyInfected)
            return;

        if (faseActual < 0 || faseActual >= fasesSprites.Length)
            return;

        if (spritePersona != null)
        {
            spritePersona.sprite = fasesSprites[faseActual];

            if (coloresFases != null && faseActual < coloresFases.Length)
                spritePersona.color = coloresFases[faseActual];
        }

        if (fillingBarInnerImages != null &&
            contornosFases != null &&
            faseActual < fillingBarInnerImages.Length &&
            faseActual < contornosFases.Length)
        {
            Image barraInterna = fillingBarInnerImages[faseActual];
            if (barraInterna != null)
                barraInterna.sprite = contornosFases[faseActual];
        }

        ActualizarInstanciaBarraFalsa();
    }

    public float ObtenerDañoTotal()
    {
        float dañoBase = (faseActual < dañoPorFasePredeterminado.Length) ? dañoPorFasePredeterminado[faseActual] : 1f;

        int bonoHabilidad = 0;
        if (Guardado.instance != null)
        {
            switch (faseActual)
            {
                case 0: bonoHabilidad = Guardado.instance.dañoExtraHexagono; break;
                case 1: bonoHabilidad = Guardado.instance.dañoExtraPentagono; break;
                case 2: bonoHabilidad = Guardado.instance.dañoExtraCuadrado; break;
                case 3: bonoHabilidad = Guardado.instance.dañoExtraTriangulo; break;
                case 4: bonoHabilidad = Guardado.instance.dañoExtraCirculo; break;
            }
        }

        return dañoBase + bonoHabilidad;
    }

    private IEnumerator FlashCambioFase()
    {
        Color colorObjetivo = (faseActual < coloresFases.Length) ? coloresFases[faseActual] : originalColor;

        if (spritePersona != null)
            spritePersona.color = Color.white;

        yield return flashCambioFaseWait;

        if (spritePersona != null)
            spritePersona.color = colorObjetivo;
    }

    void Desaparecer()
    {
        gameObject.SetActive(false);
    }

    private IEnumerator InfectionColorSequence()
    {
        if (spritePersona != null)
            spritePersona.color = Color.white;

        yield return new WaitForSeconds(flashDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            if (spritePersona != null)
                spritePersona.color = Color.Lerp(Color.white, infectedColor, elapsed / fadeDuration);

            yield return null;
        }

        if (spritePersona != null)
            spritePersona.color = infectedColor;

        colorCoroutine = null;
    }

    public void IntentarAvanzarFasePorChoque(TipoChoque tipo)
    {
        if (alreadyInfected)
            return;

        int faseAnterior = faseActual;

        if (faseActual < fasesSprites.Length - 1)
        {
            currentInfectionTime = 0f;
            lastProgressSent = -1f;
            faseActual++;

            if (faseAnterior < evolucionesPorChoque.Length)
            {
                if (tipo == TipoChoque.Wall)
                    evolucionesPorChoque[faseAnterior]++;
                else if (tipo == TipoChoque.Carambola)
                    evolucionesCarambola[faseAnterior]++;
            }

            if (LevelManager.instance != null && faseAnterior < valorPorFase.Length)
            {
                int monedasADar = GetCoinsForPhase(faseAnterior);

                // Solo mostramos el texto flotante si realmente no estamos saturando el frame
                if (Time.time - ultimoTiempoTextoVolador >= 0.05f)
                {
                    LevelManager.instance.MostrarPuntosVoladores(transform.position, monedasADar);
                    SpawnFloatingMoney(monedasADar);
                }
                else
                {
                    // Sumamos el dinero silenciosamente sin generar coste gráfico
                    LevelManager.instance.AddCoins(monedasADar);
                }
            }

            if (InfectionFeedback.instance != null && Time.time >= lastPhaseEffectTime + phaseEffectCooldown)
            {
                InfectionFeedback.instance.PlayPhaseChangeEffect(transform.position, originalColor);
                lastPhaseEffectTime = Time.time;
            }

            ActualizarVisualFase();

            if (gameObject.activeInHierarchy)
                StartCoroutine(FlashCambioFase());
        }
        else
        {
            BecomeInfected();
        }
    }

    private void IniciarCambioColor(IEnumerator nuevaCorrutina)
    {
        if (gameObject.activeInHierarchy)
        {
            if (colorCoroutine != null)
                StopCoroutine(colorCoroutine);

            colorCoroutine = StartCoroutine(nuevaCorrutina);
        }
    }

    private void SpawnFloatingMoney(int cantidad)
    {
        if (TextPooler.Instance == null) return;

        // OPTIMIZACIÓN: Si han pasado menos de 0.05s desde el último texto de CUALQUIER persona, 
        // cancelamos la creación visual del texto para evitar tirones (el dinero se suma igual en LevelManager).
        if (Time.time - ultimoTiempoTextoVolador < 0.05f) return;
        ultimoTiempoTextoVolador = Time.time;

        GameObject obj = TextPooler.Instance.SpawnText(transform.position, "+" + cantidad.ToString());

        MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.sortingOrder = 32767;

        TMPro.TextMeshPro tm = obj.GetComponent<TMPro.TextMeshPro>();
        if (tm != null)
            tm.color = Color.white;
    }
    private int GetCoinsForPhase(int fase)
    {
        int baseCoins = (fase < valorPorFase.Length) ? valorPorFase[fase] : 0;

        int extra = 0;
        if (Guardado.instance != null)
        {
            switch (fase)
            {
                case 0: extra = Guardado.instance.coinsExtraHexagono; break;
                case 1: extra = Guardado.instance.coinsExtraPentagono; break;
                case 2: extra = Guardado.instance.coinsExtraCuadrado; break;
                case 3: extra = Guardado.instance.coinsExtraTriangulo; break;
                case 4: extra = Guardado.instance.coinsExtraCirculo; break;
            }
        }

        return Mathf.Max(0, baseCoins + extra);
    }

    public bool EsFaseMaxima() => faseActual >= fasesSprites.Length - 1;

    public void SetInfector(Transform nuevoInfector)
    {
        transformInfector = nuevoInfector;
    }

    private void ActualizarInstanciaBarraFalsa()
    {
        if (barrasFalsasInstanciadas == null || barrasFalsasInstanciadas.Length == 0)
        {
            instanciaBarraActual = null;
            barraFalsaActivaIndex = -1;
            return;
        }

        bool debeMostrar = !isInsideZone && !alreadyInfected;
        int nuevoIndice = -1;

        if (debeMostrar && faseActual >= 0 && faseActual < barrasFalsasInstanciadas.Length &&
            barrasFalsasInstanciadas[faseActual] != null)
        {
            nuevoIndice = faseActual;
        }

        if (barraFalsaActivaIndex == nuevoIndice)
        {
            instanciaBarraActual = (nuevoIndice >= 0) ? barrasFalsasInstanciadas[nuevoIndice] : null;
            return;
        }

        if (barraFalsaActivaIndex >= 0 &&
            barraFalsaActivaIndex < barrasFalsasInstanciadas.Length &&
            barrasFalsasInstanciadas[barraFalsaActivaIndex] != null)
        {
            barrasFalsasInstanciadas[barraFalsaActivaIndex].SetActive(false);
        }

        barraFalsaActivaIndex = nuevoIndice;
        instanciaBarraActual = null;

        if (barraFalsaActivaIndex >= 0)
        {
            instanciaBarraActual = barrasFalsasInstanciadas[barraFalsaActivaIndex];
            instanciaBarraActual.SetActive(true);
        }
    }

    private void OcultarTodasLasBarrasFalsas()
    {
        if (barrasFalsasInstanciadas != null)
        {
            for (int i = 0; i < barrasFalsasInstanciadas.Length; i++)
            {
                if (barrasFalsasInstanciadas[i] != null && barrasFalsasInstanciadas[i].activeSelf)
                    barrasFalsasInstanciadas[i].SetActive(false);
            }
        }

        instanciaBarraActual = null;
        barraFalsaActivaIndex = -1;
    }

    public void IntentarSpawnPorChoquePared()
    {
        if (!alreadyInfected) return;
        if (Time.time - ultimoSpawnParedTime < 0.2f) return;

        ultimoSpawnParedTime = Time.time;

        if (Guardado.instance != null && PopulationManager.instance != null)
        {
            // 1. Detectar el límite de población según el mapa
            int limitePoblacion = 100; // Límite por defecto

            // Obtenemos el índice del mapa actual desde PlayerPrefs (como hace tu LevelManager)
            int currentMapIndex = PlayerPrefs.GetInt("CurrentMapIndex", 0);

            // Si es el 4to mapa (índice 3), bajamos el límite a 50
            if (currentMapIndex == 3)
            {
                limitePoblacion = 50;
            }

            // 2. Aplicar el límite detectado
            if (PopulationManager.instance.GetTotalPopulationCount() > limitePoblacion)
                return;

            // 3. Lógica de spawn original
            float chance = Guardado.instance.spawnBaseOnMaxPhaseChance;
            if (Random.value < chance)
                PopulationManager.instance.SpawnPersonAtBasePhase();
        }
    }
    public void RegistrarDañoZona()
    {
        float daño = ObtenerDañoTotal();

        if (faseActual < 0 || faseActual >= dañoZonaPorFase.Length)
            return;

        dañoZonaPorFase[faseActual] += daño;
        dañoTotalZona += daño;
        golpesAlPlanetaPorFase[faseActual]++;
    }

    public void RegistrarDañoChoque(float daño)
    {
        if (faseActual < 0 || faseActual >= dañoChoquePorFase.Length)
            return;

        dañoChoquePorFase[faseActual] += daño;
        dañoTotalChoque += daño;
    }

    public void RegistrarDañoCarambola(float daño)
    {
        if (faseActual < 0 || faseActual >= dañoCarambolaPorFase.Length)
            return;

        dañoCarambolaPorFase[faseActual] += daño;
        dañoTotalCarambola += daño;
    }

    public void ReinicioTotalDesdePool(int nuevaFase, Color colorMapa)
    {
        alreadyInfected = false;
        isInsideZone = false;
        transformInfector = null;
        currentInfectionTime = 0f;
        lastProgressSent = -1f;
        ultimoSpawnParedTime = 0f;
        positionSaved = false;
        yaActivoSpawnPorFaseFinal = false;
        lastPlanetImpactTime = -999f;

        gameObject.layer = LayerMask.NameToLayer("Default");

        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }

        if (trail1 != null)
        {
            trail1.Clear();
            trail1.emitting = false;
        }

        if (trail2 != null)
        {
            trail2.Clear();
            trail2.emitting = false;
        }

        if (infectionBarCanvas != null)
            infectionBarCanvas.SetActive(false);

        OcultarTodasLasBarrasFalsas();

        transform.localScale = escalaOriginal;

        if (spritePersona != null)
            spritePersona.color = Color.white;

        faseActual = nuevaFase;
        originalColor = colorMapa;
        AplicarColor(colorMapa);

        if (particulasDeFuego != null)
            particulasDeFuego.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        ActualizarVisualFase();
        ActualizarProgresoBarras(0f);
    }
}