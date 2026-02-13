using UnityEngine;
using System.Collections;

public class LevelTransitioner : MonoBehaviour
{
    [Header("Configuración de Giro")]
    public float aceleracion = 1500f;
    public float frenado = 1200f;
    public float velocidadMaxima = 3500f;

    private float velocidadActual = 0f;

    public void StartLevelTransition()
    {
        StartCoroutine(ExecuteFullTransition());
    }

    private IEnumerator ExecuteFullTransition()
    {
        // 1. Bloqueamos el juego
        LevelManager.instance.isGameActive = false;

        // --- NUEVO: BUSCAR PLANETA Y HACERLO INVULNERABLE ---
        // Buscamos el objeto que tenga el script PlanetCrontrollator
        PlanetCrontrollator planeta = FindFirstObjectByType<PlanetCrontrollator>();
        if (planeta != null) planeta.isInvulnerable = true;

        // 2. Identificamos el mapa actual
        int currentIdx = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        GameObject mapaVisual = LevelManager.instance.mapList[currentIdx];

        // --- FASE: ACELERAR ---
        while (velocidadActual < velocidadMaxima)
        {
            velocidadActual += aceleracion * Time.deltaTime;
            if (mapaVisual) mapaVisual.transform.Rotate(Vector3.forward, velocidadActual * Time.deltaTime);
            yield return null;
        }

        // --- FASE: INTERCAMBIO ---
        int nextMap = currentIdx + 1;
        if (nextMap < LevelManager.instance.mapList.Length)
        {
            LevelManager.instance.ActivateMap(nextMap);
            mapaVisual = LevelManager.instance.mapList[nextMap];

            // Al cambiar de mapa, volvemos a asegurar que el nuevo script (si es otro objeto) sea invulnerable
            planeta = FindFirstObjectByType<PlanetCrontrollator>();
            if (planeta != null) planeta.isInvulnerable = true;

            LevelManager.instance.currentSessionInfected = 0;
        }

        // --- FASE: FRENAR ---
        while (velocidadActual > 0)
        {
            velocidadActual -= frenado * Time.deltaTime;
            if (mapaVisual) mapaVisual.transform.Rotate(Vector3.forward, velocidadActual * Time.deltaTime);
            yield return null;
        }

        velocidadActual = 0;

        // --- NUEVO: QUITAR INVULNERABILIDAD ---
        if (planeta != null) planeta.isInvulnerable = false;

        LevelManager.instance.isGameActive = true;
    }

    /// <summary>
    /// Busca todos los colliders en los hijos del objeto y los activa/desactiva.
    /// </summary>

}