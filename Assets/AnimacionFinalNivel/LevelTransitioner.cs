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

    [Header("Configuración de Explosión Final")]
    public GameObject prefabExplosion;
    public float escalaMaximaExplosion1 = 5f;
    public float tiempoCrecimiento1 = 0.2f;
    public float tiempoEncogimiento = 0.15f;

    public float escalaMaximaExplosion2 = 8f;
    public float tiempoCrecimiento2 = 0.4f;

    public float tiempoCambioColorNegro = 1.5f;
    private GameObject instanciaExplosionActual;

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

    [Header("UI y Fondo")]
    public GameObject panelFinal;
    public GameObject panelHUD;
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

        if (fondoNebula != null)
            escalaOriginalFondoNebula = fondoNebula.localScale;

        CapturarEscalas();
        RefreshCurrentPlanet();
        ResetPlanetRotation();
        ForzarEscalaMapasAUno();
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
        if (lm == null) lm = LevelManager.instance;
        if (lm?.mapList == null) return;

        foreach (GameObject map in lm.mapList)
        {
            if (map != null && !escalasIniciales.ContainsKey(map.transform))
            {
                escalasIniciales[map.transform] = Vector3.one;
            }
        }
    }

    private void ResetPlanetRotation()
    {
        if (cachedPlaneta != null)
            cachedPlaneta.transform.rotation = Quaternion.identity;
    }

    private void ForzarEscalaMapasAUno(bool ignorarUltimoMapa = false)
    {
        if (lm == null) lm = LevelManager.instance;
        if (lm?.mapList == null) return;

        for (int i = 0; i < lm.mapList.Length; i++)
        {
            if (lm.mapList[i] != null)
            {
                if (ignorarUltimoMapa && i == lm.mapList.Length - 1) continue;

                lm.mapList[i].transform.localScale = Vector3.one;
            }
        }
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
        CapturarEscalas();
        ForzarEscalaMapasAUno();

        lm.isTransitioning = true;
      

        if (lm.virusMovementScript != null)
            lm.virusMovementScript.enabled = false;

        if (cachedPlaneta != null)
        {
            cachedPlaneta.isInvulnerable = true;
            cachedPlaneta.ClearPendingDamage();

            if (lm.esVersionDemo)
                cachedPlaneta.SetVisibleUI(false);
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

        escalaOriginal = Vector3.one;
        mapaTransform.localScale = Vector3.one;

        if (manualSetCycler != null)
            manualSetCycler.TriggerTransition(velocidadMaxima / aceleracion, velocidadMaxima / frenado);

        OnTransitionStart?.Invoke();

        Vector3 escalaObjetivoMin = esUltimoNivel ? Vector3.zero : (escalaOriginal * escalaMinima);

        while (velocidadActual < velocidadMaxima)
        {
            float dt = Time.deltaTime;
            velocidadActual += aceleracion * dt;

            if (materialFondo != null)
                materialFondo.SetFloat(vortexProp, (velocidadActual / velocidadMaxima) * 75f);

            if (lm.esVersionDemo && mainCam != null)
                mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, zoomMaximo, velocidadZoomIn * dt);

            mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
            mapaTransform.localScale = Vector3.Lerp(mapaTransform.localScale, escalaObjetivoMin, suavizadoEscala * dt);

            yield return null;
        }

        if (esUltimoNivel)
        {
            mapaTransform.localScale = Vector3.zero;

            if (prefabExplosion != null)
            {
                if (instanciaExplosionActual != null) Destroy(instanciaExplosionActual);
                instanciaExplosionActual = Instantiate(prefabExplosion, Vector3.zero, Quaternion.identity);
                StartCoroutine(AnimarExplosion(instanciaExplosionActual.transform));
            }
        }

        if (!esUltimoNivel)
        {
            if (popManager != null)
                popManager.ClearAllPersonas();

            if (lm.esVersionDemo)
            {
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

                if (mapaVisual != null)
                    mapaVisual.SetActive(false);

                lm.MostrarFinDeDemo();

                mapaTransform.localScale = Vector3.one;

                if (mapaVisual != null)
                    mapaVisual.SetActive(true);

                ForzarEscalaMapasAUno();

                if (cachedPlaneta != null)
                    cachedPlaneta.SetVisibleUI(true);

                yield break;
            }
            else
            {
                int nextIdx;

                if (MapSequenceManager.instance != null)
                {
                    MapSequenceManager.instance.NextMap();
                    nextIdx = MapSequenceManager.instance.GetCurrentMapIndex();
                }
                else
                {
                    nextIdx = currentIdx + 1;
                }

                lm.ActivateMap(nextIdx);

                ForzarEscalaMapasAUno();

                mapaVisual = lm.mapList[nextIdx];
                mapaTransform = mapaVisual.transform;

                Vector3 escalaOriginalNuevoMapa = Vector3.one;
                Vector3 escalaMinNuevoMapa = escalaOriginalNuevoMapa * escalaMinima;

                mapaTransform.localScale = escalaMinNuevoMapa;

                float distanciaFrenado = (velocidadActual * velocidadActual) / (2f * frenado);

                while (true)
                {
                    float dt = Time.deltaTime;
                    mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);

                    float anguloFinalPredecido = (mapaTransform.localEulerAngles.z + distanciaFrenado) % 360f;
                    if (anguloFinalPredecido < (velocidadActual * dt) || velocidadActual < 10f)
                        break;

                    yield return null;
                }

                while (velocidadActual > 0.1f)
                {
                    float dt = Time.deltaTime;
                    velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, frenado * dt);

                    if (materialFondo != null)
                        materialFondo.SetFloat(vortexProp, (velocidadActual / velocidadMaxima) * 75f);

                    mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
                    mapaTransform.localScale = Vector3.Lerp(
                        mapaTransform.localScale,
                        Vector3.one,
                        suavizadoEscala * dt
                    );

                    yield return null;
                }

                mapaTransform.rotation = Quaternion.identity;
                mapaTransform.localScale = Vector3.one;

                ForzarEscalaMapasAUno();

                if (popManager != null)
                    popManager.ConfigureRound(nextIdx);

                yield return null;
                ForzarEscalaMapasAUno();

                yield return null;
                ForzarEscalaMapasAUno();
            }
        }

        RefreshCurrentPlanet();

        if (cachedPlaneta != null)
        {
            cachedPlaneta.isInvulnerable = esUltimoNivel;  
            cachedPlaneta.SetVisibleUI(true);
        }

        lm.isGameActive = !esUltimoNivel;
        lm.isTransitioning = false;

        if (lm.virusMovementScript != null)
            lm.virusMovementScript.enabled = !esUltimoNivel;

        Time.timeScale = 1f;
        velocidadActual = 0f;

        if (materialFondo != null)
            materialFondo.SetFloat(vortexProp, 0f);

        if (mainCam != null && lm.esVersionDemo)
            mainCam.orthographicSize = zoomOriginal;

        ForzarEscalaMapasAUno(esUltimoNivel);
        yield return null;
        ForzarEscalaMapasAUno(esUltimoNivel);
        yield return null;
        ForzarEscalaMapasAUno(esUltimoNivel);

        yield return StartCoroutine(DryImpactShake());

        ForzarEscalaMapasAUno(esUltimoNivel);
    }

    private IEnumerator AnimarExplosion(Transform explosionTransform)
    {
        // --- MODIFICACIÓN: Desactivar HUD al empezar la transición ---
        if (panelHUD != null)
        {
            panelHUD.SetActive(false);
        }
        explosionTransform.localScale = Vector3.zero;
        Vector3 escalaObjetivo1 = Vector3.one * escalaMaximaExplosion1;
        Vector3 escalaObjetivo2 = Vector3.one * escalaMaximaExplosion2;
        float tiempo;

        // Fase 1: Crecer de 0 a Max 1
        tiempo = 0f;
        while (tiempo < tiempoCrecimiento1)
        {
            tiempo += Time.deltaTime;
            explosionTransform.localScale = Vector3.Lerp(Vector3.zero, escalaObjetivo1, tiempo / tiempoCrecimiento1);
            yield return null;
        }
        explosionTransform.localScale = escalaObjetivo1;

        // Fase 2: Encoger de Max 1 a 0
        tiempo = 0f;
        while (tiempo < tiempoEncogimiento)
        {
            tiempo += Time.deltaTime;
            explosionTransform.localScale = Vector3.Lerp(escalaObjetivo1, Vector3.zero, tiempo / tiempoEncogimiento);
            yield return null;
        }
        explosionTransform.localScale = Vector3.zero;

        // Fase 3: Crecer de nuevo de 0 a Max 2
        tiempo = 0f;
        while (tiempo < tiempoCrecimiento2)
        {
            tiempo += Time.deltaTime;
            explosionTransform.localScale = Vector3.Lerp(Vector3.zero, escalaObjetivo2, tiempo / tiempoCrecimiento2);
            yield return null;
        }
        explosionTransform.localScale = escalaObjetivo2;

        // --- MODIFICACIÓN: Activamos el panel final aquí ---
        if (panelFinal != null)
        {
            panelFinal.SetActive(true);
        }
        // ---------------------------------------------------

        // Fase 4: Cambiar color a negro lentamente
        SpriteRenderer[] renderers2D = explosionTransform.GetComponentsInChildren<SpriteRenderer>();
        Image[] imagesUI = explosionTransform.GetComponentsInChildren<Image>();

        if (renderers2D.Length > 0 || imagesUI.Length > 0)
        {
            Dictionary<SpriteRenderer, Color> coloresOriginales2D = new Dictionary<SpriteRenderer, Color>();
            foreach (var sr in renderers2D) coloresOriginales2D[sr] = sr.color;

            Dictionary<Image, Color> coloresOriginalesUI = new Dictionary<Image, Color>();
            foreach (var img in imagesUI) coloresOriginalesUI[img] = img.color;

            tiempo = 0f;
            while (tiempo < tiempoCambioColorNegro)
            {
                tiempo += Time.deltaTime;
                float progreso = tiempo / tiempoCambioColorNegro;

                foreach (var sr in renderers2D)
                {
                    sr.color = Color.Lerp(coloresOriginales2D[sr], Color.black, progreso);
                }
                foreach (var img in imagesUI)
                {
                    img.color = Color.Lerp(coloresOriginalesUI[img], Color.black, progreso);
                }

                yield return null;
            }

            foreach (var sr in renderers2D) sr.color = Color.black;
            foreach (var img in imagesUI) img.color = Color.black;
        }
    }

    private IEnumerator DryImpactShake()
    {
        if (GameSettings.instance == null || !GameSettings.instance.shakeEnabled || camTransform == null)
            yield break;

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

    public void ResetFinalLevelEffects()
    {
        StopAllCoroutines();

        if (instanciaExplosionActual != null)
        {
            Destroy(instanciaExplosionActual);
        }

        // --- MODIFICACIÓN: Ocultamos el panel si el jugador reinicia ---
        if (panelFinal != null)
        {
            panelFinal.SetActive(false);
        }
        // ---------------------------------------------------------------

        if (fondoNebula != null)
        {
            fondoNebula.gameObject.SetActive(true);
            fondoNebula.localScale = escalaOriginalFondoNebula;
        }
        if (panelHUD != null) panelHUD.SetActive(true);

        if (lm == null) lm = LevelManager.instance;

        if (lm?.mapList != null)
        {
            for (int i = 0; i < lm.mapList.Length; i++)
            {
                GameObject map = lm.mapList[i];
                if (map == null) continue;

                map.transform.localScale = Vector3.one;
                map.transform.rotation = Quaternion.identity;
            }
        }

        RefreshCurrentPlanet();

        if (materialFondo != null)
            materialFondo.SetFloat(vortexProp, 0f);

        if (mainCam != null)
            mainCam.orthographicSize = zoomOriginal;

        Time.timeScale = 1f;
        velocidadActual = 0f;

        ForzarEscalaMapasAUno();
    }
}