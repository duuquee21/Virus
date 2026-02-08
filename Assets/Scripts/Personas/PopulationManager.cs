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

    [Header("Spawn Area Logic")]
    private Collider2D currentSpawnCollider;
    public float margenSeguridad = 0.5f; // Ajusta esto para alejar del borde

    [Header("Spawn Animation")]
    public float growDuration = 0.4f;

    private float timer;

    void Awake()
    {
        if (personPrefabs.Length > 0) currentPrefab = personPrefabs[0];
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

        // Limpieza de seguridad al empezar la ronda
        GameObject[] antiguos = GameObject.FindGameObjectsWithTag("Persona");
        foreach (var p in antiguos) Destroy(p);

        for (int i = 0; i < initialPopulation; i++)
        {
            SpawnPerson();
        }
    }

    void Update()
    {
        if (LevelManager.instance != null && !LevelManager.instance.isGameActive) return;

        timer += Time.deltaTime;

        // --- LIMPIEZA DINÁMICA ---
        // Buscamos si alguno se ha salido por error y lo borramos rápido
        CheckForOutsiders();

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
            // Si el punto central de la persona NO está en el colisionador, fuera.
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

        // Doble comprobación: si el punto elegido no es válido, no instanciamos
        if (!currentSpawnCollider.OverlapPoint(spawnPos)) return;

        GameObject newPerson = Instantiate(currentPrefab, spawnPos, Quaternion.identity);

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
            // Aplicamos el margen de seguridad para que no nazcan pegados al borde
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