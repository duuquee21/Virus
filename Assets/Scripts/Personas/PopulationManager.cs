using UnityEngine;
using System.Collections.Generic;

public class PopulationManager : MonoBehaviour
{
    public GameObject personPrefab;

    public float spawnInterval = 3f;
    public float maxPopulation = 15f;
    float baseSpawnInterval;
    public float baseMaxPopulation = 15f;

    public int initialPopulation = 10;

    public Vector2 spawnAreaMin = new Vector2(-8, -4);
    public Vector2 spawnAreaMax = new Vector2(8, 4);

    private float timer;
    private int personsSpawnedToday = 0;
    private int shiniesRemainingToSpawn = 0; // Cambiado a int para control exacto

    private List<int> shinyIndices = new List<int>();


    [Header("Spawn Animation")]
    public float growDuration = 0.4f;


    // Este método ahora recibe el número exacto (0, 1 o 2) desde LevelManager
    public void ConfigureRound(int shinyCount)
    {
        shiniesRemainingToSpawn = shinyCount;
        personsSpawnedToday = 0;
        timer = 0;
        shinyIndices.Clear();

        if (shiniesRemainingToSpawn > 0)
        {
            // --- AJUSTE DE RANGO ---
            // Si hay pocos shinies (como en la Zona 1), los forzamos a aparecer 
            // casi siempre dentro de la población inicial (initialPopulation).
            int maxRange = initialPopulation + (shiniesRemainingToSpawn * 2);

            for (int i = 0; i < shiniesRemainingToSpawn; i++)
            {
                int newIndex;
                int safety = 0;
                do
                {
                    // Rango desde 1 hasta el máximo calculado
                    newIndex = Random.Range(1, maxRange);
                    safety++;
                }
                while (shinyIndices.Contains(newIndex) && safety < 100);

                shinyIndices.Add(newIndex);
            }

            shinyIndices.Sort();
            Debug.Log("<color=cyan>PopulationManager:</color> Stock para esta zona: " + shiniesRemainingToSpawn + ". Aparecerán en los índices: " + string.Join(", ", shinyIndices));
        }

        baseSpawnInterval = spawnInterval;
        ApplySpawnBonus();

        // Spawn inicial: Aquí nacerán los primeros shinies si su índice es <= initialPopulation
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
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPos = new Vector3(x, y, 0);

        GameObject newPerson = Instantiate(personPrefab, spawnPos, Quaternion.identity);

        // Guardamos la escala original
        Vector3 targetScale = newPerson.transform.localScale;

        // Empieza en 0
        newPerson.transform.localScale = Vector3.zero;

        // Animación de crecimiento
        StartCoroutine(GrowFromZero(newPerson.transform, targetScale));

        personsSpawnedToday++;

        // Verificamos si debe ser Shiny
        if (shinyIndices.Contains(personsSpawnedToday))
        {
            PersonaInfeccion infeccion = newPerson.GetComponent<PersonaInfeccion>();
            if (infeccion != null)
            {
                infeccion.MakeShiny();
                Debug.Log("<color=yellow>¡Ha nacido un Shiny!</color> Persona nº: " + personsSpawnedToday);
            }
        }
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

            // Suavizado bonito
            float eased = Mathf.SmoothStep(0f, 1f, normalized);

            target.localScale = Vector3.Lerp(Vector3.zero, finalScale, eased);
            yield return null;
        }

        if (target != null)
            target.localScale = finalScale;
    }

    void OnDrawGizmos()
    {
        // Color del área de spawn
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);

        // Centro y tamaño del rectángulo
        Vector3 center = new Vector3(
            (spawnAreaMin.x + spawnAreaMax.x) / 2f,
            (spawnAreaMin.y + spawnAreaMax.y) / 2f,
            0f
        );

        Vector3 size = new Vector3(
            Mathf.Abs(spawnAreaMax.x - spawnAreaMin.x),
            Mathf.Abs(spawnAreaMax.y - spawnAreaMin.y),
            0.1f
        );

        // Área rellena
        Gizmos.DrawCube(center, size);

        // Borde más visible
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }

}