using System;
using System.Collections;
using UnityEngine;
using System.Collections; 
public class LevelTransitioner : MonoBehaviour
{
    [Header("Configuración de Giro")]
    public float aceleracion = 1500f;
    public float frenado = 1200f;
    public float velocidadMaxima = 3500f;

    [Header("Configuración de Escala")]
    public float escalaMinima = 0.6f;
    public float suavizadoEscala = 8f; // Un poco más rápido para el impacto

    [Header("Configuración de Impacto Seco")]
    [Tooltip("Fuerza del primer golpe")]
    public float intensidadImpacto = 0.5f;
    [Tooltip("Qué tan rápido se detiene la vibración (más alto = más seco)")]
    public float velocidadRetorno = 5f;

    private float velocidadActual = 0f;
    private Vector3 escalaOriginal = Vector3.one;
    private Camera mainCam;

    // 2. Crea el Evento Estático
    public static event Action<float> OnImpactShake;

    void Awake()
    {
        mainCam = Camera.main;
    }

    public void StartLevelTransition()
    {
        StartCoroutine(ExecuteFullTransition());
    }

    private IEnumerator ExecuteFullTransition()
    {
        LevelManager.instance.isGameActive = false;

        PlanetCrontrollator planeta = FindFirstObjectByType<PlanetCrontrollator>();
        if (planeta != null) planeta.isInvulnerable = true;

        int currentIdx = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        GameObject mapaVisual = LevelManager.instance.mapList[currentIdx];

        if (mapaVisual)
        {
            escalaOriginal = mapaVisual.transform.localScale;

            // --- NUEVA CONEXIÓN: Avisar al ManualSetCycler ---
            // Buscamos el script en el mapa actual o sus hijos
            ManualSetCycler cycler = mapaVisual.GetComponentInChildren<ManualSetCycler>();
            if (cycler != null)
            {
                cycler.TriggerTransition();
            }
        }

        // --- FASE 1: ACELERAR Y ENCOGER ---
        // Al empezar este bucle, el mapa empieza a girar y la escala ya ha sido activada arriba
        Vector3 escalaObjetivoMin = escalaOriginal * escalaMinima;
        while (velocidadActual < velocidadMaxima)
        {
            velocidadActual += aceleracion * Time.deltaTime;
            if (mapaVisual)
            {
                mapaVisual.transform.Rotate(Vector3.forward, velocidadActual * Time.deltaTime);
                mapaVisual.transform.localScale = Vector3.Lerp(mapaVisual.transform.localScale, escalaObjetivoMin, suavizadoEscala * Time.deltaTime);
            }
            yield return null;
        }

        // --- FASE 2: CAMBIO DE MAPA ---
        int nextMap = currentIdx + 1;
        if (nextMap < LevelManager.instance.mapList.Length)
        {
            LevelManager.instance.ActivateMap(nextMap);
            mapaVisual = LevelManager.instance.mapList[nextMap];
            mapaVisual.transform.localScale = escalaObjetivoMin;

            planeta = FindFirstObjectByType<PlanetCrontrollator>();
            if (planeta != null) planeta.isInvulnerable = true;
            LevelManager.instance.currentSessionInfected = 0;
        }

        // --- FASE 3: ESPERA TÉCNICA ---
        float distanciaFrenado = (velocidadActual * velocidadActual) / (2f * frenado);
        bool calculandoMomento = true;
        while (calculandoMomento)
        {
            mapaVisual.transform.Rotate(Vector3.forward, velocidadActual * Time.deltaTime);
            float anguloActualZ = mapaVisual.transform.localEulerAngles.z;
            float anguloFinalPredecido = (anguloActualZ + distanciaFrenado) % 360f;
            if (anguloFinalPredecido < (velocidadActual * Time.deltaTime)) calculandoMomento = false;
            yield return null;
        }

        // --- FASE 4: FRENAR Y CRECER ---
        while (velocidadActual > 0)
        {
            velocidadActual -= frenado * Time.deltaTime;
            velocidadActual = Mathf.Max(velocidadActual, 0);

            if (mapaVisual)
            {
                mapaVisual.transform.Rotate(Vector3.forward, velocidadActual * Time.deltaTime);
                mapaVisual.transform.localScale = Vector3.Lerp(mapaVisual.transform.localScale, escalaOriginal, suavizadoEscala * Time.deltaTime);
            }
            yield return null;
        }

        // --- IMPACTO FINAL ---
        mapaVisual.transform.localRotation = Quaternion.identity;
        mapaVisual.transform.localScale = escalaOriginal;
        // 3. Disparamos el evento para TODOS los objetos
        OnImpactShake?.Invoke(intensidadImpacto);

        // Lanzamos el impacto seco
        StartCoroutine(DryImpactShake());

        if (planeta != null) planeta.isInvulnerable = false;
        LevelManager.instance.isGameActive = true;
    }

    private IEnumerator DryImpactShake()
    {
        Vector3 posOriginal = mainCam.transform.localPosition;
        float fuerzaActual = intensidadImpacto;

        while (fuerzaActual > 0.01f)
        {
            // Cambia Random por UnityEngine.Random
            float x = UnityEngine.Random.Range(-1f, 1f) * fuerzaActual;
            float y = UnityEngine.Random.Range(-1f, 1f) * fuerzaActual;

            mainCam.transform.localPosition = posOriginal + new Vector3(x, y, 0);
            fuerzaActual = Mathf.Lerp(fuerzaActual, 0, Time.deltaTime * velocidadRetorno);
            yield return null;
        }
        mainCam.transform.localPosition = posOriginal;
    }
}