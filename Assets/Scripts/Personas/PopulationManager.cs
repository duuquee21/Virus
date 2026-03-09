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
    private Collider2D currentSpawnCollider; // Donde NACEN
    private Collider2D playableAreaCollider; // Donde se PERMITE que estén
    public float margenSeguridad = 0.5f;

    [Header("Spawn Animation")]
    public float growDuration = 0.4f;

    [Header("Duplication Settings")]
    public float fuerzaImpulsoClon = 2f;

    public GameObject buggedPersonPrefab;
    [Range(0f, 100f)]
    public float buggedSpawnChance = 5f;

    private float timer;

    void Awake()
    {
        instance = this;
        if (personPrefabs.Length > 0)
            currentPrefab = personPrefabs[0];
    }

    public void InstanciarCopia(Vector3 posicion, int faseDestino, GameObject objetoQueChoco)
    {
        if (currentPrefab == null) return;

        GameObject nuevaCopia = Instantiate(currentPrefab, posicion, Quaternion.identity);
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

    // --- CAMBIO: Buscamos ambos colliders por sus respectivos Tags ---
    public void RefreshSpawnArea()
    {
        // 1. Área de NACIMIENTO
        GameObject areaObj = GameObject.FindWithTag("SpawnArea");
        if (areaObj != null)
        {
            currentSpawnCollider = areaObj.GetComponent<Collider2D>();
        }

        // 2. Área JUGABLE (Para destruir si salen)
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
        timer = 0f;
        baseSpawnInterval = spawnInterval;
        ApplySpawnBonus();
  

        // CALCULAR POBLACIÓN INICIAL CON EL BONUS
        int poblacionReal = initialPopulation;
        if (Guardado.instance != null)
        {
            // Si el bonus es 1f, sumará 1 persona extra. 
            // Si quieres que sea un porcentaje, usa: initialPopulation * (1 + bonus)
            poblacionReal = initialPopulation + (int)Guardado.instance.populationBonus;
        }

        for (int i = 0; i < poblacionReal; i++)
        {
            SpawnPerson(false);
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

        int currentCount = GetTotalPopulationCount();

        if (timer >= spawnInterval )
        {
            SpawnPerson(true);
            timer = 0;
        }
       // Debug.Log($"Spawn interval after bonus: {spawnInterval} seconds");

    }

    private int GetTotalPopulationCount()
    {
        int personas = GameObject.FindGameObjectsWithTag("Persona").Length;
        int corales = GameObject.FindGameObjectsWithTag("Coral").Length;
        return personas + corales;
    }

    void CheckForOutsiders()
    {
        // --- CAMBIO: Si no hay PlayableArea definida, usamos SpawnArea como respaldo ---
        Collider2D areaDeChequeo = (playableAreaCollider != null) ? playableAreaCollider : currentSpawnCollider;

        if (areaDeChequeo == null) return;

        LimpiarSiEstaFuera("Persona", areaDeChequeo);
        LimpiarSiEstaFuera("Coral", areaDeChequeo);
    }

    // --- CAMBIO: Ahora recibe el collider contra el cual comparar ---
    void LimpiarSiEstaFuera(string tag, Collider2D areaReferencia)
    {
        GameObject[] objetos = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objetos)
        {
            // Si NO está dentro del área de juego, se destruye
            if (!areaReferencia.OverlapPoint(obj.transform.position))
            {
                Destroy(obj);
            }
        }
    }

    void SpawnPerson(bool allowRandomPhase)
    {
        // --- AQUÍ SE SIGUE USANDO currentSpawnCollider PARA NACER ---
        if (currentPrefab == null || currentSpawnCollider == null) return;

        Vector3 spawnPos = GetRandomPointInCollider(currentSpawnCollider);

        GameObject prefabToSpawn = currentPrefab;
        if (buggedPersonPrefab != null && Random.Range(0f, 100f) < buggedSpawnChance)
        {
            prefabToSpawn = buggedPersonPrefab;
        }

        GameObject newPerson = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

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

        // Convertimos el bonus a int para restar segundos enteros
        float bonusSegundos = Guardado.instance.spawnSpeedBonus;

        // Restamos los segundos directamente a la base
        spawnInterval = baseSpawnInterval - bonusSegundos;

        // Seguridad: No permitimos que el intervalo sea menor a 0.3 segundos
        if (spawnInterval < 0.3f)
        {
            spawnInterval = 0.3f;
        }
    }
    public float GetCurrentSpawnInterval()
    {
        return spawnInterval;
    }
    public void ClearAllPersonas()
    {
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

    public int GetRoundInitialPopulation()
    {
        int poblacionReal = initialPopulation;

        if (Guardado.instance != null)
        {
            // Replicamos la lógica de ConfigureRound
            poblacionReal = initialPopulation + (int)Guardado.instance.populationBonus;
        }

        return poblacionReal;
    }
}