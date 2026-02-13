using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PopulationManager : MonoBehaviour
{
    [Header("Prefabs & Selection")]
    public GameObject[] personPrefabs;
    private GameObject currentPrefab;

    [Header("Settings")]
    public float spawnInterval = 3f;
    public float maxPopulation = 15f;
    private float baseSpawnInterval;
    public float baseMaxPopulation = 15f;
    public int initialPopulation = 10;

    [Header("Outsider Settings")]
    public float tiempoGraciaFuera = 2f; // Tiempo en segundos antes de eliminar
    private Dictionary<GameObject, float> outsidersTimer = new Dictionary<GameObject, float>();

    [Header("Spawn Area Logic")]
    private Collider2D currentSpawnCollider;
    public float margenSeguridad = 0.5f;

    [Header("Spawn Animation")]
    public float growDuration = 0.4f;

    [Header("Duplication Settings")]
    public float fuerzaImpulsoClon = 2f; // Fuerza para que el clon no se encime

    private float timer;

    void Awake()
    {
        if (personPrefabs.Length > 0) currentPrefab = personPrefabs[0];
    }

    // --- NUEVA FUNCIÓN: INSTANCIAR COPIA (Habilidad Duplicación) ---
    // En PopulationManager.cs, actualiza la función InstanciarCopia:

    // --- MODIFICA ESTA FUNCIÓN DENTRO DE TU PopulationManager.cs ---

    public void InstanciarCopia(Vector3 posicion, int faseDestino, GameObject objetoQueChoco)
    {
        // CAMBIO CLAVE: En lugar de instanciar 'objetoQueChoco' (el duplicado con fallos),
        // instanciamos 'currentPrefab', que es la copia limpia del disco.
        if (currentPrefab == null) return;

        // 1. Instancia el prefab "virgen" del mapa actual
        GameObject nuevaCopia = Instantiate(currentPrefab, posicion, Quaternion.identity);

        // 2. Configuramos su fase inicial inmediatamente
        PersonaInfeccion script = nuevaCopia.GetComponent<PersonaInfeccion>();
        if (script != null)
        {
            // Esto hará que el prefab limpio se transforme visualmente 
            // a la fase en la que chocaste (Triángulo, Cuadrado, etc.)
            script.EstablecerFaseDirecta(faseDestino);
        }

        // 3. Reset físico y pequeño impulso para que no se encimen
        Rigidbody2D rb = nuevaCopia.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.AddForce(Random.insideUnitCircle.normalized * fuerzaImpulsoClon, ForceMode2D.Impulse);
        }

        // 4. Animación de crecimiento para que la aparición sea suave
        Vector3 targetScale = nuevaCopia.transform.localScale;
        nuevaCopia.transform.localScale = Vector3.zero;
        StartCoroutine(GrowFromZero(nuevaCopia.transform, targetScale));

        Debug.Log("<color=green>Instanciado nuevo prefab limpio en fase: </color>" + faseDestino);
    }
    public void RefreshSpawnArea()
    {
        GameObject areaObj = GameObject.FindWithTag("SpawnArea");
        if (areaObj != null)
        {
            currentSpawnCollider = areaObj.GetComponent<Collider2D>();
        }
    }

    public void SelectPrefab(int index)
    {
        if (index >= 0 && index < personPrefabs.Length)
        {
            currentPrefab = personPrefabs[index];
        }
    }

    public void ConfigureRound(int ignored)
    {
        RefreshSpawnArea();
        timer = 0;
        baseSpawnInterval = spawnInterval;
        ApplySpawnBonus();

        // ❌ ELIMINA O COMENTA ESTO: No queremos borrar a nadie al cambiar de nivel
        /*
        GameObject[] antiguos = GameObject.FindGameObjectsWithTag("Persona");
        foreach (var p in antiguos) Destroy(p);
        */

        // Opcional: Solo spawnear la diferencia si hay poca gente
        int currentCount = GameObject.FindGameObjectsWithTag("Persona").Length;
        for (int i = 0; i < (initialPopulation - currentCount); i++)
        {
            SpawnPerson();
        }
    }

    void Update()
    {
        // Mantenemos esto para que el spawn sea constante
        if (LevelManager.instance != null && !LevelManager.instance.isGameActive) return;

        timer += Time.deltaTime;

        if (LevelManager.instance != null && LevelManager.instance.isGameActive)
        {
            //  CheckForOutsiders();
        }

        int currentCount = GameObject.FindGameObjectsWithTag("Persona").Length;

        if (timer >= spawnInterval && currentCount < maxPopulation)
        {
            SpawnPerson();
            timer = 0;
        }

        if (Guardado.instance != null)
        {
            float bonus = Guardado.instance.populationBonus;
            maxPopulation = baseMaxPopulation * (1f + bonus);
        }
    }

    void CheckForOutsiders()
    {
        if (currentSpawnCollider == null) return;

        GameObject[] personas = GameObject.FindGameObjectsWithTag("Persona");
        foreach (GameObject p in personas)
        {
            if (!currentSpawnCollider.OverlapPoint(p.transform.position))
            {
                Destroy(p);
            }
        }
    }
    void SpawnPerson()
    {
        if (currentPrefab == null || currentSpawnCollider == null) return;

        Vector3 spawnPos = GetRandomPointInCollider(currentSpawnCollider);
        if (!currentSpawnCollider.OverlapPoint(spawnPos)) return;

        GameObject newPerson = Instantiate(currentPrefab, spawnPos, Quaternion.identity);

        // 🔥 NUEVO: aplicar fase según mapa
        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);

        PersonaInfeccion script = newPerson.GetComponent<PersonaInfeccion>();
        if (script != null && LevelManager.instance != null)
        {
            if (currentMap < LevelManager.instance.faseInicialPorMapa.Length)
            {
                script.EstablecerFaseDirecta(LevelManager.instance.faseInicialPorMapa[currentMap]);
            }
        }

        Vector3 targetScale = newPerson.transform.localScale;
        newPerson.transform.localScale = Vector3.zero;
        StartCoroutine(GrowFromZero(newPerson.transform, targetScale));
    }

    Vector3 GetRandomPointInCollider(Collider2D collider)
    {
        Bounds bounds = collider.bounds;
        Vector3 point;
        int attempts = 0;

        do
        {
            float x = Random.Range(bounds.min.x + margenSeguridad, bounds.max.x - margenSeguridad);
            float y = Random.Range(bounds.min.y + margenSeguridad, bounds.max.y - margenSeguridad);
            point = new Vector3(x, y, 0);
            attempts++;

            if (collider.OverlapPoint(point)) return point;

        } while (attempts < 50);

        return bounds.center;
    }

    void ApplySpawnBonus()
    {
        if (Guardado.instance == null) return;
        float bonus = Guardado.instance.spawnSpeedBonus;
        spawnInterval = baseSpawnInterval * (1f - bonus);
        if (spawnInterval < 0.3f) spawnInterval = 0.3f;
    }

    IEnumerator GrowFromZero(Transform target, Vector3 finalScale)
    {
        float t = 0f;
        while (t < growDuration)
        {
            if (target == null) yield break;
            t += Time.deltaTime;
            float normalized = t / growDuration;
            float eased = Mathf.SmoothStep(0f, 1f, normalized);
            target.localScale = Vector3.Lerp(Vector3.zero, finalScale, eased);
            yield return null;
        }
        if (target != null) target.localScale = finalScale;
    }

    public void SetZonePrefab(int index) => SelectPrefab(index);
}