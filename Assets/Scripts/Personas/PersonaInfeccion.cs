using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PersonaInfeccion : MonoBehaviour
{
    public static float globalInfectTime = 2f;

    [Header("Sprites de Evolución")]
    public Sprite[] fasesSprites;
    public Sprite spriteInfectado; // <-- Añade esta línea
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

    // Daño por fase (0..4) separado por tipo
    public static float[] dañoZonaPorFase = new float[5];
    public static float[] dañoChoquePorFase = new float[5];
    public static float[] dañoCarambolaPorFase = new float[5];
    public static int[] golpesAlPlanetaPorFase = new int[5];

    [Header("Barras Falsas por Fase")]
    public GameObject[] prefabsBarraFalsa;
    private GameObject instanciaBarraActual;


    [Header("Recompensa Económica (Coins)")]
    public int[] monedasPorFase = { 5, 4, 3, 2, 1 };
    private readonly int[] valorPorFase = { 1, 2, 3, 4, 5 };

    [Header("Ajustes de Daño")]
    // Fase 0 (Círculo) = 5 | Fase 1 (Triángulo) = 4 | Fase 2 (Cuadrado) = 3 | Fase 3 (Pentágono) = 2 | Fase 4 (Hexágono) = 1
    public float[] dañoPorFasePredeterminado = { 1f, 2f, 3f, 4f, 5f };


    [Header("Referencias Visuales")]
    public SpriteRenderer spritePersona;
    public Image[] fillingBarImages;
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
    public int faseActual; // Ahora el planeta sí puede leerla
    private Transform transformInfector;
    private Movement movementScript;

    private Coroutine colorCoroutine;

    private ManagerAnimacionJugador managerAnimacion; // Nueva referencia

    public float shakeIntentarçsity = 0.05f;
    private Vector3 originalPosition;
    private Vector3 initialPosition;
    private bool positionSaved = false;
    private bool yaActivoSpawnPorFaseFinal = false;





    // --- REFERENCIA QUE FALTABA ---
    private Rigidbody2D rb;

    public ParticleSystem particulasDeFuego;


    public GameObject floatingTextPrefab;


    [Header("Ajustes del Rastro")]
    private TrailRenderer trail1; // El que está en el mismo objeto
    public TrailRenderer trail2;  // El que asignarás desde el Inspector
    private float trailTimer = 0f;
    private bool trailActivatedThisCycle = false;


    private float lastProgressSent = -1f;

    void Start()
    {
        movementScript = GetComponent<Movement>();
        rb = GetComponent<Rigidbody2D>();

        if (spritePersona == null) spritePersona = GetComponent<SpriteRenderer>();
        originalColor = spritePersona.color;

        // --- CAMBIO AQUÍ: Empezar con el Canvas APAGADO ---
        if (infectionBarCanvas != null) infectionBarCanvas.SetActive(false);

        trail1 = GetComponent<TrailRenderer>();
        if (trail1 != null) trail1.emitting = false;
        if (trail2 != null) trail2.emitting = false;

        ActualizarVisualFase();
        ActualizarProgresoBarras(0f);

        // Referencia al Manager de animación
        GameObject jugadorVirus = GameObject.FindGameObjectWithTag("Virus");
        if (jugadorVirus != null)
            managerAnimacion = jugadorVirus.GetComponent<ManagerAnimacionJugador>();
    }
    private void SetTrailsEmitting(bool emit)
    {
        if (trail1 != null) trail1.emitting = emit;
        if (trail2 != null) trail2.emitting = emit;
    }
    void OnEnable()
    {
        // Nos suscribimos al evento de destrucción
        LevelTransitioner.OnTransitionStart += Desaparecer;
        if (PersonaManager.Instance != null) PersonaManager.Instance.RegistrarPersona(this);

    }

    void OnDisable()
    {
        // Muy importante desuscribirse para evitar errores de memoria
        LevelTransitioner.OnTransitionStart -= Desaparecer;
        if (PersonaManager.Instance != null)
            PersonaManager.Instance.DesregistrarPersona(this);
    }
    public enum TipoChoque
    {
        Wall,
        Carambola
    }

    // Añade este método para que el Manager lo controle
    public void SetInsideZoneManual(bool inside, Transform infector)
    {
        isInsideZone = inside;
        if (inside) transformInfector = infector;
    }

    public void ActualizacionOptimizada(bool dentro, Transform infector, float deltaTime)
    {
        if (alreadyInfected) return;

        isInsideZone = dentro;
        transformInfector = dentro ? infector : null;

        if (isInsideZone)
        {
            // 1. ACTIVAR Canvas de vida y DESACTIVAR Barra Falsa
            if (infectionBarCanvas != null && !infectionBarCanvas.activeSelf)
                infectionBarCanvas.SetActive(true);

            if (instanciaBarraActual != null && instanciaBarraActual.activeSelf)
                instanciaBarraActual.SetActive(false);

            // --- Lógica de cálculo de tiempo ---
            float resistencia = (faseActual < resistenciaPorFase.Length) ? resistenciaPorFase[faseActual] : 1f;
            float tiempoNecesario = globalInfectTime * resistencia;

            float multiplier = 1f;
            if (Guardado.instance != null)
            {
                multiplier = Guardado.instance.infectSpeedMultiplier;
                if (Guardado.instance.infectSpeedPerPhase != null && faseActual < Guardado.instance.infectSpeedPerPhase.Length)
                    multiplier *= Guardado.instance.infectSpeedPerPhase[faseActual];
            }

            currentInfectionTime += deltaTime * multiplier;
            float progresoActual = currentInfectionTime / tiempoNecesario;

            if (Mathf.Abs(progresoActual - lastProgressSent) > 0.01f)
            {
                ActualizarProgresoBarras(progresoActual);
                lastProgressSent = progresoActual;
            }

            if (currentInfectionTime >= tiempoNecesario)
                IntentarAvanzarFase();
        }
        else
        {
            // Si no está dentro, el tiempo baja
            if (currentInfectionTime > 0)
            {
                currentInfectionTime -= deltaTime * 2f;
                float resistencia = (faseActual < resistenciaPorFase.Length) ? resistenciaPorFase[faseActual] : 1f;
                ActualizarProgresoBarras(currentInfectionTime / (globalInfectTime * resistencia));
            }
            else
            {
                // 2. DESACTIVAR Canvas de vida (cuando llega a 0) y ACTIVAR Barra Falsa
                if (infectionBarCanvas != null && infectionBarCanvas.activeSelf)
                    infectionBarCanvas.SetActive(false);

                if (instanciaBarraActual != null && !instanciaBarraActual.activeSelf)
                    instanciaBarraActual.SetActive(true);
            }
        }
    }
    // 3. Limpieza al morir
    void OnDestroy()
    {
        if (PersonaManager.Instance != null) PersonaManager.Instance.DesregistrarPersona(this);
        // Cuando el objeto se destruye (por cualquier razón), se quita de la lista
        if (PopulationManager.instance != null)
        {
            PopulationManager.instance.UnregisterPersona(this.gameObject);
        }
    }



  
    void ActualizarProgresoBarras(float progress)
    {
        if (fillingBarImages == null) return;
        float inverseProgress = 1f - progress;

        for (int i = 0; i < fillingBarImages.Length; i++)
        {
            if (fillingBarImages[i] == null) continue;

            if (i == faseActual)
            {
                // ACTIVAMOS el objeto de la lista (El Padre/Outline)
                fillingBarImages[i].gameObject.SetActive(true);

                // Buscamos al HIJO para vaciarlo
                if (fillingBarImages[i].transform.childCount > 0)
                {
                    Image barraInterna = fillingBarImages[i].transform.GetChild(0).GetComponent<Image>();
                    if (barraInterna != null)
                    {
                        // Solo el HIJO se vacía y cambia de color
                        barraInterna.fillAmount = inverseProgress;
                        barraInterna.color = Color.Lerp(colorCargaFinal, colorCargaInicial, inverseProgress);
                    }
                }
            }
            else
            {
                // DESACTIVAMOS el objeto de la lista completo
                fillingBarImages[i].gameObject.SetActive(false);
            }
        }
    }
    private IEnumerator ActivarRastroTemporal()
    {
        SetTrailsEmitting(true);
        yield return new WaitForSeconds(0.5f);
        SetTrailsEmitting(false);
    }

    public static void ResetearEstadisticas()
    {
        // 1. Reiniciar totales de daño
        dañoTotalZona = 0f;
        dañoTotalChoque = 0f;
        dañoTotalCarambola = 0f;

        // 2. Limpiar todos los arrays (Daño, Evoluciones y Golpes)
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
    public void EstablecerFaseDirecta(int fase)
    {
        faseActual = fase;
        currentInfectionTime = 0f;
        ActualizarVisualFase();
        ActualizarProgresoBarras(0f);
    }

    public void AplicarColor(Color nuevoColor)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Aseguramos que el alfa sea 1 antes de aplicar
            nuevoColor.a = 1f;
            sr.color = nuevoColor;
        }
    }

    public void IntentarAvanzarFase(int cantidadFases = 1)
    {
        if (alreadyInfected || faseActual >= fasesSprites.Length) return;

        int faseAnterior = faseActual;
        currentInfectionTime = 0f;
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ActivarRastroTemporal());
        }


        // Calculamos el avance base
        int steps = cantidadFases;

        // Mantenemos tu lógica de probabilidad de "doble mejora" si es un avance normal (1 fase)
        if (steps == 1 && Guardado.instance != null)
        {
            float chanceDouble = Guardado.instance.doubleUpgradeChance;
            if (chanceDouble > 0f && Random.value < chanceDouble)
                steps = 2;
        }

        // Aplicamos el avance
        faseActual += steps;
      

        // Tutorial: primera vez que una figura avanza al menos una fase
        if (faseAnterior == 0 && faseActual > 0)
        {
            if (TutorialManager.instance != null)
            {
                TutorialManager.instance.OnFirstPhaseAdvance();
            }
        }

        // --- RECOMPENSAS ACUMULADAS ---
        if (LevelManager.instance != null)
        {
            for (int i = faseAnterior; i < faseActual && i < valorPorFase.Length; i++)
            {
                int monedasADar = GetCoinsForPhase(i);
                LevelManager.instance.MostrarPuntosVoladores(transform.position, monedasADar);
                SpawnFloatingMoney(monedasADar);

                if (i < evolucionesEntreFases.Length) evolucionesEntreFases[i]++;
            }
        }

        // Seguridad: no sobrepasar el límite
        if (faseActual >= fasesSprites.Length)
        {
            faseActual = fasesSprites.Length - 1; // Mantener el último índice válido
            BecomeInfected();
        }
        else
        {
            ActualizarVisualFase();
            IniciarCambioColor(FlashCambioFase());

            if (InfectionFeedback.instance != null)
                InfectionFeedback.instance.PlayPhaseChangeEffect(transform.position, originalColor);

            if (transformInfector != null && movementScript != null)
            {
                Vector2 dirEmpuje = (transform.position - transformInfector.position).normalized;
                movementScript.AplicarEmpuje(dirEmpuje, fuerzaRetroceso, fuerzaRotacion);
            }
        }

        if (!LevelManager.instance.timerStarted) LevelManager.instance.timerStarted = true;
    }


    void BecomeInfected()
    {
        alreadyInfected = true;

        gameObject.layer = LayerMask.NameToLayer("Infectado");

        // 1. Detener corrutinas de color previas para evitar conflictos
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);

        // 2. CAMBIO CLAVE: Asignar el sprite ANTES de cualquier otra lógica
        if (spriteInfectado != null)
        {
            spritePersona.sprite = spriteInfectado;
            spritePersona.color = infectedColor; // Color base por si la corrutina falla
        }
        if (instanciaBarraActual != null) Destroy(instanciaBarraActual);
        if (spriteInfectado != null)
        {
            spritePersona.sprite = spriteInfectado;
        }

        if (Guardado.instance != null && PopulationManager.instance != null)
        {
            // Supongamos que tu variable en Guardado se llama 'probabilidadSpawnAlInfectar' (0.0f a 1.0f)
            // Si usas 'spawnBaseOnMaxPhaseChance', cámbiala aquí:
            float chance = Guardado.instance.spawnBaseOnMaxPhaseChance;

            if (Random.value < chance)
            {
                // Llamamos al PopulationManager para que cree una nueva persona en posición aleatoria
                PopulationManager.instance.SpawnPersonAtBasePhase();
                Debug.Log("¡Suerte! Se ha generado una nueva persona tras la infección.");
            }
        }

        if (infectionBarCanvas != null)
            infectionBarCanvas.SetActive(false);

        if (InfectionFeedback.instance != null)
            InfectionFeedback.instance.PlayEffect(transform.position, Color.white, false);

        if (particulasDeFuego != null)
        {
            particulasDeFuego.Play();
        }

        if (LevelManager.instance != null)
            LevelManager.instance.RegisterInfection();

        // Empuje garantizado hacia fuera
        if (movementScript != null)
        {
            Vector2 dirEmpuje = Vector2.zero;

            if (transformInfector != null)
            {
                dirEmpuje = (Vector2)(transform.position - transformInfector.position);
            }

            // Si está demasiado cerca del centro o la dirección sale mal,
            // usamos una dirección aleatoria para asegurar el rebote
            if (dirEmpuje.sqrMagnitude < 0.0001f)
            {
                dirEmpuje = Random.insideUnitCircle.normalized;
            }
            else
            {
                dirEmpuje = dirEmpuje.normalized;
            }

            movementScript.AplicarEmpuje(dirEmpuje, fuerzaRetroceso, fuerzaRotacion);
        }

        transform.localScale = transform.localScale * 1.125f;

        IniciarCambioColor(InfectionColorSequence());
    }
    public int GetMaxFaseIndex()
    {
        if (fasesSprites == null || fasesSprites.Length == 0) return 0;
        return fasesSprites.Length - 1;
    }
    void ActualizarVisualFase()
    {
        if (alreadyInfected) return;

        if (faseActual < fasesSprites.Length)
        {

            spritePersona.sprite = fasesSprites[faseActual];
            // Solo aplicar color si el índice existe en el array de colores
            if (coloresFases != null && faseActual < coloresFases.Length)
                spritePersona.color = coloresFases[faseActual];
            ActualizarInstanciaBarraFalsa();

            for (int i = 0; i < fillingBarImages.Length; i++)
            {
                if (fillingBarImages[i] != null && i < contornosFases.Length)
                {
                    // IMPORTANTE: Aquí decidimos a quién le damos el sprite de la fase
                    // Si quieres que el relleno (hijo) sea el que cambie de forma:
                    if (fillingBarImages[i].transform.childCount > 0)
                    {
                        Image barraInterna = fillingBarImages[i].transform.GetChild(0).GetComponent<Image>();
                        if (barraInterna != null)
                        {
                            // El hijo recibe el sprite de la forma (Triángulo, Círculo, etc.)
                            barraInterna.sprite = contornosFases[i];
                        }
                    }
                }
            }
        }
    }
    public float ObtenerDañoTotal()
    {
        // 1. Daño base según la forma
        float dañoBase = (faseActual < dañoPorFasePredeterminado.Length) ? dañoPorFasePredeterminado[faseActual] : 1f;

        // 2. Sumar la mejora específica de esta fase
        int bonoHabilidad = 0;
        if (Guardado.instance != null)
        {
            switch (faseActual)
            {
                case 0: bonoHabilidad = Guardado.instance.dañoExtraHexagono; break;  // HEX
                case 1: bonoHabilidad = Guardado.instance.dañoExtraPentagono; break; // PENT
                case 2: bonoHabilidad = Guardado.instance.dañoExtraCuadrado; break;  // CUAD
                case 3: bonoHabilidad = Guardado.instance.dañoExtraTriangulo; break; // TRI
                case 4: bonoHabilidad = Guardado.instance.dañoExtraCirculo; break;   // CIRC
            }
        }

        return dañoBase + bonoHabilidad;
    }

    private IEnumerator FlashCambioFase()
    {
        // Guardamos el color que debería tener según su fase actual
        Color colorObjetivo = (faseActual < coloresFases.Length) ? coloresFases[faseActual] : originalColor;

        spritePersona.color = Color.white;
        yield return new WaitForSeconds(0.05f);

        // Forzamos que vuelva al color de la fase, no al blanco
        spritePersona.color = colorObjetivo;
    }

   

    void Desaparecer()
    {
     //InfectionFeedback.instance.PlayBasicImpactEffect(transform.position, Color.white,false);

       gameObject.SetActive(false);
    }




    private IEnumerator InfectionColorSequence()
    {
        spritePersona.color = Color.white;
        yield return new WaitForSeconds(flashDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            // Usamos una variable temporal para evitar saltos visuales
            spritePersona.color = Color.Lerp(Color.white, infectedColor, elapsed / fadeDuration);
            yield return null;
        }

        // SEGURO: Forzamos el color final
        spritePersona.color = infectedColor;
        colorCoroutine = null;
    }
    public void IntentarAvanzarFasePorChoque(TipoChoque tipo)
    {
        if (alreadyInfected) return;

        int faseAnterior = faseActual;

        if (faseActual < fasesSprites.Length - 1)
        {
            currentInfectionTime = 0f;
            faseActual++;

            if (faseAnterior < evolucionesPorChoque.Length)
            {
                if (tipo == TipoChoque.Wall)
                {
                    evolucionesPorChoque[faseAnterior]++;
                }
                else if (tipo == TipoChoque.Carambola)
                {
                    evolucionesCarambola[faseAnterior]++;
                }
            }

            if (LevelManager.instance != null && faseAnterior < valorPorFase.Length)
            {
                int monedasADar = GetCoinsForPhase(faseAnterior);
                LevelManager.instance.MostrarPuntosVoladores(transform.position, monedasADar);
                SpawnFloatingMoney(monedasADar);
            }

            if (InfectionFeedback.instance != null)
                InfectionFeedback.instance.PlayPhaseChangeEffect(transform.position, originalColor);

            ActualizarVisualFase();

            if (this.gameObject.activeInHierarchy)
            {
                StartCoroutine(FlashCambioFase());
            }
            else
            {
                Debug.LogWarning("Se intentó iniciar corrutina en " + name + " pero está inactivo.");
            }
        }
        else
        {
         
            BecomeInfected();
        }
    }





    private void IniciarCambioColor(IEnumerator nuevaCorrutina)
    {
        // 1. Verificamos si el componente está habilitado y el GameObject activo en la jerarquía
        if (this.gameObject.activeInHierarchy)
        {
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            colorCoroutine = StartCoroutine(nuevaCorrutina);
        }
        else
        {
            // Opcional: Si el objeto está desactivado, simplemente aplicamos el color final 
            // directamente sin animación para evitar que se quede con un color extraño.
            Debug.LogWarning($"No se pudo iniciar corrutina en {gameObject.name} porque está inactivo.");
        }
    }
    private void SpawnFloatingMoney(int cantidad)
    {
        if (TextPooler.Instance != null)
        {
            // 1. Pedimos el objeto al Pooler
            GameObject obj = TextPooler.Instance.SpawnText(transform.position, "+" + cantidad.ToString());

            // 2. Configuramos lo que faltaba usando 'obj' (antes textObj)
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 32767;
            }

            TMPro.TextMeshPro tm = obj.GetComponent<TMPro.TextMeshPro>();
            if (tm != null)
            {
                tm.color = Color.white;
            }
        }
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
        if (instanciaBarraActual != null) Destroy(instanciaBarraActual);

        if (prefabsBarraFalsa != null && faseActual < prefabsBarraFalsa.Length)
        {
            if (prefabsBarraFalsa[faseActual] != null)
            {
                instanciaBarraActual = Instantiate(prefabsBarraFalsa[faseActual], transform);
                // Si ya estamos en la zona, la barra falsa nace desactivada
                instanciaBarraActual.SetActive(!isInsideZone);
            }
        }
    }
}