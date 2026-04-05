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

    [Header("UI y Fondo")]
    public GameObject panelFinal;
    public RectTransform fondoNebula;
    private Vector3 escalaOriginalFondoNebula;
    private Dictionary<Transform, Vector3> escalasIniciales = new Dictionary<Transform, Vector3>();

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

        if (fondoNebula != null) escalaOriginalFondoNebula = fondoNebula.localScale;

        CapturarEscalas();
        RefreshCurrentPlanet();
        ResetPlanetRotation();
    }

    private void RefreshCurrentPlanet()
    {
        cachedPlaneta = null;
        if (lm == null) lm = LevelManager.instance;
        if (lm?.mapList == null) return;

        foreach (GameObject map in lm.mapList)
        {
            if (map != null && map.activeInHierarchy)
            {
                cachedPlaneta = map.GetComponentInChildren<PlanetCrontrollator>(true);
                if (cachedPlaneta != null) return;
            }
        }
    }

    private void CapturarEscalas()
    {
        if (escalasIniciales.Count > 0) return;
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
        RefreshCurrentPlanet();

        lm.isTransitioning = true;
        lm.isGameActive = false;
        if (lm.virusMovementScript != null) lm.virusMovementScript.enabled = false;

        if (cachedPlaneta != null)
        {
            cachedPlaneta.isInvulnerable = true;
            cachedPlaneta.ClearPendingDamage();
            if (lm.esVersionDemo) cachedPlaneta.SetVisibleUI(false);
        }

        int currentIdx = 0;
        for (int i = 0; i < lm.mapList.Length; i++)
        {
            if (lm.mapList[i] != null && lm.mapList[i].activeInHierarchy)
            {
                currentIdx = i;
                break;
            }
        }

        bool esUltimoNivel = (currentIdx >= lm.mapList.Length - 1);
        GameObject mapaVisual = lm.mapList[currentIdx];
        Transform mapaTransform = mapaVisual.transform;
        escalaOriginal = mapaTransform.localScale;

        if (manualSetCycler != null)
            manualSetCycler.TriggerTransition(velocidadMaxima / aceleracion, velocidadMaxima / frenado);

        // FASE 0: VIBRACIÓN (Solo Normal + Último Nivel)
        if (!lm.esVersionDemo && esUltimoNivel)
        {
            float tiempoVibracion = 0f;
            Vector3 posOriginalMapa = mapaTransform.localPosition;
            if (popManager != null) popManager.StartGradualClear(duracionVibracionPrevia);

            while (tiempoVibracion < duracionVibracionPrevia)
            {
                tiempoVibracion += Time.deltaTime;
                mapaTransform.localPosition = posOriginalMapa + (Vector3)(UnityEngine.Random.insideUnitCircle * intensidadVibracionPrevia);
                yield return null;
            }
            mapaTransform.localPosition = posOriginalMapa;
        }

        // FASE 1: ACELERAR Y ENCOGER
        OnTransitionStart?.Invoke();
        Vector3 escalaObjetivoMin = escalaOriginal * escalaMinima;

        while (velocidadActual < velocidadMaxima)
        {
            float dt = Time.deltaTime;
            velocidadActual += aceleracion * dt;
            if (materialFondo != null) materialFondo.SetFloat(vortexProp, (velocidadActual / velocidadMaxima) * 75f);
            
            // MODIFICACIÓN: Solo hacer zoom de cámara si ES demo
            if (lm.esVersionDemo && mainCam != null) 
                mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, zoomMaximo, velocidadZoomIn * dt);

            mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
            mapaTransform.localScale = Vector3.Lerp(mapaTransform.localScale, escalaObjetivoMin, suavizadoEscala * dt);
            yield return null;
        }

        // FASE 2: SALIDA
        if (!esUltimoNivel)
        {
            if (popManager != null) popManager.ClearAllPersonas();

            if (lm.esVersionDemo)
            {
                // COMPORTAMIENTO DEMO
                float velAlEmpezar = velocidadActual;
                Vector3 escalaAlEmpezar = mapaTransform.localScale;
                while (velocidadActual > 0.1f)
                {
                    float dt = Time.deltaTime;
                    velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, frenado * dt);
                    float progreso = 1f - (velocidadActual / velAlEmpezar);
                    mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
                    mapaTransform.localScale = Vector3.Lerp(escalaAlEmpezar, escalaOriginal * 10f, progreso * progreso);
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.3f);
                if (mapaVisual != null) mapaVisual.SetActive(false);
                lm.MostrarFinDeDemo();
                
                mapaTransform.localScale = escalaOriginal;
                if (mapaVisual != null) mapaVisual.SetActive(true);
                if (cachedPlaneta != null) cachedPlaneta.SetVisibleUI(true);
                yield break;
            }
            else
            {
                // COMPORTAMIENTO NORMAL (Igual a la versión antigua)
                int nextIdx = MapSequenceManager.instance != null ? MapSequenceManager.instance.GetCurrentMapIndex() : currentIdx + 1;
                lm.ActivateMap(nextIdx);

                mapaVisual = lm.mapList[nextIdx];
                mapaTransform = mapaVisual.transform;
                mapaTransform.localScale = escalaObjetivoMin;

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
                    
                    // Solo restauramos zoom si por algún motivo se cambió (seguridad)
                  //  if (mainCam != null) mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, zoomOriginal, velocidadZoomOut * dt);
                    
                    yield return null;
                }
                mapaTransform.rotation = Quaternion.identity;
                mapaTransform.localScale = escalaOriginal;
                if (popManager != null) popManager.ConfigureRound(nextIdx);
            }
        }
        else
        {
            // ÚLTIMO NIVEL
            yield return StartCoroutine(FinalPulseAndSpawn());
        }

        // LIMPIEZA FINAL
        RefreshCurrentPlanet();
        if (cachedPlaneta != null)
        {
            cachedPlaneta.isInvulnerable = esUltimoNivel;
            cachedPlaneta.SetVisibleUI(true);
        }
        lm.isGameActive = !esUltimoNivel;
        lm.isTransitioning = false;
        if (lm.virusMovementScript != null) lm.virusMovementScript.enabled = !esUltimoNivel;

        Time.timeScale = 1f;
        velocidadActual = 0f;
        if (materialFondo != null) materialFondo.SetFloat(vortexProp, 0f);
        if (mainCam != null && lm.esVersionDemo)
        {
            mainCam.orthographicSize = zoomOriginal;
        }

        yield return StartCoroutine(DryImpactShake());
    }

    // ... (El resto de funciones DryImpactShake, FinalPulseAndSpawn y ResetFinalLevelEffects se mantienen igual)
    
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
        if (spritePulsoPrefab == null || popManager == null) yield break;
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 posOriginalCam = cam.transform.position;
        float zoomOrig = cam.orthographicSize;
        Vector3 centroEfecto = new Vector3(posOriginalCam.x, posOriginalCam.y, 0f);

        GameObject pulso = Instantiate(spritePulsoPrefab, centroEfecto, Quaternion.identity);
        SpriteRenderer srPulso = pulso.GetComponent<SpriteRenderer>();
        pulso.transform.localScale = Vector3.zero;

        for (int i = 0; i < numeroPulsos; i++)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * velocidadPulso;
                pulso.transform.localScale = Vector3.one * Mathf.Lerp(0f, escalaMaxPulso, t);
                yield return null;
            }
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * velocidadPulso;
                pulso.transform.localScale = Vector3.one * Mathf.Lerp(escalaMaxPulso, 0f, t);
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.33f);

        float tFinal = 0f;
        float escalaObjetivo = escalaMaxPulso * 25f;
        float zoomObjetivo = zoomOrig * 1.5f;

        while (tFinal < 1f)
        {
            tFinal += Time.deltaTime * (velocidadPulso / 3f);
            pulso.transform.localScale = Vector3.one * Mathf.Lerp(0f, escalaObjetivo, tFinal);
            cam.orthographicSize = Mathf.Lerp(zoomOrig, zoomObjetivo, tFinal);
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        if (srPulso != null)
        {
            float tFade = 0f;
            Color colorInicial = srPulso.color;
            while (tFade < 1f)
            {
                tFade += Time.deltaTime * 2f;
                srPulso.color = Color.Lerp(colorInicial, new Color(colorInicial.r, colorInicial.g, colorInicial.b, 0f), tFade);
                yield return null;
            }
        }

        Destroy(pulso);
        if (fondoNebula != null) fondoNebula.localScale = escalaOriginalFondoNebula;
        if (panelFinal != null) panelFinal.SetActive(true);
    }

    public void ResetFinalLevelEffects()
    {
        StopAllCoroutines();
        if (fondoNebula != null)
        {
            fondoNebula.gameObject.SetActive(true);
            fondoNebula.localScale = escalaOriginalFondoNebula;
        }

        CapturarEscalas();

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

        if (materialFondo != null) materialFondo.SetFloat(vortexProp, 0f);
        if (mainCam != null) mainCam.orthographicSize = zoomOriginal;

        Time.timeScale = 1f;
        velocidadActual = 0f;
    }
}