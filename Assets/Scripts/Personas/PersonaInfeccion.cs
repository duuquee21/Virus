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

    void Start()
    {
        movementScript = GetComponent<Movement>();
        rb = GetComponent<Rigidbody2D>(); // Asignación del Rigidbody

        if (spritePersona == null) spritePersona = GetComponent<SpriteRenderer>();
        originalColor = spritePersona.color;

        if (infectionBarCanvas != null) infectionBarCanvas.SetActive(true);

        ActualizarVisualFase();
        ActualizarProgresoBarras(0f);
        GameObject jugadorVirus = GameObject.FindGameObjectWithTag("Virus");
        if (jugadorVirus != null)
        {
            managerAnimacion = jugadorVirus.GetComponent<ManagerAnimacionJugador>();
        }
    }
    public enum TipoChoque
    {
        Wall,
        Carambola
    }

    void Update()
    {
        if (alreadyInfected) return;

        float resistenciaActual = (faseActual < resistenciaPorFase.Length) ? resistenciaPorFase[faseActual] : 1f;
        float tiempoNecesarioEstaFase = globalInfectTime * resistenciaActual;

        if (isInsideZone)
        {
            // CASO A: Jugador Jugable (Infección normal)
            if (managerAnimacion == null || managerAnimacion.playable)
            {
                if (positionSaved) { transform.position = initialPosition; positionSaved = false; }

                float multiplier = 1f;
                if (Guardado.instance != null) multiplier = Guardado.instance.infectSpeedMultiplier;
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
                fillingBarImages[i].gameObject.SetActive(true);
                fillingBarImages[i].fillAmount = inverseProgress;
                fillingBarImages[i].color = Color.Lerp(colorCargaFinal, colorCargaInicial, inverseProgress);
            }
            else
            {
                fillingBarImages[i].gameObject.SetActive(false);
            }
        }
    }

    public void EstablecerFaseDirecta(int fase)
    {
        faseActual = fase;
        currentInfectionTime = 0f;
        ActualizarVisualFase();
        ActualizarProgresoBarras(0f);
    }

    void IntentarAvanzarFase()
    {
        // 1. SEGURIDAD: Si ya está infectado o ya pasó el límite, ignoramos cualquier llamada extra
        if (alreadyInfected || faseActual >= fasesSprites.Length) return;

        int faseAnterior = faseActual;
        currentInfectionTime = 0f; // Reset inmediato del progreso para evitar re-entrada
        faseActual++;

        // 2. SISTEMA DE RECOMPENSAS (Unificado aquí)
        if (LevelManager.instance != null && faseAnterior < valorPorFase.Length)
        {
            int monedasADar = valorPorFase[faseAnterior];
            LevelManager.instance.MostrarPuntosVoladores(transform.position, monedasADar);
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
    }
    void BecomeInfected()
    {
        // Si ya entramos aquí por IntentarAvanzarFase, marcamos como infectado
        alreadyInfected = true;

        if (infectionBarCanvas != null) infectionBarCanvas.SetActive(false);

        if (InfectionFeedback.instance != null)
            InfectionFeedback.instance.PlayEffect(transform.position, originalColor);

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
        // --------------------------------------------

        IniciarCambioColor(InfectionColorSequence());
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
                    fillingBarImages[i].sprite = contornosFases[i];
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
                case 0: bonoHabilidad = Guardado.instance.dañoExtraCirculo; break;
                case 1: bonoHabilidad = Guardado.instance.dañoExtraTriangulo; break;
                case 2: bonoHabilidad = Guardado.instance.dañoExtraCuadrado; break;
                case 3: bonoHabilidad = Guardado.instance.dañoExtraPentagono; break;
                case 4: bonoHabilidad = Guardado.instance.dañoExtraHexagono; break;
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
        if (other.CompareTag("Coral"))
        {
            Movement mov = GetComponent<Movement>();

            if (mov != null && mov.EstaEmpujado())
            {
                Desaparecer();
            }
        }
        
        {
            Debug.Log($"[TRIGGER] {gameObject.name} tocó {other.name} | Tag: {other.tag} | alreadyInfected: {alreadyInfected}");


        }

    }

    void Desaparecer()
    {
        // Opcional: efecto visual antes
        if (InfectionFeedback.instance != null)
        {
            InfectionFeedback.instance.PlayBasicImpactEffect(transform.position, Color.red, true);
        }

        //Destroy(gameObject);
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
                int monedasADar = valorPorFase[faseAnterior];
                LevelManager.instance.MostrarPuntosVoladores(transform.position, monedasADar);
            }

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


    public bool EsFaseMaxima() => faseActual >= fasesSprites.Length - 1;
}