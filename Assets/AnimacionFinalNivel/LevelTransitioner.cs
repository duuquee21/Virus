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

    public ManualSetCycler manualSetCycler;

    public static event Action<float> OnImpactShake;
    public static event Action OnTransitionStart;

    private float velocidadActual = 0f;
    private Vector3 escalaOriginal = Vector3.one;
    private Camera mainCam;
    private Transform camTransform;
    private float zoomOriginal;
    private PlanetCrontrollator cachedPlaneta;

    // OPTIMIZACIÓN 1: Caché de componentes pesados
    private PopulationManager popManager;
    private LevelManager lm;

    [Header("Configuración de Shader")]
    public Material materialFondo; // Arrastra aquí el material que tiene el shader del vortex
    private string vortexProp = "_VortexStrength";

    void Awake()
    {
        mainCam = Camera.main;
        if (mainCam != null)
        {
            camTransform = mainCam.transform;
            zoomOriginal = mainCam.orthographicSize;
        }


        Image img = GetComponentInChildren<Image>();
        if (img != null)
        {
            // Esto crea una instancia única para este objeto
            materialFondo = img.material;
            materialFondo.SetFloat(vortexProp, 0f);
        }

        // Buscamos las instancias una sola vez al inicio
        lm = LevelManager.instance;
        popManager = FindFirstObjectByType<PopulationManager>();
        cachedPlaneta = FindFirstObjectByType<PlanetCrontrollator>();

        ResetPlanetRotation();
    }

    private void ResetPlanetRotation()
    {
        if (cachedPlaneta != null) cachedPlaneta.transform.rotation = Quaternion.identity;
    }

    public void StartLevelTransition()
    {
        StopAllCoroutines();
        StartCoroutine(ExecuteFullTransition());
    }

    private IEnumerator ExecuteFullTransition()
    {
        // Usamos la referencia cacheada del LevelManager
        if (lm == null) lm = LevelManager.instance;

        // Detenemos el movimiento actual del virus para que no continúe arrastrando la velocidad durante la transición
        if (lm != null)
        {
            lm.isTransitioning = true;
        

            lm.isGameActive = false;
        }

        if (cachedPlaneta != null) cachedPlaneta.isInvulnerable = true;

        int currentIdx = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        GameObject mapaVisual = lm.mapList[currentIdx];
        Transform mapaTransform = mapaVisual.transform;

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

            float progresoAcel = velocidadActual / velocidadMaxima;
            if (materialFondo != null)
                materialFondo.SetFloat(vortexProp, progresoAcel * 25f);

            if (mainCam != null)
                mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, zoomMaximo, velocidadZoomIn * dt);

            if (mapaVisual)
            {
                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
                mapaTransform.localScale = Vector3.Lerp(mapaTransform.localScale, escalaObjetivoMin, suavizadoEscala * dt);
            }
            yield return null;
        }

        // --- FASE 2: CAMBIO DE MAPA (Punto crítico de LAG) ---
        // Dentro de ExecuteFullTransition en LevelTransitioner.cs
        // Busca la Fase 2 (Cambio de mapa)

        // --- FASE 2: CAMBIO DE MAPA (En el pico de velocidad) ---
        int nextMap = currentIdx + 1;
        if (nextMap < lm.mapList.Length)
        {
            // 1. Limpiamos antes de activar el siguiente para liberar RAM
            if (popManager != null) popManager.ClearAllPersonas();

            // 2. Activamos el nuevo mapa en el LevelManager
            lm.ActivateMap(nextMap);

            // 3. ACTUALIZACIÓN CRÍTICA: Ahora mapaVisual es el nuevo mapa
            mapaVisual = lm.mapList[nextMap];
            mapaTransform = mapaVisual.transform;

            // 4. Aplicamos la escala pequeña al nuevo mapa para que no aparezca gigante de golpe
            mapaTransform.localScale = escalaObjetivoMin;

            // 5. Reset de estados
            if (cachedPlaneta != null) cachedPlaneta.isInvulnerable = true;
            lm.currentSessionInfected = 0;
        }

        // --- FASE 3: ESPERA TÉCNICA ---
        float distanciaFrenado = (velocidadActual * velocidadActual) / (2f * frenado);
        while (true)
        {
            float dt = Time.deltaTime;
            mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
            float anguloActualZ = mapaTransform.localEulerAngles.z;
            float anguloFinalPredecido = (anguloActualZ + distanciaFrenado) % 360f;

            // Seguridad: si la velocidad es muy baja, salir para evitar bucle infinito
            if (anguloFinalPredecido < (velocidadActual * dt) || velocidadActual < 10f) break;
            yield return null;
        }

        // --- FASE 4: FRENAR Y CRECER ---
        while (velocidadActual > 0.1f)
        {
            float dt = Time.deltaTime;
            velocidadActual = Mathf.MoveTowards(velocidadActual, 0, frenado * dt);

            float progresoFrenado = velocidadActual / velocidadMaxima;
            // Aplicamos el valor al shader (50 a 0)
            if (materialFondo != null)
                materialFondo.SetFloat(vortexProp, progresoFrenado * 25f);

            if (mapaVisual)
            {
                mapaTransform.Rotate(Vector3.forward, velocidadActual * dt);
                mapaTransform.localScale = Vector3.Lerp(mapaTransform.localScale, escalaOriginal, suavizadoEscala * dt);
            }
            yield return null;
        }

        // --- IMPACTO FINAL Y SPAWN ---
        mapaTransform.SetPositionAndRotation(mapaTransform.position, Quaternion.identity);
        mapaTransform.localScale = escalaOriginal;

        // OPTIMIZACIÓN 3: ConfigureRound ahora es una corrutina que no spawnea todo de golpe
        if (popManager != null)
        {
            popManager.ConfigureRound(0);
        }

        if (GameSettings.instance.shakeEnabled)
        {
            OnImpactShake?.Invoke(intensidadImpacto);
        }
        if (mainCam != null)
            mainCam.orthographicSize = zoomOriginal;

        yield return StartCoroutine(DryImpactShake());

        if (cachedPlaneta != null) cachedPlaneta.isInvulnerable = false;

        // Restauramos el estado normal de juego y permitimos el control de jugador
        if (lm != null)
        {
            lm.isGameActive = true;
            lm.isTransitioning = false;
            if (lm.virusMovementScript != null) lm.virusMovementScript.enabled = true;
        }

        Time.timeScale = 1f; // En caso de que alguna transición anterior lo dejara en 0
    }

    private IEnumerator DryImpactShake()
    {
        if (!GameSettings.instance.shakeEnabled || camTransform == null) yield break;

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