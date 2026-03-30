using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelTransitioner : MonoBehaviour
{
    [Header("Configuración de Giro")]
    public float aceleracion = 1500f;
    public float frenado = 1200f;
    public float velocidadMaxima = 3500f;

    [Header("Configuración de Escala")]
    public float escalaMinima = 0.6f;
    public float suavizadoEscala = 8f;

    [Header("Configuración de Impacto Seco")]
    public float intensidadImpacto = 0.5f;
    public float velocidadRetorno = 5f;

    [Header("Configuración de Zoom Cámara")]
    public float zoomMaximo = 7f;
    public float velocidadZoomIn = 5f;
    public float velocidadZoomOut = 3f;

    [Header("Referencias")]
    public ManualSetCycler manualSetCycler;

    public static event Action<float> OnImpactShake;
    public static event Action OnTransitionStart;

    private float velocidadActual = 0f;
    private Vector3 escalaOriginal = Vector3.one;
    private Camera mainCam;
    private Transform camTransform;
    private float zoomOriginal;
    private PlanetCrontrollator cachedPlaneta;

    private PopulationManager popManager;
    private LevelManager lm;

    [Header("Configuración de Shader")]
    public Material materialFondo;
    private readonly string vortexProp = "_VortexStrength";

    [Header("Configuración Final (Último Nivel)")]
    public float duracionVibracionPrevia = 1.5f;
    public float intensidadVibracionPrevia = 0.2f;
    public float duracionColapsoFinal = 0.33f;
    public float velocidadGiroFinal = -1600f;

    [Header("Efecto Final Spawn")]
    public GameObject spritePulsoPrefab;
    public int numeroPulsos = 3;
    public float escalaMaxPulso = 1.3f;
    public float velocidadPulso = 8f;

    [Header("Spawn Personas Final")]
    public int personasPorPulso = 10;

    [Header("UI Final")]
    public GameObject panelFinal; // Arrastra aquí tu Panel de Unity UI
    public float delayAntesDePanel = 0.5f; // Tiempo de espera tras la explosión

    [Header("Referencia al Fondo")]
    public RectTransform fondoNebula;
    private Vector3 escalaOriginalFondoNebula; // Para guardar TU escala del inspector

    private Dictionary<Transform, Vector3> escalasIniciales = new Dictionary<Transform, Vector3>();
    private GameObject pulsoInstanciado;
    void Awake()
    {
        mainCam = Camera.main;
        if (mainCam != null)
        {
            camTransform = mainCam.transform;
            zoomOriginal = mainCam.orthographicSize;
        }

        Image img = GetComponentInChildren<Image>();
        if (img != null && img.material != null)
        {
            materialFondo = img.material;
            materialFondo.SetFloat(vortexProp, 0f);
        }
        if (lm != null && lm.mapList != null)
        {
            foreach (GameObject map in lm.mapList)
            {
                if (map != null)
                {
                    // Guardamos la escala que pusiste en el Inspector
                    escalasIniciales[map.transform] = map.transform.localScale;
                }
            }
        }
        if (fondoNebula != null)
        {
            escalaOriginalFondoNebula = fondoNebula.localScale; // Memoriza tu escala del Inspector
        }
        lm = LevelManager.instance;
        popManager = FindFirstObjectByType<PopulationManager>();

        RefreshCurrentPlanet();
        ResetPlanetRotation();
    }

    private void RefreshCurrentPlanet()
    {
        cachedPlaneta = null;
        if (lm == null) lm = LevelManager.instance;

        if (lm != null && lm.mapList != null && lm.mapList.Length > 0)
        {
            for (int i = 0; i < lm.mapList.Length; i++)
            {
                GameObject map = lm.mapList[i];
                if (map != null && map.activeInHierarchy)
                {
                    cachedPlaneta = map.GetComponentInChildren<PlanetCrontrollator>(true);
                    if (cachedPlaneta != null) return;
                }
            }
        }
        cachedPlaneta = FindFirstObjectByType<PlanetCrontrollator>(FindObjectsInactive.Include);
    }

    private void CapturarEscalas()
    {
        if (escalasIniciales.Count > 0) return; // Si ya las tenemos, no hacemos nada
        if (lm == null) lm = LevelManager.instance;
        if (lm?.mapList == null) return;

        foreach (GameObject map in lm.mapList)
        {
            if (map != null && !escalasIniciales.ContainsKey(map.transform))
                escalasIniciales[map.transform] = map.transform.localScale;
        }
    }
    private void ResetPlanetRotation()
    {
        if (cachedPlaneta != null)
            cachedPlaneta.transform.rotation = Quaternion.identity;
    }

    public void StartLevelTransition()
    {
        StopAllCoroutines();
        StartCoroutine(ExecuteFullTransition());
    }

    private IEnumerator ExecuteFullTransition()
    {
        if (lm == null) lm = LevelManager.instance;
        if (popManager == null) popManager = FindFirstObjectByType<PopulationManager>();

        RefreshCurrentPlanet();

        // --- 1. CAPTURAR ESTADO ORIGINAL DEL INSPECTOR ---
        Quaternion rotacionOriginalInspector = Quaternion.identity;
        int currentIdx = 0;

        if (lm != null && lm.mapList != null)
        {
            for (int i = 0; i < lm.mapList.Length; i++)
            {
                if (lm.mapList[i] != null && lm.mapList[i].activeInHierarchy)
                {
                    currentIdx = i;
                    rotacionOriginalInspector = lm.mapList[i].transform.localRotation;
                    break;
                }
            }
        }

        // --- 🛑 DESACTIVAR BARRA DE VIDA INMEDIATAMENTE ---
        // Se oculta en cuanto empieza el giro de victoria/derrota
        if (cachedPlaneta != null)
        {
            cachedPlaneta.SetVisibleUI(false);
        }

        if (lm != null)
        {
            lm.isTransitioning = true;
            lm.isGameActive = false;
            if (lm.virusMovementScript != null) lm.virusMovementScript.enabled = false;
        }

        if (cachedPlaneta != null)
        {
            cachedPlaneta.isInvulnerable = true;
            cachedPlaneta.ClearPendingDamage();
        }

        bool esUltimoNivel = (lm != null && lm.mapList != null && currentIdx >= lm.mapList.Length - 1);
        GameObject mapaVisual = lm.mapList[currentIdx];
        Transform mapaTransform = mapaVisual != null ? mapaVisual.transform : null;

        if (mapaVisual != null && mapaTransform != null)
        {
            escalaOriginal = mapaTransform.localScale;
            if (manualSetCycler != null)
                manualSetCycler.TriggerTransition(velocidadMaxima / aceleracion, velocidadMaxima / frenado);
        }

        OnTransitionStart?.Invoke();
        Vector3 escalaObjetivoMin = escalaOriginal * escalaMinima;

        // FASE 1: ACELERAR Y ENCOGER
        while (velocidadActual < velocidadMaxima)
        {
            float dt = Time.deltaTime;
            velocidadActual += aceleracion * dt;
            float progresoAcel = velocidadActual / velocidadMaxima;

            if (materialFondo != null) materialFondo.SetFloat(vortexProp, progresoAcel * 75f);
            if (mainCam != null) mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, zoomMaximo, velocidadZoomIn * dt);

            if (mapaTransform != null)
            {
                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
                mapaTransform.localScale = Vector3.Lerp(mapaTransform.localScale, escalaObjetivoMin, suavizadoEscala * dt);
            }
            yield return null;
        }

        // FASE DE FRENADO (Versión Demo con CRECIMIENTO GIGANTE)
        if (!esUltimoNivel)
        {
            if (popManager != null) popManager.ClearAllPersonas();

            // FASE 3: ESPERA PARA FRENADO (Apunta a la rotación del Inspector)
            float distanciaFrenado = (velocidadActual * velocidadActual) / (2f * frenado);
            while (true)
            {
                float dt = Time.deltaTime;
                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);

                float anguloActual = mapaTransform.localEulerAngles.z;
                float anguloObjetivo = rotacionOriginalInspector.eulerAngles.z;
                float anguloFinalPredecido = (anguloActual + distanciaFrenado) % 360f;

                if (Mathf.Abs(anguloFinalPredecido - anguloObjetivo) < (velocidadActual * dt) || velocidadActual < 10f) break;
                yield return null;
            }

            // VARIABLES PARA EL CRECIMIENTO
            float velocidadAlEmpezarFrenado = velocidadActual;
            Vector3 escalaAlEmpezarFrenado = mapaTransform.localScale;
            float multiplicadorGigante = 10f;
            Vector3 escalaGiganteObjetivo = escalaOriginal * multiplicadorGigante;

            // FASE 4: FRENAR + CRECER GIGANTE
            while (velocidadActual > 0.1f)
            {
                float dt = Time.deltaTime;
                velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, frenado * dt);
                float progresoFrenado = 1f - (velocidadActual / velocidadAlEmpezarFrenado);

                if (materialFondo != null) materialFondo.SetFloat(vortexProp, (1f - progresoFrenado) * 75f);
                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);

                if (lm != null && lm.esVersionDemo)
                {
                    float curvaCrecimiento = progresoFrenado * progresoFrenado;
                    mapaTransform.localScale = Vector3.Lerp(escalaAlEmpezarFrenado, escalaGiganteObjetivo, curvaCrecimiento);
                }
                else
                {
                    mapaTransform.localScale = Vector3.Lerp(mapaTransform.localScale, escalaOriginal, suavizadoEscala * dt);
                }

                if (mainCam != null) mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, zoomOriginal, velocidadZoomOut * dt);
                yield return null;
            }

            // AJUSTE FINAL DE ROTACIÓN
            mapaTransform.localRotation = rotacionOriginalInspector;

            // 🛑 LÓGICA FINAL DE VERSIÓN DEMO 🛑
            if (lm != null && lm.esVersionDemo)
            {
                mapaTransform.localScale = escalaGiganteObjetivo;
                yield return new WaitForSecondsRealtime(0.3f);

                // --- DESACTIVAR EL PLANETA ANTES DE MOSTRAR EL PANEL ---
                if (mapaVisual != null) mapaVisual.SetActive(false);

                lm.MostrarFinDeDemo();

                // RESTAURAR DATOS (Invisible tras el panel)
                mapaTransform.localScale = escalaOriginal;
                if (mapaVisual != null) mapaVisual.SetActive(true);
                if (cachedPlaneta != null) cachedPlaneta.SetVisibleUI(true); // Reactivar para el futuro

                yield break;
            }

            // --- LÓGICA DE CAMBIO DE MAPA (Versión Completa) ---
            int nextMap = currentIdx + 1;
            lm.ActivateMap(nextMap);
            RefreshCurrentPlanet();
            if (cachedPlaneta != null)
            {
                cachedPlaneta.ResetHealthToInitial();
                cachedPlaneta.SetVisibleUI(true); // Reactivar UI para el nuevo planeta
            }
            lm.currentSessionInfected = 0;
            yield return StartCoroutine(DryImpactShake());
        }
        else
        {
            // FASE 5: COLAPSO FINAL (Último nivel real)
            // ... (resto de tu código de colapso)
            yield return StartCoroutine(DryImpactShake());
            yield return StartCoroutine(FinalPulseAndSpawn());
        }

        // LIMPIEZA FINAL
        RefreshCurrentPlanet();
        if (lm != null)
        {
            lm.isGameActive = !esUltimoNivel;
            lm.isTransitioning = false;
            if (lm.virusMovementScript != null) lm.virusMovementScript.enabled = !esUltimoNivel;
        }
        Time.timeScale = 1f;
        velocidadActual = 0f;
        if (materialFondo != null) materialFondo.SetFloat(vortexProp, 0f);
    }

    private IEnumerator DryImpactShake()
    {
        if (GameSettings.instance == null || !GameSettings.instance.shakeEnabled || camTransform == null) yield break;
        Vector3 posOriginal = camTransform.localPosition;
        float fuerzaActual = intensidadImpacto;
        while (fuerzaActual > 0.01f)
        {
            camTransform.localPosition = posOriginal + (Vector3)(UnityEngine.Random.insideUnitCircle * fuerzaActual);
            fuerzaActual = Mathf.Lerp(fuerzaActual, 0f, Time.deltaTime * velocidadRetorno);
            yield return null;
        }
        camTransform.localPosition = posOriginal;
    }

    private IEnumerator FinalPulseAndSpawn()
    {
        if (spritePulsoPrefab == null || popManager == null)
            yield break;

        Camera cam = Camera.main;
        if (cam == null) yield break;

        // --- CONFIGURACIÓN INICIAL ---
        Vector3 posOriginalCam = cam.transform.position;
        float zoomOriginal = cam.orthographicSize;
        Vector3 centroEfecto = posOriginalCam;
        centroEfecto.z = 0f;

        GameObject pulso = Instantiate(spritePulsoPrefab, centroEfecto, Quaternion.identity);
        SpriteRenderer srPulso = pulso.GetComponent<SpriteRenderer>(); // Para el fade final
        pulso.transform.localScale = Vector3.zero;

        // --- 2. CICLO DE PULSOS E INSTANCIACIÓN ---
        for (int i = 0; i < numeroPulsos; i++)
        {
            // CRECER
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * velocidadPulso;
                pulso.transform.localScale = Vector3.one * Mathf.Lerp(0f, escalaMaxPulso, t);
                yield return null;
            }

            // REDUCIR (Implosión)
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * velocidadPulso;
                pulso.transform.localScale = Vector3.one * Mathf.Lerp(escalaMaxPulso, 0f, t);
                yield return null;
            }

         

          
        }

        // --- 3. PREPARACIÓN PARA LA EXPLOSIÓN FINAL ---
        yield return new WaitForSeconds(0.33f);

        // --- 4. EXPANSIÓN FINAL Y ZOOM OUT ---
        float tFinal = 0f;
        float escalaObjetivo = escalaMaxPulso * 25f; // Aumentado para asegurar que cubra la pantalla
        float zoomObjetivo = zoomOriginal * 1.5f;

        while (tFinal < 1f)
        {
            tFinal += Time.deltaTime * (velocidadPulso / 3f);

            // Crecimiento de la mancha blanca
            pulso.transform.localScale = Vector3.one * Mathf.Lerp(0f, escalaObjetivo, tFinal);

            // Zoom Out fluido
            cam.orthographicSize = Mathf.Lerp(zoomOriginal, zoomObjetivo, tFinal);

            yield return null;
        }

        pulso.transform.localScale = Vector3.one * escalaObjetivo;
        cam.orthographicSize = zoomObjetivo;

        // --- 5. DESVANECIMIENTO Y PANEL FINAL ---
        // Pequeña pausa con la pantalla en blanco para impacto emocional
        yield return new WaitForSeconds(0.5f);

        if (srPulso != null)
        {
            float tFade = 0f;
            Color colorInicial = srPulso.color;
            while (tFade < 1f)
            {
                tFade += Time.deltaTime * 2f; // Velocidad del fade out
                srPulso.color = Color.Lerp(colorInicial, new Color(colorInicial.r, colorInicial.g, colorInicial.b, 0f), tFade);
                yield return null;
            }
        }

        // Limpieza
        Destroy(pulso);

        if (fondoNebula != null)
        {
            fondoNebula.localScale = escalaOriginalFondoNebula;
            // Esto devuelve el fondo a su escala de Inspector (ej. 1,1,1 o la que tuvieras)
        }

        // Activamos el panel de victoria/final
        if (panelFinal != null)
        {
            panelFinal.SetActive(true);
        }
    }
    // Llama a esto para limpiar cualquier rastro de la animación final
public void ResetFinalLevelEffects()
    {
        StopAllCoroutines();

        // RESTAURAR FONDO CON TU ESCALA ORIGINAL
        if (fondoNebula != null)
        {
            fondoNebula.gameObject.SetActive(true);
            fondoNebula.localScale = escalaOriginalFondoNebula; // Usa la del Awake
        }

        CapturarEscalas();

        // --- RESTAURAR MAPAS (Hexágono, etc.) ---
        if (lm?.mapList != null)
        {
            foreach (GameObject map in lm.mapList)
            {
                if (map != null)
                {
                    map.SetActive(true);
                    if (escalasIniciales.TryGetValue(map.transform, out Vector3 original))
                    {
                        map.transform.localScale = original;
                        map.transform.rotation = Quaternion.identity;
                    }
                }
            }
        }

        // Reset de efectos visuales y cámara
        if (materialFondo != null) materialFondo.SetFloat(vortexProp, 0f);
        if (mainCam != null) mainCam.orthographicSize = zoomOriginal;

        Time.timeScale = 1f;
        velocidadActual = 0f;
    }
}