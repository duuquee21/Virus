using System;
using System.Collections;
using UnityEngine;

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

    public ManualSetCycler manualSetCycler;

    // Eventos
    public static event Action<float> OnImpactShake;
    public static event Action OnTransitionStart;

    // Caché de variables privadas
    private float velocidadActual = 0f;
    private Vector3 escalaOriginal = Vector3.one;
    private Camera mainCam;
    private Transform camTransform; // Cache de transform de la cámara
    private float zoomOriginal;
    private PlanetCrontrollator cachedPlaneta; // Cache del planeta
    private WaitForEndOfFrame frameWait = new WaitForEndOfFrame(); // Evita allocs en corrutinas

    void Awake()
    {
        mainCam = Camera.main;
        if (mainCam != null)
        {
            camTransform = mainCam.transform;
            zoomOriginal = mainCam.orthographicSize;
        }

        // Centralizamos el reseteo
        ResetPlanetRotation();
    }

    private void Start()
    {
        // Si el planeta no se encontró en Awake, lo buscamos aquí una sola vez
        if (cachedPlaneta == null) ResetPlanetRotation();
    }

    private void ResetPlanetRotation()
    {
        if (cachedPlaneta == null) cachedPlaneta = FindFirstObjectByType<PlanetCrontrollator>();
        if (cachedPlaneta != null) cachedPlaneta.transform.rotation = Quaternion.identity;
    }

    public void StartLevelTransition()
    {
        StopAllCoroutines(); // Seguridad para evitar corrutinas duplicadas
        StartCoroutine(ExecuteFullTransition());
    }

    private IEnumerator ExecuteFullTransition()
    {
        LevelManager lm = LevelManager.instance;
        lm.isGameActive = false;

        if (cachedPlaneta == null) cachedPlaneta = FindFirstObjectByType<PlanetCrontrollator>();
        if (cachedPlaneta != null) cachedPlaneta.isInvulnerable = true;

        int currentIdx = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        GameObject mapaVisual = lm.mapList[currentIdx];
        Transform mapaTransform = mapaVisual.transform; // Caché del transform del mapa

        float tiempoAcel = velocidadMaxima / aceleracion;
        float tiempoFren = velocidadMaxima / frenado;

        if (mapaVisual != null)
        {
            escalaOriginal = mapaTransform.localScale;
            if (manualSetCycler != null) manualSetCycler.TriggerTransition(tiempoAcel, tiempoFren);
        }

        OnTransitionStart?.Invoke();
        Vector3 escalaObjetivoMin = escalaOriginal * escalaMinima;

        // --- FASE 1: ACELERAR Y ENCOGER ---
        while (velocidadActual < velocidadMaxima)
        {
            float dt = Time.deltaTime;
            velocidadActual += aceleracion * dt;
            mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, zoomMaximo, velocidadZoomIn * dt);

            if (mapaVisual)
            {
                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
                mapaTransform.localScale = Vector3.Lerp(mapaTransform.localScale, escalaObjetivoMin, suavizadoEscala * dt);
            }
            yield return null;
        }

        // --- FASE 2: CAMBIO DE MAPA ---
        int nextMap = currentIdx + 1;
        if (nextMap < lm.mapList.Length)
        {
            lm.ActivateMap(nextMap);
            mapaVisual = lm.mapList[nextMap];
            mapaTransform = mapaVisual.transform; // Actualizamos caché del transform
            mapaTransform.localScale = escalaObjetivoMin;

            if (cachedPlaneta != null) cachedPlaneta.isInvulnerable = true;
            lm.currentSessionInfected = 0;
        }

        // --- FASE 3: ESPERA TÉCNICA (Optimización de cálculo de ángulo) ---
        float distanciaFrenado = (velocidadActual * velocidadActual) / (2f * frenado);
        while (true)
        {
            float dt = Time.deltaTime;
            mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
            float anguloActualZ = mapaTransform.localEulerAngles.z;
            float anguloFinalPredecido = (anguloActualZ + distanciaFrenado) % 360f;

            if (anguloFinalPredecido < (velocidadActual * dt)) break;
            yield return null;
        }

        // --- FASE 4: FRENAR Y CRECER ---
        while (velocidadActual > 0.1f) // Cambiado a 0.1f para evitar frames muertos
        {
            float dt = Time.deltaTime;
            velocidadActual = Mathf.MoveTowards(velocidadActual, 0, frenado * dt);

            if (mapaVisual)
            {
                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
                mapaTransform.localScale = Vector3.Lerp(mapaTransform.localScale, escalaOriginal, suavizadoEscala * dt);
            }
            yield return null;
        }

        // --- IMPACTO FINAL ---
        mapaTransform.SetPositionAndRotation(mapaTransform.position, Quaternion.identity);
        mapaTransform.localScale = escalaOriginal;

        // Caché de PopulationManager
        PopulationManager popManager = FindFirstObjectByType<PopulationManager>();
        if (popManager != null) popManager.ConfigureRound(0);

        OnImpactShake?.Invoke(intensidadImpacto);
        mainCam.orthographicSize = zoomOriginal;

        yield return StartCoroutine(DryImpactShake());

        if (cachedPlaneta != null) cachedPlaneta.isInvulnerable = false;
        lm.isGameActive = true;
    }

    private IEnumerator DryImpactShake()
    {
        Vector3 posOriginal = camTransform.localPosition;
        float fuerzaActual = intensidadImpacto;

        while (fuerzaActual > 0.01f)
        {
            float dt = Time.deltaTime;
            float x = UnityEngine.Random.Range(-fuerzaActual, fuerzaActual);
            float y = UnityEngine.Random.Range(-fuerzaActual, fuerzaActual);

            camTransform.localPosition = posOriginal + new Vector3(x, y, 0);
            fuerzaActual = Mathf.Lerp(fuerzaActual, 0, dt * velocidadRetorno);
            yield return null;
        }
        camTransform.localPosition = posOriginal;
    }
}