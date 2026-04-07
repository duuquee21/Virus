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
    private float checkOutsidersInterval = 0.1f;

    [Header("Pooling")]
    // Diccionario para manejar múltiples pools (uno por cada prefab diferente que tengas)
    private Dictionary<GameObject, Queue<GameObject>> poolDePersonas = new Dictionary<GameObject, Queue<GameObject>>();

    private bool limpiandoGradualmente = false;

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
    public void HardResetPool()
    {
        // 1. Destruir físicamente todos los objetos en las colas del pool
        foreach (var pool in poolDePersonas.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }
        poolDePersonas.Clear();

        // 2. Limpiar las listas de seguimiento
        personasVivas.Clear();
        coralesVivos.Clear();
        buggedPersonas.Clear();

        Debug.Log("<color=red>Pool y Listas de población vaciadas por completo.</color>");
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

    // --- MODIFICACIÓN DEL SPAWN INICIAL ---
    private IEnumerator SpawnInitialPopulationRoutine(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            // true: SÍ permitimos que nazcan bugeados al instanciar las primeras figuras
            SpawnLogic(false, true);
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
            // false: NO permitimos bugeados por respawn automático
            SpawnLogic(true, false);
            timer = 0;
        }
    }

    public int GetTotalPopulationCount()
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

                // AÑADIDO: Comprobamos en qué caja debe ir si se sale de la pantalla
                GameObject cajaCorrecta = buggedPersonas.Contains(obj) ? buggedPersonPrefab : currentPrefab;
                DevolverAlPool(obj, cajaCorrecta);
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

    // --- NUEVA LÓGICA UNIFICADA DE PREFABS ---
    private GameObject ObtenerPrefabAdecuado(bool permitirBugged, out bool esBugged)
    {
        esBugged = false;
        GameObject prefabSeleccionado = currentPrefab;

        // Si el límite en Guardado es 0 o menor, ignoramos la lógica de bugeados
        if (permitirBugged && buggedPersonPrefab != null && Guardado.instance != null && Guardado.instance.buggedSpawnLimit > 0)
        {
            buggedPersonas.RemoveWhere(obj => obj == null);
            if (buggedPersonas.Count < Guardado.instance.buggedSpawnLimit && Random.Range(0f, 100f) < buggedSpawnChance)
            {
                prefabSeleccionado = buggedPersonPrefab;
                esBugged = true;
            }
        }

        return prefabSeleccionado;
    }

    // --- NUEVA LÓGICA UNIFICADA DE SPAWN ---
    void SpawnLogic(bool allowRandomPhase, bool permitirBugeado)
    {
        if (currentPrefab == null || currentSpawnCollider == null) return;

        Vector3 spawnPos = GetRandomPointInCollider(currentSpawnCollider);

        bool isBuggedSpawn;
        GameObject prefabToSpawn = ObtenerPrefabAdecuado(permitirBugeado, out isBuggedSpawn);

        GameObject newPerson = ObtenerDelPool(prefabToSpawn, spawnPos);
        personasVivas.Add(newPerson);

        if (isBuggedSpawn) buggedPersonas.Add(newPerson);

        ConfigurarPersonaInstanciada(newPerson, allowRandomPhase);
    }

    // Mantenemos la función original como wrapper por si otros scripts la llaman
    void SpawnPerson(bool allowRandomPhase)
    {
        SpawnLogic(allowRandomPhase, false);
    }

    private void ConfigurarPersonaInstanciada(GameObject newPerson, bool allowRandomPhase)
    {
        int currentMap = PlayerPrefs.GetInt("CurrentMapIndex", 0);
        PersonaInfeccion script = newPerson.GetComponent<PersonaInfeccion>();
        Movement movScript = newPerson.GetComponent<Movement>(); // Obtenemos el script de movimiento

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
                    faseFinal = Random.Range(0, script.GetMaxFaseIndex() + 1);
                }
            }

            Color colorNivel = LevelManager.instance.GetCurrentLevelColor();

            // AQUI USAMOS EL REINICIO TOTAL
            script.ReinicioTotalDesdePool(faseFinal, colorNivel);
            if (movScript != null) movScript.ResetearMovimientoDesdePool();
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
    public void StartGradualClear(float duration)
    {
        StopAllCoroutines(); // Evita conflictos con otros spawns
        StartCoroutine(ClearGraduallyRoutine(duration));
    }

    private IEnumerator ClearGraduallyRoutine(float duration)
    {
        limpiandoGradualmente = true;

        // 1. Obtenemos todos los vivos actualmente (usamos los HashSets para ser más eficientes)
        List<GameObject> aEliminar = new List<GameObject>(personasVivas);
        aEliminar.AddRange(coralesVivos);

        if (aEliminar.Count == 0)
        {
            limpiandoGradualmente = false;
            yield break;
        }

        // 2. Calculamos el intervalo exacto. 
        // Usamos el 90% de la duración para asegurar que el mapa esté vacío un poco antes de que termine el zoom.
        float intervalo = (duration * 0.9f) / aEliminar.Count;

        for (int i = 0; i < aEliminar.Count; i++)
        {
            if (aEliminar[i] != null)
            {
                // Si usas pooling, lo ideal es DevolverAlPool, si no, Destroy.
                // Aquí usamos Destroy para asegurar limpieza total al fin del nivel.
                Destroy(aEliminar[i]);
            }

            // Usamos Realtime para que no le afecte el Slow Motion (Time.timeScale) del LevelManager
            yield return new WaitForSecondsRealtime(intervalo);
        }

        // 3. Limpieza final de las listas
        personasVivas.Clear();
        coralesVivos.Clear();
        buggedPersonas.Clear();
        limpiandoGradualmente = false;
    }
    public float GetCurrentSpawnInterval()
    {
        float bonusSegundos = Guardado.instance.spawnSpeedBonus;
        spawnInterval = baseSpawnInterval - bonusSegundos;
        return spawnInterval;
    }

    public GameObject SpawnPersonAtPosition(Vector3 pos, bool infectada = false)
    {
        if (currentPrefab == null) return null;

        UpdateBuggedChance();
        buggedPersonas.RemoveWhere(obj => obj == null);

        // false: Este es un spawn manual/posicional, no queremos bugeados aquí
        bool isBuggedSpawn;
        GameObject prefabToSpawn = ObtenerPrefabAdecuado(false, out isBuggedSpawn);

        GameObject newPerson = ObtenerDelPool(prefabToSpawn, pos);
        personasVivas.Add(newPerson);
        if (isBuggedSpawn) buggedPersonas.Add(newPerson);

        // CONFIGURACIÓN BASE (sin random)
        ConfigurarPersonaInstanciada(newPerson, false);

        // FORZAR POSICIÓN EXACTA CENTRO
        newPerson.transform.position = pos;

        // 🔥 FORZAR INFECTADA SI SE PIDE
        if (infectada)
        {
            PersonaInfeccion p = newPerson.GetComponent<PersonaInfeccion>();
            if (p != null)
            {
                p.EstablecerFaseDirecta(p.GetMaxFaseIndex());
                p.IntentarAvanzarFase();
            }
        }

        return newPerson;
    }
    public void ClearAllPersonas()
    {
        if (limpiandoGradualmente) return;

        // 1. Creamos una COPIA de la lista para poder borrar sin que dé error
        List<GameObject> copiaVivos = new List<GameObject>(personasVivas);

        foreach (var p in copiaVivos)
        {
            if (p != null)
            {
                // Comprobamos en qué caja (pool) debe ir para no mezclarlos
                GameObject cajaCorrecta = buggedPersonas.Contains(p) ? buggedPersonPrefab : currentPrefab;
                DevolverAlPool(p, cajaCorrecta);
            }
        }

        personasVivas.Clear();
        buggedPersonas.Clear();

        // Hacemos lo mismo con los corales por si acaso
        List<GameObject> copiaCorales = new List<GameObject>(coralesVivos);
        foreach (var c in copiaCorales)
        {
            if (c != null) Destroy(c);
        }
        coralesVivos.Clear();

        timer = 0;
    }
    public void SpawnPersonAtBasePhase()
    {
        // false: Tampoco queremos bugeados en esta llamada
        SpawnLogic(false, false);
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
    private GameObject ObtenerDelPool(GameObject prefab, Vector3 posicion)
    {
        if (prefab == null) return null;

        if (!poolDePersonas.ContainsKey(prefab))
        {
            poolDePersonas[prefab] = new Queue<GameObject>();
        }

        // Buscamos en la caja hasta encontrar uno válido
        while (poolDePersonas[prefab].Count > 0)
        {
            GameObject obj = poolDePersonas[prefab].Dequeue();

            if (obj != null)
            {
                // CONTROL DE SEGURIDAD EXTRA:
                // Si un impostor logró colarse en esta caja antes de aplicar este parche, 
                // lo detectamos aquí mismo y lo eliminamos. ¡El pool se limpia solo!
                if (obj.name.Contains(prefab.name))
                {
                    obj.transform.position = posicion;
                    obj.SetActive(true);
                    return obj;
                }
                else
                {
                    // ¡Era un infiltrado de la run anterior! Lo destruimos para siempre.
                    Destroy(obj);
                }
            }
        }

        // Si la caja estaba vacía o solo tenía impostores, creamos uno nuevo 100% puro
        GameObject nuevoObj = Instantiate(prefab, posicion, Quaternion.identity);
        return nuevoObj;
    }

    public void DevolverAlPool(GameObject obj, GameObject prefabOriginal)
    {
        if (obj == null) return;

        // 1. EL PORTERO: Miramos su nombre real, sin importar de qué lista venga.
        GameObject cajaCorrecta = prefabOriginal;

        if (buggedPersonPrefab != null && obj.name.Contains(buggedPersonPrefab.name))
        {
            cajaCorrecta = buggedPersonPrefab;
        }
        else if (currentPrefab != null && obj.name.Contains(currentPrefab.name))
        {
            cajaCorrecta = currentPrefab;
        }

        // 2. Lo desactivamos (ahora no importa si se borra de las listas aquí)
        obj.SetActive(false);

        // 3. Lo metemos en su caja blindada
        if (!poolDePersonas.ContainsKey(cajaCorrecta))
        {
            poolDePersonas[cajaCorrecta] = new Queue<GameObject>();
        }

        poolDePersonas[cajaCorrecta].Enqueue(obj);
    }

    public void NukeMapAndPools()
    {
        // 1. Destruimos FÍSICAMENTE todo lo que esté vivo en las listas (nada de guardarlos en cajas)
        foreach (GameObject obj in personasVivas) { if (obj != null) Destroy(obj); }
        foreach (GameObject obj in buggedPersonas) { if (obj != null) Destroy(obj); }
        foreach (GameObject obj in coralesVivos) { if (obj != null) Destroy(obj); }

        personasVivas.Clear();
        buggedPersonas.Clear();
        coralesVivos.Clear();

        // 2. Vaciamos las cajas del Pool y destruimos la "basura" que haya dentro
        foreach (var pool in poolDePersonas.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }
        poolDePersonas.Clear();
    }
}