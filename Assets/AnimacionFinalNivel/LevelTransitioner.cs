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

    [Header("Configuración Final (Último Nivel)")]
    public float duracionVibracionPrevia = 1.5f;
    public float intensidadVibracionPrevia = 0.2f;
    public float duracionColapsoFinal = 0.33f;
    public float velocidadGiroFinal = -1600f;

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

        bool esUltimoNivel = (lm != null && lm.mapList != null && currentIdx >= lm.mapList.Length - 1);

        GameObject mapaVisual = null;
        Transform mapaTransform = null;

        if (lm != null && lm.mapList != null && currentIdx < lm.mapList.Length)
        {
            mapaVisual = lm.mapList[currentIdx];
            if (mapaVisual != null) mapaTransform = mapaVisual.transform;
        }

        if (mapaVisual != null && mapaTransform != null)
        {
            escalaOriginal = mapaTransform.localScale;
            if (manualSetCycler != null)
                manualSetCycler.TriggerTransition(velocidadMaxima / aceleracion, velocidadMaxima / frenado);
        }

        OnTransitionStart?.Invoke();
        Vector3 escalaObjetivoMin = escalaOriginal * escalaMinima;

        // FASE 0: VIBRACIÓN PREVIA
        if (esUltimoNivel && mapaTransform != null)
        {
            float tiempoVibracion = 0f;
            Vector3 posOriginalMapa = mapaTransform.localPosition;

            // --- INICIO LIMPIEZA GRADUAL ---
            if (popManager != null)
            {
                popManager.StartGradualClear(duracionVibracionPrevia);
            }

            while (tiempoVibracion < duracionVibracionPrevia)
            {
                tiempoVibracion += Time.deltaTime;
                float offsetX = UnityEngine.Random.Range(-intensidadVibracionPrevia, intensidadVibracionPrevia);
                float offsetY = UnityEngine.Random.Range(-intensidadVibracionPrevia, intensidadVibracionPrevia);
                mapaTransform.localPosition = posOriginalMapa + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }

            mapaTransform.localPosition = posOriginalMapa;
        }

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

        // FASE 2: CAMBIO DE MAPA (Solo si NO es el último)
        if (!esUltimoNivel)
        {
            // Aquí SI borramos de golpe porque cambiamos de nivel
            if (popManager != null) popManager.ClearAllPersonas();
            yield return null;

            int nextMap = MapSequenceManager.instance != null ? MapSequenceManager.instance.GetCurrentMapIndex() : currentIdx;
            lm.ActivateMap(nextMap);

            mapaVisual = lm.mapList[nextMap];
            mapaTransform = mapaVisual != null ? mapaVisual.transform : null;
            if (mapaTransform != null) mapaTransform.localScale = escalaObjetivoMin;

            RefreshCurrentPlanet();
            if (cachedPlaneta != null)
            {
                cachedPlaneta.ResetHealthToInitial();
                cachedPlaneta.isInvulnerable = true;
            }
            lm.currentSessionInfected = 0;

            // FASE 3 Y 4: FRENADO (Solo ocurre si NO es el último nivel)
            float distanciaFrenado = (velocidadActual * velocidadActual) / (2f * frenado);
            while (true)
            {
                float dt = Time.deltaTime;
                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
                float anguloFinalPredecido = (mapaTransform.localEulerAngles.z + distanciaFrenado) % 360f;
                if (anguloFinalPredecido < (velocidadActual * dt) || velocidadActual < 10f) break;
                yield return null;
            }

            while (velocidadActual > 0.1f)
            {
                float dt = Time.deltaTime;
                velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, frenado * dt);
                if (materialFondo != null) materialFondo.SetFloat(vortexProp, (velocidadActual / velocidadMaxima) * 75f);

                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
                mapaTransform.localScale = Vector3.Lerp(mapaTransform.localScale, escalaOriginal, suavizadoEscala * dt);
                if (mainCam != null) mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, zoomOriginal, velocidadZoomOut * dt);
                yield return null;
            }
        }
        else
        {
            // --- AQUÍ ESTABA EL ERROR ---
            // Si es el último nivel, NO llamamos a ClearAllPersonas() aquí, 
            // ya que se están borrando gradualmente desde la Fase 0.
            yield return null;
        }

        // FASE 5: COLAPSO FINAL (Solo si es el último nivel)
        if (esUltimoNivel && mapaTransform != null)
        {
            float tiempoPasado = 0f;
            Transform fondoTransform = null;
            Vector3 escalaOriginalFondo = Vector3.one;

            Image imgFondo = GetComponentInChildren<Image>();
            if (imgFondo != null) { fondoTransform = imgFondo.transform; escalaOriginalFondo = fondoTransform.localScale; }

            Vector3 escalaAlEmpezarColapso = mapaTransform.localScale;

            while (tiempoPasado < duracionColapsoFinal)
            {
                tiempoPasado += Time.deltaTime;
                float t = tiempoPasado / duracionColapsoFinal;

                if (materialFondo != null) materialFondo.SetFloat(vortexProp, Mathf.Lerp(75f, 50f, t));
                if (fondoTransform != null) fondoTransform.localScale = Vector3.Lerp(escalaOriginalFondo, Vector3.zero, t);

                mapaTransform.Rotate(Vector3.forward, velocidadGiroFinal * Time.deltaTime);
                mapaTransform.localScale = Vector3.Lerp(escalaAlEmpezarColapso, Vector3.zero, t);
                yield return null;
            }

            mapaTransform.localScale = Vector3.zero;
            if (fondoTransform != null) fondoTransform.localScale = Vector3.zero;
            mapaVisual.SetActive(false);
            if (materialFondo != null) materialFondo.SetFloat(vortexProp, 0f);

            if (GameSettings.instance != null && GameSettings.instance.shakeEnabled)
                OnImpactShake?.Invoke(intensidadImpacto * 2.5f);

            yield return StartCoroutine(DryImpactShake());
        }
        else
        {
            // RESTAURACIÓN NORMAL
            if (mapaTransform != null)
            {
                mapaTransform.rotation = Quaternion.identity;
                mapaTransform.localScale = escalaOriginal;
            }
            if (popManager != null) popManager.ConfigureRound(0);
            if (mainCam != null) mainCam.orthographicSize = zoomOriginal;
            yield return StartCoroutine(DryImpactShake());
        }

        // Limpieza final
        RefreshCurrentPlanet();
        if (cachedPlaneta != null) cachedPlaneta.isInvulnerable = esUltimoNivel;
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
}