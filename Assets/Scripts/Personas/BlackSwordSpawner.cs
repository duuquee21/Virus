using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Necesario para la lista de afectados

public class BlackSwordSpawner : MonoBehaviour
{
    [Header("Configuración del Prefab")]
    public GameObject slashPrefab;
    public Color slashColor = Color.black;
    public Color colorFaseInicial = Color.white;
    public float largoDelTajo = 30f;
    public float radioDeAparicion = 2f;

    [Header("Tiempos y Frecuencia")]
    public float frecuenciaSpawn = 0.8f;
    public float pausaIntermedia = 0.25f;
    public float esperaFinalAntesDeBorrar = 0.5f;
    public float duracionFadeOut = 0.3f;
    private float nextSpawnTime;

    [Header("Efecto de Vibración")]
    public float intensidadVibracion = 0.1f;

    [Header("Fase 1: Aparición")]
    public float duracionAparicion = 0.1f;
    public float grosorInicial = 0.1f;
    public float duracionExpansion1 = 0.15f;
    public float grosorExpansion1 = 1.8f;

    [Header("Fase 2: Latigazo")]
    public float multiplicadorVelocidadFinal = 3f;
    public float multiplicadorGrosorFinal = 3f;

    void Update()
    {
        if (Guardado.instance.hojaNegraData && Time.time > nextSpawnTime && LevelManager.instance.isGameActive)
        {
            StartCoroutine(ExecuteSlashSequence());
            nextSpawnTime = Time.time + Guardado.instance.hojaSpawnRate;
        }
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        // Si es el del agujero negro, también resetea el contador aquí
        // agujerosActivos = 0; 
    }
    IEnumerator ExecuteSlashSequence()
    {
        // 1. Setup inicial
        Vector2 desplazamientoAleatorio = Random.insideUnitCircle * radioDeAparicion;
        Vector3 posicionSpawn = new Vector3(desplazamientoAleatorio.x, desplazamientoAleatorio.y, 0);
        Quaternion randomRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        GameObject slash = Instantiate(slashPrefab, posicionSpawn, randomRotation);
        SpriteRenderer sr = slash.GetComponent<SpriteRenderer>();
        Transform t = slash.transform;

        // Lista para no golpear a la misma persona dos veces con el mismo tajo
        HashSet<PersonaInfeccion> personasGolpeadas = new HashSet<PersonaInfeccion>();

        // Color inicial (Blanco)
        Color colActual = colorFaseInicial;
        colActual.a = 0;
        if (sr != null) sr.color = colActual;

        // --- PASO 1 y 2: Expansión Blanca (Previsualización) ---
        float elapsed = 0f;
        while (elapsed < (duracionAparicion + duracionExpansion1))
        {
            float progress = elapsed / (duracionAparicion + duracionExpansion1);
            float targetGrosor = Mathf.Lerp(0f, grosorExpansion1, progress);
            t.localScale = new Vector3(largoDelTajo, targetGrosor, 1f);

            colActual.a = Mathf.Min(1f, progress * 2f);
            sr.color = colActual;

            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(pausaIntermedia);

        // --- PASO 3: EL GOLPE (Cambio a Negro y Detección) ---
        colActual = slashColor;
        colActual.a = 1f;
        if (sr != null) sr.color = colActual;

        // Aquí ejecutamos la lógica de "daño"
        AplicarDanoEnArea(slash, personasGolpeadas);

        // --- PASO 4: Expansión Final (Latigazo) ---
        float duracionExpansionFinal = duracionExpansion1 / multiplicadorVelocidadFinal;
        float grosorFinalTarget = grosorExpansion1 * multiplicadorGrosorFinal;
        elapsed = 0f;
        while (elapsed < duracionExpansionFinal)
        {
            t.localScale = new Vector3(largoDelTajo, Mathf.Lerp(grosorExpansion1, grosorFinalTarget, elapsed / duracionExpansionFinal), 1f);
            // Seguimos detectando durante el latigazo por si alguien entra justo ahora
            AplicarDanoEnArea(slash, personasGolpeadas);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- PASO 5: Vibración y Borrado ---
        // (Misma lógica de vibración que tenías...)
        float tiempoDeVibracion = 0.1f;
        Vector3 posOriginal = t.position;
        elapsed = 0f;
        while (elapsed < tiempoDeVibracion)
        {
            t.position = posOriginal + (Vector3)(Random.insideUnitCircle * intensidadVibracion);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.position = posOriginal;

        yield return new WaitForSeconds(esperaFinalAntesDeBorrar);

        // Fade out y Destroy
        elapsed = 0f;
        while (elapsed < duracionFadeOut)
        {
            colActual.a = Mathf.Lerp(1f, 0f, elapsed / duracionFadeOut);
            if (sr) sr.color = colActual;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(slash);
    }

    void AplicarDanoEnArea(GameObject slashObj, HashSet<PersonaInfeccion> golpeados)
    {
        // Usamos el Collider del tajo para encontrar víctimas
        Collider2D collider = slashObj.GetComponent<Collider2D>();
        if (collider == null) return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        List<Collider2D> resultados = new List<Collider2D>();

        collider.Overlap(filter, resultados);

        foreach (Collider2D col in resultados)
        {
            PersonaInfeccion persona = col.GetComponent<PersonaInfeccion>();

            // Si tiene el componente y no ha sido golpeada por ESTE tajo aún
            if (persona != null && !golpeados.Contains(persona))
            {
                persona.IntentarAvanzarFase(Guardado.instance.hojaFases);
                golpeados.Add(persona);

                // Debug visual
                Debug.Log("<color=black>Tajo Negro impactó a: </color>" + col.name);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, radioDeAparicion);

    }
}