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
    public static PopulationManager instance;



    [Header("Spawn Area Logic")]
    private Collider2D currentSpawnCollider;
    public float margenSeguridad = 0.5f;

    [Header("Spawn Animation")]
    public float growDuration = 0.4f;

    [Header("Duplication Settings")]
    public float fuerzaImpulsoClon = 2f; // Fuerza para que el clon no se encime

    public GameObject buggedPersonPrefab; // Arrastra aquí el prefab bugeado
    [Range(0f, 100f)]
    public float buggedSpawnChance = 5f;  // Probabilidad de 0 a 100

    private float timer;

    void Awake()
    {
        instance = this;

        if (personPrefabs.Length > 0)
            currentPrefab = personPrefabs[0];
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
            if (LevelManager.instance != null)
            {
                script.AplicarColor(LevelManager.instance.GetCurrentLevelColor());
            }
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

        timer = 0f;

        baseSpawnInterval = spawnInterval;
        ApplySpawnBonus();

        for (int i = 0; i < initialPopulation; i++)
        {
            SpawnPerson(false); // iniciales: SIEMPRE base del mapa
        }
    }


    void Update()
    {
        if (LevelManager.instance != null && !LevelManager.instance.isGameActive) return;

        timer += Time.deltaTime;

        if (LevelManager.instance != null && LevelManager.instance.isGameActive)
        {
            CheckForOutsiders();
        }

        // --- CAMBIO AQUÍ: Contar ambos tags ---
        int currentCount = GetTotalPopulationCount();

        if (timer >= spawnInterval && currentCount < maxPopulation)
        {
            SpawnPerson(true);
            timer = 0;
        }

        if (Guardado.instance != null)
        {
            float bonus = Guardado.instance.populationBonus;
            maxPopulation = baseMaxPopulation * (1f + bonus);
        }
    }

    // --- NUEVA FUNCIÓN AUXILIAR PARA CONTAR ---
    private int GetTotalPopulationCount()
    {
        int personas = GameObject.FindGameObjectsWithTag("Persona").Length;
        int corales = GameObject.FindGameObjectsWithTag("Coral").Length;
        return personas + corales;
    }

    void CheckForOutsiders()
    {
        if (currentSpawnCollider == null) return;

        // --- CAMBIO AQUÍ: Buscar y limpiar ambos ---
        LimpiarSiEstaFuera("Persona");
        LimpiarSiEstaFuera("Coral");
    }

    void LimpiarSiEstaFuera(string tag)
    {
        GameObject[] objetos = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objetos)
        {
            if (!currentSpawnCollider.OverlapPoint(obj.transform.position))
            {
                Destroy(obj);
            }
        }
    }
    void SpawnPerson(bool allowRandomPhase)
    {
        if (currentPrefab == null || currentSpawnCollider == null) return;

        Vector3 spawnPos = GetRandomPointInCollider(currentSpawnCollider);
        if (!currentSpawnCollider.OverlapPoint(spawnPos)) return;

        // Decidir qué prefab usar
        GameObject prefabToSpawn = currentPrefab;
        if (buggedPersonPrefab != null && Random.Range(0f, 100f) < buggedSpawnChance)
        {
            prefabToSpawn = buggedPersonPrefab;
        }

        // Instanciar el elegido
        GameObject newPerson = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);

        PersonaInfeccion script = newPerson.GetComponent<PersonaInfeccion>();
        if (script != null && LevelManager.instance != null)
        {
            int baseFase = 0;

            if (currentMap < LevelManager.instance.faseInicialPorMapa.Length)
                baseFase = LevelManager.instance.faseInicialPorMapa[currentMap];

            int faseFinal = baseFase;

            // Habilidad: SOLO si allowRandomPhase == true
            if (allowRandomPhase && Guardado.instance != null)
            {
                float chance = Guardado.instance.randomSpawnPhaseChance; // 0..1 (0.05 = 5%)
                if (chance > 0f && Random.value < chance)
                {
                    int maxFase = script.GetMaxFaseIndex(); // fasesSprites.Length - 1
                    faseFinal = Random.Range(0, maxFase + 1);
                }
            }

            script.EstablecerFaseDirecta(faseFinal);

            Color colorNivel = LevelManager.instance.GetCurrentLevelColor();
            script.AplicarColor(colorNivel);
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
    public void ClearAllPersonas()
    {
        // --- CAMBIO AQUÍ: Borrar ambos tags ---
        GameObject[] personas = GameObject.FindGameObjectsWithTag("Persona");
        foreach (var p in personas) Destroy(p);

        GameObject[] corales = GameObject.FindGameObjectsWithTag("Coral");
        foreach (var c in corales) Destroy(c);

        timer = 0;
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