using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PersonaInfeccion : MonoBehaviour
{
    public static float globalInfectTime = 2f;

    [Header("Sprites de Evolución")]
    public Sprite[] fasesSprites;
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

    public float shakeIntensity = 0.05f;
    private Vector3 originalPosition;
    private Vector3 initialPosition;
    private bool positionSaved = false;




    // --- REFERENCIA QUE FALTABA ---
    private Rigidbody2D rb;

    public ParticleSystem particulasDeFuego;


    public GameObject floatingTextPrefab;


    [Header("Ajustes del Rastro")]
    private TrailRenderer trail1; // El que está en el mismo objeto
    public TrailRenderer trail2;  // El que asignarás desde el Inspector
    private float trailTimer = 0f;
    private bool trailActivatedThisCycle = false;

    void Start()
    {
        movementScript = GetComponent<Movement>();
        rb = GetComponent<Rigidbody2D>(); // Asignación del Rigidbody

        if (spritePersona == null) spritePersona = GetComponent<SpriteRenderer>();
        originalColor = spritePersona.color;

        if (infectionBarCanvas != null) infectionBarCanvas.SetActive(true);

        trail1 = GetComponent<TrailRenderer>();
        // Apagamos ambos al inicio
        if (trail1 != null) trail1.emitting = false;
        if (trail2 != null) trail2.emitting = false;

        ActualizarVisualFase();
        ActualizarProgresoBarras(0f);
        GameObject jugadorVirus = GameObject.FindGameObjectWithTag("Virus");
        if (jugadorVirus != null)
        {
            managerAnimacion = jugadorVirus.GetComponent<ManagerAnimacionJugador>();
        }
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
    }

    void OnDisable()
    {
        // Muy importante desuscribirse para evitar errores de memoria
        LevelTransitioner.OnTransitionStart -= Desaparecer;
    }
    public enum TipoChoque
    {
        Wall,
        Carambola
    }

    void Update()
    {
        if (alreadyInfected)
        {
            SetTrailsEmitting(false); // Cambia trail.emitting = false por esto
            return;
        }

        float resistenciaActual = (faseActual < resistenciaPorFase.Length) ? resistenciaPorFase[faseActual] : 1f;
        float tiempoNecesarioEstaFase = globalInfectTime * resistenciaActual;

        if (isInsideZone)
        {
           
            // CASO A: Jugador Jugable (Infección normal)
            if (managerAnimacion == null || managerAnimacion.playable)
            {
                if (positionSaved) { transform.position = initialPosition; positionSaved = false; }

                float multiplier = 1f;

                if (Guardado.instance != null)
                {
                    // Multiplicador global (lo que ya tenías)
                    multiplier = Guardado.instance.infectSpeedMultiplier;

                    // Multiplicador por fase (nuevo)
                    if (Guardado.instance.infectSpeedPerPhase != null &&
                        faseActual < Guardado.instance.infectSpeedPerPhase.Length)
                    {
                        multiplier *= Guardado.instance.infectSpeedPerPhase[faseActual];
                    }
                }

                currentInfectionTime += Time.deltaTime * multiplier;
            }
            // CASO B: Jugador NO Jugable (Atracción al centro)
            else
            {
                // 1. Calculamos la distancia al centro de la pantalla
                Vector3 centroMundo = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
                centroMundo.z = 0; // Asegurar que estamos en el mismo plano 2D
                float distanciaAlCentro = Vector2.Distance(transform.position, (Vector2)centroMundo);

                // 2. Solo vibramos si ya estamos "cerca" del centro (ej. radio de 0.5)
                if (distanciaAlCentro < 0.5f)
                {
                    if (!positionSaved)
                    {
                        initialPosition = transform.position;
                        positionSaved = true;
                    }

                    float currentShake = 0.05f;
                    float offsetX = Random.Range(-1f, 1f) * currentShake;
                    float offsetY = Random.Range(-1f, 1f) * currentShake;

                    transform.position = initialPosition + new Vector3(offsetX, offsetY, 0);
                }
                else
                {
                    // Si aún no llegamos al centro exacto, reseteamos la posición base para que el Movement.cs 
                    // pueda seguir moviéndolo suavemente sin saltos
                    if (positionSaved) { transform.position = initialPosition; positionSaved = false; }
                }
            }
        }
        else
        {
            // Fuera de zona: Reset
            if (positionSaved) { transform.position = initialPosition; positionSaved = false; }
            currentInfectionTime -= Time.deltaTime * 2f;
        }

        // ... resto del código de barras y avance de fase ...
        currentInfectionTime = Mathf.Clamp(currentInfectionTime, 0f, tiempoNecesarioEstaFase);
        float progress = currentInfectionTime / tiempoNecesarioEstaFase;
        ActualizarProgresoBarras(progress);

        if (currentInfectionTime >= tiempoNecesarioEstaFase) IntentarAvanzarFase();
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

    public static void ResetDaños()
    {
        dañoTotalZona = 0f;
        dañoTotalChoque = 0f;
        dañoTotalCarambola = 0f;

        for (int i = 0; i < 5; i++)
        {
            dañoZonaPorFase[i] = 0f;
            dañoChoquePorFase[i] = 0f;
            dañoCarambolaPorFase[i] = 0f;
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

   public void IntentarAvanzarFase()
    {
        // 1. SEGURIDAD: Si ya está infectado o ya pasó el límite, ignoramos cualquier llamada extra
        if (alreadyInfected || faseActual >= fasesSprites.Length) return;

        int faseAnterior = faseActual;
        currentInfectionTime = 0f; // Reset inmediato del progreso para evitar re-entrada
        StartCoroutine(ActivarRastroTemporal());
        // 1.5 HABILIDAD: probabilidad de subir 2 fases en vez de 1
        int steps = 1;
        if (Guardado.instance != null)
        {
            float chanceDouble = Guardado.instance.doubleUpgradeChance; // 0..1 (0.05, 0.10, 0.15, 0.20, 0.25)
            if (chanceDouble > 0f && Random.value < chanceDouble)
                steps = 2;
        }

        faseActual += steps;

        // Seguridad: no sobrepasar el final (el final válido es fasesSprites.Length)
        if (faseActual > fasesSprites.Length)
            faseActual = fasesSprites.Length;

        // 2. SISTEMA DE RECOMPENSAS (Unificado aquí) - usa faseAnterior (correcto)
        if (LevelManager.instance != null && faseAnterior < valorPorFase.Length)
        {
            int monedasADar = GetCoinsForPhase(faseAnterior);
            LevelManager.instance.MostrarPuntosVoladores(transform.position, monedasADar);
            SpawnFloatingMoney(monedasADar); // <--- AÑADIR ESTA LÍNEA
        }

        // 2.5 HABILIDAD: probabilidad variable de sumar +1s al tiempo al subir fase por zona
        if (Guardado.instance != null)
        {
            float chanceTime = Guardado.instance.addTimeOnPhaseChance; // 0..1 (0.10f, 0.15f, 0.20f, 0.25f)
            if (chanceTime > 0f && Random.value < chanceTime)
            {
                if (LevelManager.instance != null)
                    LevelManager.instance.AddTimeToCurrentTimer(1f);
            }
        }

        // 3. ESTADÍSTICAS
        if (faseAnterior < evolucionesEntreFases.Length)
        {
            evolucionesEntreFases[faseAnterior]++;
            Debug.Log($"[EVOLUCIÓN] {gameObject.name}: {faseAnterior} -> {faseActual}");
        }

        // 4. CAMBIO VISUAL O FINALIZACIÓN
        if (faseActual < fasesSprites.Length)
        {
            // Aún no es el final: Cambiamos sprite y damos feedback de empuje
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
        else
        {
            // ES EL FINAL: Infección completa
            BecomeInfected();
        }

        if(!LevelManager.instance.timerStarted)
        {
            LevelManager.instance.timerStarted= true;
        }
    }
    void BecomeInfected()
    {
        // Si ya entramos aquí por IntentarAvanzarFase, marcamos como infectado
        alreadyInfected = true;

        if (infectionBarCanvas != null) infectionBarCanvas.SetActive(false);

        if (InfectionFeedback.instance != null)
            InfectionFeedback.instance.PlayEffect(transform.position, Color.white,false);

        particulasDeFuego?.Play();

        if (LevelManager.instance != null)
        {
            LevelManager.instance.RegisterInfection();
        }

        // --- NUEVA LÓGICA DE EMPUJE AL INFECTARSE ---
        if (transformInfector != null && movementScript != null)
        {
            // Calculamos la dirección desde el infector hacia la persona
            Vector2 dirEmpuje = (transform.position - transformInfector.position).normalized;
            // Aplicamos la misma fuerza que usas en el cambio de fase
            movementScript.AplicarEmpuje(dirEmpuje, fuerzaRetroceso, fuerzaRotacion);
        }

        transform.localScale =transform.localScale * 1.125f; // Aumentamos el tamaño para destacar la infección
        // --------------------------------------------

        IniciarCambioColor(InfectionColorSequence());
    }
    public int GetMaxFaseIndex()
    {
        if (fasesSprites == null || fasesSprites.Length == 0) return 0;
        return fasesSprites.Length - 1;
    }
    void ActualizarVisualFase()
    {
        if (faseActual < fasesSprites.Length)
        {
            spritePersona.sprite = fasesSprites[faseActual];
            if (faseActual < coloresFases.Length) spritePersona.color = coloresFases[faseActual];

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

                    // Nota: El Padre NO cambia de sprite aquí para mantener su fondo/outline original
                    // que configuraste manualmente en el Inspector.
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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!alreadyInfected && other.CompareTag("InfectionZone"))
        {
            isInsideZone = true;
            transformInfector = other.transform;
        }

        // 🔴 NUEVA CONDICIÓN
      
        {
            Debug.Log($"[TRIGGER] {gameObject.name} tocó {other.name} | Tag: {other.tag} | alreadyInfected: {alreadyInfected}");


        }

    }

    void Desaparecer()
    {
     InfectionFeedback.instance.PlayBasicImpactEffect(transform.position, Color.white,false);

        Destroy(gameObject);
    }




    void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("InfectionZone")) isInsideZone = false; }

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

            // -------- ESTADÍSTICAS --------
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

            // -------- RECOMPENSA UNIFICADA --------
            if (LevelManager.instance != null && faseAnterior < valorPorFase.Length)
            {
                int monedasADar = GetCoinsForPhase(faseAnterior);
                LevelManager.instance.MostrarPuntosVoladores(transform.position, monedasADar);
                SpawnFloatingMoney(monedasADar); // <--- AÑADIR ESTA LÍNEA
            }
            if (InfectionFeedback.instance != null)
                InfectionFeedback.instance.PlayPhaseChangeEffect(transform.position, originalColor);


            ActualizarVisualFase();
            StartCoroutine(FlashCambioFase());
        }
        else
        {
            BecomeInfected();
        }
    }



    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"[COLISION] {gameObject.name} chocó con {collision.collider.name} | Tag: {collision.collider.tag} | alreadyInfected: {alreadyInfected}");

        // --- Habilidad nueva ---
        if (alreadyInfected &&
            Guardado.instance != null &&
            Guardado.instance.destroyCoralOnInfectedImpact &&
            collision.collider.CompareTag("Coral"))
        {
            Debug.Log("<color=red>[IMPACTO CORAL]</color> Destruyendo Coral y Bola Blanca");

            //Destroy(collision.collider.gameObject);
            //Destroy(gameObject);
            return;
        }

        // --- Pared infectiva ---
        if (!collision.collider.CompareTag("Wall")) return;

        if (Guardado.instance == null) return;
        if (Guardado.instance.nivelParedInfectiva <= 0) return;

        if (Guardado.instance.nivelParedInfectiva > faseActual)
        {
            Debug.Log("<color=blue>[PARED INFECTIVA]</color> Evolución por choque con pared");
            IntentarAvanzarFasePorChoque(TipoChoque.Wall);
        }
    }


    private void IniciarCambioColor(IEnumerator nuevaCorrutina)
    {
        if (colorCoroutine != null) StopCoroutine(colorCoroutine);
        colorCoroutine = StartCoroutine(nuevaCorrutina);
    }
    private void SpawnFloatingMoney(int cantidad)
    {
        if (floatingTextPrefab != null)
        {
            GameObject textObj = Instantiate(floatingTextPrefab, transform.position, Quaternion.identity);

            // Accedemos al MeshRenderer para cambiar el Order in Layer
            MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                // 32767 es el valor máximo permitido en el Sorting Order de Unity
                meshRenderer.sortingOrder = 32767;
            }

            // Accedemos al componente de texto para cambiar el color a negro
            TMPro.TextMeshPro tm = textObj.GetComponent<TMPro.TextMeshPro>();
            if (tm != null)
            {
                tm.color = Color.black;
                tm.text = "+" + cantidad.ToString(); // También puedes asignarlo aquí directamente
            }

            FloatingText ft = textObj.GetComponent<FloatingText>();
            if (ft != null) ft.SetText("+" + cantidad.ToString());
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
}