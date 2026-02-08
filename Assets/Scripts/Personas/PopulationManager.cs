using UnityEngine;
using System.Collections.Generic;

public class PopulationManager : MonoBehaviour
{
    [Header("Prefabs & Selection")]
    [Tooltip("Arrastra aquí tus 4 prefabs de personajes")]
    public GameObject[] personPrefabs;
    private GameObject currentPrefab;

    [Header("Settings")]
    public float spawnInterval = 3f;
    public float maxPopulation = 15f;
    float baseSpawnInterval;
    public float baseMaxPopulation = 15f;
    public int initialPopulation = 10;

    [Header("Spawn Area")]
    public Vector2 spawnAreaMin = new Vector2(-8, -4);
    public Vector2 spawnAreaMax = new Vector2(8, 4);

    [Header("Spawn Animation")]
    public float growDuration = 0.4f;

    private float timer;
    private int personsSpawnedToday = 0;

    void Awake()
    {
        if (personPrefabs.Length > 0)
        {
            currentPrefab = personPrefabs[0];
        }
    }

    public void SelectPrefab(int index)
    {
        if (index >= 0 && index < personPrefabs.Length)
        {
            currentPrefab = personPrefabs[index];
          //  Debug.Log("<color=green>PopulationManager:</color> Prefab cambiado a: " + currentPrefab.name);
        }
        else
        {
          //  Debug.LogWarning("Índice de prefab fuera de rango.");
        }
    }

    // El parámetro shinyCount se mantiene para no romper la llamada desde LevelManager, 
    // pero el cuerpo del método ya no hace nada con él.
    public void ConfigureRound(int ignoredShinyCount)
    {
        personsSpawnedToday = 0;
        timer = 0;

        baseSpawnInterval = spawnInterval;
        ApplySpawnBonus();

        // Spawn inicial de habitantes normales
        for (int i = 0; i < initialPopulation; i++)
        {
            SpawnPerson();
        }
    }

    void Update()
    {
        if (LevelManager.instance != null && !LevelManager.instance.isGameActive) return;

        timer += Time.deltaTime;
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

    void SpawnPerson()
    {
        if (currentPrefab == null)
        {
            Debug.LogError("No hay un prefab seleccionado en PopulationManager.");
            return;
        }

        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPos = new Vector3(x, y, 0);

        GameObject newPerson = Instantiate(currentPrefab, spawnPos, Quaternion.identity);

        Vector3 targetScale = newPerson.transform.localScale;
        newPerson.transform.localScale = Vector3.zero;
        StartCoroutine(GrowFromZero(newPerson.transform, targetScale));

        personsSpawnedToday++;
        // LÓGICA DE SHINY ELIMINADA
    }

    void ApplySpawnBonus()
    {
        if (Guardado.instance == null) return;
        float bonus = Guardado.instance.spawnSpeedBonus;
        spawnInterval = baseSpawnInterval * (1f - bonus);
        if (spawnInterval < 0.3f) spawnInterval = 0.3f;
    }

    System.Collections.IEnumerator GrowFromZero(Transform target, Vector3 finalScale)
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

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Vector3 center = new Vector3((spawnAreaMin.x + spawnAreaMax.x) / 2f, (spawnAreaMin.y + spawnAreaMax.y) / 2f, 0f);
        Vector3 size = new Vector3(Mathf.Abs(spawnAreaMax.x - spawnAreaMin.x), Mathf.Abs(spawnAreaMax.y - spawnAreaMin.y), 0.1f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }

    public void SetZonePrefab(int index)
    {
        SelectPrefab(index);
    }
}