using System;
using System.Collections;
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

        lm = LevelManager.instance;
        popManager = FindFirstObjectByType<PopulationManager>();

        RefreshCurrentPlanet();
        ResetPlanetRotation();
    }

    private void RefreshCurrentPlanet()
    {
        cachedPlaneta = null;

        if (lm == null)
            lm = LevelManager.instance;

        if (lm != null && lm.mapList != null && lm.mapList.Length > 0)
        {
            for (int i = 0; i < lm.mapList.Length; i++)
            {
                GameObject map = lm.mapList[i];
                if (map != null && map.activeInHierarchy)
                {
                    cachedPlaneta = map.GetComponentInChildren<PlanetCrontrollator>(true);
                    if (cachedPlaneta != null)
                        return;
                }
            }

            int currentIdx = Mathf.Clamp(PlayerPrefs.GetInt("CurrentMapIndex", 0), 0, lm.mapList.Length - 1);
            GameObject currentMap = lm.mapList[currentIdx];
            if (currentMap != null)
            {
                cachedPlaneta = currentMap.GetComponentInChildren<PlanetCrontrollator>(true);
                if (cachedPlaneta != null)
                    return;
            }
        }

        cachedPlaneta = FindFirstObjectByType<PlanetCrontrollator>(FindObjectsInactive.Include);
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
        if (lm == null)
            lm = LevelManager.instance;

        if (popManager == null)
            popManager = FindFirstObjectByType<PopulationManager>();

        RefreshCurrentPlanet();

        if (lm != null)
        {
            lm.isTransitioning = true;
            lm.isGameActive = false;

            if (lm.virusMovementScript != null)
                lm.virusMovementScript.enabled = false;
        }

        if (cachedPlaneta != null)
        {
            cachedPlaneta.isInvulnerable = true;
            cachedPlaneta.ClearPendingDamage();
        }

        // Cogemos el mapa REAL que está activo ahora mismo en escena
        int currentIdx = 0;
        if (lm != null && lm.mapList != null)
        {
            for (int i = 0; i < lm.mapList.Length; i++)
            {
                if (lm.mapList[i] != null && lm.mapList[i].activeInHierarchy)
                {
                    currentIdx = i;
                    break;
                }
            }
        }

        GameObject mapaVisual = null;
        Transform mapaTransform = null;

        if (lm != null && lm.mapList != null && currentIdx < lm.mapList.Length)
        {
            mapaVisual = lm.mapList[currentIdx];
            if (mapaVisual != null)
                mapaTransform = mapaVisual.transform;
        }

        float tiempoAcel = velocidadMaxima / aceleracion;
        float tiempoFren = velocidadMaxima / frenado;

        if (mapaVisual != null && mapaTransform != null)
        {
            escalaOriginal = mapaTransform.localScale;

            if (manualSetCycler != null)
                manualSetCycler.TriggerTransition(tiempoAcel, tiempoFren);
        }

        OnTransitionStart?.Invoke();
        Vector3 escalaObjetivoMin = escalaOriginal * escalaMinima;

        // FASE 1: ACELERAR Y ENCOGER
        while (velocidadActual < velocidadMaxima)
        {
            float dt = Time.deltaTime;
            velocidadActual += aceleracion * dt;

            float progresoAcel = velocidadActual / velocidadMaxima;

            if (materialFondo != null)
                materialFondo.SetFloat(vortexProp, progresoAcel * 25f);

            if (mainCam != null)
                mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, zoomMaximo, velocidadZoomIn * dt);

            if (mapaVisual != null && mapaTransform != null)
            {
                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
                mapaTransform.localScale = Vector3.Lerp(mapaTransform.localScale, escalaObjetivoMin, suavizadoEscala * dt);
            }

            yield return null;
        }

        // FASE 2: CAMBIO DE MAPA
        // El mapa destino lo decide MapSequenceManager, no esta función
        int nextMap = currentIdx;

        if (MapSequenceManager.instance != null)
            nextMap = MapSequenceManager.instance.GetCurrentMapIndex();

        if (lm != null && lm.mapList != null && nextMap < lm.mapList.Length)
        {
            if (popManager != null)
                popManager.ClearAllPersonas();

            // Dejar un frame para que Unity procese destrucciones pendientes
            yield return null;

            lm.ActivateMap(nextMap);

            mapaVisual = lm.mapList[nextMap];
            mapaTransform = mapaVisual != null ? mapaVisual.transform : null;

            if (mapaTransform != null)
                mapaTransform.localScale = escalaObjetivoMin;

            RefreshCurrentPlanet();

            if (cachedPlaneta != null)
            {
                cachedPlaneta.ResetHealthToInitial();
                cachedPlaneta.ClearPendingDamage();
                cachedPlaneta.isInvulnerable = true;
            }

            lm.currentSessionInfected = 0;
        }

        // FASE 3: ESPERA TÉCNICA
        if (mapaTransform != null)
        {
            float distanciaFrenado = (velocidadActual * velocidadActual) / (2f * frenado);

            while (true)
            {
                float dt = Time.deltaTime;
                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);

                float anguloActualZ = mapaTransform.localEulerAngles.z;
                float anguloFinalPredecido = (anguloActualZ + distanciaFrenado) % 360f;

                if (anguloFinalPredecido < (velocidadActual * dt) || velocidadActual < 10f)
                    break;

                yield return null;
            }
        }

        // FASE 4: FRENAR Y CRECER
        while (velocidadActual > 0.1f)
        {
            float dt = Time.deltaTime;
            velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, frenado * dt);

            float progresoFrenado = velocidadActual / velocidadMaxima;

            if (materialFondo != null)
                materialFondo.SetFloat(vortexProp, progresoFrenado * 25f);

            if (mapaVisual != null && mapaTransform != null)
            {
                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
                mapaTransform.localScale = Vector3.Lerp(mapaTransform.localScale, escalaOriginal, suavizadoEscala * dt);
            }

            if (mainCam != null)
                mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, zoomOriginal, velocidadZoomOut * dt);

            yield return null;
        }

        // IMPACTO FINAL Y SPAWN
        if (mapaTransform != null)
        {
            mapaTransform.SetPositionAndRotation(mapaTransform.position, Quaternion.identity);
            mapaTransform.localScale = escalaOriginal;
        }

        if (popManager != null)
            popManager.ConfigureRound(0);

        if (GameSettings.instance != null && GameSettings.instance.shakeEnabled)
            OnImpactShake?.Invoke(intensidadImpacto);

        if (mainCam != null)
            mainCam.orthographicSize = zoomOriginal;

        yield return StartCoroutine(DryImpactShake());

        RefreshCurrentPlanet();
        if (cachedPlaneta != null)
        {
            cachedPlaneta.ClearPendingDamage();
            cachedPlaneta.isInvulnerable = false;
        }

        if (lm != null)
        {
            lm.isGameActive = true;
            lm.isTransitioning = false;

            if (lm.virusMovementScript != null)
                lm.virusMovementScript.enabled = true;
        }

        Time.timeScale = 1f;
        velocidadActual = 0f;

        if (materialFondo != null)
            materialFondo.SetFloat(vortexProp, 0f);
    }
    private IEnumerator DryImpactShake()
    {
        if (GameSettings.instance == null || !GameSettings.instance.shakeEnabled)
            yield break;

        if (camTransform == null)
            yield break;

        Vector3 posOriginal = camTransform.localPosition;
        float fuerzaActual = intensidadImpacto;

        while (fuerzaActual > 0.01f)
        {
            float dt = Time.deltaTime;
            float x = UnityEngine.Random.Range(-fuerzaActual, fuerzaActual);
            float y = UnityEngine.Random.Range(-fuerzaActual, fuerzaActual);

            camTransform.localPosition = posOriginal + new Vector3(x, y, 0f);
            fuerzaActual = Mathf.Lerp(fuerzaActual, 0f, dt * velocidadRetorno);

            yield return null;
        }

        camTransform.localPosition = posOriginal;
    }
}