using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PopulationManager : MonoBehaviour
{
    [Header("Prefabs & Selection")]
    public GameObject[] personPrefabs;
    private GameObject currentPrefab;

    [Header("Settings")]
    public float spawnInterval = 18f;
    private float baseSpawnInterval;

    public int initialPopulation = 10;
    public static PopulationManager instance;

    [Header("Spawn & Playable Areas")]
    private Collider2D currentSpawnCollider;
    private Collider2D playableAreaCollider;
    public float margenSeguridad = 0.5f;

    [Header("Spawn Animation")]
    public float growDuration = 0.4f;

    [Header("Duplication Settings")]
    public float fuerzaImpulsoClon = 2f;

    [Header("Bugged Person Settings")]
    public GameObject buggedPersonPrefab;
    [Range(0f, 100f)]
    public float buggedSpawnChance = 5f;

    private float timer;

    // === CACHÉ Y OPTIMIZACIÓN ===
    private HashSet<GameObject> personasVivas = new HashSet<GameObject>();
    private HashSet<GameObject> coralesVivos = new HashSet<GameObject>();
    private HashSet<GameObject> buggedPersonas = new HashSet<GameObject>(); // <-- NUEVA CACHÉ

    private float checkOutsidersTimer = 0f;
    private float checkOutsidersInterval = 0.25f;

    void Awake()
    {
        instance = this;
        baseSpawnInterval = spawnInterval;

        if (personPrefabs.Length > 0)
            currentPrefab = personPrefabs[0];
    }

    private void UpdateBuggedChance()
    {
        if (Guardado.instance != null)
        {
            buggedSpawnChance = Guardado.instance.buggedSpawnChance;
        }
    }

    public void InstanciarCopia(Vector3 posicion, int faseDestino, GameObject objetoQueChoco)
    {
        if (currentPrefab == null) return;

        GameObject nuevaCopia = Instantiate(currentPrefab, posicion, Quaternion.identity);
        personasVivas.Add(nuevaCopia);

        PersonaInfeccion script = nuevaCopia.GetComponent<PersonaInfeccion>();

        if (script != null)
        {
            script.EstablecerFaseDirecta(faseDestino);
            if (LevelManager.instance != null)
            {
                script.AplicarColor(LevelManager.instance.GetCurrentLevelColor());
            }
        }

        Rigidbody2D rb = nuevaCopia.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.AddForce(Random.insideUnitCircle.normalized * fuerzaImpulsoClon, ForceMode2D.Impulse);
        }

        Vector3 targetScale = nuevaCopia.transform.localScale;
        nuevaCopia.transform.localScale = Vector3.zero;
        StartCoroutine(GrowFromZero(nuevaCopia.transform, targetScale));
    }

    public void RefreshSpawnArea()
    {
        GameObject areaObj = GameObject.FindWithTag("SpawnArea");
        if (areaObj != null)
        {
            currentSpawnCollider = areaObj.GetComponent<Collider2D>();
        }

        GameObject playableObj = GameObject.FindWithTag("PlayableArea");
        if (playableObj != null)
        {
            playableAreaCollider = playableObj.GetComponent<Collider2D>();
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
        UpdateBuggedChance();
        timer = 0f;

        int poblacionReal = initialPopulation;
        if (Guardado.instance != null)
            poblacionReal = initialPopulation + (int)Guardado.instance.populationBonus;

        StartCoroutine(SpawnInitialPopulationRoutine(poblacionReal));
    }

    private IEnumerator SpawnInitialPopulationRoutine(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            SpawnPerson(false);
            yield return null;
        }
    }

    void Update()
    {
        if (LevelManager.instance != null && !LevelManager.instance.isGameActive) return;

        timer += Time.deltaTime;

        checkOutsidersTimer += Time.deltaTime;
        if (checkOutsidersTimer >= checkOutsidersInterval)
        {
            CheckForOutsiders();
            checkOutsidersTimer = 0f;
        }

        if (timer >= spawnInterval)
        {
            UpdateBuggedChance();
            SpawnPerson(true);
            timer = 0;
        }
    }

    private int GetTotalPopulationCount()
    {
        return personasVivas.Count + coralesVivos.Count;
    }

    void CheckForOutsiders()
    {
        Collider2D areaDeChequeo = (playableAreaCollider != null) ? playableAreaCollider : currentSpawnCollider;
        if (areaDeChequeo == null) return;

        LimpiarSiEstaFuera(personasVivas, areaDeChequeo);
        LimpiarSiEstaFuera(coralesVivos, areaDeChequeo);
    }

    void LimpiarSiEstaFuera(HashSet<GameObject> objetos, Collider2D areaReferencia)
    {
        List<GameObject> toRemove = new List<GameObject>();

        foreach (GameObject obj in objetos)
        {
            if (obj == null) { toRemove.Add(obj); continue; }

            if (!areaReferencia.OverlapPoint(obj.transform.position))
            {
                toRemove.Add(obj);
                Destroy(obj);
            }
        }

        foreach (GameObject obj in toRemove)
        {
            objetos.Remove(obj);
        }
    }

    private void LimpiarCacheObjetosDestruidos()
    {
        personasVivas.RemoveWhere(obj => obj == null);
        coralesVivos.RemoveWhere(obj => obj == null);
        buggedPersonas.RemoveWhere(obj => obj == null); // <-- LIMPIRAR CACHÉ DE BUGEADOS
    }

    void SpawnPerson(bool allowRandomPhase)
    {
        if (currentPrefab == null || currentSpawnCollider == null) return;

        Vector3 spawnPos = GetRandomPointInCollider(currentSpawnCollider);

        // --- LÓGICA DE SPAWN BUGGEADO CON LÍMITE ---
        buggedPersonas.RemoveWhere(obj => obj == null); // Aseguramos un conteo exacto

        GameObject prefabToSpawn = currentPrefab;
        bool isBuggedSpawn = false;

        if (buggedPersonPrefab != null && Guardado.instance != null)
        {
            // Verificamos si NO hemos superado el límite y si pasamos la probabilidad
            if (buggedPersonas.Count < Guardado.instance.buggedSpawnLimit && Random.Range(0f, 100f) < buggedSpawnChance)
            {
                prefabToSpawn = buggedPersonPrefab;
                isBuggedSpawn = true;
            }
        }

        GameObject newPerson = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        personasVivas.Add(newPerson);

        if (isBuggedSpawn) buggedPersonas.Add(newPerson); // Añadimos a su caché específica

        ConfigurarPersonaInstanciada(newPerson, allowRandomPhase);
    }

    private void ConfigurarPersonaInstanciada(GameObject newPerson, bool allowRandomPhase)
    {
        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        PersonaInfeccion script = newPerson.GetComponent<PersonaInfeccion>();

        if (script != null && LevelManager.instance != null)
        {
            int baseFase = 0;
            if (currentMap < LevelManager.instance.faseInicialPorMapa.Length)
                baseFase = LevelManager.instance.faseInicialPorMapa[currentMap];

            int faseFinal = baseFase;

            if (allowRandomPhase && Guardado.instance != null)
            {
                float chance = Guardado.instance.randomSpawnPhaseChance;
                if (chance > 0f && Random.value < chance)
                {
                    int maxFase = script.GetMaxFaseIndex();
                    faseFinal = Random.Range(0, maxFase + 1);
                }
            }

            script.EstablecerFaseDirecta(faseFinal);
            script.AplicarColor(LevelManager.instance.GetCurrentLevelColor());
        }

        Vector3 targetScale = newPerson.transform.localScale;
        newPerson.transform.localScale = Vector3.zero;
        if (gameObject.activeInHierarchy) StartCoroutine(GrowFromZero(newPerson.transform, targetScale));
    }

    Vector3 GetRandomPointInCollider(Collider2D col)
    {
        if (col == null) return Vector3.zero;

        Bounds bounds = col.bounds;

        if (bounds.size.magnitude < 0.1f)
        {
            Debug.LogWarning("El SpawnCollider tiene un tamaño casi nulo. Revisando posición...");
            return col.transform.position;
        }

        Vector2 randomPoint = Vector2.zero;
        bool puntoValido = false;
        int intentos = 0;

        while (!puntoValido && intentos < 30)
        {
            float rx = Random.Range(bounds.min.x + margenSeguridad, bounds.max.x - margenSeguridad);
            float ry = Random.Range(bounds.min.y + margenSeguridad, bounds.max.y - margenSeguridad);
            randomPoint = new Vector2(rx, ry);

            if (col.OverlapPoint(randomPoint))
            {
                puntoValido = true;
            }
            intentos++;
        }

        return puntoValido ? (Vector3)randomPoint : new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            0
        );
    }

    void ApplySpawnBonus()
    {
        if (Guardado.instance == null) return;
        float bonusSegundos = Guardado.instance.spawnSpeedBonus;
        spawnInterval = baseSpawnInterval - bonusSegundos;

        if (spawnInterval < 0.3f)
        {
            spawnInterval = 0.3f;
        }
    }

    public float GetCurrentSpawnInterval()
    {
        float bonusSegundos = Guardado.instance.spawnSpeedBonus;
        spawnInterval = baseSpawnInterval - bonusSegundos;
        return spawnInterval;
    }

    public void ClearAllPersonas()
    {
        foreach (var p in personasVivas)
        {
            if (p != null) Destroy(p);
        }
        personasVivas.Clear();
        buggedPersonas.Clear(); // <-- VACIAR CACHÉ

        foreach (var c in coralesVivos)
        {
            if (c != null) Destroy(c);
        }
        coralesVivos.Clear();

        timer = 0;
    }

    public void SpawnPersonAtBasePhase()
    {
        if (currentPrefab == null || currentSpawnCollider == null) return;

        UpdateBuggedChance();
        buggedPersonas.RemoveWhere(obj => obj == null); // Aseguramos un conteo exacto

        Vector3 spawnPos = GetRandomPointInCollider(currentSpawnCollider);

        GameObject prefabToSpawn = currentPrefab;
        bool isBuggedSpawn = false;

        if (buggedPersonPrefab != null && Guardado.instance != null)
        {
            // Verificamos límite y probabilidad
            if (buggedPersonas.Count < Guardado.instance.buggedSpawnLimit && Random.Range(0f, 100f) < buggedSpawnChance)
            {
                prefabToSpawn = buggedPersonPrefab;
                isBuggedSpawn = true;
            }
        }

        GameObject newPerson = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        personasVivas.Add(newPerson);
        if (isBuggedSpawn) buggedPersonas.Add(newPerson); // Añadimos a su caché específica

        ConfigurarPersonaInstanciada(newPerson, false);
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

    public int GetRoundInitialPopulation()
    {
        int poblacionReal = initialPopulation;

        if (Guardado.instance != null)
        {
            poblacionReal = initialPopulation + (int)Guardado.instance.populationBonus;
        }

        return poblacionReal;
    }

    public void RegisterCoral(GameObject coral)
    {
        if (coral != null)
            coralesVivos.Add(coral);
    }

    public void UnregisterCoral(GameObject coral)
    {
        if (coral != null)
            coralesVivos.Remove(coral);
    }

    public void RegisterPersona(GameObject persona)
    {
        if (persona != null)
            personasVivas.Add(persona);
    }

    public void UnregisterPersona(GameObject persona)
    {
        if (persona != null)
        {
            personasVivas.Remove(persona);
            buggedPersonas.Remove(persona); // Por si se desregistra a mano
        }
    }
}